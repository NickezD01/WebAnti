using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.Subscription
{
    public class UpdateSubscriptionRequest
    {
        //public int SubscriptionId { get; set; }
        public int PlanId { get; set; }  // New plan if upgrading/downgrading
        public string Status { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
