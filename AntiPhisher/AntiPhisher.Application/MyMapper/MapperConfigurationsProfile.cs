using AntiPhisher.Application.Request.AttemptRequest;
using AntiPhisher.Application.Request.Orders;
using AntiPhisher.Application.Request.ScenarioRequest;
using AntiPhisher.Application.Request.Subscription;
using AntiPhisher.Application.Request.SubscriptionPlan;
using AntiPhisher.Application.Request.User;
using AntiPhisher.Application.Request.UserAccount;
using AntiPhisher.Application.Response.AttemptRespond;
using AntiPhisher.Application.Response.CampaignResponse;
using AntiPhisher.Application.Response.Orders;
using AntiPhisher.Application.Response.Role;
using AntiPhisher.Application.Response.ScenarioRespond;
using AntiPhisher.Application.Response.Subscription;
using AntiPhisher.Application.Response.SubscriptionPlan;
using AntiPhisher.Application.Response.TeamResponse;
using AntiPhisher.Application.Response.UserAccount;
using AntiPhisher.Domain.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiPhisher.Application.MyMapper
{
    public class MapperConfigurationsProfile : Profile
    {
        public MapperConfigurationsProfile()
        {
            // =========================================================
            // USER & TEAM ACCOUNT MAPPINGS (ĐÃ BỔ SUNG ĐẦY ĐỦ)
            // =========================================================
            CreateMap<Role, RoleResponse>()
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.RoleName));

            CreateMap<User, UserProfileResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

            CreateMap<User, AccountResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : (src.IsEmailVerified ? "Banned" : "Unverified")));

            // [MỚI] Map thông tin nhân viên công ty (Dùng cho danh sách phân trang)
            CreateMap<User, CompanyEmployeeResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? string.Empty))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt));

            // [MỚI] Map thông tin sếp quản lý (Dùng cho api /my-manager nếu map từ thực thể Team/User)
            CreateMap<User, MyManagerResponse>()
                .ForMember(dest => dest.ManagerId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.ManagerEmail, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.TeamName, opt => opt.Ignore()); // Sẽ được map thủ công hoặc qua Include từ Team sau

            // [MỚI] Map thông tin thành viên trong nhóm (Giải quyết triệt để lỗi trống danh sách nhóm)
            CreateMap<User, TeamMemberResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? string.Empty))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : "User"));


            // =========================================================
            // SCENARIO CREATE MAPPING 
            // =========================================================
            CreateMap<CreateScenarioRequest, Scenario>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.SenderName))
                .ForMember(dest => dest.SenderEmail, opt => opt.MapFrom(src => src.SenderEmail))
                .ForMember(dest => dest.RecipientName, opt => opt.MapFrom(src => src.RecipientName))
                .ForMember(dest => dest.EmailBodyHtml, opt => opt.MapFrom(src => src.EmailBodyHtml))
                .ForMember(dest => dest.IsPhishing, opt => opt.MapFrom(src => src.IsPhishing))
                .ForMember(dest => dest.PhishingIndicators, opt => opt.MapFrom(src => src.PhishingIndicators))
                .ForMember(dest => dest.ExplanationHint, opt => opt.MapFrom(src => src.ExplanationHint))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.DifficultyId, opt => opt.MapFrom(src => src.DifficultyId))

                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsAIGenerated, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.AttachmentUrl, opt => opt.MapFrom(src => string.Empty))

                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.SimulationId, opt => opt.Ignore())
                .ForMember(dest => dest.SimulationMetaJson, opt => opt.Ignore());


            // =========================================================
            // SCENARIO UPDATE MAPPING 
            // =========================================================
            CreateMap<UpdateScenarioRequest, Scenario>()
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.SenderEmail, opt => opt.MapFrom(src => src.SenderEmail))
                .ForMember(dest => dest.EmailBodyHtml, opt => opt.MapFrom(src => src.EmailBodyHtml))
                .ForMember(dest => dest.IsPhishing, opt => opt.MapFrom(src => src.IsPhishing))
                .ForMember(dest => dest.PhishingIndicators, opt => opt.MapFrom(src => src.PhishingIndicators))
                .ForMember(dest => dest.DifficultyId, opt => opt.MapFrom(src => src.DifficultyId))

                .ForMember(dest => dest.Description, opt => opt.Ignore())
                .ForMember(dest => dest.SenderName, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<Scenario, ScenarioDetailResponse>()
                .ForMember(dest => dest.DifficultyName, opt => opt.MapFrom(src => src.Difficulty != null ? src.Difficulty.LevelName : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.CategoryName : null));


            // =========================================================
            // ATTEMPT MAPPING
            // =========================================================
            CreateMap<SubmitAttemptRequest, UserAttempt>();

            CreateMap<UserAttempt, AttemptResultResponse>()
                .ForMember(dest => dest.AttemptId, opt => opt.MapFrom(src => src.AttemptId))
                .ForMember(dest => dest.IsCorrect, opt => opt.MapFrom(src => src.IsCorrect))
                .ForMember(dest => dest.ScoreEarned, opt => opt.MapFrom(src => src.Score));

            CreateMap<AIFeedback, AttemptResultResponse>()
                .ForMember(dest => dest.FeedbackText, opt => opt.MapFrom(src => src.FeedbackText))
                .ForMember(dest => dest.IndicatorsExplained, opt => opt.MapFrom(src => src.IndicatorsExplained))
                .ForMember(dest => dest.ImprovementTips, opt => opt.MapFrom(src => src.ImprovementTips))
                .ForMember(dest => dest.AIModel, opt => opt.MapFrom(src => src.AIModel));


            // =========================================================
            // CAMPAIGN MAPPING
            // =========================================================
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            CreateMap<Campaign, CampaignDetailResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    !src.IsActive ? "Tạm dừng" :
                    (src.StartDate.HasValue && today < src.StartDate.Value) ? "Sắp diễn ra" :
                    (src.EndDate.HasValue && today > src.EndDate.Value) ? "Đã kết thúc" : "Đang chạy"))
                .ForMember(dest => dest.Scenarios, opt => opt.MapFrom(src =>
                    src.CampaignScenarios != null
                        ? src.CampaignScenarios
                            .OrderBy(cs => cs.OrderIndex)
                            .Select(cs => cs.Scenario)
                            .ToList()
                        : new List<Scenario>()));


            // =========================================================
            // SUBSCRIPTION PLAN MAPPINGS
            // =========================================================
            CreateMap<CreateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(dest => dest.DurationMonth, opt => opt.MapFrom(src => src.DurationInMonths))
                .ForMember(dest => dest.MaxSlots, opt => opt.MapFrom(src => src.MaxSlots))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<UpdateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<SubscriptionPlan, SubscriptionPlanResponse>()
                .ForMember(dest => dest.DurationInMonths, opt => opt.MapFrom(src => src.DurationMonth))
                .ForMember(dest => dest.ActiveSubscribersCount, opt =>
                    opt.MapFrom(src => src.Subscriptions != null ?
                        src.Subscriptions.Count(s =>
                            s.Status == SubscriptionStatus.Active &&
                            s.PaymentStatus == PaymentStatus.Paid &&
                            s.EndDate > DateTime.UtcNow) : 0));


            // =========================================================
            // SUBSCRIPTION MAPPINGS
            // =========================================================
            CreateMap<CreateSubscriptionRequest, Subscription>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => SubscriptionStatus.PendingPayment))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => PaymentStatus.Pending))
                .ForMember(dest => dest.UsedSlots, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.StartDate, opt => opt.Ignore())
                .ForMember(dest => dest.EndDate, opt => opt.Ignore());

            CreateMap<UpdateSubscriptionRequest, Subscription>()
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Subscription, SubscriptionResponse>()
                .ForMember(dest => dest.AccountName, opt =>
                    opt.MapFrom(src => src.Account != null ? src.Account.FullName : ""))
                .ForMember(dest => dest.PlanName, opt =>
                    opt.MapFrom(src => src.SubscriptionPlans != null ? src.SubscriptionPlans.Name : ""))
                .ForMember(dest => dest.Price, opt =>
                    opt.MapFrom(src => src.SubscriptionPlans != null ? src.SubscriptionPlans.Price : 0))
                .ForMember(dest => dest.Features, opt =>
                    opt.MapFrom(src => src.SubscriptionPlans != null ? src.SubscriptionPlans.Feature : ""))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()));


            // =========================================================
            // ORDER MAPPING
            // =========================================================
            CreateMap<OrderRequest, Order>();
            CreateMap<Order, OrderResponse>();



            // =========================================================
            // TEAM MAPPINGS
            // =========================================================
            CreateMap<Team, TeamResponse>()
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.TeamMembers != null ? src.TeamMembers.Count : 0));

            CreateMap<TeamMember, TeamMemberResponse>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty));

        }
    }
}