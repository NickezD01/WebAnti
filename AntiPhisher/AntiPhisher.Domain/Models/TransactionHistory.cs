using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain.Models
{
    public class TransactionHistory : Base
    {
        public int Id { get; set; }
        public int Amount { get; set; }
        public Status Status { get; set; }
        public List<Payment>? Payments { get; set; }
    }
    public enum Status
    {
        Success,
        Failed,
        Pending
    }
}
