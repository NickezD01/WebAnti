using AntiPhisher.Application.Services;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IOrgReportService
    {
        /// <summary>
        /// Trả về toàn bộ báo cáo công ty: thống kê thuần + AI executive summary.
        /// </summary>
        Task<OrgReportResult> GetOrgReportAsync(int companyId, bool forceRefresh = false);
    }
}