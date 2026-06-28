using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.Subscription
{
    public class CreateSubscriptionRequest
    {
        //public int AccountId { get; set; }
        public int PlanId { get; set; }
        public DateTime StartDate { get; set; }
    }
}
