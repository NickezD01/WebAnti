using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain.Models
{
    public class SubscriptionPlan : Base
    {
        public int Id { get; set; }
        public SubscriptionPlanName Name { get; set; }
        [Precision(18, 2)]
        public decimal Price { get; set; }
        public int DurationMonth { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Feature { get; set; }

        public List<Subscription>? Subscriptions { get; set; }
    }
    public enum SubscriptionPlanName
    {
        Bronze,
        Silver,
        Gold
    }
}
