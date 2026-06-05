namespace AntiPhisher.Application.Response.Analytics
{
    public class CompanyOverviewResponse
    {
        // ── Nhân sự ─────────────────────────────────────────────────
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }

        // ── Kết quả thực hành phishing ──────────────────────────────
        public int TotalAttempts { get; set; }
        public double OverallDetectionRate { get; set; }
        public double AvgRiskScore { get; set; }

        // ── Tiến độ học lý thuyết ───────────────────────────────────
        public double LessonCompletionRate { get; set; }

        // ── Phân bố mức rủi ro ──────────────────────────────────────
        public RiskDistribution RiskDistribution { get; set; } = new();

        // ── Heatmap click nhầm UTC+7 (7 rows × 24 hours) ────────────
        public List<HeatmapRow> Heatmap { get; set; } = new();

        // ── Thống kê hành vi phishing ───────────────────────────────
        public int TotalClickedLink { get; set; }
        public int TotalCredentialLeaked { get; set; }
        public int TotalReported { get; set; }

        // ── Thời gian phản ứng trung bình (s) ──────────────────────
        public double AvgDetectionSeconds { get; set; }
    }

    public class RiskDistribution
    {
        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }
        public int Critical { get; set; }
    }

    public class HeatmapRow
    {
        public string Day { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public List<int> Hours { get; set; } = new(new int[24]);
    }
}
