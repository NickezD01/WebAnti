using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain.Models
{
    public class Payment : Base
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public StatusPayment StatusPayment { get; set; }
        public decimal Amount { get; set; }
        //public int OrderId { get; set; }
        public int? TransactionHId { get; set; }
        public string? Note { get; set; }
        //public Order? Order { get; set; }
        public User? Account { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; } = PaymentMethodEnum.VNPay;
        public TransactionHistory? TransactionHistory { get; set; }

        //public int Id { get; set; }


        public int OrderId { get; set; }
        public Order? Order { get; set; }
    }
    public enum StatusPayment
    {
        NotCompleted,
        Success,
        Failed,
        Pending,
        Paid
    }
    public enum PaymentMethodEnum
    {
        VNPay,
        Momo,
        CreditCard
    }
}
