using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response;
using MimeKit;
using System;
using System.Linq;
using MailKit.Net.Smtp;
using MailKit.Security; // Thêm namespace này để dùng SecureSocketOptions
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AntiPhisher.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _sender;
        private readonly string _appPassword;
        private readonly string _smtpHost;
        private readonly int _smtpPort;

        public EmailService(IConfiguration configuration)
        {
            // Sửa lại cách đọc cấu hình: Hỗ trợ cả dấu gạch dưới hai lần __ (Render dùng cách này) và dấu hai chấm :
            _sender = configuration["EmailSettings__Sender"] ?? configuration["EmailSettings:Sender"] ?? string.Empty;
            _appPassword = configuration["EmailSettings__AppPassword"] ?? configuration["EmailSettings:AppPassword"] ?? string.Empty;
            _smtpHost = configuration["EmailSettings__SmtpHost"] ?? configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";

            var portRaw = configuration["EmailSettings__SmtpPort"] ?? configuration["EmailSettings:SmtpPort"];
            _smtpPort = int.TryParse(portRaw, out var port) ? port : 587; // Mặc định nên để 587 cho môi trường Cloud
        }

        private ApiResponse ValidateEmailConfig()
        {
            if (string.IsNullOrWhiteSpace(_sender) || string.IsNullOrWhiteSpace(_appPassword))
            {
                var detail = $"Sender={(string.IsNullOrWhiteSpace(_sender) ? "EMPTY" : "SET")}, " +
                             $"AppPassword={(string.IsNullOrWhiteSpace(_appPassword) ? "EMPTY" : "SET")}, " +
                             $"SmtpHost='{_smtpHost}', SmtpPort={_smtpPort}";
                Console.Error.WriteLine($"[EMAIL CONFIG ERROR] {detail}");
                Console.Error.WriteLine("[EMAIL CONFIG HINT] Trên Render, hãy set env: EmailSettings__Sender, EmailSettings__AppPassword");

                return new ApiResponse().SetBadRequest(
                    message: $"Email service is not configured. {detail}"
                );
            }

            return new ApiResponse().SetOk();
        }

        public async Task<ApiResponse> SendNotiMail(string recievedUser, string emailContent)
        {
            try
            {
                var configState = ValidateEmailConfig();
                if (!configState.IsSuccess) return configState;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("AntiPhisher System", _sender));
                message.To.Add(new MailboxAddress("", recievedUser));
                message.Subject = "Notification";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = emailContent;
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // SỬA TẠI ĐÂY: Thay 'true' bằng 'SecureSocketOptions.Auto' để tự tương thích cổng 587 trên Render
                    await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.Auto);
                    await client.AuthenticateAsync(_sender, _appPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                return new ApiResponse().SetOk("Mail Sent!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SMTP ERROR] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine($"[SMTP INNER] {ex.InnerException.Message}");
                return new ApiResponse().SetBadRequest($"Something went wrong: {ex.Message}");
            }
        }

        public async Task<ApiResponse> SendValidationEmail(string recievedUser, string emailContent)
        {
            try
            {
                var configState = ValidateEmailConfig();
                if (!configState.IsSuccess) return configState;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("AntiPhisher System", _sender));
                message.To.Add(new MailboxAddress("", recievedUser));
                message.Subject = "Verification Email";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = emailContent;
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // SỬA TẠI ĐÂY: Thay 'true' bằng 'SecureSocketOptions.Auto'
                    await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.Auto);
                    await client.AuthenticateAsync(_sender, _appPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                return new ApiResponse().SetOk("Mail Sent!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SMTP ERROR] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine($"[SMTP INNER] {ex.InnerException.Message}");
                return new ApiResponse().SetBadRequest($"Something went wrong: {ex.Message}");
            }
        }
    }
}