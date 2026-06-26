using AntiPhisher.Application.Request.AIKnowledge;
using AntiPhisher.Application.Response.AIKnowledge;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IAIKnowledgeService
    {
        Task<AIKnowledgeResponse> CreateAsync(CreateAIKnowledgeRequest request, int createdByUserId);
        Task<List<AIKnowledgeResponse>> GetAllActiveAsync();
        Task<List<AIKnowledgeResponse>> SearchByTagAsync(string tag);
    }
}