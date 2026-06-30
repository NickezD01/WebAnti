using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.ScenarioRequest
{
    public class UpdateScenarioRequest
    {
        public string Subject { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string EmailBodyHtml { get; set; } = null!;
        public bool IsPhishing { get; set; }
        public string? PhishingIndicators { get; set; }
        public int DifficultyId { get; set; }
    }
}
