using AntiPhisher.Application;
using AntiPhisher.Application.Repository;
using AntiPhisher.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        // =========================
        // AUTH
        // =========================
        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IEmailVerificationRepository EmailVerifications { get; }
        public IPasswordResetTokenRepository PasswordResetTokens { get; }
        public ILoginHistoryRepository LoginHistories { get; }

        // =========================
        // COMPANY / TEAM
        // =========================
        public ICompanyRepository Companies { get; }
        public ITeamRepository Teams { get; }
        public ITeamMemberRepository TeamMembers { get; }

        // =========================
        // CAMPAIGN
        // =========================
        public ICampaignRepository Campaigns { get; }
        public ICampaignScenarioRepository CampaignScenarios { get; }
        public ICampaignTeamAssignmentRepository CampaignTeamAssignments { get; }
        public ICampaignUserAssignmentRepository CampaignUserAssignments { get; }

        // =========================
        // SCENARIO
        // =========================
        public IScenarioRepository Scenarios { get; }
        public ICategoryRepository Categories { get; }
        public IDifficultyLevelRepository DifficultyLevels { get; }
        public IPhishingTypeRepository PhishingTypes { get; }

        // =========================
        // USER TRAINING
        // =========================
        public IUserAttemptRepository UserAttempts { get; }
        public IAIFeedbackRepository AIFeedbacks { get; }
        public INotificationRepository Notifications { get; }
        public IUserFlawSummaryRepository UserFlawSummaries { get; }
        public IAIExpandedKnowledgeRepository AIExpandedKnowledges { get; }

        // =========================
        // SUBSCRIPTION / PAYMENT
        // =========================
        public ISubscriptionRepository Subscriptions { get; }
        public ISubscriptionPlanRepository SubscriptionPlans { get; }
        public IOrderRepository Orders { get; }
        public IPaymentRepository Payments { get; }

        // ========================================================
        // THEORY LESSONS (MỚI BỔ SUNG)
        // ========================================================
        public IPhaseRepository Phases { get; }
        public IModuleRepository Modules { get; }
        public ILessonRepository Lessons { get; }
        public ICampaignPrerequisiteRepository CampaignPrerequisites { get; }
        public IUserLessonProgressRepository UserLessonProgresses { get; }
        public ICompanyInvitationRepository CompanyInvitations { get; }
        public ILessonQuizRepository   LessonQuizzes  { get; }
        public IQuizQuestionRepository QuizQuestions  { get; }
        public IQuizOptionRepository   QuizOptions    { get; }

        // =========================
        // CONSTRUCTOR
        // =========================
        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            // AUTH
            Users = new UserAccountRepository(context);
            Roles = new RoleRepository(context);
            RefreshTokens = new RefreshTokenRepository(context);
            EmailVerifications = new EmailVerificationRepository(context);
            PasswordResetTokens = new PasswordResetTokenRepository(context);
            LoginHistories = new LoginHistoryRepository(context);

            // COMPANY / TEAM
            Companies = new CompanyRepository(context);
            Teams = new TeamRepository(context);
            TeamMembers = new TeamMemberRepository(context);

            // CAMPAIGN
            Campaigns = new CampaignRepository(context);
            CampaignScenarios = new CampaignScenarioRepository(context);
            CampaignTeamAssignments = new CampaignTeamAssignmentRepository(context);
            CampaignUserAssignments = new CampaignUserAssignmentRepository(context);

            // SCENARIO
            Scenarios = new ScenarioRepository(context);
            Categories = new CategoryRepository(context);
            DifficultyLevels = new DifficultyLevelRepository(context);
            PhishingTypes = new PhishingTypeRepository(context);

            // USER TRAINING
            UserAttempts = new UserAttemptRepository(context);
            AIFeedbacks = new AIFeedbackRepository(context);
            Notifications = new NotificationRepository(context);
            UserFlawSummaries = new UserFlawSummaryRepository(context);
            AIExpandedKnowledges = new AIExpandedKnowledgeRepository(context);

            // SUBSCRIPTION / PAYMENT
            Subscriptions = new SubscriptionRepository(context);
            SubscriptionPlans = new SubscriptionPlanRepository(context);
            Orders = new OrderRepository(context);
            Payments = new PaymentRepository(context);

            // THEORY LESSONS (MỚI KHỞI TẠO BẰNG CONTEXT)
            Phases = new PhaseRepository(context);
            Modules = new ModuleRepository(context);
            Lessons = new LessonRepository(context);
            CampaignPrerequisites = new CampaignPrerequisiteRepository(context);
            UserLessonProgresses = new UserLessonProgressRepository(context);
            CompanyInvitations = new CompanyInvitationRepository(context);
            LessonQuizzes = new LessonQuizRepository(context);
            QuizQuestions = new QuizQuestionRepository(context);
            QuizOptions   = new QuizOptionRepository(context);
        }

        // =========================
        // SAVE CHANGES
        // =========================
        public async Task SaveChangeAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // =========================
        // EXECUTE SCALAR
        // =========================
        public async Task<T> ExecuteScalarAsync<T>(string sql)
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            try
            {
                if (command.Connection!.State != ConnectionState.Open)
                {
                    await command.Connection.OpenAsync();
                }
                command.CommandText = sql;
                var result = await command.ExecuteScalarAsync();
                return (T)Convert.ChangeType(result!, typeof(T));
            }
            finally
            {
                if (command.Connection!.State == ConnectionState.Open)
                {
                    await command.Connection.CloseAsync();
                }
            }
        }

        // =========================
        // EXECUTE RAW SQL
        // =========================
        public async Task ExecuteRawSqlAsync(string sql)
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            try
            {
                if (command.Connection!.State != ConnectionState.Open)
                {
                    await command.Connection.OpenAsync();
                }
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                if (command.Connection!.State == ConnectionState.Open)
                {
                    await command.Connection.CloseAsync();
                }
            }
        }

        // =========================
        // TRANSACTION
        // =========================
        public async Task ExecuteInTransactionAsync(Func<Task> work)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await work();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}