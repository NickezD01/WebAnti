using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AntiPhisher.Infrastructure
{
    /// <summary>
    /// Tự động khởi tạo dữ liệu mặc định (Roles, Users, DifficultyLevels, Categories)
    /// khi ứng dụng chạy lần đầu hoặc database trống.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetService<ILogger<AppDbContext>>();

            try
            {
                // Đảm bảo database đã được tạo & migrate
                await context.Database.MigrateAsync();

                await SeedRoles(context, logger);
                await SeedUsers(context, logger);
                await SeedDifficultyLevels(context, logger);
                await SeedCategories(context, logger);
                await SeedSubscriptionPlans(context, logger);
                await SeedCompanyAndEmployees(context, logger);

                logger?.LogInformation("✅ Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "❌ An error occurred while seeding the database.");
            }
        }

        // ─── ROLES ───────────────────────────────────────────────────
        private static async Task SeedRoles(AppDbContext context, ILogger? logger)
        {
            if (await context.Roles.AnyAsync()) return;

            var roles = new List<Role>
            {
                new Role { RoleName = "Admin",   Description = "Quản trị viên hệ thống" },
                new Role { RoleName = "Manager", Description = "Quản lý doanh nghiệp" },
                new Role { RoleName = "User",    Description = "Nhân viên / Người dùng" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            logger?.LogInformation("🔑 Seeded {Count} default roles.", roles.Count);
        }

        // ─── USERS ───────────────────────────────────────────────────
        private static async Task SeedUsers(AppDbContext context, ILogger? logger)
        {
            var defaultAccounts = new[]
            {
                new { Email = "admin@gmail.com",   FullName = "AntiPhisher Admin",   Password = "admin",   RoleName = "Admin"   },
                new { Email = "manager@gmail.com", FullName = "AntiPhisher Manager", Password = "manager", RoleName = "Manager" },
                new { Email = "user@gmail.com",    FullName = "AntiPhisher User",    Password = "user",    RoleName = "User"    },
            };

            foreach (var account in defaultAccounts)
            {
                // Bỏ qua nếu email đã tồn tại
                if (await context.Users.AnyAsync(u => u.Email == account.Email))
                    continue;

                var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == account.RoleName);
                if (role == null) continue;

                // Hash mật khẩu bằng HMACSHA512 (giống AuthService)
                using var hmac = new HMACSHA512();
                var passwordHash = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.UTF8.GetBytes(account.Password))
                );
                var passwordSalt = Convert.ToBase64String(hmac.Key);

                var user = new User
                {
                    Email           = account.Email,
                    FullName        = account.FullName,
                    PasswordHash    = passwordHash,
                    PasswordSalt    = passwordSalt,
                    RoleId          = role.RoleId,
                    IsActive        = true,
                    IsEmailVerified = true,   // Đã xác thực sẵn, không cần OTP
                    CreatedAt       = DateTime.UtcNow,
                    UpdatedAt       = DateTime.UtcNow
                };

                await context.Users.AddAsync(user);
                logger?.LogInformation("👤 Seeded default user: {Email} ({Role})", account.Email, account.RoleName);
            }

            await context.SaveChangesAsync();
        }

        // ─── DIFFICULTY LEVELS ───────────────────────────────────────
        private static async Task SeedDifficultyLevels(AppDbContext context, ILogger? logger)
        {
            if (await context.DifficultyLevels.AnyAsync()) return;

            var levels = new List<DifficultyLevel>
            {
                new DifficultyLevel { LevelName = "Dễ",        LevelOrder = 1, BaseScore = 10 },
                new DifficultyLevel { LevelName = "Trung bình", LevelOrder = 2, BaseScore = 15 },
                new DifficultyLevel { LevelName = "Khó",        LevelOrder = 3, BaseScore = 20 },
            };

            await context.DifficultyLevels.AddRangeAsync(levels);
            await context.SaveChangesAsync();
            logger?.LogInformation("📊 Seeded {Count} difficulty levels.", levels.Count);
        }

        // ─── SUBSCRIPTION PLANS ──────────────────────────────────────
        // CHANGED: Name từ enum (Bronze/Silver/Gold) → string tiếng Việt thân thiện
        private static async Task SeedSubscriptionPlans(AppDbContext context, ILogger? logger)
        {
            if (await context.SubscriptionPlans.AnyAsync()) return;

            var plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    Name        = "Gói Cơ Bản",
                    Price       = 990_000m,
                    DurationMonth = 1,
                    MaxSlots    = 10,
                    Description = "Phù hợp cho nhóm nhỏ dưới 10 nhân viên.",
                    Feature     = "10 nhân viên, 50 kịch bản, Báo cáo cơ bản",
                    IsActive    = true
                },
                new SubscriptionPlan
                {
                    Name        = "Gói Chuyên Nghiệp",
                    Price       = 2_490_000m,
                    DurationMonth = 1,
                    MaxSlots    = 30,
                    Description = "Phù hợp cho doanh nghiệp vừa 10-30 nhân viên.",
                    Feature     = "30 nhân viên, 200 kịch bản, Báo cáo nâng cao, AI Feedback",
                    IsActive    = true
                },
                new SubscriptionPlan
                {
                    Name        = "Gói Doanh Nghiệp Pro",
                    Price       = 5_990_000m,
                    DurationMonth = 1,
                    MaxSlots    = 100,
                    Description = "Giải pháp toàn diện cho doanh nghiệp lớn.",
                    Feature     = "100 nhân viên, Không giới hạn kịch bản, Analytics chuyên sâu, Hỗ trợ ưu tiên",
                    IsActive    = true
                }
            };

            await context.SubscriptionPlans.AddRangeAsync(plans);
            await context.SaveChangesAsync();
            logger?.LogInformation("📦 Seeded {Count} subscription plans.", plans.Count);
        }

        // ─── COMPANY + EMPLOYEE (dữ liệu demo cho Analytics) ────────
        private static async Task SeedCompanyAndEmployees(AppDbContext context, ILogger? logger)
        {
            // Bỏ qua nếu đã có công ty
            if (await context.Companies.AnyAsync()) return;

            // Tạo công ty demo
            var company = new Company
            {
                CompanyName = "AntiPhisher Demo Corp",
                Domain      = "antiphisher.vn",
                LogoUrl     = "",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            // Gán CompanyId cho Manager seed
            var manager = await context.Users.FirstOrDefaultAsync(u => u.Email == "manager@gmail.com");
            if (manager != null)
            {
                manager.CompanyId = company.CompanyId;
                manager.UpdatedAt = DateTime.UtcNow;
            }

            // Tạo 3 nhân viên demo để Analytics có dữ liệu hiển thị
            using var hmac = new HMACSHA512();
            var demoEmployees = new[]
            {
                new { Email = "nv1@demo.vn", FullName = "Nguyễn Văn An" },
                new { Email = "nv2@demo.vn", FullName = "Trần Thị Bình" },
                new { Email = "nv3@demo.vn", FullName = "Lê Minh Cường" },
            };

            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
            if (userRole != null)
            {
                foreach (var emp in demoEmployees)
                {
                    if (await context.Users.AnyAsync(u => u.Email == emp.Email)) continue;

                    using var h = new HMACSHA512();
                    await context.Users.AddAsync(new User
                    {
                        Email           = emp.Email,
                        FullName        = emp.FullName,
                        PasswordHash    = Convert.ToBase64String(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes("demo123"))),
                        PasswordSalt    = Convert.ToBase64String(h.Key),
                        RoleId          = userRole.RoleId,
                        CompanyId       = company.CompanyId,
                        IsActive        = true,
                        IsEmailVerified = true,
                        CreatedAt       = DateTime.UtcNow,
                        UpdatedAt       = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
            logger?.LogInformation("🏢 Seeded demo company + {Count} employees.", demoEmployees.Length);
        }

        // ─── CATEGORIES ─────────────────────────────────────────────
        private static async Task SeedCategories(AppDbContext context, ILogger? logger)
        {
            if (await context.Categories.AnyAsync()) return;

            var categories = new List<Category>
            {
                new Category
                {
                    CategoryName = "Banking Phishing",
                    Description  = "Các cuộc tấn công lừa đảo giả mạo ngân hàng",
                    IconUrl      = "bank-icon.png"
                },
                new Category
                {
                    CategoryName = "Social Engineering",
                    Description  = "Tấn công kỹ thuật xã hội qua email",
                    IconUrl      = "social-icon.png"
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            logger?.LogInformation("📁 Seeded {Count} categories.", categories.Count);
        }
    }
}
