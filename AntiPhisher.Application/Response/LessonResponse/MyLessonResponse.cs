namespace AntiPhisher.Application.Response.LessonResponse
{
    public class MyLessonResponse
    {
        public int LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int PhaseNumber { get; set; }
        public int ModuleNumber { get; set; }
        public double LessonOrder { get; set; }
        public int EstimatedMinutes { get; set; }

        // "NotStarted" | "InProgress" | "Completed"
        public string Status { get; set; } = "NotStarted";
        public DateTime? CompletedAt { get; set; }

        // Campaign nguồn giao bài học này
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;

        // false = Phase 1 hoặc user unlocked; true = Phase 2+ và user chưa nâng cấp
        public bool IsLocked { get; set; }
    }
}
