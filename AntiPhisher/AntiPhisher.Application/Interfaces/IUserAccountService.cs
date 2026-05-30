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
    }
}
