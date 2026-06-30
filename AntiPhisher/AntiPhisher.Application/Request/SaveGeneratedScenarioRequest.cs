namespace AntiPhisher.Application.Request.CampaignGenerator
{
    // Admin gửi lại đúng nội dung preview họ đã xem (có thể đã tự sửa vài chỗ trước khi lưu),
    // kèm CategoryId tự chọn. Không gọi lại AI ở bước này.
    public class SaveGeneratedScenarioRequest
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
        public int SourceKnowledgeId { get; set; }

        // === MỚI: Admin chọn category khi xác nhận lưu ===
        public int CategoryId { get; set; }
    }
}