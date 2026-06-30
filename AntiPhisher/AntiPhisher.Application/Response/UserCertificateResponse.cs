using System;

namespace AntiPhisher.Application.Response
{
    public class UserCertificateResponse
    {
        public int CertificateId { get; set; }
        public string CertificateCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public decimal CorrectRateSnapshot { get; set; }
        public int TotalAttemptsSnapshot { get; set; }
        public string FullNameSnapshot { get; set; } = string.Empty;
        public string? CompanyNameSnapshot { get; set; }
        public bool IsRevoked { get; set; }
    }

    public class CertificateVerifyResponse
    {
        public bool IsValid { get; set; }
        public string CertificateCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string FullNameSnapshot { get; set; } = string.Empty;
        public string? CompanyNameSnapshot { get; set; }
        public decimal CorrectRateSnapshot { get; set; }
        public int TotalAttemptsSnapshot { get; set; }
    }
}
