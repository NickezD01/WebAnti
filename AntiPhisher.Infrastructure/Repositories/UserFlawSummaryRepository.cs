using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class UserFlawSummaryRepository : GenericRepository<UserFlawSummary>, IUserFlawSummaryRepository
    {
        public UserFlawSummaryRepository(AppDbContext context) : base(context)
        {
        }
    }
}