using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.SubscriptionPlan
{
    public class UpdateSubscriptionPlanRequest
    {
        // CHANGED: string thay cho SubscriptionPlanName enum
        [MaxLength(200)]
        public string? Name { get; set; }

        public decimal? Price { get; set; }

        [Range(1, 120, ErrorMessage = "Thời hạn gói phải từ 1 đến 120 tháng")]
        public int? DurationMonth { get; set; }
        public string? Description { get; set; }
        public string? Feature { get; set; }
        public bool? IsActive { get; set; }

        // NEW: Cho phép cập nhật MaxSlots khi nâng cấp gói
        public int? MaxSlots { get; set; }
    }
}
