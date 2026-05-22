using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.AttemptRequest
{
    public class SubmitAttemptRequest
    {
        public int ScenarioId { get; set; }
        public int? CampaignId { get; set; }
        public string UserAnswer { get; set; } = null!; // "Safe" hoặc "Phishing"
        public int TimeTakenSeconds { get; set; }
    }
}
