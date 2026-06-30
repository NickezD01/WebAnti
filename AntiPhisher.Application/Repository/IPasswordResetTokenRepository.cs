using AntiPhisher.Application.Features;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Application.Repository
{
    public interface IPasswordResetTokenRepository : IGenericRepository<PasswordResetToken>
    {
    }
}
