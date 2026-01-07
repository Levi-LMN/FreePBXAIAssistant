using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace FreePBXAIAssistant.Services
{
    public class AzureSpeechService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureSpeechService> _logger;
        private readonly SpeechConfig _speechConfig;

        public AzureSpeechService(IConfiguration configuration, ILogger<AzureSpeechService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var speechKey = _configuration["Azure:SpeechKey"];
            var speechRegion = _configuration["Azure:SpeechRegion"];

            _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            _speechConfig.SpeechRecognitionLanguage = "en-US";
            _speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural";
        }

        public async Task<string> RecognizeSpeechAsync(Stream audioStream)
        {
            try
            {
                using var audioConfig = AudioConfig.FromStreamInput(
                    AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1)));

                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

                var result = await recognizer.RecognizeOnceAsync();

                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    _logger.LogInformation($"Recognized: {result.Text}");
                    return result.Text;
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    _logger.LogWarning("No speech could be recognized");
                    return string.Empty;
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    _logger.LogError($"Speech recognition canceled: {cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        _logger.LogError($"Error details: {cancellation.ErrorDetails}");
                    }
                    return string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in speech recognition");
                return string.Empty;
            }
        }

        public async Task<string> RecognizeContinuousSpeechAsync(Stream audioStream, CancellationToken cancellationToken)
        {
            var recognizedText = new System.Text.StringBuilder();

            try
            {
                var pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));
                using var audioConfig = AudioConfig.FromStreamInput(pushStream);
                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

                var stopRecognition = new TaskCompletionSource<int>();

                recognizer.Recognizing += (s, e) =>
                {
                    _logger.LogDebug($"Recognizing: {e.Result.Text}");
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
                    {
                        recognizedText.AppendLine(e.Result.Text);
                        _logger.LogInformation($"Recognized: {e.Result.Text}");
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    _logger.LogWarning($"Recognition canceled: {e.Reason}");
                    stopRecognition.TrySetResult(0);
                };

                recognizer.SessionStopped += (s, e) =>
                {
                    _logger.LogInformation("Recognition session stopped");
                    stopRecognition.TrySetResult(0);
                };

                await recognizer.StartContinuousRecognitionAsync();

                // Feed audio data to the push stream
                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = await audioStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    pushStream.Write(buffer, bytesRead);
                }

                pushStream.Close();

                // Wait for recognition to complete
                await Task.WhenAny(stopRecognition.Task, Task.Delay(TimeSpan.FromSeconds(30), cancellationToken));

                await recognizer.StopContinuousRecognitionAsync();

                return recognizedText.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in continuous speech recognition");
                return recognizedText.ToString();
            }
        }

        public async Task<byte[]> SynthesizeSpeechAsync(string text)
        {
            try
            {
                using var synthesizer = new SpeechSynthesizer(_speechConfig, null);
                var result = await synthesizer.SpeakTextAsync(text);

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    _logger.LogInformation($"Speech synthesized for text: {text.Substring(0, Math.Min(50, text.Length))}...");
                    return result.AudioData;
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    _logger.LogError($"Speech synthesis canceled: {cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        _logger.LogError($"Error details: {cancellation.ErrorDetails}");
                    }
                }

                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in speech synthesis");
                return Array.Empty<byte>();
            }
        }

        public async Task<Stream> SynthesizeSpeechStreamAsync(string text)
        {
            try
            {
                var audioData = await SynthesizeSpeechAsync(text);

                if (audioData.Length > 0)
                {
                    return new MemoryStream(audioData);
                }

                return Stream.Null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating speech stream");
                return Stream.Null;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testText = "Testing Azure Speech Service connection.";
                var result = await SynthesizeSpeechAsync(testText);
                return result.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure Speech Service connection test failed");
                return false;
            }
        }
    }
}