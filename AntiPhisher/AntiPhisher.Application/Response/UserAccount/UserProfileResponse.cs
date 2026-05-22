using AntiPhisher.Application.Response.Role;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.UserAccount
{
    public class AccountResponse
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public string AvatarUrl { get; set; }

        public string Email { get; set; }

        public int? CompanyId { get; set; }

        public RoleResponse Role { get; set; }
    }


}

