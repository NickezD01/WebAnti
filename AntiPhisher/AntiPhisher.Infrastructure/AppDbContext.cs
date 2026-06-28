using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiPhisher.Infrastructure.Data;
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
        public DbSet<UserFlawSummary> UserFlawSummaries { get; set; }
        public DbSet<AIExpandedKnowledge> AIExpandedKnowledges { get; set; }

        // --- Bảo mật, Nhật ký & Thông báo ---
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        // --- Gói dịch vụ & Thanh toán (Subscription) ---
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<TransactionHistory> Transactions { get; set; }

        // --- HỆ THỐNG BÀI HỌC LÝ THUYẾT (MỚI BỔ SUNG) ---
        public DbSet<Phase> Phases { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<CampaignPrerequisite> CampaignPrerequisites { get; set; }
        public DbSet<UserLessonProgress> UserLessonProgresses { get; set; }

        // --- LỜI MỜI THAM GIA CÔNG TY ---
        public DbSet<CompanyInvitation> CompanyInvitations { get; set; }

        // --- QUIZ BÀI HỌC ---
        public DbSet<LessonQuiz> LessonQuizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }

        // --- KẾT QUẢ BÀI HỌC & CHIẾN DỊCH ---
        public DbSet<UserCampaignResult> UserCampaignResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình Fluent API tự động từ Assembly
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );
        }
    }
}