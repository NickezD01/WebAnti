using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class PhishingTypeRepository : GenericRepository<PhishingType>, IPhishingTypeRepository
    {
        public PhishingTypeRepository(AppDbContext context) : base(context)
        {
        }
    }
}
