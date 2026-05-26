using AntiPhisher.Application.Features;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Repository
{
    public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
    {
        Task<List<SubscriptionPlan>> GetActivePlans();
        Task<SubscriptionPlan> GetPlanWithSubscribers(int planId);
        Task<bool> IsPlanActive(int planId);
        Task<List<SubscriptionPlan>> GetPlansByPriceRange(decimal minPrice, decimal maxPrice);
        Task<bool> IsPlanNameExists(SubscriptionPlanName planName);
        Task<SubscriptionPlan> GetPlanByName(SubscriptionPlanName name);
        Task<List<SubscriptionPlan>> GetPlansByFeature(string feature);
        Task<int> GetTotalSubscribersCount(int planId);
    }
}
