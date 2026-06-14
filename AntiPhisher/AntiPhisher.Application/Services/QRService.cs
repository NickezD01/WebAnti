using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Payment;
using AntiPhisher.Application.Response;
using AntiPhisher.Domain.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class QRService : IQRService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IClaimService _claimService;

        public QRService(IUnitOfWork unitOfWork, IConfiguration config, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _claimService = claimService;
        }

        public async Task<ApiResponse> CreateOrderAsync(int subscriptionId)
        {
            var response = new ApiResponse();
            var userId = _claimService.GetUserClaim().Id;

            // Lấy thông tin gói
            var sub = await _unitOfWork.Subscriptions.GetAsync(x => x.Id == subscriptionId);
            if (sub == null) return response.SetNotFound("Gói không tồn tại.");

            // Tạo order
            var order = new Order
            {
                AccountId = userId,
                SubscriptionId = subscriptionId,
                Price = sub.Price,
                Note = "PENDING",
                Status = OrderStatus.Pending
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangeAsync();

            // Cập nhật Note để dùng làm mã đối soát
            order.Note = $"ANTI{order.Id}";
            await _unitOfWork.SaveChangeAsync();

            // Tạo URL QR
            var qrUrl = $"https://img.vietqr.io/image/{_config["SePay:BankBin"]}-{_config["SePay:AccountNumber"]}-compact.png" +
                        $"?amount={(long)order.Price}&addInfo={order.Note}&accountName={Uri.EscapeDataString(_config["SePay:AccountName"]!)}";

            return response.SetOk(new { orderId = order.Id, amount = order.Price, content = order.Note, qrUrl });
        }

        public async Task<bool> ProcessWebhookAsync(SepayWebhookPayload p)
        {
            if (p.TransferType != "in") return false;

            var orderId = ExtractOrderId(p.Content);
            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId);

            if (order == null || order.Status == OrderStatus.Paid) return false;

            if (p.TransferAmount >= order.Price)
            {
                order.Status = OrderStatus.Paid;
                order.SepayTransactionId = p.Id.ToString();

                var sub = await _unitOfWork.Subscriptions.GetAsync(x => x.Id == order.SubscriptionId);
                if (sub != null)
                {
                    sub.Status = SubscriptionStatus.Active;
                    sub.PaymentStatus = PaymentStatus.Paid;
                }

                await _unitOfWork.SaveChangeAsync();
                return true;
            }
            return false;
        }

        private int ExtractOrderId(string content)
        {
            var match = System.Text.RegularExpressions.Regex.Match(content, @"ANTI(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }
}
