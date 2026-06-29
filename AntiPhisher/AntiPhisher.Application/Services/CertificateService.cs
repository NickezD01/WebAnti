using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly IUnitOfWork _unitOfWork;

        // Characters that are unambiguous to read: no O/0, I/1/l
        private static readonly char[] CodeChars =
            "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

        public CertificateService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse> GetMyCertificateAsync(int userId)
        {
            var cert = await _unitOfWork.Certificates.GetAsync(c => c.UserId == userId && !c.IsRevoked);
            if (cert == null)
                return new ApiResponse().SetNotFound(null, "Bạn chưa có chứng chỉ.");

            return new ApiResponse().SetOk(ToResponse(cert));
        }

        public async Task<ApiResponse> IssueOrGetCertificateAsync(int userId, string fullName)
        {
            var existing = await _unitOfWork.Certificates.GetAsync(c => c.UserId == userId && !c.IsRevoked);
            if (existing != null)
                return new ApiResponse().SetOk(ToResponse(existing));

            var attempts = await _unitOfWork.UserAttempts.GetAllAsync(filter: a => a.UserId == userId);
            int total = attempts.Count;
            if (total < 10)
                return new ApiResponse().SetBadRequest(null, $"Cần ít nhất 10 lần thực hành (hiện tại: {total}).");

            int correct = attempts.Count(a => a.IsCorrect);
            decimal rate = (correct * 100.0m) / total;
            if (rate < 70.0m)
                return new ApiResponse().SetBadRequest(null, $"Tỷ lệ trả lời đúng cần ≥ 70% (hiện tại: {rate:F1}%).");

            var user = await _unitOfWork.Users.GetAsync(
                u => u.UserId == userId,
                include: q => q.Include(u => u.Company));

            string code;
            do { code = GenerateCode(); }
            while (await _unitOfWork.Certificates.AnyAsync(c => c.CertificateCode == code));

            var cert = new UserCertificate
            {
                UserId = userId,
                CertificateCode = code,
                IssuedAt = DateTime.UtcNow,
                CorrectRateSnapshot = Math.Round(rate, 2),
                TotalAttemptsSnapshot = total,
                FullNameSnapshot = user?.FullName ?? fullName,
                CompanyNameSnapshot = user?.Company?.CompanyName,
            };

            await _unitOfWork.Certificates.AddAsync(cert);
            await _unitOfWork.SaveChangeAsync();

            return new ApiResponse().SetOk(ToResponse(cert));
        }

        public async Task<ApiResponse> VerifyCodeAsync(string code)
        {
            var cert = await _unitOfWork.Certificates.GetAsync(c => c.CertificateCode == code);
            if (cert == null)
                return new ApiResponse().SetNotFound(null, "Không tìm thấy chứng chỉ với mã này.");

            return new ApiResponse().SetOk(new CertificateVerifyResponse
            {
                IsValid = !cert.IsRevoked,
                CertificateCode = cert.CertificateCode,
                IssuedAt = cert.IssuedAt,
                FullNameSnapshot = cert.FullNameSnapshot,
                CompanyNameSnapshot = cert.CompanyNameSnapshot,
                CorrectRateSnapshot = cert.CorrectRateSnapshot,
                TotalAttemptsSnapshot = cert.TotalAttemptsSnapshot,
            });
        }

        private static UserCertificateResponse ToResponse(UserCertificate c) => new()
        {
            CertificateId = c.CertificateId,
            CertificateCode = c.CertificateCode,
            IssuedAt = c.IssuedAt,
            CorrectRateSnapshot = c.CorrectRateSnapshot,
            TotalAttemptsSnapshot = c.TotalAttemptsSnapshot,
            FullNameSnapshot = c.FullNameSnapshot,
            CompanyNameSnapshot = c.CompanyNameSnapshot,
            IsRevoked = c.IsRevoked,
        };

        private static string GenerateCode()
        {
            var rng = new Random();
            var chars = new char[8];
            for (int i = 0; i < 8; i++)
                chars[i] = CodeChars[rng.Next(CodeChars.Length)];
            return $"ANTI-{DateTime.UtcNow.Year}-{new string(chars)}";
        }
    }
}
