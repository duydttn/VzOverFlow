using System;

namespace VzOverFlow.Models
{
    public class AiConfig
    {
        public int Id { get; set; }
        public string Provider { get; set; } = string.Empty; // Gemini, OpenAI, etc.
        public string ApiKey { get; set; } = string.Empty;
        public string? ModelName { get; set; } // Optional: e.g. gemini-pro, gpt-4
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
        public int UsageCount { get; set; } = 0;
        public string? Note { get; set; }
    }
}