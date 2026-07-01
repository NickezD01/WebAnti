using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using MailKit.Net.Smtp;
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
            _sender      = configuration["EmailSettings:Sender"]      ?? string.Empty;
            _appPassword = configuration["EmailSettings:AppPassword"]  ?? string.Empty;
            _smtpHost    = configuration["EmailSettings:SmtpHost"]     ?? "smtp.gmail.com";
            _smtpPort    = int.TryParse(configuration["EmailSettings:SmtpPort"], out var port) ? port : 465;
        }

        private ApiResponse ValidateEmailConfig()
        {
            if (string.IsNullOrWhiteSpace(_sender) || string.IsNullOrWhiteSpace(_appPassword))
            {
                return new ApiResponse().SetBadRequest(
                    message: "Email service is not configured. Please set EmailSettings:Sender and EmailSettings:AppPassword."
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
                    await client.ConnectAsync(_smtpHost, _smtpPort, true);
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
                    await client.ConnectAsync(_smtpHost, _smtpPort, true);
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
