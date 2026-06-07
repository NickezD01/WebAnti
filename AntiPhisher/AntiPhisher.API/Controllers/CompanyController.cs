using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CompanyRequest; // Thêm dòng này để nhận diện AddEmployeeDto
using AntiPhisher.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập (gửi kèm Token JWT) mới gọi được để lấy đúng Claim User
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        // Inject Interface vào thông qua Constructor
        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        /// <summary>
        /// Endpoint lấy thông tin công ty của User đang đăng nhập hiện tại
        /// Đường dẫn gọi: GET /api/Company/my-company
        /// </summary>
        [HttpGet("my-company")]
        public async Task<IActionResult> GetMyCompany()
        {
            var response = await _companyService.GetMyCompanyAsync();

            // Luôn trả về HTTP 200 OK kèm theo dữ liệu (hoặc null nếu không có công ty)
            return Ok(response);
        }

        /// <summary>
        /// Endpoint thêm nhân viên mới vào công ty của người quản lý đang đăng nhập hiện tại
        /// Đường dẫn gọi: POST /api/Company/add-employee
        /// </summary>
        [Authorize(Roles = "Admin,Manager")] // Bảo mật nâng cao: Chỉ cho phép tài khoản có quyền Admin hoặc Manager gọi
        [HttpPost("add-employee")]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto dto)
        {
            // Kiểm tra dữ liệu đầu vào (Validation) từ Frontend truyền lên xem có hợp lệ không
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _companyService.AddEmployeeAsync(dto);

            // Kiểm tra thuộc tính kết quả xử lý trong ApiResponse của bạn
            if (response.StatusCode == (System.Net.HttpStatusCode)400)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}