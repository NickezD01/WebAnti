using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.User
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class UpdateUserStatusOrRoleRequest
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SystemScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
    }
}
