#nullable disable
using System;

namespace AntiPhisher.Domain.Models;

/// <summary>
/// Bảng cache tổng hợp — 1 dòng / User.
/// Được tính lại từ UserAttempt + Scenario.PhishingIndicators theo định kỳ hoặc on-demand,
/// KHÔNG tính lại mỗi lần user mở trang báo cáo (tránh chậm + tốn AI quota).
/// </summary>
public partial class UserFlawSummary
{
    public int UserId { get; set; }

    /// <summary>JSON: [{"tactic":"Urgency","count":7,"lastSeen":"2026-06-10"}, ...]</summary>
    public string TopTacticsJson { get; set; }

    public int TotalAttempts { get; set; }

    public int TotalFlaws { get; set; }

    public decimal CurrentRiskScore { get; set; }

    /// <summary>Cache kết quả AI advice gần nhất, tránh gọi AI lại nếu dữ liệu chưa đổi</summary>
    public string LastAiAdviceJson { get; set; }

    public DateTime? LastAnalyzedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; }
}