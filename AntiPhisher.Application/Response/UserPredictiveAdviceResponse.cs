using System.Collections.Generic;

namespace AntiPhisher.Application.Response.UserFlawSummary
{
    // DTO khớp đúng schema JSON mà AI trả về ở GenerateUserPredictiveAdviceAsync.
    // Dùng để parse JSON string từ Service thành object có cấu trúc trả ra Controller,
    // tránh trả raw string JSON trực tiếp cho Frontend.
    public class UserPredictiveAdviceResponse
    {
        public string RiskPattern { get; set; }
        public string PrimaryWeakness { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> ActionableAdvice { get; set; } = new();
        public string RecommendedNextDifficulty { get; set; }
    }
}