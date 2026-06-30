using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class UserCertificateRepository : GenericRepository<UserCertificate>, IUserCertificateRepository
    {
        public UserCertificateRepository(AppDbContext context) : base(context) { }
    }
}
