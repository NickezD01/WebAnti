using AntiPhisher.Application.Request.CompanyRequest;
using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ICompanyService
    {
        // Endpoint lấy thông tin công ty của user đang đăng nhập hiện tại
        Task<ApiResponse> GetMyCompanyAsync();

        /// <summary>Gửi lời mời email tới nhân viên (email mới hoặc đã có account).</summary>
        Task<ApiResponse> InviteEmployeeAsync(InviteByEmailDto dto);

        /// <summary>Nhân viên bấm link xác nhận trong email → thêm vào công ty.</summary>
        Task<ApiResponse> AcceptInvitationAsync(string token);

        /// <summary>Cập nhật tên công ty (chủ công ty, không cần role Manager).</summary>
        Task<ApiResponse> UpdateCompanyNameAsync(string companyName);
    }
}
