using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.AttemptRespond
{
    public class AttemptResultResponse
    {
        public int AttemptId { get; set; }
        public bool IsCorrect { get; set; }
        public int ScoreEarned { get; set; }
        public string FeedbackText { get; set; } = null!;
        public string IndicatorsExplained { get; set; } = null!;
        public string ImprovementTips { get; set; } = null!;
        public string AIModel { get; set; } = null!;
    }
}
