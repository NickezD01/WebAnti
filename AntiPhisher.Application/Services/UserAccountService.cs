using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.User;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.UserAccount;
using AntiPhisher.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class UserAccountService : IUserAccountService
    {
        private IUnitOfWork _unitOfWork;
        private IMapper _mapper;
        private IClaimService _claim;
        public UserAccountService(IUnitOfWork unitOfWork, IMapper mapper, IClaimService claim)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _claim = claim;
        }
        public async Task<ApiResponse> GetUserProfileAsync()
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var claim = _claim.GetUserClaim();
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == claim.Id, include: source => source.Include(x => x.Role));
                var userResponse = _mapper.Map<UserProfileResponse>(user);
                return apiResponse.SetOk(userResponse);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }
        public async Task<ApiResponse> UpdateUserProfileAsync(UpdateUserRequest updateUserRequest)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var claim = _claim.GetUserClaim();
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == claim.Id);
                _mapper.Map(updateUserRequest, user);

                user.FullName = user.FullName;


                await _unitOfWork.SaveChangeAsync();
                return apiResponse.SetOk("Update Success");
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }
        public async Task<ApiResponse> GetAllAccountAsync(string searchTerm, int pageIndex, int pageSize)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var query = await _unitOfWork.Users.GetAllAsync(null, include: q => q.Include(u => u.Role), pageIndex: 1, pageSize: 99999);
                
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(u => u.FullName.ToLower().Contains(searchTerm) || u.Email.ToLower().Contains(searchTerm)).ToList();
                }

                int totalCount = query.Count();
                var items = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

                var userResponse = _mapper.Map<List<AccountResponse>>(items);

                // Add synthetic mappings if they don't exist in DB for now
                foreach (var user in userResponse)
                {
                    // Synthetic fields that might not be mapped by EF Core
                    user.SystemScore = 80;
                    user.RiskLevel = "Low";
                }

                return apiResponse.SetOk(new {
                    Items = userResponse,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> UpdateUserStatusOrRoleAsync(UpdateUserStatusOrRoleRequest request)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // 🔒 CHỐT CHẶN BẢO MẬT CUỐI: Đảm bảo RoleId luôn nằm trong tập hợp lệ [1, 2, 3]
                var validRoleIds = new List<int> { 1, 2, 3 };
                if (!validRoleIds.Contains(request.RoleId))
                {
                    return apiResponse.SetBadRequest($"Mã quyền (RoleId = {request.RoleId}) không hợp lệ. Chỉ chấp nhận giá trị: 1 (Admin), 2 (Manager), 3 (User).");
                }

                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == request.UserId);
                if (user == null)
                    return apiResponse.SetBadRequest("User not found");

                // Tiến hành gán sau khi đã vượt qua các tầng kiểm tra an toàn
                user.RoleId = request.RoleId;

                if (request.Status == "Active")
                {
                    user.IsActive = true;
                    user.IsEmailVerified = true;
                }
                else if (request.Status == "Banned")
                {
                    user.IsActive = false;
                }
                else if (request.Status == "Unverified")
                {
                    user.IsEmailVerified = false;
                }

                await _unitOfWork.SaveChangeAsync();
                return apiResponse.SetOk("Update Success");
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }

        // =====================================================================
        // PHẦN 1 — Lấy danh sách nhân viên thuộc công ty của Manager
        // =====================================================================

        public async Task<ApiResponse> GetCompanyEmployeesAsync(int managerId, string searchTerm, int pageIndex, int pageSize)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // 1. Lấy CompanyId của Manager
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager == null)
                    return apiResponse.SetNotFound("Không tìm thấy Manager");

                if (manager.CompanyId == null)
                    return apiResponse.SetBadRequest("Manager chưa được gán vào công ty.");

                int companyId = manager.CompanyId.Value;

                // 2. Lấy tất cả nhân viên (RoleId=3) cùng CompanyId, loại trừ Manager
                var allEmployees = await _unitOfWork.Users.GetAllAsync(
                    x => x.CompanyId == companyId && x.UserId != managerId && x.RoleId == 3);

                if (allEmployees == null) allEmployees = new List<User>();

                // 3. Tìm kiếm theo tên / email
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    allEmployees = allEmployees
                        .Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term))
                        .ToList();
                }

                int total = allEmployees.Count;

                // 4. Phân trang
                var paged = allEmployees
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new CompanyEmployeeResponse
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email,
                        AvatarUrl = u.AvatarUrl ?? string.Empty,
                        IsActive = u.IsActive,
                        IsEmailVerified = u.IsEmailVerified,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .ToList();

                return apiResponse.SetOk(new CompanyEmployeePagedResponse
                {
                    Items = paged,
                    TotalCount = total,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }
        // =====================================================================
        // CHỨC NĂNG 1 — Nhân viên tự xem thông tin Manager của mình
        // =====================================================================
        public async Task<ApiResponse> GetMyManagerAsync(int userId)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // Sử dụng luôn userId từ tham số truyền vào
                var teamMember = await _unitOfWork.TeamMembers.GetAsync(
                    tm => tm.UserId == userId,
                    include: query => query.Include(tm => tm.Team));

                if (teamMember == null || teamMember.Team == null)
                    return apiResponse.SetOk((object)null);

                var team = teamMember.Team;

                if (team.ManagerId <= 0)
                    return apiResponse.SetOk((object)null);

                var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == team.ManagerId);
                if (manager == null)
                    return apiResponse.SetOk((object)null);

                var response = new MyManagerResponse
                {
                    ManagerId = manager.UserId,
                    ManagerName = manager.FullName,
                    ManagerEmail = manager.Email,
                    TeamName = team.TeamName
                };

                return apiResponse.SetOk(response);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Lỗi khi tìm quản lý: {ex.Message}");
            }
        }

        // =====================================================================
        // CHỨC NĂNG 2 — Manager xem danh sách thành viên trong NHÓM của mình
        // =====================================================================
        public async Task<ApiResponse> GetMyTeamMembersAsync(int managerId)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // Sử dụng luôn managerId từ tham số truyền vào
                var team = await _unitOfWork.Teams.GetAsync(t => t.ManagerId == managerId);
                if (team == null)
                    return apiResponse.SetOk(new List<TeamMemberResponse>());

                var members = await _unitOfWork.TeamMembers.GetAllAsync(
                    tm => tm.TeamId == team.TeamId,
                    include: query => query.Include(tm => tm.User).Include(tm => tm.User.Role)
                );

                if (members == null || !members.Any())
                    return apiResponse.SetOk(new List<TeamMemberResponse>());

                var response = members
                    .Where(tm => tm.User != null)
                    .Select(tm => new TeamMemberResponse
                    {
                        UserId = tm.User.UserId,
                        FullName = tm.User.FullName,
                        Email = tm.User.Email,
                        AvatarUrl = tm.User.AvatarUrl ?? string.Empty,
                        Role = tm.User.Role?.RoleName ?? "User"
                    }).ToList();

                return apiResponse.SetOk(response);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Lỗi khi lấy thành viên nhóm: {ex.Message}");
            }
        }

        // =====================================================================
        // CHỨC NĂNG 4 — Manager xem học trình của 1 nhân viên cụ thể
        // =====================================================================
        public async Task<ApiResponse> GetEmployeeProgressAsync(int managerId, int employeeId)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // Xác nhận manager và employee cùng công ty
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager?.CompanyId == null)
                    return apiResponse.SetBadRequest("Bạn chưa được gán vào công ty nào.");

                var employee = await _unitOfWork.Users.GetAsync(x => x.UserId == employeeId);
                if (employee == null)
                    return apiResponse.SetNotFound("Không tìm thấy nhân viên.");
                if (employee.CompanyId != manager.CompanyId)
                    return apiResponse.SetBadRequest("Nhân viên này không thuộc công ty của bạn.");

                // Tải toàn bộ Phase > Module > Lesson
                var phases = await _unitOfWork.Phases.GetAllAsync(
                    p => p.IsActive,
                    include: q => q
                        .Include(p => p.Modules.Where(m => m.IsActive))
                        .ThenInclude(m => m.Lessons.Where(l => l.IsActive)));

                // Tải tiến độ học bài của nhân viên
                var lessonProgresses = await _unitOfWork.UserLessonProgresses
                    .GetAllAsync(x => x.UserId == employeeId);
                var progressDict = lessonProgresses?.ToDictionary(p => p.LessonId) ?? new Dictionary<int, UserLessonProgress>();

                // Build phase items
                var phaseItems = phases?
                    .OrderBy(p => p.OrderIndex)
                    .Select(p => new PhaseProgressItem
                    {
                        PhaseId = p.PhaseId,
                        PhaseName = p.PhaseName,
                        Modules = p.Modules
                            .OrderBy(m => m.OrderIndex)
                            .Select(m => new ModuleProgressItem
                            {
                                ModuleId = m.ModuleId,
                                ModuleName = m.ModuleName,
                                Lessons = m.Lessons
                                    .OrderBy(l => l.OrderIndex)
                                    .Select(l =>
                                    {
                                        progressDict.TryGetValue(l.LessonId, out var prog);
                                        return new LessonProgressItem
                                        {
                                            LessonId = l.LessonId,
                                            Title = l.Title,
                                            IsCompleted = prog?.IsCompleted ?? false,
                                            CompletedAt = prog?.CompletedAt
                                        };
                                    }).ToList()
                            }).ToList(),
                    }).ToList() ?? new List<PhaseProgressItem>();

                // Tính TotalLessons / Completed cho mỗi phase
                foreach (var ph in phaseItems)
                {
                    ph.TotalLessons = ph.Modules.Sum(m => m.Lessons.Count);
                    ph.CompletedLessons = ph.Modules.Sum(m => m.Lessons.Count(l => l.IsCompleted));
                }

                int totalLessons = phaseItems.Sum(p => p.TotalLessons);
                int totalCompleted = phaseItems.Sum(p => p.CompletedLessons);

                // Tải chiến dịch được giao cho nhân viên (CampaignUserAssignment)
                var assignments = await _unitOfWork.CampaignUserAssignments.GetAllAsync(
                    a => a.UserId == employeeId,
                    include: q => q.Include(a => a.Campaign).ThenInclude(c => c.CampaignScenarios));

                var campaignItems = new List<CampaignProgressItem>();

                if (assignments != null)
                {
                    foreach (var asgn in assignments)
                    {
                        var camp = asgn.Campaign;
                        if (camp == null) continue;

                        int totalScenarios = camp.CampaignScenarios?.Count ?? 0;

                        // Lấy lịch sử làm bài cho campaign này
                        var attempts = await _unitOfWork.UserAttempts.GetAllAsync(
                            a => a.UserId == employeeId && a.CampaignId == camp.CampaignId);

                        int attemptedScenarios = attempts?.Select(a => a.ScenarioId).Distinct().Count() ?? 0;
                        int totalAttempts = attempts?.Count() ?? 0;
                        double avgScore = totalAttempts > 0 ? attempts!.Average(a => a.Score) : 0;

                        campaignItems.Add(new CampaignProgressItem
                        {
                            CampaignId = camp.CampaignId,
                            CampaignName = camp.CampaignName,
                            TotalScenarios = totalScenarios,
                            AttemptedScenarios = attemptedScenarios,
                            TotalAttempts = totalAttempts,
                            AverageScore = Math.Round(avgScore, 1),
                        });
                    }
                }

                double globalAvgScore = campaignItems.Count > 0
                    ? Math.Round(campaignItems.Average(c => c.AverageScore), 1)
                    : 0;
                int totalAttemptGlobal = campaignItems.Sum(c => c.TotalAttempts);

                var result = new EmployeeProgressResponse
                {
                    UserId = employee.UserId,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    TotalLessons = totalLessons,
                    TotalLessonsCompleted = totalCompleted,
                    TotalAttempts = totalAttemptGlobal,
                    AverageScore = globalAvgScore,
                    Phases = phaseItems,
                    Campaigns = campaignItems,
                };

                return apiResponse.SetOk(result);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Lỗi khi lấy học trình nhân viên: {ex.Message}");
            }
        }

        // =====================================================================
        // CHỨC NĂNG 3 — Manager xem danh sách toàn bộ nhân viên trong CÔNG TY
        // =====================================================================
        public async Task<ApiResponse> GetEmployeesInCompanyAsync()
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // Service tự lấy ID từ token của người đang đăng nhập
                var claim = _claim.GetUserClaim();
                if (claim == null) return apiResponse.SetBadRequest("Không tìm thấy thông tin xác thực.");

                // Lấy thông tin Manager
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == claim.Id);
                if (manager?.CompanyId == null)
                    return apiResponse.SetOk(new List<CompanyEmployeeResponse>());

                // Lấy danh sách nhân viên trong cùng công ty (loại trừ chính nó)
                var employees = await _unitOfWork.Users.GetAllAsync(
                    x => x.CompanyId == manager.CompanyId && x.UserId != manager.UserId,
                    include: query => query.Include(u => u.Role)
                );

                return apiResponse.SetOk(_mapper.Map<List<CompanyEmployeeResponse>>(employees));
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }
    }
}
