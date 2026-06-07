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
        Task<ApiResponse> AddEmployeeAsync(AddEmployeeDto dto);
    }
}
