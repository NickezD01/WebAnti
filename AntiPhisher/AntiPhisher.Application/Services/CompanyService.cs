using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CompanyRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.CompanyResponse;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimService _claimService;
        private readonly IAuthService _authService;

        public CompanyService(IUnitOfWork unitOfWork, IClaimService claimService, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _claimService = claimService;
            _authService = authService;
        }

        public async Task<ApiResponse> GetMyCompanyAsync()
        {
            try
            {
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                {
                    return new ApiResponse().SetBadRequest("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
                }

                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);

                if (user == null || user.CompanyId == null)
                {
                    return new ApiResponse().SetOk((object)null);
                }

                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == user.CompanyId.Value);
                if (company == null)
                {
                    return new ApiResponse().SetOk((object)null);
                }

                var response = new CompanyResponse
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    Domain = company.Domain,
                    LogoUrl = company.LogoUrl
                };

                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi lấy thông tin công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse> AddEmployeeAsync(AddEmployeeDto dto)
        {
            try
            {
                // 1. Kiểm tra session phiên đăng nhập
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                {
                    return new ApiResponse().SetBadRequest("Yêu cầu đăng nhập để thực hiện chức năng này.");
                }

                // 2. Tìm thông tin của người gọi API
                var currentUser = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (currentUser == null || currentUser.CompanyId == null)
                {
                    return new ApiResponse().SetBadRequest("Tài khoản của bạn không thuộc quyền quản lý của bất kỳ công ty nào.");
                }

                int targetCompanyId = currentUser.CompanyId.Value;

                // 3. Kiểm tra tính trùng lặp Email
                var isEmailExist = await _unitOfWork.Users.GetAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
                if (isEmailExist != null)
                {
                    return new ApiResponse().SetBadRequest("Email này đã tồn tại trong hệ thống AntiPhisher.");
                }

                // 4. FIX HOÀN TOÀN TẠI ĐÂY: Gọi hàm trả về PasswordDTO từ AuthService
                var passwordData = _authService.CreatePasswordHash(dto.Password);

                // 5. Tạo thực thể User nhân viên mới (Khớp hoàn toàn cấu trúc dữ liệu và Base64 mã hóa)
                var newEmployee = new User
                {
                    CompanyId = targetCompanyId,
                    RoleId = 3,
                    Email = dto.Email.Trim(),
                    FullName = dto.FullName.Trim(),

                    // Ép kiểu chuẩn từ mảng byte trong DTO sang Base64 chuỗi cho Database
                    PasswordHash = Convert.ToBase64String(passwordData.PasswordHash),
                    PasswordSalt = Convert.ToBase64String(passwordData.PasswordSalt),

                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 6. Lưu dữ liệu thông qua UnitOfWork
                await _unitOfWork.Users.AddAsync(newEmployee);
                await _unitOfWork.SaveChangeAsync();

                return new ApiResponse().SetOk(new { message = "Thêm nhân viên mới vào công ty thành công!" });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi hệ thống khi xử lý thêm nhân viên: {ex.Message}");
            }
        }
    }
}