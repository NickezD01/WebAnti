namespace AntiPhisher.Application.DTOs.Analytics
{
    public class MyReportResponse
    {
        public int TotalAttempts { get; set; }
        public float CorrectRate { get; set; }
        public List<DailyTrend> RecentTrend { get; set; } = new();
        public SkillBreakdown SkillBreakdown { get; set; } = new();
        public List<RecentFeedback> TopFeedbacks { get; set; } = new();
    }

    public class DailyTrend
    {
        public string Date { get; set; } = "";
        public int Correct { get; set; }
        public int Total { get; set; }
    }

    public class SkillBreakdown
    {
        public float ClickedLinkRate { get; set; }
        public float CredentialLeakedRate { get; set; }
        public float ReportedCorrectlyRate { get; set; }
        public float PhishingDetectionRate { get; set; }
        public float SafeEmailAccuracy { get; set; }
    }

    public class RecentFeedback
    {
        public string ScenarioTitle { get; set; } = "";
        public string FeedbackText { get; set; } = "";
        public string ImprovementTips { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }
}
