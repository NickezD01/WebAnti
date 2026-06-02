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
            // USER MAPPING
            // =========================================================
            CreateMap<Role, RoleResponse>()
                .ForMember(dest => dest.RoleId,
                    opt => opt.MapFrom(src => src.RoleId))
                .ForMember(dest => dest.RoleName,
                    opt => opt.MapFrom(src => src.RoleName));

            CreateMap<User, UserProfileResponse>()
                .ForMember(dest => dest.Id,
                    opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Role));

            CreateMap<User, AccountResponse>()
                .ForMember(dest => dest.Id,
                    opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.IsActive ? "Active" : (src.IsEmailVerified ? "Banned" : "Unverified")));

            // =========================================================
            // SCENARIO CRUD MAPPING (Khớp 100% với Entity Model Scenario)
            // =========================================================

            // 1. Map từ CreateScenarioRequest sang Scenario (Xử lý bù đắp các trường NOT NULL)
            CreateMap<CreateScenarioRequest, Scenario>()
                // Tự động lấy Subject (Tiêu đề email) làm Title cho kịch bản
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Subject))

                // Tự động sinh Description dựa trên email người gửi
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => $"Kịch bản mô phỏng Email Phishing gửi từ nguồn giả mạo: {src.SenderEmail}"))

                // Tách phần tên trước ký tự '@' của Email để làm SenderName (Ví dụ: support@paypal.com -> support)
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.SenderEmail.Contains("@") ? src.SenderEmail.Split('@', StringSplitOptions.None)[0] : "Hệ thống"))

                // Đặt tên mặc định cho người nhận giả định
                .ForMember(dest => dest.RecipientName, opt => opt.MapFrom(src => "Học viên hệ thống"))

                // Đặt lời gợi ý/giải thích mặc định cho học viên dựa trên tài liệu Theory Section
                .ForMember(dest => dest.ExplanationHint, opt => opt.MapFrom(src => "Hãy chú ý kiểm tra kỹ địa chỉ Email người gửi (Domain mạo danh), các liên kết ẩn khi hover chuột và tính cấp bách thúc giục trong nội dung."))

                // Gán các giá trị mặc định cho hệ thống vận hành hệ thống
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => 1)) // Mặc định thuộc nhóm 1 (Email lừa đảo)
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsAIGenerated, opt => opt.MapFrom(src => false))

                // Tránh lỗi null cho các trường chuỗi ký tự khác
                .ForMember(dest => dest.AttachmentUrl, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore());

            // 2. Map từ UpdateScenarioRequest sang Scenario (Giữ tính đồng bộ khi chỉnh sửa kịch bản)
            CreateMap<UpdateScenarioRequest, Scenario>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Subject))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => $"Kịch bản mô phỏng Email Phishing gửi từ nguồn giả mạo: {src.SenderEmail}"))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.SenderEmail.Contains("@") ? src.SenderEmail.Split('@', StringSplitOptions.None)[0] : "Hệ thống"));

            // 3. Map từ Entity gốc ra Response (Lấy kèm thông tin tên độ khó)
            CreateMap<Scenario, ScenarioDetailResponse>()
                .ForMember(dest => dest.DifficultyName, opt => opt.MapFrom(src => src.Difficulty != null ? src.Difficulty.LevelName : null));

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
            // CAMPAIGN MAPPING (Xử lý DateOnly & CampaignScenarios)
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
                            .OrderBy(cs => cs.OrderIndex) // Sắp xếp theo thứ tự hiển thị kịch bản trong Campaign
                            .Select(cs => cs.Scenario)
                            .ToList()
                        : new List<Scenario>()));

            // SubscriptionPlan mappings
            // CHANGED: Name từ enum → string, AutoMapper tự map string→string
            CreateMap<CreateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(dest => dest.DurationMonth, opt => opt.MapFrom(src => src.DurationInMonths))
                .ForMember(dest => dest.MaxSlots,      opt => opt.MapFrom(src => src.MaxSlots))
                .ForMember(dest => dest.IsActive,      opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedDate,   opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<UpdateSubscriptionPlanRequest, SubscriptionPlan>()
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<SubscriptionPlan, SubscriptionPlanResponse>()
                .ForMember(dest => dest.DurationInMonths, opt => opt.MapFrom(src => src.DurationMonth))
                .ForMember(dest => dest.ActiveSubscribersCount, opt =>
                    opt.MapFrom(src => src.Subscriptions != null ?
                        src.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active &&
                                     s.PaymentStatus == PaymentStatus.Paid &&
                                     s.EndDate > DateTime.Now) : 0));

            // Subscription mappings
            CreateMap<CreateSubscriptionRequest, Subscription>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Active"))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => "Pending"))
                .ForMember(dest => dest.EndDate, opt => opt.Ignore()); // Will be calculated in service

            CreateMap<UpdateSubscriptionRequest, Subscription>()
                .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Subscription, SubscriptionResponse>()
                .ForMember(dest => dest.AccountName, opt =>
                    opt.MapFrom(src => src.Account != null ?
                        $"{src.Account.FullName} " : ""))
                .ForMember(dest => dest.PlanName, opt =>
                    opt.MapFrom(src => src.SubscriptionPlans != null ?
                        src.SubscriptionPlans.Name.ToString() : ""))
                .ForMember(dest => dest.Price, opt =>
                    opt.MapFrom(src => src.SubscriptionPlans != null ?
                        src.SubscriptionPlans.Price : 0))
                .ForMember(dest => dest.Features, opt =>
                    opt.MapFrom(src => src.SubscriptionPlans != null ?
                        src.SubscriptionPlans.Feature : ""));

            //Order
            CreateMap<OrderRequest, Order>();
            CreateMap<Order, OrderResponse>();
        }
    }
}