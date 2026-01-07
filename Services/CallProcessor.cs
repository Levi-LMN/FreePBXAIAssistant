namespace FreePBXAIAssistant.Services
{
    public class CallProcessor
    {
        private readonly AzureSpeechService _speechService;
        private readonly AzureOpenAIService _openAIService;
        private readonly DatabaseService _databaseService;
        private readonly SipService _sipService;
        private readonly ILogger<CallProcessor> _logger;

        private readonly Dictionary<string, CallSession> _activeSessions = new();
        private readonly object _sessionLock = new();

        public CallProcessor(
            AzureSpeechService speechService,
            AzureOpenAIService openAIService,
            DatabaseService databaseService,
            SipService sipService,
            ILogger<CallProcessor> logger)
        {
            _speechService = speechService;
            _openAIService = openAIService;
            _databaseService = databaseService;
            _sipService = sipService;
            _logger = logger;
        }

        public async Task ProcessAudioStreamAsync(byte[] audioData, string callerNumber, string callId)
        {
            try
            {
                CallSession session;

                lock (_sessionLock)
                {
                    if (!_activeSessions.TryGetValue(callId, out session!))
                    {
                        session = new CallSession
                        {
                            CallId = callId,
                            CallerNumber = callerNumber,
                            StartTime = DateTime.UtcNow
                        };
                        _activeSessions[callId] = session;

                        // Create call record in database
                        _ = Task.Run(async () => await _databaseService.CreateCallRecordAsync(callId, callerNumber));
                    }
                }

                // Accumulate audio data
                session.AudioBuffer.AddRange(audioData);

                // Process when we have enough data (e.g., 3 seconds of audio at 16kHz)
                if (session.AudioBuffer.Count >= 48000) // 3 seconds * 16000 Hz
                {
                    var audioToProcess = session.AudioBuffer.ToArray();
                    session.AudioBuffer.Clear();

                    // Process in background
                    _ = Task.Run(async () => await ProcessAudioChunkAsync(session, audioToProcess));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing audio stream for call {callId}");
            }
        }

        private async Task ProcessAudioChunkAsync(CallSession session, byte[] audioData)
        {
            try
            {
                // Convert audio to stream
                using var audioStream = new MemoryStream(audioData);

                // Speech to text
                var transcribedText = await _speechService.RecognizeSpeechAsync(audioStream);

                if (string.IsNullOrWhiteSpace(transcribedText))
                    return;

                _logger.LogInformation($"Call {session.CallId} - User said: {transcribedText}");

                // Add to session transcription
                session.Transcription.Add(new ConversationMessage
                {
                    Role = "user",
                    Content = transcribedText,
                    Timestamp = DateTime.UtcNow
                });

                // Get AI settings and knowledge base
                var settings = await _databaseService.GetAISettingsAsync();
                var knowledgeContext = await _databaseService.GetRelevantKnowledgeAsync(transcribedText);

                // Get AI response
                var aiResponse = await _openAIService.GetResponseAsync(
                    transcribedText,
                    session.Transcription,
                    settings.SystemPrompt,
                    knowledgeContext
                );

                _logger.LogInformation($"Call {session.CallId} - AI response: {aiResponse.Message}");

                // Add AI response to session
                session.Transcription.Add(new ConversationMessage
                {
                    Role = "assistant",
                    Content = aiResponse.Message,
                    Timestamp = DateTime.UtcNow
                });

                // Update session metadata
                session.Intent = aiResponse.Intent;
                session.SentimentScore = aiResponse.Sentiment;

                // Check if transfer is needed
                if (aiResponse.RequiresTransfer || session.Transcription.Count > settings.MaxConversationTurns)
                {
                    await HandleTransferAsync(session, aiResponse.Intent);
                }
                else
                {
                    // Synthesize speech response
                    var responseAudio = await _speechService.SynthesizeSpeechAsync(aiResponse.Message);

                    if (responseAudio.Length > 0)
                    {
                        // Send audio back through SIP (implementation depends on SIP library)
                        // For now, we'll log it
                        _logger.LogInformation($"Synthesized {responseAudio.Length} bytes of audio response");
                    }
                }

                // Update database
                await _databaseService.UpdateCallRecordAsync(
                    session.CallId,
                    session.Transcription,
                    session.Intent,
                    session.SentimentScore
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing audio chunk for call {session.CallId}");
            }
        }

        private async Task HandleTransferAsync(CallSession session, string intent)
        {
            try
            {
                _logger.LogInformation($"Initiating transfer for call {session.CallId} - Intent: {intent}");

                // Determine target extension based on intent
                var targetExtension = DetermineTransferExtension(intent);

                // Announce transfer to caller
                var transferMessage = $"I'm transferring you now to a specialist who can help you. Please hold.";
                var transferAudio = await _speechService.SynthesizeSpeechAsync(transferMessage);

                // Perform transfer
                var transferred = await _sipService.TransferCall(session.CallId, targetExtension);

                if (transferred)
                {
                    session.TransferredToAgent = true;
                    session.AgentExtension = targetExtension;
                    session.Outcome = "Transferred";

                    await _databaseService.UpdateCallTransferAsync(
                        session.CallId,
                        true,
                        targetExtension,
                        $"Transferred to {intent} department"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling transfer for call {session.CallId}");
            }
        }

        private string DetermineTransferExtension(string intent)
        {
            return intent switch
            {
                "Sales" => "2000",
                "Claims" => "2001",
                "Billing" => "2002",
                "Policy Inquiry" => "2003",
                _ => "2100" // General support
            };
        }

        public async Task EndCallAsync(string callId)
        {
            try
            {
                CallSession? session;

                lock (_sessionLock)
                {
                    if (_activeSessions.TryGetValue(callId, out session))
                    {
                        _activeSessions.Remove(callId);
                    }
                }

                if (session != null)
                {
                    session.EndTime = DateTime.UtcNow;
                    session.DurationSeconds = (int)(session.EndTime.Value - session.StartTime).TotalSeconds;

                    // Determine outcome if not already set
                    if (session.Outcome == "In Progress")
                    {
                        session.Outcome = session.TransferredToAgent ? "Transferred" : "Completed";
                    }

                    // Final database update
                    await _databaseService.FinalizeCallRecordAsync(
                        session.CallId,
                        session.EndTime.Value,
                        session.DurationSeconds,
                        session.Outcome
                    );

                    _logger.LogInformation($"Call {callId} ended - Duration: {session.DurationSeconds}s, Outcome: {session.Outcome}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ending call {callId}");
            }
        }

        public int GetActiveCallCount()
        {
            lock (_sessionLock)
            {
                return _activeSessions.Count;
            }
        }

        public List<CallSession> GetActiveSessions()
        {
            lock (_sessionLock)
            {
                return _activeSessions.Values.ToList();
            }
        }
    }

    public class CallSession
    {
        public string CallId { get; set; } = string.Empty;
        public string CallerNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DurationSeconds { get; set; }
        public List<byte> AudioBuffer { get; set; } = new();
        public List<ConversationMessage> Transcription { get; set; } = new();
        public string Intent { get; set; } = "Unknown";
        public float SentimentScore { get; set; }
        public bool TransferredToAgent { get; set; }
        public string? AgentExtension { get; set; }
        public string Outcome { get; set; } = "In Progress";
    }
}