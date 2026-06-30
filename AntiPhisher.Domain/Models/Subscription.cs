namespace AntiPhisher.Domain.Models
{
    /// <summary>
    /// Bản ghi đăng ký gói dịch vụ của Manager/Company.
    /// DB Change: Thêm CompanyId (FK → Companies) để liên kết rõ ràng với doanh nghiệp.
    /// DB Change: Thêm UsedSlots (int) để tracking slot đã dùng không cần COUNT(*) mỗi request.
    /// </summary>
    public class Subscription
    {
        public int Id { get; set; }

        // AccountId = Manager (người mua gói)
        public int AccountId { get; set; }

        public int PlanId { get; set; }

        public decimal Price { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public SubscriptionStatus Status { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        public DateTime? NextBillingDate { get; set; }

        // NEW: CompanyId liên kết trực tiếp (không cần join qua User.CompanyId mỗi lần)
        public int? CompanyId { get; set; }

        // NEW: Số slot đang sử dụng (tăng khi invite, giảm khi remove)
        public int UsedSlots { get; set; } = 0;

        public DateTime ModifiedDate { get; set; }

        // Navigation
        public User? Account { get; set; }
        public SubscriptionPlan? SubscriptionPlans { get; set; }
        public Company? Company { get; set; }
        public Order? Order { get; set; }
    }

    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        PendingPayment
    }

    public enum PaymentStatus
    {
        Paid,
        Pending,
        Failed
    }
}
