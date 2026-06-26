#nullable disable
using System;

namespace AntiPhisher.Domain.Models;

// File riêng, KHÔNG đụng vào Scenario.cs gốc (auto-generated).
// Chứa các field phục vụ luồng "AI tạo Scenario mới rồi chờ Admin duyệt".
public partial class Scenario
{
    /// <summary>'Manual' | 'PendingReview' | 'Approved' | 'Rejected'</summary>
    public string GenerationStatus { get; set; } = "Manual";

    /// <summary>Danh sách ScenarioId dùng làm few-shot để sinh ra bản này, dạng "12,45"</summary>
    public string SourceScenarioIds { get; set; }

    /// <summary>SHA-256 hash của Subject+EmailBodyHtml, dùng chống trùng lặp khi AI sinh mới</summary>
    public byte[] ContentHash { get; set; }

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual User ReviewedByUser { get; set; }
}