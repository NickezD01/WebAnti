using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Payment;
using AntiPhisher.Application.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace AntiPhisher.Application.Services
{
    public class MoMoService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;

        public MoMoService(IConfiguration config, IUnitOfWork unitOfWork)
        {
            _config = config;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse> CreatePayment(PaymentRequest model, HttpContext context)
        {
            var order = await _unitOfWork.Orders.GetAsync(x => x.Id == model.OrderId);
            if (order == null) return new ApiResponse().SetNotFound("Đơn hàng không tồn tại.");

            var amount = (long)(order.Price ?? 0);
            var requestId = Guid.NewGuid().ToString();
            var orderId = model.OrderId.ToString();
            var orderInfo = "Thanh toan dich vu AntiPhisher";

            // Lấy từ Config
            var partnerCode = _config["MoMo:PartnerCode"];
            var accessKey = _config["MoMo:AccessKey"];
            var secretKey = _config["MoMo:SecretKey"];
            var ipnUrl = _config["MoMo:IpnUrl"];
            var redirectUrl = _config["MoMo:RedirectUrl"];

            // THỨ TỰ BẮT BUỘC: accessKey, amount, extraData, ipnUrl, orderId, orderInfo, partnerCode, redirectUrl, requestId, requestType
            var rawData = $"accessKey={accessKey}&amount={amount}&extraData=&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType=captureWallet";
            var signature = ComputeHmacSha256(rawData, secretKey);

            var requestObj = new
            {
                partnerCode,
                partnerName = "AntiPhisher",
                storeId = "AntiPhisherStore",
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                requestType = "captureWallet",
                extraData = "",
                signature
            };

            using var client = new HttpClient();
            var jsonPayload = JsonConvert.SerializeObject(requestObj);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://payment.momo.vn/v2/gateway/api/create", content);
            var result = await response.Content.ReadAsStringAsync();
            var payData = JsonConvert.DeserializeObject<dynamic>(result);

            if (payData?.resultCode != 0)
                return new ApiResponse().SetBadRequest($"MoMo API Error: {payData?.message}");

            return new ApiResponse().SetOk(payData.payUrl.ToString());
        }

        public async Task<ApiResponse> ProcessCallback(dynamic data)
        {
            // Xử lý IPN từ MoMo
            return await Task.FromResult(new ApiResponse().SetOk("Success"));
        }

        private string ComputeHmacSha256(string message, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}