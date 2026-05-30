using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.User;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.UserAccount;
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
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == claim.Id);
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

    }
}
