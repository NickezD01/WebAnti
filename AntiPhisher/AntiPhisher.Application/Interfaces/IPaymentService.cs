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
    public interface IPaymentService
    {
        Task<ApiResponse> CreatePayment(PaymentRequest model, HttpContext context);
        Task<ApiResponse> ProcessCallback(dynamic data); // Dùng dynamic để xử lý được cả Query (VNPay) và Body (MoMo)
    }
}
