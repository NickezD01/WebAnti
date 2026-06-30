using Microsoft.EntityFrameworkCore;

namespace AntiPhisher.Domain.Models
{
    /// <summary>
    /// Gói dịch vụ đăng ký.
    /// DB Change: Cột Name đổi từ INT (enum) → NVARCHAR(200) để đặt tên linh hoạt.
    /// DB Change: Thêm cột MaxSlots INT để giới hạn số nhân viên theo gói.
    /// </summary>
    public class SubscriptionPlan : Base
    {
        public int Id { get; set; }

        // CHANGED: Từ SubscriptionPlanName (enum/int) → string linh hoạt
        public string Name { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal Price { get; set; }

        public int DurationMonth { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Feature { get; set; }

        // NEW: Số lượng nhân viên tối đa Manager được phép mời
        public int MaxSlots { get; set; } = 10;

        public List<Subscription>? Subscriptions { get; set; }
    }

    // REMOVED: SubscriptionPlanName enum (Bronze/Silver/Gold → thay bằng string)
}
