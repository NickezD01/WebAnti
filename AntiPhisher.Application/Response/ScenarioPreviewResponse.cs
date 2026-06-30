namespace AntiPhisher.Application.Response.CampaignGenerator
{
    public class ScenarioPreviewResponse
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
        public int SourceKnowledgeId { get; set; } // dùng cho Giai đoạn 4 (AIExpandedKnowledge)

        // === THÊM MỚI cho Giai đoạn 5 — danh sách ScenarioId dùng làm few-shot ===
        public List<int> SourceScenarioIds { get; set; } = new();

        // === THÊM MỚI — category mặc định gợi ý (lấy từ few-shot, Admin có thể đổi khi Save) ===
        public int SuggestedCategoryId { get; set; }
    }
}