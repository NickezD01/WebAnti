using System.Collections.Generic;

namespace AntiPhisher.Application.Request.TemplateExpansion
{
    public class SaveSimilarScenarioRequest
    {
        public string Title { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string RecipientName { get; set; }
        public string Subject { get; set; }
        public string EmailBodyHtml { get; set; }
        public string PhishingIndicators { get; set; }
        public string ExplanationHint { get; set; }
        public bool IsPhishing { get; set; }
        public int DifficultyId { get; set; }
        public int CategoryId { get; set; }
        public List<int> SourceScenarioIds { get; set; } = new();
    }
}