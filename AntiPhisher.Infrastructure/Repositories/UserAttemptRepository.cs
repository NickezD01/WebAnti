using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class UserAttemptRepository : GenericRepository<UserAttempt>, IUserAttemptRepository
    {
        public UserAttemptRepository(AppDbContext context) : base(context)
        {
        }
    }
}
