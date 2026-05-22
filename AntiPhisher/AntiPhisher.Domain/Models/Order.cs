using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        //public int? PlanId { get; set; }
        public int SubscriptionId { get; set; }
        public decimal? Price { get; set; }
        public string Note { get; set; }
        //public OrderStatus Status { get; set; } = OrderStatus.Pending; // Pending, Paid, Canceled
        public bool IsDelete { get; set; }
        public User? UserAccount { get; set; }
        public Subscription Subscription { get; set; }
        //public SubscriptionPlan? SubscriptionPlans { get; set; }
        public List<Payment>? Payments { get; set; }
    }
}
