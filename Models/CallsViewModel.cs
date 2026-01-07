namespace FreePBXAIAssistant.Models
{
    public class CallsViewModel
    {
        public List<CallRecord> Calls { get; set; } = new();
        public int TotalCalls { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public string IntentFilter { get; set; } = string.Empty;
    }
}
