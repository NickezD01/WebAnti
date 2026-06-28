using System;

namespace AntiPhisher.Domain.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }                         // FK → Users.UserId (PK)
        public string TokenHash { get; set; } = string.Empty;  // Base64(SHA256(plainToken)), 44 chars
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual User? User { get; set; }
    }
}
