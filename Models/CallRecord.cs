
using System.ComponentModel.DataAnnotations;

namespace FreePBXAIAssistant.Models
{
    public class CallRecord
    {
        public int Id { get; set; }

        [Required]
        public string CallId { get; set; } = Guid.NewGuid().ToString();

        public string CallerNumber { get; set; } = string.Empty;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public DateTime? EndTime { get; set; }

        public int DurationSeconds { get; set; }

        public string Transcription { get; set; } = string.Empty;

        public string AIResponses { get; set; } = string.Empty;

        public string Intent { get; set; } = "Unknown";

        public string Outcome { get; set; } = "In Progress";

        public bool TransferredToAgent { get; set; }

        public string? TransferReason { get; set; }

        public string? AgentExtension { get; set; }

        public string ConversationLog { get; set; } = "[]";

        public float SentimentScore { get; set; }

        public string AudioFilePath { get; set; } = string.Empty;
    }
}
