using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AntiPhisher.Application.DataSeeding
{
    public class ScenarioJsonBatch
    {
        [JsonPropertyName("batch_id")]
        public string BatchId { get; set; }

        [JsonPropertyName("batch_name")]
        public string BatchName { get; set; }

        [JsonPropertyName("phase")]
        public int Phase { get; set; }

        [JsonPropertyName("lesson")]
        public string Lesson { get; set; }

        [JsonPropertyName("simulations")]
        public List<SimulationItem> Simulations { get; set; }
    }

    public class SimulationItem
    {
        [JsonPropertyName("simulation_id")]
        public string SimulationId { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } // easy, medium, hard

        [JsonPropertyName("email_type")]
        public string EmailType { get; set; } // malicious, legitimate

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("email")]
        public EmailContent Email { get; set; }
    }

    public class EmailContent
    {
        [JsonPropertyName("sender_name")]
        public string SenderName { get; set; }

        [JsonPropertyName("sender_email")]
        public string SenderEmail { get; set; }

        [JsonPropertyName("recipient_email")]
        public string RecipientEmail { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}
