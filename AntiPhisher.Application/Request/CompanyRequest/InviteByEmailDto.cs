using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.CompanyRequest
{
    public class InviteByEmailDto
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        /// <summary>Tên đầy đủ — bắt buộc khi email chưa tồn tại trong hệ thống.</summary>
        public string? FullName { get; set; }
    }
}
