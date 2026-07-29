#nullable disable
using System;

namespace AntiPhisher.Domain.Models;

/// <summary>
/// Kho kiến thức mới do Admin nạp (vd. trend lừa đảo Deepfake mới xuất hiện),
/// dùng làm Few-shot example khi sinh Campaign/Scenario mới — không cần fine-tune model.
/// </summary>
public partial class AIExpandedKnowledge : Base
{
    public int Id { get; set; }

    public string Title { get; set; }

    public string ContextDescription { get; set; }

    /// <summary>'AdminManual' | 'NewsArticle' | 'IncidentReport'</summary>
    public string SourceType { get; set; }

    public string SourceUrl { get; set; }

    public string SampleEmailContent { get; set; }

    public string Tags { get; set; }

    public int DifficultyId { get; set; }

    public bool IsActive { get; set; } = true;

    public int CreatedByUserId { get; set; }

    //public DateTime CreatedAt { get; set; }

    //public DateTime UpdatedAt { get; set; }

    public virtual DifficultyLevel Difficulty { get; set; }

    public virtual User CreatedByUser { get; set; }
}