using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.User;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.TeamMemberResponse;
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
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == request.UserId);
                if (user == null)
                    return apiResponse.SetBadRequest("User not found");

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
        public async Task<ApiResponse> GetMyManagerAsync()
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var claim = _claim.GetUserClaim();
                if (claim == null) return apiResponse.SetBadRequest("Phiên đăng nhập không hợp lệ.");

                var teamMember = await _unitOfWork.TeamMembers.GetAsync(
                    tm => tm.UserId == claim.Id,
                    include: query => query.Include(tm => tm.Team));

                if (teamMember == null || teamMember.Team == null)
                    return apiResponse.SetOk((object)null);

                var team = teamMember.Team;

                // Kiểm tra int: không dùng .Value hay null
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
        public async Task<ApiResponse> GetMyTeamMembersAsync()
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var claim = _claim.GetUserClaim();
                if (claim == null) return apiResponse.SetBadRequest("Phiên đăng nhập không hợp lệ.");

                // Lấy Team dựa trên ManagerId (Id người đang đăng nhập)
                var team = await _unitOfWork.Teams.GetAsync(t => t.ManagerId == claim.Id);
                if (team == null)
                    return apiResponse.SetOk(new List<TeamMemberResponse>()); // Trả mảng rỗng nếu chưa quản lý team nào

                // Quét bảng trung gian TeamMembers để lấy danh sách thành viên nhóm
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
        // CHỨC NĂNG 3 — Manager xem danh sách toàn bộ nhân viên trong CÔNG TY
        // =====================================================================
        public async Task<ApiResponse> GetEmployeesInCompanyAsync()
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var claim = _claim.GetUserClaim();
                if (claim == null) return apiResponse.SetBadRequest("Phiên đăng nhập không hợp lệ.");

                // Lấy thông tin tài khoản Manager hiện tại
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == claim.Id);
                if (manager == null)
                    return apiResponse.SetNotFound("Không tìm thấy thông tin tài khoản.");

                // Kiểm tra CompanyId (kiểu int hoặc int? tùy thuộc DB của bạn)
                if (manager.CompanyId == null || manager.CompanyId <= 0)
                    return apiResponse.SetOk(new List<CompanyEmployeeResponse>());

                int currentCompanyId = manager.CompanyId.Value;

                // Lấy tất cả User thuộc cùng CompanyId này, loại trừ chính Manager đang xem
                var employees = await _unitOfWork.Users.GetAllAsync(
                    x => x.CompanyId == currentCompanyId && x.UserId != manager.UserId,
                    include: query => query.Include(u => u.Role)
                );

                if (employees == null || !employees.Any())
                    return apiResponse.SetOk(new List<CompanyEmployeeResponse>());

                var response = employees.Select(u => new CompanyEmployeeResponse
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl ?? string.Empty,
                    IsActive = u.IsActive,
                    //Role = u.Role?.RoleName ?? "User"
                }).ToList();

                return apiResponse.SetOk(response);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Lỗi khi lấy nhân viên công ty: {ex.Message}");
            }
        }
    }
}
