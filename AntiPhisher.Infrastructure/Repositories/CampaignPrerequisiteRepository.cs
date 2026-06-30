using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class CampaignPrerequisiteRepository : GenericRepository<CampaignPrerequisite>, ICampaignPrerequisiteRepository
    {
        public CampaignPrerequisiteRepository(AppDbContext context) : base(context)
        {
        }
    }
}
