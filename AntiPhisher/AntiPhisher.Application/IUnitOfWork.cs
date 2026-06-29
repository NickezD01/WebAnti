using AntiPhisher.Application.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application
{
    public interface IUnitOfWork
    {
        // =========================
        // AUTH
        // =========================
        IUserRepository Users { get; }
        IRoleRepository Roles { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IEmailVerificationRepository EmailVerifications { get; }
        IPasswordResetTokenRepository PasswordResetTokens { get; }
        ILoginHistoryRepository LoginHistories { get; }

        // =========================
        // COMPANY / TEAM
        // =========================
        ICompanyRepository Companies { get; }
        ITeamRepository Teams { get; }
        ITeamMemberRepository TeamMembers { get; }

        // =========================
        // CAMPAIGN
        // =========================
        ICampaignRepository Campaigns { get; }
        ICampaignScenarioRepository CampaignScenarios { get; }
        ICampaignTeamAssignmentRepository CampaignTeamAssignments { get; }
        ICampaignUserAssignmentRepository CampaignUserAssignments { get; }

        // =========================
        // SCENARIO
        // =========================
        IScenarioRepository Scenarios { get; }
        ICategoryRepository Categories { get; }
        IDifficultyLevelRepository DifficultyLevels { get; }
        IPhishingTypeRepository PhishingTypes { get; }

        // =========================
        // USER TRAINING
        // =========================
        IUserAttemptRepository UserAttempts { get; }
        IAIFeedbackRepository AIFeedbacks { get; }
        INotificationRepository Notifications { get; }
        IUserFlawSummaryRepository UserFlawSummaries { get; }
        IAIExpandedKnowledgeRepository AIExpandedKnowledges { get; }

        // =========================
        // SUBSCRIPTION / PAYMENT
        // =========================
        ISubscriptionRepository Subscriptions { get; }
        ISubscriptionPlanRepository SubscriptionPlans { get; }
        IOrderRepository Orders { get; }
        IPaymentRepository Payments { get; }

        // ========================================================
        // THEORY LESSONS (MỚI BỔ SUNG)
        // ========================================================
        IPhaseRepository Phases { get; }
        IModuleRepository Modules { get; }
        ILessonRepository Lessons { get; }
        ICampaignPrerequisiteRepository CampaignPrerequisites { get; }
        IUserLessonProgressRepository UserLessonProgresses { get; }
        ICompanyInvitationRepository CompanyInvitations { get; }
        ILessonQuizRepository   LessonQuizzes  { get; }
        IQuizQuestionRepository QuizQuestions  { get; }
        IQuizOptionRepository   QuizOptions    { get; }

        // =========================
        // GAMIFICATION
        // =========================
        IUserCertificateRepository Certificates { get; }

        // =========================
        // METHODS
        // =========================
        Task SaveChangeAsync();
        Task<T> ExecuteScalarAsync<T>(string sql);
        Task ExecuteRawSqlAsync(string sql);

        /// <summary>
        /// Chạy toàn bộ work() trong một DB transaction.
        /// Nếu work() ném exception → tự động Rollback; thành công → Commit.
        /// </summary>
        Task ExecuteInTransactionAsync(Func<Task> work);
    }
}