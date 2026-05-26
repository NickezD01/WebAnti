using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.SubscriptionPlan
{
    public class UpdateSubscriptionPlanRequest
    {
        //public int PlanId { get; set; }
        public SubscriptionPlanName Name { get; set; }
        public decimal? Price { get; set; }
        public int DurationMonth { get; set; }

        public string Description { get; set; }
        public string Feature { get; set; }
        public bool? IsActive { get; set; }
    }
}
