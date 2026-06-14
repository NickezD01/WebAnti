using AntiPhisher.Application.Request.Payment;
using AntiPhisher.Application.Response;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IQRService
    {
        Task<ApiResponse> CreateOrderAsync(int subscriptionId);
        Task<bool> ProcessWebhookAsync(SepayWebhookPayload payload);
    }
}
