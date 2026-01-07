namespace FreePBXAIAssistant.Models
{
    public class KnowledgeBaseViewModel
    {
        public List<KnowledgeItem> Items { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string SelectedCategory { get; set; } = string.Empty;
        public string SearchQuery { get; set; } = string.Empty;
    }
}
