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
        private readonly ILessonService _lessonService;

        public CampaignService(IUnitOfWork unitOfWork, IMapper mapper, ILessonService lessonService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _lessonService = lessonService;
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

        public async Task<CampaignDetailResponse> CreateCampaignAsync(CreateCampaignRequest request, int callerId, string callerRole)
        {
            int? resolvedCompanyId;

            if (callerRole == "Admin")
            {
                // Admin: dùng CompanyId do client gửi (null = public, có giá trị = gán công ty cụ thể)
                resolvedCompanyId = request.CompanyId;
            }
            else if (callerRole == "Manager")
            {
                // Manager: bắt buộc phải có CompanyId, override về công ty của mình
                var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == callerId);
                if (manager?.CompanyId == null)
                    throw new UnauthorizedAccessException("Manager chưa thuộc công ty nào, không thể tạo campaign.");

                resolvedCompanyId = manager.CompanyId;
            }
            else
            {
                throw new UnauthorizedAccessException("Chỉ Admin hoặc Manager mới được tạo campaign.");
            }

            var campaign = new Campaign
            {
                CampaignName = request.CampaignName,
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                CompanyId = resolvedCompanyId,
                CreatedByUserId = callerId,
                IsActive = false,      // Draft — phải gọi PUT /status để activate
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
                    AssignedBy = callerId
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
                    AssignedBy = callerId
                }).ToList();
                await _unitOfWork.CampaignUserAssignments.AddRangeAsync(userAssignments);
            }

            await _unitOfWork.SaveChangeAsync();

            // PA1: Nếu request gửi kèm RequiredLessonIds → set prerequisites ngay lúc tạo
            // (tái dùng hàm hiện có, không viết logic mới)
            if (request.RequiredLessonIds != null && request.RequiredLessonIds.Any())
            {
                await _lessonService.SetCampaignPrerequisitesAsync(campaign.CampaignId, request.RequiredLessonIds);
            }

            var result = await GetCampaignByIdAsync(campaign.CampaignId);
            return result!;
        }

        public async Task<CampaignDetailResponse> UpdateCampaignStatusAsync(int campaignId, bool isActive, int callerId, string callerRole)
        {
            var campaign = await _unitOfWork.Campaigns.GetAsync(c => c.CampaignId == campaignId);
            if (campaign == null)
                throw new KeyNotFoundException($"Không tìm thấy chiến dịch ID {campaignId}");

            await EnforceOwnershipAsync(campaign, callerId, callerRole);

            bool wasInactive = !campaign.IsActive;
            campaign.IsActive = isActive;
            campaign.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangeAsync();

            // Trigger sinh progress CHỈ khi chuyển false→true (activate)
            if (isActive && wasInactive)
            {
                await _lessonService.SyncProgressForCampaignAsync(campaignId);
            }

            var result = await GetCampaignByIdAsync(campaignId);
            return result!;
        }

        public async Task<bool> DeleteCampaignAsync(int id, int callerId, string callerRole)
        {
            var campaign = await _unitOfWork.Campaigns.GetAsync(filter: c => c.CampaignId == id);
            if (campaign == null) throw new KeyNotFoundException($"Không tìm thấy chiến dịch ID {id}");

            await EnforceOwnershipAsync(campaign, callerId, callerRole);

            await _unitOfWork.Campaigns.RemoveByIdAsync(id);
            await _unitOfWork.SaveChangeAsync();
            return true;
        }

        // Admin thao tác bất kỳ; Manager chỉ khi campaign.CompanyId == manager.CompanyId
        private async Task EnforceOwnershipAsync(Campaign campaign, int callerId, string callerRole)
        {
            if (callerRole == "Admin") return;

            if (callerRole == "Manager")
            {
                var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == callerId);
                if (manager?.CompanyId == null
                    || campaign.CompanyId == null
                    || campaign.CompanyId != manager.CompanyId)
                    throw new UnauthorizedAccessException("Manager chỉ được thao tác campaign thuộc công ty mình.");
                return;
            }

            throw new UnauthorizedAccessException("Không có quyền thao tác campaign này.");
        }

        public async Task<IEnumerable<MyCampaignResponse>> GetMyCampaignsAsync(int userId)
        {
            // 1. Campaign gán trực tiếp cho user
            var directAssignments = await _unitOfWork.CampaignUserAssignments.GetAllAsync(x => x.UserId == userId);
            var campaignIds = directAssignments?.Select(a => a.CampaignId).ToHashSet() ?? new HashSet<int>();

            // 2. Campaign gán qua team
            var teamMemberships = await _unitOfWork.TeamMembers.GetAllAsync(x => x.UserId == userId);
            var userTeamIds = teamMemberships?.Select(tm => tm.TeamId).ToHashSet() ?? new HashSet<int>();

            if (userTeamIds.Any())
            {
                var teamAssignments = await _unitOfWork.CampaignTeamAssignments.GetAllAsync(
                    x => userTeamIds.Contains(x.TeamId));
                foreach (var ta in teamAssignments ?? new List<CampaignTeamAssignment>())
                    campaignIds.Add(ta.CampaignId);
            }

            // 3a. Luôn include campaign hệ thống (CompanyId=null) + campaign công ty của user
            var currentUser = await _unitOfWork.Users.GetAsync(u => u.UserId == userId);
            var openCampaigns = await _unitOfWork.Campaigns.GetAllAsync(
                c => c.IsActive && (c.CompanyId == null ||
                     (currentUser != null && currentUser.CompanyId != null && c.CompanyId == currentUser.CompanyId)));
            foreach (var c in openCampaigns ?? Enumerable.Empty<Campaign>())
                campaignIds.Add(c.CampaignId);

            if (!campaignIds.Any())
                return Enumerable.Empty<MyCampaignResponse>();

            // 3b. Chỉ lấy campaign IsActive + trong ngày hiệu lực (không filter theo prerequisites)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var campaigns = await _unitOfWork.Campaigns.GetAllAsync(
                x => campaignIds.Contains(x.CampaignId) && x.IsActive);

            return (campaigns ?? Enumerable.Empty<Campaign>())
                .Where(c =>
                    (c.StartDate == null || c.StartDate <= today) &&
                    (c.EndDate == null || c.EndDate >= today))
                .Select(c => new MyCampaignResponse
                {
                    CampaignId   = c.CampaignId,
                    CampaignName = c.CampaignName,
                    IsActive     = c.IsActive,
                    StartDate    = c.StartDate,
                    EndDate      = c.EndDate,
                    CompanyId    = c.CompanyId
                });
        }
    }
}
