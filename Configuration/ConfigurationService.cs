using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Configuration
{
    public static class ConfigurationService
    {
        public static UnityAnalysisSettings UnitySettings { get; }

        static ConfigurationService()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .Build();

            UnitySettings = configuration.GetSection("UnityAnalysisSettings").Get<UnityAnalysisSettings>() ?? new UnityAnalysisSettings();
        }
    }
}
