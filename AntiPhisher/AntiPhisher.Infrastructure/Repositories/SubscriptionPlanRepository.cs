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
    public class SubscriptionPlanRepository : GenericRepository<SubscriptionPlan>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<List<SubscriptionPlan>> GetActivePlans()
        {
            return await _db.Where(p => p.IsActive && !p.IsDeleted)
                          .Include(p => p.Subscriptions)
                          .OrderBy(p => p.Price)
                          .ToListAsync();
        }

        public async Task<SubscriptionPlan> GetPlanWithSubscribers(int planId)
        {
            return await _db.Where(p => p.Id == planId)
                          .Include(p => p.Subscriptions)
                            .ThenInclude(s => s.Account)
                          .FirstOrDefaultAsync();
        }

        public async Task<bool> IsPlanActive(int planId)
        {
            var plan = await _db.FindAsync(planId);
            return plan != null && plan.IsActive && !plan.IsDeleted;
        }

        public async Task<List<SubscriptionPlan>> GetPlansByPriceRange(decimal minPrice, decimal maxPrice)
        {
            return await _db.Where(p => p.IsActive &&
                                      !p.IsDeleted &&
                                      p.Price >= minPrice &&
                                      p.Price <= maxPrice)
                          .OrderBy(p => p.Price)
                          .ToListAsync();
        }

        public async Task<bool> IsPlanNameExists(SubscriptionPlanName planName)
        {
            return await _db.AnyAsync(p => p.Name == planName && !p.IsDeleted);
        }

        public async Task<SubscriptionPlan> GetPlanByName(SubscriptionPlanName name)
        {
            return await _db.FirstOrDefaultAsync(p => p.Name == name && !p.IsDeleted);
        }

        public async Task<List<SubscriptionPlan>> GetPlansByFeature(string feature)
        {
            return await _db.Where(p => p.IsActive &&
                                      !p.IsDeleted &&
                                      p.Feature.Contains(feature))
                          .ToListAsync();
        }

        public async Task<int> GetTotalSubscribersCount(int planId)
        {
            return await _db.Where(p => p.Id == planId)
                          .SelectMany(p => p.Subscriptions)
                          .CountAsync(s => s.Status == SubscriptionStatus.Active);
        }
    }
}
