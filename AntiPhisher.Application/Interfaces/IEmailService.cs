using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IEmailService
    {
        Task<ApiResponse> SendValidationEmail(string recievedUser, string emailContent);
        Task<ApiResponse> SendNotiMail(string recievedUser, string emailContent);
    }
}
