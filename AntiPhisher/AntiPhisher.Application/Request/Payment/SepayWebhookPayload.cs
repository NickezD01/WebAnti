using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.Payment
{
    public class SepayWebhookPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; } // ID giao dịch duy nhất của SePay (dùng để chống trùng lặp)

        [JsonPropertyName("gateway")]
        public string Gateway { get; set; } = string.Empty; // Ngân hàng nhận

        [JsonPropertyName("transactionDate")]
        public string TransactionDate { get; set; } = string.Empty;

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty; // Số tài khoản ngân hàng của bạn

        [JsonPropertyName("transferType")]
        public string TransferType { get; set; } = string.Empty; // "in" (tiền vào) hoặc "out" (tiền ra)

        [JsonPropertyName("transferAmount")]
        public decimal TransferAmount { get; set; } // Số tiền thực tế nhận được

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty; // Nội dung chuyển khoản

        [JsonPropertyName("code")]
        public string? Code { get; set; } // Mã đối soát SePay tự tách (nếu cấu hình thành công)
    }
}
