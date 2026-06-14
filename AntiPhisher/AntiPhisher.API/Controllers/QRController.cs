using AntiPhisher.Application;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Payment;
using AntiPhisher.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QRController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IClaimService _claimService;

        public QRController(IUnitOfWork unitOfWork, IConfiguration config, IClaimService claimService)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _claimService = claimService;
        }

        [HttpPost("create-order")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] int subscriptionId)
        {
            var userId = _claimService.GetUserClaim().Id;
            var sub = await _unitOfWork.Subscriptions.GetAsync(x => x.Id == subscriptionId);
            if (sub == null) return NotFound("Subscription not found.");

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

            // Cập nhật Note thành mã đối soát chuẩn
            order.Note = $"ANTI{order.Id}";
            await _unitOfWork.SaveChangeAsync();

            // URL VietQR (đúng chuẩn VietQR.io)
            var qrUrl = $"https://img.vietqr.io/image/{_config["SePay:BankBin"]}-{_config["SePay:AccountNumber"]}-compact.png" +
                        $"?amount={(long)order.Price}&addInfo={order.Note}&accountName={Uri.EscapeDataString(_config["SePay:AccountName"]!)}";

            return Ok(new { orderId = order.Id, amount = order.Price, content = order.Note, qrUrl });
        }

        [HttpPost("sepay-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookPayload p)
        {
            // Kiểm tra Auth (Bắt buộc)
            if (Request.Headers["Authorization"] != $"Apikey {_config["SePay:WebhookApiKey"]}")
                return Unauthorized();

            // Chỉ xử lý giao dịch tiền vào
            if (p.TransferType != "in") return Ok(new { success = true });

            // Tìm đơn hàng qua mã ANTI{id}
            var orderId = ExtractOrderId(p.Content);
            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId);

            // Kiểm tra điều kiện xác nhận (Chưa thanh toán & ID SePay chưa trùng)
            if (order == null || order.Status == OrderStatus.Paid || order.SepayTransactionId == p.Id.ToString())
                return Ok(new { success = true });

            if (p.TransferAmount >= order.Price)
            {
                order.Status = OrderStatus.Paid;
                order.SepayTransactionId = p.Id.ToString(); // Lưu ID SePay để tránh trùng

                var sub = await _unitOfWork.Subscriptions.GetAsync(x => x.Id == order.SubscriptionId);
                if (sub != null)
                {
                    sub.Status = SubscriptionStatus.Active;
                    sub.PaymentStatus = PaymentStatus.Paid;
                }

                await _unitOfWork.SaveChangeAsync();
            }
            return Ok(new { success = true });
        }

        private int ExtractOrderId(string content)
        {
            var match = System.Text.RegularExpressions.Regex.Match(content, @"ANTI(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }
}
