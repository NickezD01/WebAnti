using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CampaignRequest;
using AntiPhisher.Application.Response.CampaignResponse;
using AntiPhisher.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CampaignService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CampaignDetailResponse>> GetAllCampaignsAsync()
        {
            var campaigns = await _unitOfWork.Campaigns.GetAllAsync(
                filter: null,
                include: query => query
                    .Include(c => c.CampaignScenarios)
                    .ThenInclude(cs => cs.Scenario)
                    .ThenInclude(s => s.Difficulty),
                pageIndex: 1,
                pageSize: 100
            );
            return _mapper.Map<IEnumerable<CampaignDetailResponse>>(campaigns);
        }

        public async Task<CampaignDetailResponse?> GetCampaignByIdAsync(int id)
        {
            var campaign = await _unitOfWork.Campaigns.GetAsync(
                filter: c => c.CampaignId == id,
                include: query => query
                    .Include(c => c.CampaignScenarios)
                    .ThenInclude(cs => cs.Scenario)
                    .ThenInclude(s => s.Difficulty)
            );
            return campaign == null ? null : _mapper.Map<CampaignDetailResponse>(campaign);
        }

        public async Task<CampaignDetailResponse> CreateCampaignAsync(CreateCampaignRequest request, int adminId)
        {
            var campaign = new Campaign
            {
                CampaignName = request.CampaignName,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompanyId = request.CompanyId,
                CreatedByUserId = adminId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Campaigns.AddAsync(campaign);
            await _unitOfWork.SaveChangeAsync();

            if (request.ScenarioIds != null && request.ScenarioIds.Any())
            {
                var campaignScenarios = request.ScenarioIds.Select((scenarioId, index) => new CampaignScenario
                {
                    CampaignId = campaign.CampaignId,
                    ScenarioId = scenarioId,
                    OrderIndex = index + 1
                }).ToList();
                await _unitOfWork.CampaignScenarios.AddRangeAsync(campaignScenarios);
            }

            if (request.TeamIds != null && request.TeamIds.Any())
            {
                var teamAssignments = request.TeamIds.Select(teamId => new CampaignTeamAssignment
                {
                    CampaignId = campaign.CampaignId,
                    TeamId = teamId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = adminId
                }).ToList();
                await _unitOfWork.CampaignTeamAssignments.AddRangeAsync(teamAssignments);
            }

            if (request.UserIds != null && request.UserIds.Any())
            {
                var userAssignments = request.UserIds.Select(userId => new CampaignUserAssignment
                {
                    CampaignId = campaign.CampaignId,
                    UserId = userId,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = adminId
                }).ToList();
                await _unitOfWork.CampaignUserAssignments.AddRangeAsync(userAssignments);
            }

            await _unitOfWork.SaveChangeAsync();
            var result = await GetCampaignByIdAsync(campaign.CampaignId);
            return result!;
        }

        public async Task<bool> DeleteCampaignAsync(int id)
        {
            var campaign = await _unitOfWork.Campaigns.GetAsync(filter: c => c.CampaignId == id);
            if (campaign == null) throw new Exception($"Không tìm thấy chiến dịch ID {id}");

            await _unitOfWork.Campaigns.RemoveByIdAsync(id);
            await _unitOfWork.SaveChangeAsync();
            return true;
        }
    }
}
