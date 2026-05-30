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
