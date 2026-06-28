using AntiPhisher.Application.Features;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Repository
{
    public interface ISubscriptionRepository : IGenericRepository<Subscription>
    {
        Task<List<Subscription>> GetActiveSubscriptionsByAccountId(int accountId);
        Task<List<Subscription>> GetSubscriptionsByPlanId(int planId);
        Task<Subscription> GetSubscriptionWithDetails(int subscriptionId);

        Task<bool> HasActiveSubscription(int accountId);
        Task<int> GetActiveSubscribersCount(int planId);
        Task<List<Subscription>> GetExpiringSubscriptions(DateTime expiryDate);
        Task<List<Subscription>> GetSubscriptionsForRenewal(DateTime renewalDate);
        Task<Subscription> GetLatestSubscription(int accountId);
        Task<List<Subscription>> GetSubscriptionHistory(int accountId);
        Task<bool> IsSubscriptionValid(int subscriptionId);
    }
}
