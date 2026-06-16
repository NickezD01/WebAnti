using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.SubscriptionPlan
{
    public class CreateSubscriptionPlanRequest
    {
        // CHANGED: Từ SubscriptionPlanName enum → string tự do
        [Required(ErrorMessage = "Tên gói không được để trống")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        [Range(1, 120, ErrorMessage = "Thời hạn gói phải từ 1 đến 120 tháng")]
        public int DurationInMonths { get; set; }
        public string? Description { get; set; }
        public string? Feature { get; set; }
        public bool IsActive { get; set; } = true;

        // NEW: MaxSlots được cấu hình khi tạo gói (default 10)
        public int MaxSlots { get; set; } = 10;
    }
}
