using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPhisher.Infrastructure.Data
{
    public class UserCampaignResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int CampaignId { get; set; }

        [Required]
        [StringLength(100)]
        public string UserAction { get; set; }

        // BẮT BUỘC PHẢI CÓ 4 DÒNG NÀY ĐỂ HẾT LỖI GẠCH ĐỎ TRÊN CONTROLLER:
        public bool IsCorrect { get; set; }
        public string DetectedFlaw { get; set; }
        public string Reason { get; set; }
        public string Advice { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        // Kết nối đến bảng Campaign có sẵn của hệ thống
        [ForeignKey("CampaignId")]
        public virtual AntiPhisher.Domain.Models.Campaign Campaign { get; set; }
    }
}