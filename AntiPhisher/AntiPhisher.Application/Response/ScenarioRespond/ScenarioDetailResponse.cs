using System;

namespace AntiPhisher.Application.Response.ScenarioRespond
{
    public class ScenarioDetailResponse
    {
        public int ScenarioId { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int DifficultyId { get; set; }
        public string? DifficultyName { get; set; }

        public int? CreatedByUserId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsPhishing { get; set; }
        public bool IsAIGenerated { get; set; }
        public bool IsActive { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string RecipientName { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string EmailBodyHtml { get; set; } = null!;
        public string? AttachmentUrl { get; set; }
        public string? PhishingIndicators { get; set; }
        public string ExplanationHint { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}