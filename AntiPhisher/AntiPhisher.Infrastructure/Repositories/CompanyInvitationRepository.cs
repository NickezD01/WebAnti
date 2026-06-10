using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class CompanyInvitationRepository : GenericRepository<CompanyInvitation>, ICompanyInvitationRepository
    {
        public CompanyInvitationRepository(AppDbContext context) : base(context)
        {
        }
    }
}
