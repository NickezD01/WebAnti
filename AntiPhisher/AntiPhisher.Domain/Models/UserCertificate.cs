using System;

namespace AntiPhisher.Domain.Models
{
    public class UserCertificate
    {
        public int CertificateId { get; set; }
        public int UserId { get; set; }
        public string CertificateCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public decimal CorrectRateSnapshot { get; set; }
        public int TotalAttemptsSnapshot { get; set; }
        public string FullNameSnapshot { get; set; } = string.Empty;
        public string? CompanyNameSnapshot { get; set; }
        public bool IsRevoked { get; set; } = false;

        public User User { get; set; } = null!;
    }
}
