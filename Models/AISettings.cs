using System.ComponentModel.DataAnnotations;

namespace FreePBXAIAssistant.Models
{
    public class AISettings
    {
        public int Id { get; set; }
        
        [Required]
        public string SystemPrompt { get; set; } = string.Empty;
        
        public float Temperature { get; set; } = 0.7f;
        
        public int MaxTokens { get; set; } = 800;
        
        public float ConfidenceThreshold { get; set; } = 0.7f;
        
        public bool EnableTransferToAgent { get; set; } = true;
        
        public string TransferKeywords { get; set; } = string.Empty;
        
        public string EscalationRules { get; set; } = "[]";
        
        public int MaxConversationTurns { get; set; } = 20;
        
        public bool EnableSentimentAnalysis { get; set; } = true;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}








