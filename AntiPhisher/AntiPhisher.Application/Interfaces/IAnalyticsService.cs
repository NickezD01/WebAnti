using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.Analytics;

namespace AntiPhisher.Application.Interfaces
{
    public interface IAnalyticsService
    {
        /// <summary>
        /// Tổng quan hiệu suất bảo mật toàn công ty của Manager đang đăng nhập.
        /// Gồm: avg risk score, detection rate, lesson completion, heatmap click nhầm UTC+7.
        /// </summary>
        Task<ApiResponse> GetCompanyOverviewAsync(int managerId);

        /// <summary>
        /// Danh sách nhân viên sắp xếp theo mức rủi ro (nguy hiểm nhất lên đầu).
        /// </summary>
        Task<ApiResponse> GetHighRiskEmployeesAsync(int managerId);

        /// <summary>
        /// Tiến độ hoàn thành theo từng Campaign: lesson % + attempt score + per-user breakdown.
        /// </summary>
        Task<ApiResponse> GetCampaignCompletionAsync(int managerId);
    }
}
