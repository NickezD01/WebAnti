using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int PlanId { get; set; }
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SubscriptionStatus Status { get; set; } // Active, Expired, Cancelled, PendingPayment
        public PaymentStatus PaymentStatus { get; set; } // Paid, Pending, Failed
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextBillingDate { get; set; }
        public User? Account { get; set; }
        public SubscriptionPlan? SubscriptionPlans { get; set; }
        public DateTime ModifiedDate { get; set; }
        public Order Order { get; set; }
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
