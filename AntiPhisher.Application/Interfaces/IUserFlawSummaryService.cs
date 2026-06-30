using AntiPhisher.Application.Response.UserFlawSummary;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IUserFlawSummaryService
    {
        Task<string> RefreshAndGetAdviceAsync(int userId, bool forceRefresh = false);

        // === THÊM METHOD MỚI ===
        Task<UserPredictiveAdviceResponse> GetAdviceAsync(int userId, bool forceRefresh = false);
    }
}