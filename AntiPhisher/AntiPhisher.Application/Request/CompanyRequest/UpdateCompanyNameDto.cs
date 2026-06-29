using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.CompanyRequest
{
    public class UpdateCompanyNameDto
    {
        [Required(ErrorMessage = "Tên công ty không được để trống")]
        [MinLength(2, ErrorMessage = "Tên công ty phải có ít nhất 2 ký tự")]
        [MaxLength(200, ErrorMessage = "Tên công ty không được vượt quá 200 ký tự")]
        public string CompanyName { get; set; } = string.Empty;
    }
}
