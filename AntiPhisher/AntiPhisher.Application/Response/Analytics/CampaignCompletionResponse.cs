namespace AntiPhisher.Application.Response.Analytics
{
    public class CampaignCompletionResponse
    {
        public List<CampaignCompletionItem> Campaigns { get; set; } = new();
    }

    public class CampaignCompletionItem
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;        // Active / Sắp diễn ra / Đã kết thúc
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // ── Nhân sự được giao ────────────────────────────────────────
        public int TotalAssignedUsers { get; set; }

        // ── Tiến độ bài học bắt buộc ────────────────────────────────
        public int TotalRequiredLessons { get; set; }
        public int CompletedLessonPairs { get; set; }              // (user × lesson) đã hoàn thành
        public double LessonCompletionPct { get; set; }            // %

        // ── Kết quả thực hành kịch bản ──────────────────────────────
        public int TotalAttempts { get; set; }
        public double AvgAttemptScore { get; set; }
        public double DetectionRate { get; set; }                  // % đúng

        // ── Chi tiết theo từng user ──────────────────────────────────
        public List<CampaignUserSummary> UserSummaries { get; set; } = new();
    }

    public class CampaignUserSummary
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public double RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int AttemptCount { get; set; }
        public bool HasCredentialLeak { get; set; }
    }
}
