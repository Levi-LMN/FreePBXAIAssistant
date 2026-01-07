using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace FreePBXAIAssistant.Services
{
    public class AzureOpenAIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly AzureOpenAIClient _client;
        private readonly string _deploymentName;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var endpoint = _configuration["Azure:OpenAIEndpoint"] ?? throw new ArgumentNullException("Azure:OpenAIEndpoint");
            var apiKey = _configuration["Azure:OpenAIKey"] ?? throw new ArgumentNullException("Azure:OpenAIKey");
            _deploymentName = _configuration["Azure:DeploymentName"] ?? "gpt-4o-mini";

            _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        }

        public async Task<AIResponse> GetResponseAsync(
            string userMessage,
            List<ConversationMessage> conversationHistory,
            string systemPrompt,
            string knowledgeContext)
        {
            try
            {
                var chatClient = _client.GetChatClient(_deploymentName);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(BuildSystemPrompt(systemPrompt, knowledgeContext))
                };

                // Add conversation history
                foreach (var msg in conversationHistory)
                {
                    if (msg.Role == "user")
                        messages.Add(new UserChatMessage(msg.Content));
                    else if (msg.Role == "assistant")
                        messages.Add(new AssistantChatMessage(msg.Content));
                }

                // Add current user message
                messages.Add(new UserChatMessage(userMessage));

                var chatCompletionOptions = new ChatCompletionOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 800,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                var response = await chatClient.CompleteChatAsync(messages, chatCompletionOptions);
                var messageContent = response.Value.Content[0].Text;

                var aiResponse = new AIResponse
                {
                    Message = messageContent,
                    Intent = DetectIntent(userMessage, messageContent),
                    Confidence = CalculateConfidence(messageContent),
                    RequiresTransfer = ShouldTransfer(messageContent, userMessage),
                    SuggestedAction = DetermineAction(messageContent),
                    Sentiment = AnalyzeSentiment(userMessage)
                };

                _logger.LogInformation($"AI Response generated - Intent: {aiResponse.Intent}, Confidence: {aiResponse.Confidence}");

                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response");
                return new AIResponse
                {
                    Message = "I apologize, but I'm experiencing technical difficulties. Let me transfer you to an agent who can help.",
                    Intent = "Error",
                    RequiresTransfer = true,
                    Confidence = 0
                };
            }
        }

        private string BuildSystemPrompt(string basePrompt, string knowledgeContext)
        {
            return $@"{basePrompt}

KNOWLEDGE BASE:
{knowledgeContext}

INSTRUCTIONS:
- Be professional, empathetic, and concise
- Use the knowledge base to answer questions accurately
- If you don't know something, admit it and offer to transfer to an agent
- Keep responses under 150 words for natural conversation flow
- Listen for keywords indicating frustration or need for human assistance
- Always confirm understanding before taking actions

TRANSFER TRIGGERS:
- Customer explicitly requests to speak with a human
- Complex issues beyond your knowledge
- Customer expresses frustration or anger
- Account-specific transactions requiring authorization
- Complaints or disputes

When you determine a transfer is needed, clearly state: 'I understand this requires personal attention. Let me transfer you to one of our specialists who can help you right away.'";
        }

        private string DetectIntent(string userMessage, string aiResponse)
        {
            var message = userMessage.ToLower();

            if (message.Contains("policy") || message.Contains("coverage") || message.Contains("insurance"))
                return "Policy Inquiry";
            if (message.Contains("claim") || message.Contains("file claim"))
                return "Claims";
            if (message.Contains("payment") || message.Contains("bill") || message.Contains("premium"))
                return "Billing";
            if (message.Contains("cancel") || message.Contains("terminate"))
                return "Cancellation";
            if (message.Contains("agent") || message.Contains("representative") || message.Contains("person"))
                return "Agent Request";
            if (message.Contains("quote") || message.Contains("price") || message.Contains("cost"))
                return "Sales";
            if (message.Contains("change") || message.Contains("update") || message.Contains("modify"))
                return "Account Update";

            return "General Inquiry";
        }

        private float CalculateConfidence(string content)
        {
            // Basic confidence calculation based on response characteristics
            if (string.IsNullOrWhiteSpace(content))
                return 0f;

            // Higher confidence if response is detailed and doesn't contain uncertainty phrases
            float confidence = 0.8f;

            var uncertainPhrases = new[] { "not sure", "maybe", "might", "possibly", "i don't know" };
            if (uncertainPhrases.Any(phrase => content.ToLower().Contains(phrase)))
                confidence -= 0.3f;

            var certainPhrases = new[] { "specifically", "according to", "the policy states" };
            if (certainPhrases.Any(phrase => content.ToLower().Contains(phrase)))
                confidence += 0.1f;

            return Math.Max(0, Math.Min(1, confidence));
        }

        private bool ShouldTransfer(string aiResponse, string userMessage)
        {
            var combinedText = (aiResponse + " " + userMessage).ToLower();

            var transferKeywords = new[]
            {
                "transfer you", "speak with", "representative", "agent",
                "supervisor", "manager", "human", "person",
                "complex", "technical issue", "specialist"
            };

            return transferKeywords.Any(keyword => combinedText.Contains(keyword));
        }

        private string DetermineAction(string aiResponse)
        {
            var response = aiResponse.ToLower();

            if (response.Contains("transfer"))
                return "transfer";
            if (response.Contains("email") || response.Contains("send"))
                return "send_email";
            if (response.Contains("callback"))
                return "schedule_callback";

            return "continue_conversation";
        }

        private float AnalyzeSentiment(string text)
        {
            // Simple sentiment analysis
            var positiveWords = new[] { "thank", "great", "good", "appreciate", "help", "wonderful" };
            var negativeWords = new[] { "angry", "frustrated", "terrible", "bad", "awful", "hate", "complaint" };

            var lowerText = text.ToLower();
            var positiveCount = positiveWords.Count(word => lowerText.Contains(word));
            var negativeCount = negativeWords.Count(word => lowerText.Contains(word));

            if (positiveCount == 0 && negativeCount == 0)
                return 0.5f; // Neutral

            var sentiment = (positiveCount - negativeCount) / (float)(positiveCount + negativeCount + 1);
            return (sentiment + 1) / 2; // Normalize to 0-1 range
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var chatClient = _client.GetChatClient(_deploymentName);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a helpful assistant."),
                    new UserChatMessage("Say 'Connection successful' if you can read this.")
                };

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 50
                };

                var response = await chatClient.CompleteChatAsync(messages, options);
                return response.Value.Content.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure OpenAI connection test failed");
                return false;
            }
        }
    }

    public class AIResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public bool RequiresTransfer { get; set; }
        public string SuggestedAction { get; set; } = string.Empty;
        public float Sentiment { get; set; }
    }

    public class ConversationMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}