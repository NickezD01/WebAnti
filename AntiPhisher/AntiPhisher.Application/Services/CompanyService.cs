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

        public CompanyService(IUnitOfWork unitOfWork, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _claimService = claimService;
        }

        public async Task<ApiResponse> GetMyCompanyAsync()
        {
            try
            {
                // 1. Lấy Id của user từ phiên đăng nhập (Claim)
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                {
                    return new ApiResponse().SetBadRequest("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
                }

                // 2. Tìm thông tin User trong DB để lấy CompanyId gán trên User đó
                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);

                // NGUYÊN TẮC: Nếu không thấy user hoặc user chưa thuộc công ty nào (CompanyId NULL) -> Trả null (200 OK)
                if (user == null || user.CompanyId == null)
                {
                    return new ApiResponse().SetOk((object)null);
                }

                // 3. Truy vấn bảng Company theo đúng trường CompanyId (Dựa theo Entity Company bạn gửi)
                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == user.CompanyId.Value);
                if (company == null)
                {
                    return new ApiResponse().SetOk((object)null);
                }

                // 4. Khởi tạo đối tượng CompanyResponse trả về dữ liệu thật cho Frontend
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
                // 1. Kiểm tra session phiên đăng nhập hiện tại của người thực hiện thao tác
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                {
                    return new ApiResponse().SetBadRequest("Yêu cầu đăng nhập để thực hiện chức năng này.");
                }

                // 2. Tìm thông tin của người gọi API xem họ thuộc công ty nào
                var currentUser = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (currentUser == null || currentUser.CompanyId == null)
                {
                    return new ApiResponse().SetBadRequest("Tài khoản của bạn không thuộc quyền quản lý của bất kỳ công ty nào.");
                }

                int targetCompanyId = currentUser.CompanyId.Value;

                // 3. Kiểm tra tính trùng lặp Email của nhân viên mới định thêm
                var isEmailExist = await _unitOfWork.Users.GetAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
                if (isEmailExist != null)
                {
                    return new ApiResponse().SetBadRequest("Email này đã tồn tại trong hệ thống AntiPhisher.");
                }

                // 4. Giả lập / Thực hiện băm mật khẩu bảo mật (Thay bằng hàm mã hóa thực tế dự án đang dùng, ví dụ BCrypt)
                string passwordSalt = Guid.NewGuid().ToString("N").Substring(0, 8);
                string passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dto.Password + passwordSalt));

                // 5. Tạo thực thể User nhân viên mới (Khớp hoàn toàn với Schema DB của bạn)
                var newEmployee = new User
                {
                    CompanyId = targetCompanyId,
                    RoleId = 3,                       // Mặc định RoleId = 3 tương ứng với quyền 'User' (Nhân viên thường)
                    Email = dto.Email.Trim(),
                    FullName = dto.FullName.Trim(),
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    IsActive = true,                  // Kích hoạt luôn trạng thái hoạt động tài khoản
                    IsEmailVerified = true,           // Mặc định xác minh để có thể đăng nhập test được ngay
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 6. Lưu dữ liệu thông qua UnitOfWork
                await _unitOfWork.Users.AddAsync(newEmployee);

                // Gọi bất đồng bộ (Bỏ gán biến void)
                await _unitOfWork.SaveChangeAsync();

                // Nếu luồng code chạy thành công xuống đây mà không ném ra ngoại lệ -> Trả về kết quả thành công luôn
                return new ApiResponse().SetOk(new { message = "Thêm nhân viên mới vào công ty thành công!" });
            }
            catch (Exception ex)
            {
                // Nếu DB lưu lỗi, EF Core sẽ bắn lỗi vào đây và trả về cho Frontend hiển thị tin nhắn lỗi cụ thể
                return new ApiResponse().SetBadRequest($"Lỗi hệ thống khi xử lý thêm nhân viên: {ex.Message}");
            }
        }
    }
}