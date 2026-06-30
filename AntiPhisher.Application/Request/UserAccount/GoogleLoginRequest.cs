using System;
using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.UserAccount
{
    public class GoogleLoginRequest
    {
        [Required]
        public string Credential { get; set; } = string.Empty;
    }
}