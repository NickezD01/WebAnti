using AntiPhisher.Application.Common;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Payment;
using AntiPhisher.Application.Response;
using AntiPhisher.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimService _claimService;

        public VnPayService(IConfiguration configuration, IUnitOfWork unitOfWork, IClaimService claimService)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _claimService = claimService;
        }

        public async Task<ApiResponse> CreatePaymentUrl(PaymentRequest request, HttpContext context)
        {
            ApiResponse response = new ApiResponse();
            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById(_configuration["TimeZoneId"] ?? "SE Asia Standard Time");
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            
            // Sử dụng mã tham chiếu giao dịch độc nhất bằng cách kết hợp Ticks và OrderId tránh trùng lặp phiên VNPAY
            var tick = $"{DateTime.UtcNow.Ticks}_{request.OrderId}";
            var pay = new VnPayLibrary();

            var model = new PaymentMethod();
            var claim = _claimService.GetUserClaim();
            
            // 1. Kiểm tra tài khoản Quản lý doanh nghiệp thực hiện thanh toán
            var user = await _unitOfWork.Users.GetAsync(x => x.UserId == claim.Id);
            if (user == null)
                return response.SetNotFound("Không tìm thấy thông tin người dùng thực hiện giao dịch.");
            
            if (user.CompanyId == null)
                return response.SetBadRequest("Tài khoản này chưa được liên kết với công ty. Không thể thực hiện thanh toán gói tổ chức.");

            // 2. Kiểm tra thông tin Đơn hàng (Order)
            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == request.OrderId);
            if (order is null)
                return response.SetNotFound("Hóa đơn/Đơn hàng không tồn tại trên hệ thống.");

            // 3. Kiểm tra xem hóa đơn này đã từng được xử lý thanh toán chưa
            var existingPayment = await _unitOfWork.Payments.GetAsync(x => x.OrderId == request.OrderId);
            if (existingPayment != null)
            {
                if (existingPayment.StatusPayment == StatusPayment.Pending || existingPayment.StatusPayment == StatusPayment.Paid)
                {
                    return response.SetBadRequest("Hóa đơn này đã được thanh toán thành công hoặc đang trong quá trình xử lý.");
                }
            }

            model.Amount = order.Price ?? 0;
            model.Name = "AntiPhisher Enterprise";
            model.OrderDescription = $"Thanh toan goi dich vu AntiPhisher cho Company ID: {user.CompanyId}";
            model.OrderType = "VnPay";

            // THAY ĐỔI QUAN TRỌNG: Gắn kèm CompanyId vào CallBack URL để định danh luồng tiền thuộc về tổ chức nào
            var urlCallBack = $"{_configuration["PaymentCallBack:ReturnUrl"]}?userId={claim.Id}&companyId={user.CompanyId}&amount={model.Amount}&orderId={order.Id}";

            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString()); // Ép kiểu long phòng trường hợp giá trị gói lớn
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"] ?? "VND");
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"] ?? "vn");
            pay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
            pay.AddRequestData("vnp_OrderType", model.OrderType);
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl = pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);

            return response.SetOk(paymentUrl);
        }

        public async Task<ApiResponse> PaymentExecute(IQueryCollection collections)
        {
            try
            {
                ApiResponse apiResponse = new ApiResponse();
                var pay = new VnPayLibrary();
                var response = pay.GetFullResponseData(collections, _configuration["Vnpay:HashSecret"]);

                // Kiểm tra mã phản hồi hệ thống từ VNPAY
                if (collections.TryGetValue("vnp_ResponseCode", out var responseCode))
                {
                    if (responseCode != "00")
                    {
                        return apiResponse.SetBadRequest("Giao dịch thanh toán VNPAY thất bại hoặc đã bị hủy bỏ từ người dùng.");
                    }
                }
                else
                {
                    return apiResponse.SetBadRequest("Không tìm thấy mã phản hồi (Response Code) từ VNPAY.");
                }

                // Trích xuất các tham số định danh từ CallBack URL
                if (collections.TryGetValue("userId", out var userIdValue) && int.TryParse(userIdValue, out int userId) &&
                    collections.TryGetValue("orderId", out var orderIdValue) && int.TryParse(orderIdValue, out int orderId) &&
                    collections.TryGetValue("companyId", out var companyIdValue) && int.TryParse(companyIdValue, out int companyId)) // Nhận thêm CompanyId
                {
                    if (response.Success)
                    {
                        var user = await _unitOfWork.Users.GetAsync(x => x.UserId == userId);
                        var order = await _unitOfWork.Orders.GetAsync(x => x.Id == orderId);

                        if (user != null && order is not null)
                        {
                            if (collections.TryGetValue("amount", out var amountValue) && decimal.TryParse(amountValue, out decimal amount))
                            {
                                Payment payment = new Payment
                                {
                                    AccountId = userId,
                                    OrderId = orderId,
                                    Amount = amount,
                                    CreatedDate = DateTime.UtcNow,
                                    PaymentMethod = PaymentMethodEnum.VNPay
                                };

                                // Kiểm tra tính toàn vẹn dữ liệu: Số tiền thanh toán thực tế phải khớp với giá trị hóa đơn
                                if (amount == order.Price)
                                {
                                    payment.StatusPayment = StatusPayment.Paid;

                                    // Lấy thông tin Subscription liên kết qua OrderId
                                    var sub = await _unitOfWork.Subscriptions.GetAsync(s => s.Id == order.SubscriptionId);
                                    if (sub != null)
                                    {
                                        sub.PaymentStatus = PaymentStatus.Paid;
                                        sub.Status = SubscriptionStatus.Active; // Kích hoạt gói dịch vụ cho Công ty sử dụng
                                        sub.ModifiedDate = DateTime.UtcNow;
                                    }
                                }
                                else
                                {
                                    payment.StatusPayment = StatusPayment.Failed;
                                    await _unitOfWork.Payments.AddAsync(payment);
                                    await _unitOfWork.SaveChangeAsync();

                                    var sub = await _unitOfWork.Subscriptions.GetAsync(s => s.Id == order.SubscriptionId);
                                    if (sub != null)
                                    {
                                        sub.PaymentStatus = PaymentStatus.Failed;
                                        sub.ModifiedDate = DateTime.UtcNow;
                                    }
                                    return apiResponse.SetBadRequest("Số tiền thanh toán thực tế không trùng khớp với giá trị trên hóa đơn.");
                                }

                                await _unitOfWork.Payments.AddAsync(payment);
                            }
                            else
                            {
                                return apiResponse.SetBadRequest("Lỗi định dạng dữ liệu số tiền giao dịch (Amount Parse Error).");
                            }

                            await _unitOfWork.SaveChangeAsync();
                            
                            // Trả về đường dẫn Redirect cho Client-side (FE) hiển thị màn hình thông báo thành công
                            var redirectUrl = _configuration["PaymentCallBack:FrontendSuccessUrl"] ?? "http://localhost:5173/paymentsuccess";
                            return apiResponse.SetOk(redirectUrl);
                        }
                        else
                        {
                            return apiResponse.SetBadRequest("Thông tin tài khoản hoặc đơn hàng không tồn tại để đối soát.");
                        }
                    }
                    else
                    {
                        return apiResponse.SetBadRequest("Chữ ký phản hồi (IPN Signature) từ VNPAY không hợp lệ.");
                    }
                }
                else
                {
                    return apiResponse.SetBadRequest("Cấu trúc chuỗi CallBack URL bị thiếu thông tin định danh (UserId, CompanyId hoặc OrderId).");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Hệ thống gặp lỗi khi xử lý IPN VNPAY: {ex.Message}");
            }
        }
    }
}