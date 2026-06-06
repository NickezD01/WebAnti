using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
    {
        public PasswordResetTokenRepository(AppDbContext context) : base(context)
        {
        }
    }
}
