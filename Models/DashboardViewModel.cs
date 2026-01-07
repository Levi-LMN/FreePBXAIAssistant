namespace FreePBXAIAssistant.Models
{
    public class DashboardViewModel
    {
        public int TotalCallsToday { get; set; }
        public int ActiveCalls { get; set; }
        public int TransferredCalls { get; set; }
        public int ResolvedCalls { get; set; }
        public float AverageCallDuration { get; set; }
        public List<CallRecord> RecentCalls { get; set; } = new();
        public Dictionary<string, int> IntentDistribution { get; set; } = new();
        public bool SipConnected { get; set; }
        public string SystemStatus { get; set; } = "Unknown";
    }
}
