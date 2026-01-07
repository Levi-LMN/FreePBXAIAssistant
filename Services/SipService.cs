using SIPSorcery.SIP;
using System.Net;

namespace FreePBXAIAssistant.Services
{
    public class SipService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SipService> _logger;
        private SIPTransport? _sipTransport;
        private bool _isRegistered = false;

        public bool IsConnected => _isRegistered;
        public event EventHandler<CallEventArgs>? IncomingCall;

        public SipService(
            IConfiguration configuration,
            ILogger<SipService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("Starting SIP service...");

                // Create SIP transport
                _sipTransport = new SIPTransport();

                // Add UDP channel
                var sipChannel = new SIPUDPChannel(IPAddress.Any, 5060);
                _sipTransport.AddSIPChannel(sipChannel);

                _logger.LogInformation("SIP transport started on UDP port 5060");

                // Set flag - actual registration would require more complex setup
                _isRegistered = true;

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting SIP service");
                _isRegistered = false;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_sipTransport != null)
                {
                    _sipTransport.Shutdown();
                }

                _isRegistered = false;
                _logger.LogInformation("SIP service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SIP service");
            }

            await Task.CompletedTask;
        }

        public async Task<bool> TransferCall(string callId, string extension)
        {
            try
            {
                _logger.LogInformation($"Transfer requested for call {callId} to extension {extension}");

                // TODO: Implement actual SIP transfer logic
                // This is a placeholder that returns true for development

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error transferring call {callId}");
                return false;
            }
        }
    }

    public class CallEventArgs : EventArgs
    {
        public string CallId { get; set; } = string.Empty;
        public string CallerNumber { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}