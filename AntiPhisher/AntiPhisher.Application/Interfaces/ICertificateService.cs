using AntiPhisher.Application.Response;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ICertificateService
    {
        Task<ApiResponse> GetMyCertificateAsync(int userId);
        Task<ApiResponse> IssueOrGetCertificateAsync(int userId, string fullName);
        Task<ApiResponse> VerifyCodeAsync(string code);
    }
}
