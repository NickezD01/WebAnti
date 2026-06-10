using System;

namespace AntiPhisher.Domain.Models
{
    public class CompanyInvitation
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        /// <summary>Email của người được mời.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Tên đầy đủ (dùng khi tạo user mới).</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>GUID token gửi qua email, dùng để xác nhận lời mời.</summary>
        public string Token { get; set; } = string.Empty;

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        /// <summary>true = user chưa tồn tại → đã tạo account mới (IsActive=false).</summary>
        public bool IsNewUser { get; set; }

        /// <summary>UserId của account được tạo/liên kết (set sau khi accept).</summary>
        public int? LinkedUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }

        // Navigation
        public virtual Company? Company { get; set; }
    }

    public enum InvitationStatus
    {
        Pending,
        Accepted,
        Expired
    }
}
