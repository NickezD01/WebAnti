using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.Subscription
{
    public class SubscriptionResponse
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        //public int PlanId { get; set; }
        public string PlanName { get; set; }
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextBillingDate { get; set; }

        public string Features { get; set; }
    }
}
