using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<List<Subscription>> GetActiveSubscriptionsByAccountId(int accountId)
        {
            return await _db.Where(s => s.AccountId == accountId &&
                                        s.Status == SubscriptionStatus.Active &&
                                        s.PaymentStatus == PaymentStatus.Paid &&
                                        s.EndDate > DateTime.Now)
                .Include(s => s.SubscriptionPlans)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetSubscriptionsByPlanId(int planId)
        {
            return await _db.Where(s => s.PlanId == planId)
                          .Include(s => s.Account)
                          .ToListAsync();
        }

        public async Task<Subscription> GetSubscriptionWithDetails(int subscriptionId)
        {
            return await _db.Where(s => s.Id == subscriptionId)
                          .Include(s => s.Account)
                          .Include(s => s.SubscriptionPlans)
                          .FirstOrDefaultAsync();
        }

        public async Task<bool> HasActiveSubscription(int accountId)
        {
            return await _db.AnyAsync(s => s.AccountId == accountId &&
                                           s.Status == SubscriptionStatus.Active &&
                                           s.PaymentStatus == PaymentStatus.Paid &&
                                           s.EndDate > DateTime.Now);
        }

        public async Task<int> GetActiveSubscribersCount(int planId)
        {
            return await _db.CountAsync(s => s.PlanId == planId &&
                                           s.Status == SubscriptionStatus.Active &&
                                           s.EndDate > DateTime.Now);
        }

        public async Task<List<Subscription>> GetExpiringSubscriptions(DateTime expiryDate)
        {
            return await _db.Where(s => s.Status == SubscriptionStatus.Active &&
                                      s.EndDate.Date == expiryDate.Date)
                           .Include(s => s.Account)
                           .Include(s => s.SubscriptionPlans)
                           .ToListAsync();
        }

        public async Task<List<Subscription>> GetSubscriptionsForRenewal(DateTime renewalDate)
        {
            return await _db.Where(s => s.Status == SubscriptionStatus.Active &&
                                      s.NextBillingDate.HasValue &&
                                      s.NextBillingDate.Value.Date == renewalDate.Date)
                           .Include(s => s.Account)
                           .Include(s => s.SubscriptionPlans)
                           .ToListAsync();
        }

        public async Task<Subscription> GetLatestSubscription(int accountId)
        {
            return await _db.Where(s => s.AccountId == accountId)
                          .OrderByDescending(s => s.StartDate)
                          .FirstOrDefaultAsync();
        }

        public async Task<List<Subscription>> GetSubscriptionHistory(int accountId)
        {
            return await _db.Where(s => s.AccountId == accountId)
                          .Include(s => s.SubscriptionPlans)
                          .OrderByDescending(s => s.StartDate)
                          .ToListAsync();
        }

        public async Task<bool> IsSubscriptionValid(int subscriptionId)
        {
            var subscription = await _db.FindAsync(subscriptionId);
            return subscription != null &&
                   subscription.Status == SubscriptionStatus.Active &&
                   subscription.EndDate > DateTime.Now;
        }
    }
}
