namespace FreePBXAIAssistant.Models
{
    public class SettingsViewModel
    {
        public AISettings AISettings { get; set; } = new();
        public string FreePBXStatus { get; set; } = "Unknown";
        public string AzureStatus { get; set; } = "Unknown";
    }
}
