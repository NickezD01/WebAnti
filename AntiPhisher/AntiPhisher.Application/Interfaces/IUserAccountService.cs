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

        Task<ApiResponse> GetCompanyEmployeesAsync(int managerId, string searchTerm, int pageIndex, int pageSize);

        // Sửa 2 hàm này: Nhận ID trực tiếp từ bên ngoài truyền vào
        Task<ApiResponse> GetMyManagerAsync(int userId);
        Task<ApiResponse> GetMyTeamMembersAsync(int managerId);

        Task<ApiResponse> GetEmployeesInCompanyAsync();
        Task<ApiResponse> GetEmployeeProgressAsync(int managerId, int employeeId);
    }
}
