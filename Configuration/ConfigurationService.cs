using Microsoft.Extensions.Configuration;
using System.IO;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Configuration
{
    public static class ConfigurationService
    {
        public static UnityAnalysisSettings UnitySettings { get; } = LoadSettings();

        private static UnityAnalysisSettings LoadSettings()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build()
                .GetSection("UnityAnalysisSettings")
                .Get<UnityAnalysisSettings>() ?? new UnityAnalysisSettings();
        }
    }
}
