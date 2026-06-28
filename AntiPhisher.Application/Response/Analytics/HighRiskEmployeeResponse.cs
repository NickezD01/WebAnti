namespace AntiPhisher.Application.Response.Analytics
{
    public class HighRiskEmployeeResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        // ── Risk ─────────────────────────────────────────────────────
        public double RiskScore { get; set; }     // 0-100, thấp = nguy hiểm hơn
        public string RiskLevel { get; set; } = string.Empty; // Low / Medium / High / Critical

        // ── Thực hành phishing ───────────────────────────────────────
        public int TotalAttempts { get; set; }
        public int CorrectAttempts { get; set; }
        public double DetectionRate { get; set; } // %

        // ── Hành vi nguy hiểm ────────────────────────────────────────
        public int ClickedLinkCount { get; set; }
        public int CredentialLeakedCount { get; set; }
        public int ReportedCount { get; set; }

        // ── Tiến độ bài học ─────────────────────────────────────────
        public int CompletedLessons { get; set; }
        public int TotalAssignedLessons { get; set; }
        public double LessonCompletionPct { get; set; }

        public DateTime? LastAttemptAt { get; set; }
    }
}
