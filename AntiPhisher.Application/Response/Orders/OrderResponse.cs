using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.Orders
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        //public int? PlanId { get; set; }
        public decimal? Price { get; set; }

        //public OrderStatus Status { get; set; }

        //public string Status { get; set; }

        //public SubscriptionResponse Subscription { get; set; }

    }
}
