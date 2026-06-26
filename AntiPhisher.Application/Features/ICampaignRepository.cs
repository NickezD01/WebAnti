using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Features
{
    public interface ICampaignRepository : IGenericRepository<Campaign>
    {
        // Nếu sau này cần viết hàm query nâng cao riêng cho Campaign thì viết ở đây
    }
}
