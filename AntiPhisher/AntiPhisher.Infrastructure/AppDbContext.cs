using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // --- Hệ thống Tài khoản & Tổ chức ---
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }

        // --- Chiến dịch & Kịch bản Thực hành ---
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CampaignScenario> CampaignScenarios { get; set; }
        public DbSet<CampaignTeamAssignment> CampaignTeamAssignments { get; set; }
        public DbSet<CampaignUserAssignment> CampaignUserAssignments { get; set; }

        // --- Thuộc tính Kịch bản Phishing ---
        public DbSet<Category> Categories { get; set; }
        public DbSet<DifficultyLevel> DifficultyLevels { get; set; }
        public DbSet<Scenario> Scenarios { get; set; }
        public DbSet<PhishingType> PhishingTypes { get; set; }

        // --- Kết quả & Đánh giá (AI) ---
        public DbSet<UserAttempt> UserAttempts { get; set; }
        public DbSet<AIFeedback> AIFeedbacks { get; set; }

        // --- Bảo mật, Nhật ký & Thông báo ---
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }

        // --- Gói dịch vụ & Thanh toán (Subscription) ---
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // --- HỆ THỐNG BÀI HỌC LÝ THUYẾT (MỚI BỔ SUNG) ---
        public DbSet<Phase> Phases { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<CampaignPrerequisite> CampaignPrerequisites { get; set; }
        public DbSet<UserLessonProgress> UserLessonProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tự động quét quét và cấu hình Fluent API từ tất cả các file kế thừa IEntityTypeConfiguration
            // bao gồm cả OrderConfiguration lẫn các cấu hình LessonConfiguration, PhaseConfiguration,... vừa tạo.
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );
        }
    }
}