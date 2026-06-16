using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.Analytics;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiPhisher.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        // Múi giờ Việt Nam — thử Windows trước, fallback sang IANA (Linux/Docker)
        private static readonly TimeZoneInfo _vnTz = GetVnTimeZone();
        private static TimeZoneInfo GetVnTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        }

        // Nhãn ngày theo thứ tiếng Việt (0=Sun → index hoán đổi sang 0=Mon)
        private static readonly string[] _dayLabels = { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

        public AnalyticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ======================================================================
        // HELPER: Lấy danh sách UserId nhân viên thuộc công ty của Manager
        // ======================================================================
        private async Task<(int companyId, List<int> employeeIds)> GetCompanyContextAsync(int managerId)
        {
            var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == managerId);
            if (manager?.CompanyId == null)
                return (-1, new List<int>());

            int companyId = manager.CompanyId.Value;
            var employees = await _unitOfWork.Users.GetAllAsync(
                u => u.CompanyId == companyId && u.RoleId == 3); // RoleId=3 = User/Employee

            return (companyId, employees?.Select(u => u.UserId).ToList() ?? new List<int>());
        }

        // ======================================================================
        // HELPER: Tính Risk Score và Risk Level cho một user
        // ======================================================================
        private static (double score, string level) ComputeRisk(IEnumerable<UserAttempt> attempts)
        {
            var list = attempts.ToList();
            if (!list.Any()) return (50.0, "Medium"); // Không có dữ liệu → neutral

            int correct = list.Count(a => a.IsCorrect);
            int total = list.Count;
            int leaks = list.Count(a => a.IsCredentialLeaked);

            double detectionRate = (double)correct / total * 100;

            // Trừ điểm thêm mỗi lần để lộ thông tin đăng nhập (nghiêm trọng nhất)
            double score = detectionRate - (leaks * 10.0);
            score = Math.Clamp(score, 0.0, 100.0);

            string level = score >= 80 ? "Low"
                : score >= 60 ? "Medium"
                : score >= 40 ? "High"
                : "Critical";

            return (Math.Round(score, 1), level);
        }

        // ======================================================================
        // HELPER: Tạo heatmap click nhầm từ danh sách attempt (UTC → UTC+7)
        // ======================================================================
        private static List<HeatmapRow> BuildHeatmap(IEnumerable<UserAttempt> failedAttempts)
        {
            // Khởi tạo 7 hàng (T2–CN), mỗi hàng 24 cột giờ
            var rows = Enumerable.Range(0, 7).Select(i => new HeatmapRow
            {
                DayOfWeek = i + 1,                    // 1=Mon ... 7=Sun
                Day = _dayLabels[i],
                Hours = new List<int>(new int[24])
            }).ToList();

            foreach (var a in failedAttempts)
            {
                var vnTime = TimeZoneInfo.ConvertTimeFromUtc(a.SubmittedAt, _vnTz);
                int dow = (int)vnTime.DayOfWeek;       // 0=Sun ... 6=Sat
                // Chuyển sang 0=Mon ... 6=Sun
                int rowIndex = dow == 0 ? 6 : dow - 1;
                int hour = vnTime.Hour;
                rows[rowIndex].Hours[hour]++;
            }

            return rows;
        }

        // ======================================================================
        // 1. COMPANY OVERVIEW
        // ======================================================================
        public async Task<ApiResponse> GetCompanyOverviewAsync(int managerId)
        {
            try
            {
                var (companyId, employeeIds) = await GetCompanyContextAsync(managerId);
                if (companyId == -1)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty.");

                // Lấy thông tin employees đầy đủ
                var employees = await _unitOfWork.Users.GetAllAsync(
                    u => u.CompanyId == companyId && u.RoleId == 3);

                if (employees == null || !employees.Any())
                {
                    return new ApiResponse().SetOk(new CompanyOverviewResponse
                    {
                        TotalEmployees = 0,
                        ActiveEmployees = 0
                    });
                }

                // Lấy tất cả attempts của nhân viên trong công ty
                var allAttempts = new List<UserAttempt>();
                if (employeeIds.Any())
                {
                    allAttempts = await _unitOfWork.UserAttempts.GetAllAsync(
                        a => employeeIds.Contains(a.UserId));
                    allAttempts ??= new List<UserAttempt>();
                }

                // Lấy tiến độ bài học
                var allProgress = new List<UserLessonProgress>();
                if (employeeIds.Any())
                {
                    allProgress = await _unitOfWork.UserLessonProgresses.GetAllAsync(
                        p => employeeIds.Contains(p.UserId));
                    allProgress ??= new List<UserLessonProgress>();
                }

                // ── Tính các chỉ số tổng hợp ──────────────────────────────────
                int totalAttempts = allAttempts.Count;
                int correctAttempts = allAttempts.Count(a => a.IsCorrect);
                double overallDetectionRate = totalAttempts > 0
                    ? Math.Round((double)correctAttempts / totalAttempts * 100, 1) : 0;

                int totalLessons = allProgress.Count;
                int completedLessons = allProgress.Count(p => p.IsCompleted);
                double lessonCompletionRate = totalLessons > 0
                    ? Math.Round((double)completedLessons / totalLessons * 100, 1) : 0;

                // ── Risk per employee → distribution ──────────────────────────
                var riskDist = new RiskDistribution();
                double totalRiskScore = 0;

                foreach (var emp in employees)
                {
                    var empAttempts = allAttempts.Where(a => a.UserId == emp.UserId);
                    var (score, level) = ComputeRisk(empAttempts);
                    totalRiskScore += score;

                    switch (level)
                    {
                        case "Low":      riskDist.Low++;      break;
                        case "Medium":   riskDist.Medium++;   break;
                        case "High":     riskDist.High++;     break;
                        case "Critical": riskDist.Critical++; break;
                    }
                }

                double avgRiskScore = employees.Any()
                    ? Math.Round(totalRiskScore / employees.Count, 1) : 0;

                // ── Heatmap: click nhầm theo giờ/ngày (UTC+7) ─────────────────
                var failedAttempts = allAttempts.Where(a => !a.IsCorrect || a.IsClickedLink);
                var heatmap = BuildHeatmap(failedAttempts);

                // ── Thời gian phản ứng trung bình ─────────────────────────────
                var withTime = allAttempts.Where(a => a.TimeTakenSeconds.HasValue).ToList();
                double avgDetectionSeconds = withTime.Any()
                    ? Math.Round(withTime.Average(a => (double)a.TimeTakenSeconds!.Value), 1)
                    : 0;

                return new ApiResponse().SetOk(new CompanyOverviewResponse
                {
                    TotalEmployees = employees.Count,
                    ActiveEmployees = employees.Count(e => e.IsActive),
                    TotalAttempts = totalAttempts,
                    OverallDetectionRate = overallDetectionRate,
                    AvgRiskScore = avgRiskScore,
                    LessonCompletionRate = lessonCompletionRate,
                    RiskDistribution = riskDist,
                    Heatmap = heatmap,
                    TotalClickedLink = allAttempts.Count(a => a.IsClickedLink),
                    TotalCredentialLeaked = allAttempts.Count(a => a.IsCredentialLeaked),
                    TotalReported = allAttempts.Count(a => a.IsReported),
                    AvgDetectionSeconds = avgDetectionSeconds
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi lấy company overview: {ex.Message}");
            }
        }

        // ======================================================================
        // 2. HIGH-RISK EMPLOYEES
        // ======================================================================
        public async Task<ApiResponse> GetHighRiskEmployeesAsync(int managerId)
        {
            try
            {
                var (companyId, employeeIds) = await GetCompanyContextAsync(managerId);
                if (companyId == -1)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty.");

                var employees = await _unitOfWork.Users.GetAllAsync(
                    u => u.CompanyId == companyId && u.RoleId == 3);

                if (employees == null || !employees.Any())
                    return new ApiResponse().SetOk(new List<HighRiskEmployeeResponse>());

                // Lấy attempts và progress một lần cho toàn bộ employees
                var allAttempts = employeeIds.Any()
                    ? await _unitOfWork.UserAttempts.GetAllAsync(a => employeeIds.Contains(a.UserId))
                    : new List<UserAttempt>();
                allAttempts ??= new List<UserAttempt>();

                var allProgress = employeeIds.Any()
                    ? await _unitOfWork.UserLessonProgresses.GetAllAsync(p => employeeIds.Contains(p.UserId))
                    : new List<UserLessonProgress>();
                allProgress ??= new List<UserLessonProgress>();

                var result = new List<HighRiskEmployeeResponse>();

                foreach (var emp in employees)
                {
                    var empAttempts = allAttempts.Where(a => a.UserId == emp.UserId).ToList();
                    var empProgress = allProgress.Where(p => p.UserId == emp.UserId).ToList();

                    var (score, level) = ComputeRisk(empAttempts);

                    int total = empAttempts.Count;
                    int correct = empAttempts.Count(a => a.IsCorrect);
                    int completed = empProgress.Count(p => p.IsCompleted);
                    int totalAssigned = empProgress.Count;

                    result.Add(new HighRiskEmployeeResponse
                    {
                        UserId = emp.UserId,
                        FullName = emp.FullName ?? string.Empty,
                        Email = emp.Email ?? string.Empty,
                        AvatarUrl = emp.AvatarUrl ?? string.Empty,
                        RiskScore = score,
                        RiskLevel = level,
                        TotalAttempts = total,
                        CorrectAttempts = correct,
                        DetectionRate = total > 0 ? Math.Round((double)correct / total * 100, 1) : 0,
                        ClickedLinkCount = empAttempts.Count(a => a.IsClickedLink),
                        CredentialLeakedCount = empAttempts.Count(a => a.IsCredentialLeaked),
                        ReportedCount = empAttempts.Count(a => a.IsReported),
                        CompletedLessons = completed,
                        TotalAssignedLessons = totalAssigned,
                        LessonCompletionPct = totalAssigned > 0 ? Math.Round((double)completed / totalAssigned * 100, 1) : 0,
                        LastAttemptAt = empAttempts.OrderByDescending(a => a.SubmittedAt).FirstOrDefault()?.SubmittedAt
                    });
                }

                // Sắp xếp: nguy hiểm nhất (score thấp nhất) lên đầu
                var sorted = result.OrderBy(r => r.RiskScore).ToList();
                return new ApiResponse().SetOk(sorted);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi lấy high-risk employees: {ex.Message}");
            }
        }

        // ======================================================================
        // 3. CAMPAIGN COMPLETION
        // ======================================================================
        public async Task<ApiResponse> GetCampaignCompletionAsync(int managerId)
        {
            try
            {
                var (companyId, employeeIds) = await GetCompanyContextAsync(managerId);
                if (companyId == -1)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty.");

                // Lấy campaigns thuộc công ty này
                var campaigns = await _unitOfWork.Campaigns.GetAllAsync(c => c.CompanyId == companyId);
                if (campaigns == null || !campaigns.Any())
                    return new ApiResponse().SetOk(new CampaignCompletionResponse());

                // Prefetch dữ liệu dùng chung
                var allUserAssignments = await _unitOfWork.CampaignUserAssignments.GetAllAsync(null);
                var allTeamAssignments = await _unitOfWork.CampaignTeamAssignments.GetAllAsync(null);
                var allTeamMembers = await _unitOfWork.TeamMembers.GetAllAsync(null);
                var allPrereqs = await _unitOfWork.CampaignPrerequisites.GetAllAsync(null);
                var allProgress = employeeIds.Any()
                    ? await _unitOfWork.UserLessonProgresses.GetAllAsync(p => employeeIds.Contains(p.UserId))
                    : new List<UserLessonProgress>();
                var allAttempts = employeeIds.Any()
                    ? await _unitOfWork.UserAttempts.GetAllAsync(a => employeeIds.Contains(a.UserId))
                    : new List<UserAttempt>();
                var employees = await _unitOfWork.Users.GetAllAsync(u => u.CompanyId == companyId && u.RoleId == 3);

                allUserAssignments ??= new();
                allTeamAssignments ??= new();
                allTeamMembers ??= new();
                allPrereqs ??= new();
                allProgress ??= new();
                allAttempts ??= new();
                employees ??= new();

                var employeeDict = employees.ToDictionary(e => e.UserId);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var items = new List<CampaignCompletionItem>();

                foreach (var campaign in campaigns)
                {
                    // ── Tập hợp User được gán vào campaign ────────────────────
                    var directUserIds = allUserAssignments
                        .Where(a => a.CampaignId == campaign.CampaignId)
                        .Select(a => a.UserId)
                        .ToHashSet();

                    var teamIds = allTeamAssignments
                        .Where(a => a.CampaignId == campaign.CampaignId)
                        .Select(a => a.TeamId)
                        .ToHashSet();

                    var viaTeamUserIds = allTeamMembers
                        .Where(m => teamIds.Contains(m.TeamId))
                        .Select(m => m.UserId)
                        .ToHashSet();

                    // Union, chỉ lấy nhân viên thuộc công ty này
                    var assignedUserIds = directUserIds
                        .Union(viaTeamUserIds)
                        .Where(id => employeeIds.Contains(id))
                        .ToList();

                    // ── Bài học bắt buộc ──────────────────────────────────────
                    var requiredLessonIds = allPrereqs
                        .Where(p => p.CampaignId == campaign.CampaignId)
                        .Select(p => p.RequiredLessonId)
                        .Distinct()
                        .ToList();

                    int maxPairs = assignedUserIds.Count * requiredLessonIds.Count; // tổng (user×lesson) cần đạt
                    int completedPairs = allProgress
                        .Where(p => assignedUserIds.Contains(p.UserId)
                                 && requiredLessonIds.Contains(p.LessonId)
                                 && p.IsCompleted)
                        .Count();

                    double lessonPct = maxPairs > 0
                        ? Math.Round((double)completedPairs / maxPairs * 100, 1) : 0;

                    // ── Kết quả thực hành ──────────────────────────────────────
                    var campAttempts = allAttempts
                        .Where(a => a.CampaignId == campaign.CampaignId && assignedUserIds.Contains(a.UserId))
                        .ToList();

                    int totalAttempts = campAttempts.Count;
                    double avgScore = totalAttempts > 0
                        ? Math.Round(campAttempts.Average(a => (double)a.Score), 1) : 0;
                    int correct = campAttempts.Count(a => a.IsCorrect);
                    double detectionRate = totalAttempts > 0
                        ? Math.Round((double)correct / totalAttempts * 100, 1) : 0;

                    // ── Trạng thái campaign ────────────────────────────────────
                    string status = !campaign.IsActive ? "Tạm dừng"
                        : campaign.StartDate.HasValue && today < campaign.StartDate.Value ? "Sắp diễn ra"
                        : campaign.EndDate.HasValue && today > campaign.EndDate.Value ? "Đã kết thúc"
                        : "Đang chạy";

                    // ── Per-user summary ───────────────────────────────────────
                    var userSummaries = assignedUserIds.Select(uid =>
                    {
                        var ua = campAttempts.Where(a => a.UserId == uid).ToList();
                        var up = allProgress.Where(p => p.UserId == uid && requiredLessonIds.Contains(p.LessonId)).ToList();
                        var (rScore, rLevel) = ComputeRisk(ua);
                        employeeDict.TryGetValue(uid, out var user);

                        return new CampaignUserSummary
                        {
                            UserId = uid,
                            FullName = user?.FullName ?? $"User #{uid}",
                            RiskScore = rScore,
                            RiskLevel = rLevel,
                            CompletedLessons = up.Count(p => p.IsCompleted),
                            TotalLessons = requiredLessonIds.Count,
                            AttemptCount = ua.Count,
                            HasCredentialLeak = ua.Any(a => a.IsCredentialLeaked)
                        };
                    }).OrderBy(s => s.RiskScore).ToList();

                    items.Add(new CampaignCompletionItem
                    {
                        CampaignId = campaign.CampaignId,
                        CampaignName = campaign.CampaignName,
                        Status = status,
                        StartDate = campaign.StartDate,
                        EndDate = campaign.EndDate,
                        TotalAssignedUsers = assignedUserIds.Count,
                        TotalRequiredLessons = requiredLessonIds.Count,
                        CompletedLessonPairs = completedPairs,
                        LessonCompletionPct = lessonPct,
                        TotalAttempts = totalAttempts,
                        AvgAttemptScore = avgScore,
                        DetectionRate = detectionRate,
                        UserSummaries = userSummaries
                    });
                }

                return new ApiResponse().SetOk(new CampaignCompletionResponse { Campaigns = items });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi lấy campaign completion: {ex.Message}");
            }
        }

        // ======================================================================
        // ADMIN OVERVIEW
        // ======================================================================
        public async Task<ApiResponse> GetAdminOverviewAsync()
        {
            try
            {
                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTz);
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);

                // ── Load raw data ──────────────────────────────────────────────
                var allOrders = await _unitOfWork.Orders.GetAllAsync(null);
                var allUsers  = await _unitOfWork.Users.GetAllAsync(null);
                var allSubs   = await _unitOfWork.Subscriptions.GetAllAsync(null);
                var allPlans  = await _unitOfWork.SubscriptionPlans.GetAllAsync(null);
                var allCompanies = await _unitOfWork.Companies.GetAllAsync(null);

                var planDict = allPlans?.ToDictionary(p => p.Id, p => p.Name) ?? new Dictionary<int, string>();
                var userDict = allUsers?.ToDictionary(u => u.UserId, u => u.Email) ?? new Dictionary<int, string>();

                // ── KPIs ───────────────────────────────────────────────────────
                var paidOrders    = allOrders?.Where(o => o.Status == OrderStatus.Paid).ToList()   ?? new List<Order>();
                var pendingOrders = allOrders?.Where(o => o.Status == OrderStatus.Pending).ToList() ?? new List<Order>();

                long totalRevenue      = paidOrders.Sum(o => (long)(o.Price ?? 0));
                long revenueThisMonth  = paidOrders.Where(o => o.PaidAt.HasValue
                    && TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt.Value, _vnTz) >= startOfMonth)
                    .Sum(o => (long)(o.Price ?? 0));
                long revenueLastMonth  = paidOrders.Where(o => o.PaidAt.HasValue
                    && TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt.Value, _vnTz) >= startOfLastMonth
                    && TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt.Value, _vnTz) < startOfMonth)
                    .Sum(o => (long)(o.Price ?? 0));

                int newUsersThisMonth = allUsers?.Count(u =>
                    TimeZoneInfo.ConvertTimeFromUtc(u.CreatedAt, _vnTz) >= startOfMonth) ?? 0;

                int activeSubscriptions = allSubs?.Count(s => s.Status == SubscriptionStatus.Active) ?? 0;

                // ── Revenue by month (last 6 months) ──────────────────────────
                var revenueByMonth = new List<MonthlyStatItem>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = startOfMonth.AddMonths(-i);
                    var monthEnd   = monthStart.AddMonths(1);
                    var rev = paidOrders
                        .Where(o => o.PaidAt.HasValue
                            && TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt.Value, _vnTz) >= monthStart
                            && TimeZoneInfo.ConvertTimeFromUtc(o.PaidAt.Value, _vnTz) < monthEnd)
                        .Sum(o => (long)(o.Price ?? 0));
                    revenueByMonth.Add(new MonthlyStatItem
                    {
                        Month = $"Th.{monthStart.Month}",
                        Value = rev
                    });
                }

                // ── User growth by month (last 6 months) ──────────────────────
                var userGrowthByMonth = new List<MonthlyStatItem>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = startOfMonth.AddMonths(-i);
                    var monthEnd   = monthStart.AddMonths(1);
                    var cnt = allUsers?.Count(u =>
                        TimeZoneInfo.ConvertTimeFromUtc(u.CreatedAt, _vnTz) >= monthStart
                        && TimeZoneInfo.ConvertTimeFromUtc(u.CreatedAt, _vnTz) < monthEnd) ?? 0;
                    userGrowthByMonth.Add(new MonthlyStatItem
                    {
                        Month = $"Th.{monthStart.Month}",
                        Value = cnt
                    });
                }

                // ── Plan breakdown (active subscriptions) ─────────────────────
                var activeSubs = allSubs?.Where(s => s.Status == SubscriptionStatus.Active).ToList() ?? new List<Subscription>();
                var planBreakdown = activeSubs
                    .GroupBy(s => s.PlanId)
                    .Select(g => new PlanBreakdownItem
                    {
                        PlanName = planDict.TryGetValue(g.Key, out var pn) ? pn : $"Gói #{g.Key}",
                        Count    = g.Count(),
                        Revenue  = paidOrders.Where(o => g.Select(s => s.Id).Contains(o.SubscriptionId))
                                             .Sum(o => (long)(o.Price ?? 0))
                    })
                    .OrderByDescending(p => p.Revenue)
                    .ToList();

                // ── Recent orders (last 10 paid) ──────────────────────────────
                var recentOrders = paidOrders
                    .OrderByDescending(o => o.PaidAt)
                    .Take(10)
                    .Select(o =>
                    {
                        var sub = allSubs?.FirstOrDefault(s => s.Id == o.SubscriptionId);
                        return new RecentOrderItem
                        {
                            OrderId   = o.Id,
                            UserEmail = userDict.TryGetValue(o.AccountId, out var email) ? email : $"User #{o.AccountId}",
                            PlanName  = sub != null && planDict.TryGetValue(sub.PlanId, out var pn) ? pn : "—",
                            Amount    = (long)(o.Price ?? 0),
                            Status    = o.Status.ToString(),
                            CreatedAt = o.PaidAt ?? DateTime.UtcNow,
                            PaidAt    = o.PaidAt
                        };
                    })
                    .ToList();

                var result = new AdminOverviewResponse
                {
                    TotalRevenue         = totalRevenue,
                    RevenueThisMonth     = revenueThisMonth,
                    RevenueLastMonth     = revenueLastMonth,
                    TotalTransactions    = allOrders?.Count ?? 0,
                    PaidTransactions     = paidOrders.Count,
                    PendingTransactions  = pendingOrders.Count,
                    TotalUsers           = allUsers?.Count ?? 0,
                    NewUsersThisMonth    = newUsersThisMonth,
                    ActiveSubscriptions  = activeSubscriptions,
                    TotalCompanies       = allCompanies?.Count ?? 0,
                    RevenueByMonth       = revenueByMonth,
                    UserGrowthByMonth    = userGrowthByMonth,
                    PlanBreakdown        = planBreakdown,
                    RecentOrders         = recentOrders,
                };

                return new ApiResponse().SetOk(result);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi lấy admin overview: {ex.Message}");
            }
        }
    }
}
