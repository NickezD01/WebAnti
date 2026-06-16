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
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            var subscriptionId = req.SubscriptionId;
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

        [HttpPost("create-order-individual")]
        [Authorize]
        public async Task<IActionResult> CreateOrderIndividual([FromBody] CreateOrderIndividualRequest req)
        {
            var planId = req.PlanId;
            var userId = _claimService.GetUserClaim().Id;

            var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == planId && p.IsActive);
            if (plan == null)
                return BadRequest(new { message = "Gói dịch vụ không tồn tại hoặc đã ngừng cung cấp." });

            // Chặn spam: user lẻ đã có sub đang chờ hoặc active thì không tạo mới
            var existingSub = await _unitOfWork.Subscriptions.GetAsync(
                s => s.AccountId == userId && s.CompanyId == null
                  && (s.Status == SubscriptionStatus.PendingPayment || s.Status == SubscriptionStatus.Active));
            if (existingSub != null)
            {
                if (existingSub.Status == SubscriptionStatus.Active)
                    return BadRequest(new { message = "Bạn đang có gói cá nhân đang hoạt động." });

                // PendingPayment: trả về QR của đơn hàng đang chờ để user tiếp tục thanh toán
                var existingOrder = await _unitOfWork.Orders.GetAsync(
                    o => o.SubscriptionId == existingSub.Id && o.Status == OrderStatus.Pending);
                if (existingOrder != null)
                {
                    var existingQrUrl = $"https://img.vietqr.io/image/{_config["SePay:BankBin"]}-{_config["SePay:AccountNumber"]}-compact.png" +
                                        $"?amount={(long)existingOrder.Price!}&addInfo={existingOrder.Note}&accountName={Uri.EscapeDataString(_config["SePay:AccountName"]!)}";
                    return Ok(new { orderId = existingOrder.Id, amount = existingOrder.Price, content = existingOrder.Note, qrUrl = existingQrUrl });
                }
            }

            // Tạo Subscription cho user lẻ — StartDate/EndDate chưa set, sẽ set khi Paid (webhook)
            var subscription = new Subscription
            {
                AccountId = userId,
                CompanyId = null,
                PlanId = planId,
                Price = plan.Price,
                Status = SubscriptionStatus.PendingPayment,
                PaymentStatus = PaymentStatus.Pending,
                UsedSlots = 0,
                ModifiedDate = DateTime.UtcNow
            };

            await _unitOfWork.Subscriptions.AddAsync(subscription);
            await _unitOfWork.SaveChangeAsync();

            var order = new Order
            {
                AccountId = userId,
                SubscriptionId = subscription.Id,
                Price = plan.Price,
                Note = "PENDING",
                Status = OrderStatus.Pending
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangeAsync();

            order.Note = $"ANTI{order.Id}";
            await _unitOfWork.SaveChangeAsync();

            var qrUrl = $"https://img.vietqr.io/image/{_config["SePay:BankBin"]}-{_config["SePay:AccountNumber"]}-compact.png" +
                        $"?amount={(long)order.Price!}&addInfo={order.Note}&accountName={Uri.EscapeDataString(_config["SePay:AccountName"]!)}";

            return Ok(new { orderId = order.Id, amount = order.Price, content = order.Note, qrUrl });
        }

        [HttpGet("status/{orderId:int}")]
        [Authorize]
        public async Task<IActionResult> GetStatus(int orderId)
        {
            var userId = _claimService.GetUserClaim().Id;
            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId);
            if (order == null || order.AccountId != userId)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            return Ok(new { orderId = order.Id, status = order.Status.ToString() });
        }

        [HttpPost("sepay-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookPayload p)
        {
            // 1) Xác thực API key
            if (Request.Headers["Authorization"] != $"Apikey {_config["SePay:WebhookApiKey"]}")
                return Unauthorized();

            // 2) Chỉ xử lý tiền vào
            if (!string.Equals(p.TransferType, "in", StringComparison.OrdinalIgnoreCase))
                return Ok(new { success = true });

            // 3) Dedup toàn bảng: nếu ID giao dịch SePay đã được ghi nhận thì bỏ qua
            var sepayId = p.Id.ToString();
            if (await _unitOfWork.Orders.AnyAsync(o => o.SepayTransactionId == sepayId))
                return Ok(new { success = true });

            // 4) Trích mã đối soát — ưu tiên p.Code, fallback sang p.Content
            var orderId = !string.IsNullOrWhiteSpace(p.Code)
                ? ExtractOrderIdFromCode(p.Code)
                : ExtractOrderId(p.Content);
            if (orderId == 0) return Ok(new { success = true });

            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId);

            // 5) Lớp bảo vệ 2: order không tồn tại hoặc đã Paid
            if (order == null || order.Status == OrderStatus.Paid)
                return Ok(new { success = true });

            // 6) Kiểm tra số tiền
            if (p.TransferAmount < order.Price)
                return Ok(new { success = true });

            // 7) Xác nhận thanh toán — toàn bộ ghi nhận trong 1 transaction
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
                order.SepayTransactionId = sepayId;

                var sub = await _unitOfWork.Subscriptions.GetAsync(x => x.Id == order.SubscriptionId);
                if (sub != null)
                {
                    sub.Status = SubscriptionStatus.Active;
                    sub.PaymentStatus = PaymentStatus.Paid;
                    sub.ModifiedDate = DateTime.UtcNow;

                    var subPlan = await _unitOfWork.SubscriptionPlans.GetAsync(sp => sp.Id == sub.PlanId);

                    // StartDate chưa set → set từ lúc thanh toán thành công
                    if (sub.StartDate == default && subPlan != null)
                    {
                        sub.StartDate = DateTime.UtcNow;
                        sub.EndDate = DateTime.UtcNow.AddMonths(subPlan.DurationMonth);
                        sub.NextBillingDate = sub.EndDate;
                    }

                    // Tự động thăng Manager khi mua gói doanh nghiệp (CompanyId != null, MaxSlots > 1)
                    if (sub.CompanyId != null && subPlan != null && subPlan.MaxSlots > 1)
                    {
                        var accountUser = await _unitOfWork.Users.GetAsync(u => u.UserId == sub.AccountId);
                        if (accountUser != null && accountUser.RoleId == 3)
                            accountUser.RoleId = 2;
                    }
                }

                await _unitOfWork.SaveChangeAsync();
            });
            return Ok(new { success = true });
        }

        [HttpPost("create-order-business-upgrade")]
        [Authorize]
        public async Task<IActionResult> CreateOrderBusinessUpgrade([FromBody] CreateOrderIndividualRequest req)
        {
            var planId = req.PlanId;
            var userId = _claimService.GetUserClaim().Id;

            var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == planId && p.IsActive);
            if (plan == null)
                return BadRequest(new { message = "Gói dịch vụ không tồn tại hoặc đã ngừng cung cấp." });
            if (plan.MaxSlots <= 1)
                return BadRequest(new { message = "Dùng luồng mua gói cá nhân cho gói này." });

            var user = await _unitOfWork.Users.GetAsync(u => u.UserId == userId);
            if (user == null) return Unauthorized();

            // Chặn nếu đã có sub active hoặc pending cho công ty này
            var companyId = user.CompanyId;
            if (companyId != null)
            {
                var existingSub = await _unitOfWork.Subscriptions.GetAsync(
                    s => s.CompanyId == companyId &&
                         (s.Status == SubscriptionStatus.PendingPayment || s.Status == SubscriptionStatus.Active));
                if (existingSub != null)
                {
                    if (existingSub.Status == SubscriptionStatus.Active)
                        return BadRequest(new { message = "Công ty bạn đã có gói đang hoạt động." });

                    var existingOrder = await _unitOfWork.Orders.GetAsync(
                        o => o.SubscriptionId == existingSub.Id && o.Status == OrderStatus.Pending);
                    if (existingOrder != null)
                    {
                        var existingQrUrl = $"https://img.vietqr.io/image/{_config["SePay:BankBin"]}-{_config["SePay:AccountNumber"]}-compact.png" +
                                            $"?amount={(long)existingOrder.Price!}&addInfo={existingOrder.Note}&accountName={Uri.EscapeDataString(_config["SePay:AccountName"]!)}";
                        return Ok(new { orderId = existingOrder.Id, amount = existingOrder.Price, content = existingOrder.Note, qrUrl = existingQrUrl });
                    }
                }
            }

            // Tạo công ty mới nếu user chưa có
            if (companyId == null)
            {
                var company = new Company
                {
                    CompanyName = $"Tổ chức của {user.FullName}",
                    Domain = user.Email.Contains('@') ? user.Email.Split('@')[1] : "company.vn",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Companies.AddAsync(company);
                await _unitOfWork.SaveChangeAsync();

                companyId = company.CompanyId;
                user.CompanyId = companyId;
                await _unitOfWork.SaveChangeAsync();
            }

            // Tạo Subscription cho công ty
            var subscription = new Subscription
            {
                AccountId = userId,
                CompanyId = companyId,
                PlanId = planId,
                Price = plan.Price,
                Status = SubscriptionStatus.PendingPayment,
                PaymentStatus = PaymentStatus.Pending,
                UsedSlots = 0,
                ModifiedDate = DateTime.UtcNow
            };
            await _unitOfWork.Subscriptions.AddAsync(subscription);
            await _unitOfWork.SaveChangeAsync();

            var order = new Order
            {
                AccountId = userId,
                SubscriptionId = subscription.Id,
                Price = plan.Price,
                Note = "PENDING",
                Status = OrderStatus.Pending
            };
            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangeAsync();

            order.Note = $"ANTI{order.Id}";
            await _unitOfWork.SaveChangeAsync();

            var qrUrl = $"https://img.vietqr.io/image/{_config["SePay:BankBin"]}-{_config["SePay:AccountNumber"]}-compact.png" +
                        $"?amount={(long)order.Price!}&addInfo={order.Note}&accountName={Uri.EscapeDataString(_config["SePay:AccountName"]!)}";

            return Ok(new { orderId = order.Id, amount = order.Price, content = order.Note, qrUrl });
        }

        [HttpPost("cancel-order/{orderId:int}")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = _claimService.GetUserClaim().Id;

            var order = await _unitOfWork.Orders.GetAsync(o => o.Id == orderId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            // IDOR check
            if (order.AccountId != userId)
                return Forbid();

            if (order.Status != OrderStatus.Pending)
                return BadRequest(new { message = "Chỉ có thể hủy đơn hàng đang chờ thanh toán." });

            order.Status = OrderStatus.Canceled;

            var sub = await _unitOfWork.Subscriptions.GetAsync(s => s.Id == order.SubscriptionId);
            if (sub != null && sub.Status == SubscriptionStatus.PendingPayment)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                sub.PaymentStatus = PaymentStatus.Failed;
            }

            await _unitOfWork.SaveChangeAsync();
            return Ok(new { success = true, message = "Đã hủy đơn hàng." });
        }

        [HttpDelete("cancel-pending")]
        [Authorize]
        public async Task<IActionResult> CancelPending()
        {
            var userId = _claimService.GetUserClaim().Id;

            var sub = await _unitOfWork.Subscriptions.GetAsync(
                s => s.AccountId == userId && s.CompanyId == null && s.Status == SubscriptionStatus.PendingPayment);
            if (sub == null)
                return NotFound(new { message = "Không có đơn hàng chờ thanh toán." });

            var order = await _unitOfWork.Orders.GetAsync(
                o => o.SubscriptionId == sub.Id && o.Status == OrderStatus.Pending);
            if (order != null)
                order.Status = OrderStatus.Canceled;

            sub.Status = SubscriptionStatus.Cancelled;
            sub.PaymentStatus = PaymentStatus.Failed;

            await _unitOfWork.SaveChangeAsync();
            return Ok(new { success = true });
        }

        // Fallback: tách orderId từ nội dung chuyển khoản đầy đủ (vd "thanh toan ANTI42 goi dich vu")
        private static int ExtractOrderId(string content)
        {
            var match = System.Text.RegularExpressions.Regex.Match(content ?? "", @"ANTI(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        // Ưu tiên: SePay đã tách sẵn code (vd "ANTI42"), chỉ cần strip prefix
        private static int ExtractOrderIdFromCode(string code)
        {
            if (code.StartsWith("ANTI", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(code[4..], out var id))
                return id;
            return 0;
        }
    }
}
