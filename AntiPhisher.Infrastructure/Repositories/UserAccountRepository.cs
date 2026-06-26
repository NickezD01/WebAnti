using AntiPhisher.Application.Features;
using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class UserAccountRepository : GenericRepository<User>, IUserRepository
    {
        public UserAccountRepository(AppDbContext context) : base(context)
        {
        }
    }
}
