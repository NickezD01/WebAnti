using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure
{
    /// <summary>
    /// Tự động khởi tạo dữ liệu mặc định (Roles, Users, DifficultyLevels, Categories, Scenarios)
    /// khi ứng dụng chạy lần đầu hoặc database trống từ đường dẫn thư mục cố định.
    /// </summary>
    public static class DbInitializer
    {
        // 🔥 ĐỊNH NGHĨA ĐƯỜNG DẪN GỐC CHỨA FILE JSON ĐÚNG THEO ĐỊA CHỈ BẠN CUNG CẤP
        private static readonly string BaseSeedFolderPath = @"D:\WebAnti\AntiPhisher\AntiPhisher.API\DataSeedFolder";

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
                await SeedLessons(context, logger);

                // 🔥 TIẾN HÀNH SEED 7 BATCH PHISHING SCENARIOS TỪ THƯ MỤC CỐ ĐỊNH
                await SeedPhishingScenarios(context, logger);

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
                new { Email = "user@gmail.com",    FullName = "AntiPhisher User",    Password = "user",   RoleName = "User"    },
            };

            foreach (var account in defaultAccounts)
            {
                if (await context.Users.AnyAsync(u => u.Email == account.Email))
                    continue;

                var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == account.RoleName);
                if (role == null) continue;

                using var hmac = new HMACSHA512();
                var passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(account.Password)));
                var passwordSalt = Convert.ToBase64String(hmac.Key);

                var user = new User
                {
                    Email = account.Email,
                    FullName = account.FullName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = role.RoleId,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
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

        // ─── COMPANY + EMPLOYEE ──────────────────────────────────────
        private static async Task SeedCompanyAndEmployees(AppDbContext context, ILogger? logger)
        {
            if (await context.Companies.AnyAsync()) return;

            var company = new Company
            {
                CompanyName = "AntiPhisher Demo Corp",
                Domain = "antiphisher.vn",
                LogoUrl = "",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            var manager = await context.Users.FirstOrDefaultAsync(u => u.Email == "manager@gmail.com");
            if (manager != null)
            {
                manager.CompanyId = company.CompanyId;
                manager.UpdatedAt = DateTime.UtcNow;
            }

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
                        Email = emp.Email,
                        FullName = emp.FullName,
                        PasswordHash = Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes("demo123"))),
                        PasswordSalt = Convert.ToBase64String(h.Key),
                        RoleId = userRole.RoleId,
                        CompanyId = company.CompanyId,
                        IsActive = true,
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
            logger?.LogInformation("🏢 Seeded demo company + {Count} employees.", demoEmployees.Length);
        }

        // ─── LESSONS ─────────────────────────────────────────────────
        private static async Task SeedLessons(AppDbContext context, ILogger? logger)
        {
            if (await context.Phases.AnyAsync()) return;

            // 🔥 ĐÃ CẬP NHẬT: Lấy file lessons_seed.json từ thư mục cố định
            var seedPath = Path.Combine(BaseSeedFolderPath, "lessons_seed.json");

            if (!File.Exists(seedPath))
            {
                logger?.LogError("❌ SeedLessons aborted: lessons_seed.json not found at {Path}", seedPath);
                return;
            }

            LessonSeedRoot? seedData;
            try
            {
                var json = await File.ReadAllTextAsync(seedPath);
                seedData = JsonSerializer.Deserialize<LessonSeedRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "❌ SeedLessons aborted: cannot deserialize lessons_seed.json");
                return;
            }

            if (seedData?.Lessons == null || seedData.Lessons.Count == 0) return;

            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                var phase1 = new Phase { PhaseName = "Nền Tảng Cơ Bản", Description = "Hiểu phishing là gì, tại sao người ta bị lừa, và các kênh tấn công.", OrderIndex = 1, IsActive = true };
                var phase2 = new Phase { PhaseName = "Nhận Diện Email Đáng Ngờ", Description = "Nhận diện các Red Flag cụ thể trong email phishing.", OrderIndex = 2, IsActive = true };
                var phase3 = new Phase { PhaseName = "Phishing Trong Môi Trường Doanh Nghiệp", Description = "Các kịch bản phishing thực tế tại nơi làm việc.", OrderIndex = 3, IsActive = true };
                var phase4 = new Phase { PhaseName = "Ứng Phó Khi Gặp Phishing", Description = "Quy trình xác minh, ứng phó và báo cáo sự cố.", OrderIndex = 4, IsActive = true };
                await context.Phases.AddRangeAsync(phase1, phase2, phase3, phase4);
                await context.SaveChangesAsync();

                var m1 = new Module { PhaseId = phase1.PhaseId, ModuleName = "Introduction to Phishing", OrderIndex = 1, IsActive = true };
                var m2 = new Module { PhaseId = phase2.PhaseId, ModuleName = "Identifying Suspicious Emails", OrderIndex = 1, IsActive = true };
                var m3 = new Module { PhaseId = phase3.PhaseId, ModuleName = "Corporate & Business Scenarios", OrderIndex = 1, IsActive = true };
                var m4 = new Module { PhaseId = phase4.PhaseId, ModuleName = "Responding to Phishing", OrderIndex = 1, IsActive = true };
                await context.Modules.AddRangeAsync(m1, m2, m3, m4);
                await context.SaveChangesAsync();

                var moduleMap = new Dictionary<string, int> { ["m1"] = m1.ModuleId, ["m2"] = m2.ModuleId, ["m3"] = m3.ModuleId, ["m4"] = m4.ModuleId };

                var now = DateTime.UtcNow;
                var lessons = new List<Lesson>();
                foreach (var item in seedData.Lessons)
                {
                    if (moduleMap.TryGetValue(item.ModuleKey, out var moduleId))
                    {
                        lessons.Add(new Lesson
                        {
                            ModuleId = moduleId,
                            Title = item.Title,
                            TheoryContent = item.TheoryContent,
                            SimulationGuide = "",
                            OrderIndex = item.OrderIndex,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now,
                        });
                    }
                }
                await context.Lessons.AddRangeAsync(lessons);
                await context.SaveChangesAsync();
                await tx.CommitAsync();
                logger?.LogInformation("📚 Seeded 4 Phases, 4 Modules, {Count} Lessons.", lessons.Count);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                logger?.LogError(ex, "❌ SeedLessons thất bại, đã rollback toàn bộ.");
                throw;
            }
        }

        private sealed record LessonSeedRoot(List<LessonSeedItem> Lessons);
        private sealed record LessonSeedItem(string ModuleKey, string Title, string TheoryContent, int OrderIndex);

        // ─── CATEGORIES ─────────────────────────────────────────────
        private static async Task SeedCategories(AppDbContext context, ILogger? logger)
        {
            if (await context.Categories.AnyAsync()) return;

            var categories = new List<Category>
            {
                new Category { CategoryName = "Banking Phishing", Description = "Các cuộc tấn công lừa đảo giả mạo ngân hàng", IconUrl = "bank-icon.png" },
                new Category { CategoryName = "Social Engineering", Description = "Tấn công kỹ thuật xã hội qua email", IconUrl = "social-icon.png" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            logger?.LogInformation("📁 Seeded {Count} categories.", categories.Count);
        }


        // ─── 🔥 SEED PHISHING SCENARIOS (7 BATCH FILES) ─────────────────
        private static async Task SeedPhishingScenarios(AppDbContext context, ILogger? logger)
        {
            // 🔥 ĐÃ CẬP NHẬT: Quét dữ liệu trực tiếp trong thư mục con "Scenarios" thuộc DataSeedFolder của bạn
            // Nếu bạn để các file batch_*.json nằm ngay ngoài DataSeedFolder thì sửa đoạn dưới thành: BaseSeedFolderPath
            string folderPath = Path.Combine(BaseSeedFolderPath, "Scenarios");

            if (!Directory.Exists(folderPath))
            {
                // Dự phòng: Nếu bạn không tạo thư mục Scenarios mà vứt trực tiếp file batch vào DataSeedFolder
                folderPath = BaseSeedFolderPath;
            }

            if (!Directory.Exists(folderPath))
            {
                logger?.LogWarning("⚠️ Thư mục chứa kịch bản mô phỏng không tồn tại: {Path}. Bỏ qua seed scenarios.", folderPath);
                return;
            }

            // Quét tất cả các file có cấu trúc đặt tên dạng batch_*.json
            var jsonFiles = Directory.GetFiles(folderPath, "batch_*.json");
            if (!jsonFiles.Any())
            {
                logger?.LogWarning("⚠️ Không tìm thấy file dạng batch_*.json nào trong thư mục: {Path}", folderPath);
                return;
            }

            var difficulties = await context.DifficultyLevels.ToDictionaryAsync(d => d.LevelName.ToLower(), d => d.DifficultyId);
            var diffEnglishMap = new Dictionary<string, string> { ["easy"] = "dễ", ["medium"] = "trung bình", ["hard"] = "khó" };

            foreach (var file in jsonFiles)
            {
                try
                {
                    string jsonContent = await File.ReadAllTextAsync(file);
                    var batchRoot = JsonSerializer.Deserialize<ScenarioBatchJsonModel>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (batchRoot?.Simulations == null || !batchRoot.Simulations.Any()) continue;

                    logger?.LogInformation("🚀 Đang import kịch bản: {FileName} ({Count} kịch bản)", Path.GetFileName(file), batchRoot.Simulations.Count);

                    foreach (var sim in batchRoot.Simulations)
                    {
                        if (sim.Email == null) continue;

                        if (await context.Scenarios.AnyAsync(s => s.Title == sim.Email.Subject)) continue;

                        var categoryEntity = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == sim.Category);
                        if (categoryEntity == null)
                        {
                            categoryEntity = new Category { CategoryName = sim.Category, Description = $"Kịch bản chuyên sâu về {sim.Category}", IconUrl = "default-icon.png" };
                            await context.Categories.AddAsync(categoryEntity);
                            await context.SaveChangesAsync();
                        }

                        string diffKey = sim.Difficulty?.ToLower() ?? "easy";
                        if (diffEnglishMap.ContainsKey(diffKey)) diffKey = diffEnglishMap[diffKey];

                        if (!difficulties.TryGetValue(diffKey, out int difficultyId))
                        {
                            difficultyId = 1;
                        }

                        bool isPhishingScenario = string.Equals(sim.CorrectAnswer, "phishing", StringComparison.OrdinalIgnoreCase);

                        var scenario = new Scenario
                        {
                            SimulationId = sim.SimulationId,
                            Title = sim.Email.Subject,
                            Subject = sim.Email.Subject,
                            Description = sim.LearningObjective,
                            EmailBodyHtml = sim.Email.Body,
                            SenderName = sim.Email.SenderName ?? "Hệ thống bảo mật",
                            SenderEmail = sim.Email.SenderEmail,
                            IsPhishing = isPhishingScenario,
                            IsActive = true,
                            IsAIGenerated = false,

                            // Đồng bộ mảng lỗi tương tác RedFlags từ file JSON cố định vào DB dưới dạng String JSON thô
                            PhishingIndicators = sim.RedFlags != null && sim.RedFlags.Any()
                                ? JsonSerializer.Serialize(sim.RedFlags)
                                : "[]",

                            ExplanationHint = sim.Feedback != null
                                ? $"**{sim.Feedback.Title}**\n\n*Tóm tắt:* {sim.Feedback.Summary}\n\n*Điểm cốt lõi:* {sim.Feedback.LearningPoint}"
                                : "Kiểm tra kỹ địa chỉ người gửi, tính cấp bách và các đường dẫn ẩn.",

                            CategoryId = categoryEntity.CategoryId,
                            DifficultyId = difficultyId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await context.Scenarios.AddAsync(scenario);
                    }

                    await context.SaveChangesAsync();
                    logger?.LogInformation("✨ Đã nạp thành công dữ liệu kịch bản từ file: {FileName}", Path.GetFileName(file));
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "❌ Lỗi khi import tệp dữ liệu kịch bản: {FileName}", Path.GetFileName(file));
                }
            }
        }

        // ─── JSON MODELS ĐỂ PARSE DỮ LIỆU KỊCH BẢN CHUYÊN SÂU ──────────────
        private class ScenarioBatchJsonModel
        {
            [JsonPropertyName("simulations")] public List<SimulationJsonItem> Simulations { get; set; } = new();
        }

        private class SimulationJsonItem
        {
            [JsonPropertyName("simulation_id")] public string SimulationId { get; set; }
            [JsonPropertyName("difficulty")] public string Difficulty { get; set; }
            [JsonPropertyName("category")] public string Category { get; set; }
            [JsonPropertyName("learning_objective")] public string LearningObjective { get; set; }
            [JsonPropertyName("email")] public EmailJsonContent Email { get; set; }
            [JsonPropertyName("correct_answer")] public string CorrectAnswer { get; set; }
            [JsonPropertyName("feedback")] public FeedbackJsonModel Feedback { get; set; }
            [JsonPropertyName("red_flags")] public List<RedFlagJsonModel> RedFlags { get; set; } = new();
        }

        private class EmailJsonContent
        {
            [JsonPropertyName("sender_name")] public string SenderName { get; set; }
            [JsonPropertyName("sender_email")] public string SenderEmail { get; set; }
            [JsonPropertyName("subject")] public string Subject { get; set; }
            [JsonPropertyName("body")] public string Body { get; set; }
        }

        private class FeedbackJsonModel
        {
            [JsonPropertyName("title")] public string Title { get; set; }
            [JsonPropertyName("summary")] public string Summary { get; set; }
            [JsonPropertyName("learning_point")] public string LearningPoint { get; set; }
        }

        private class RedFlagJsonModel
        {
            [JsonPropertyName("id")] public string Id { get; set; }
            [JsonPropertyName("type")] public string Type { get; set; }
            [JsonPropertyName("visible_text")] public string VisibleText { get; set; }
            [JsonPropertyName("explanation")] public string Explanation { get; set; }
            [JsonPropertyName("severity")] public string Severity { get; set; }
        }
    }
}