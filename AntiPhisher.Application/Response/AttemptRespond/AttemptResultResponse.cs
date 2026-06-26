namespace AntiPhisher.Application.Response.AttemptRespond
{
    public class AttemptResultResponse
    {
        public int AttemptId { get; set; }
        public bool IsCorrect { get; set; }
        public int ScoreEarned { get; set; }

        // ===================================================
        // NEW: Phản chiếu hành vi phishing của User
        // ===================================================
        public bool IsClickedLink { get; set; }
        public bool IsCredentialLeaked { get; set; }
        public bool IsReported { get; set; }

        /// <summary>
        /// Mức độ rủi ro tổng hợp: "Low" | "Medium" | "High" | "Critical"
        /// </summary>
        public string RiskLevel { get; set; } = "Low";

        // AI Feedback
        public string FeedbackText { get; set; } = null!;
        public string IndicatorsExplained { get; set; } = null!;
        public string ImprovementTips { get; set; } = null!;
        public string AIModel { get; set; } = null!;
    }
}
