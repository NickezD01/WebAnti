using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class AIExpandedKnowledgeRepository : GenericRepository<AIExpandedKnowledge>, IAIExpandedKnowledgeRepository
    {
        public AIExpandedKnowledgeRepository(AppDbContext context) : base(context)
        {
        }
    }
}