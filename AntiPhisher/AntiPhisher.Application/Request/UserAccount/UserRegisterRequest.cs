using AntiPhisher.Application.Response.Role;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.UserAccount
{
    public class UserRegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string FullName { get; set; } = string.Empty;
        public RoleResponse Role { get; set; }
    }


}

