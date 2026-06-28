using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.AIKnowledge;
using AntiPhisher.Application.Response.AIKnowledge;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class AIKnowledgeService : IAIKnowledgeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AIKnowledgeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AIKnowledgeResponse> CreateAsync(CreateAIKnowledgeRequest request, int createdByUserId)
        {
            var entity = new AIExpandedKnowledge
            {
                Title = request.Title,
                ContextDescription = request.ContextDescription,
                SourceType = request.SourceType,
                SourceUrl = request.SourceUrl,
                SampleEmailContent = request.SampleEmailContent,
                Tags = request.Tags,
                DifficultyId = request.DifficultyId,
                IsActive = true,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AIExpandedKnowledges.AddAsync(entity);
            await _unitOfWork.SaveChangeAsync();

            return MapToResponse(entity, difficultyName: null);
        }

        public async Task<List<AIKnowledgeResponse>> GetAllActiveAsync()
        {
            var items = await _unitOfWork.AIExpandedKnowledges.GetAllAsync(
                filter: k => k.IsActive,
                include: q => q.Include(k => k.Difficulty));

            return (items ?? new List<AIExpandedKnowledge>())
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => MapToResponse(k, k.Difficulty?.LevelName))
                .ToList();
        }

        public async Task<List<AIKnowledgeResponse>> SearchByTagAsync(string tag)
        {
            var items = await _unitOfWork.AIExpandedKnowledges.GetAllAsync(
                filter: k => k.IsActive && k.Tags != null && k.Tags.Contains(tag),
                include: q => q.Include(k => k.Difficulty));

            return (items ?? new List<AIExpandedKnowledge>())
                .Select(k => MapToResponse(k, k.Difficulty?.LevelName))
                .ToList();
        }

        private static AIKnowledgeResponse MapToResponse(AIExpandedKnowledge entity, string difficultyName)
        {
            return new AIKnowledgeResponse
            {
                Id = entity.Id,
                Title = entity.Title,
                ContextDescription = entity.ContextDescription,
                Tags = entity.Tags,
                DifficultyId = entity.DifficultyId,
                DifficultyName = difficultyName,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}