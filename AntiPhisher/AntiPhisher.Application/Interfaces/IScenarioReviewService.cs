using AntiPhisher.Application.Response.CampaignGenerator;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IScenarioReviewService
    {
        /// <summary>
        /// Duyệt 1 Scenario PendingReview thành Active — chính thức vào pool sử dụng cho Campaign thật.
        /// </summary>
        Task<SavedScenarioResponse> ApproveAsync(int scenarioId, int reviewedByUserId);

        /// <summary>
        /// Từ chối 1 Scenario PendingReview — giữ lại để audit, KHÔNG xóa cứng.
        /// </summary>
        Task<SavedScenarioResponse> RejectAsync(int scenarioId, string reason, int reviewedByUserId);

        /// <summary>
        /// Lấy danh sách Scenario đang chờ duyệt — phục vụ trang "Hàng chờ duyệt" cho Admin.
        /// </summary>
        Task<List<SavedScenarioResponse>> GetPendingReviewAsync();
    }
}