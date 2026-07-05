using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain
{
    public class AppSetting
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public Logging Logging { get; set; }
        public string AllowedHosts { get; set; }
        public SecretToken SecretToken { get; set; }

        // Hỗ trợ cả 2 cách cấu hình:
        // 1. JSON: "Frontend": { "BaseUrl": "..." } → bind qua Frontend.BaseUrl
        // 2. Env var flat: FrontendUrl=...
        public FrontendConfig Frontend { get; set; } = new();
        public string FrontendUrl
        {
            get => Frontend?.BaseUrl ?? "http://localhost:5173";
            set { Frontend ??= new(); Frontend.BaseUrl = value; }
        }
    }

    public class FrontendConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:5173";
    }
    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; }
        public string LocalDockerConnection { get; set; }
    }

    public class Logging
    {
        public LogLevel LogLevel { get; set; }
    }

    public class LogLevel
    {
        public string Default { get; set; }
        public string MicrosoftAspNetCore { get; set; }
    }

    public class SecretToken
    {
        public string Value { get; set; }

        public string Issuer { get; set; }

        public string Audience { get; set; }

        public int DurationInMinutes { get; set; }
    }
}

