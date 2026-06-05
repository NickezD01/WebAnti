using AntiPhisher.Application.Features;
using AntiPhisher.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Repository
{
    public interface ISubscriptionPlanRepository : IGenericRepository<SubscriptionPlan>
    {
        Task<List<SubscriptionPlan>> GetActivePlans();
        Task<SubscriptionPlan> GetPlanWithSubscribers(int planId);
        Task<bool> IsPlanActive(int planId);
        Task<List<SubscriptionPlan>> GetPlansByPriceRange(decimal minPrice, decimal maxPrice);

        // CHANGED: SubscriptionPlanName enum → string
        Task<bool> IsPlanNameExists(string planName);
        Task<SubscriptionPlan> GetPlanByName(string name);

        Task<List<SubscriptionPlan>> GetPlansByFeature(string feature);
        Task<int> GetTotalSubscribersCount(int planId);
    }
}
