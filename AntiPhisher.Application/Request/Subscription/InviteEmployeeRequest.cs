using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.Subscription
{
    public class InviteEmployeeRequest
    {
        [Required(ErrorMessage = "Email nhân viên không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên nhân viên không được để trống")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
    }
}
