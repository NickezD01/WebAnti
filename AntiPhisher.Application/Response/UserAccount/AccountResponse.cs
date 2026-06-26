using AntiPhisher.Application.Response.Role;
using System;

namespace AntiPhisher.Application.Response.UserAccount
{
    public class UserProfileResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        public RoleResponse Role { get; set; }
    }

    public class AccountResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        public RoleResponse Role { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SystemScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
    }
}
