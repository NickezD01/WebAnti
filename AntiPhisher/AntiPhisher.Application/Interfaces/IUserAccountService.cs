using AntiPhisher.Application.Request.User;
using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IUserAccountService
    {
        Task<ApiResponse> GetUserProfileAsync();
        Task<ApiResponse> UpdateUserProfileAsync(UpdateUserRequest updateUserRequest);
        Task<ApiResponse> GetAllAccountAsync(string searchTerm, int pageIndex, int pageSize);
        Task<ApiResponse> UpdateUserStatusOrRoleAsync(UpdateUserStatusOrRoleRequest request);

        /// <summary>Lấy danh sách nhân viên thuộc công ty của Manager (hỗ trợ tìm kiếm + phân trang).</summary>
        Task<ApiResponse> GetCompanyEmployeesAsync(int managerId, string searchTerm, int pageIndex, int pageSize);
        Task<ApiResponse> GetMyManagerAsync();
        // 2. Dành cho Manager: Xem thành viên trong NHÓM (Team) mình quản lý
        Task<ApiResponse> GetMyTeamMembersAsync();

        // 3. Dành cho Manager: Xem toàn bộ nhân viên trong CÔNG TY (Company) của mình
        Task<ApiResponse> GetEmployeesInCompanyAsync();
    }
}
