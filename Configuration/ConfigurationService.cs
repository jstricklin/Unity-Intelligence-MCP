using Microsoft.Extensions.Configuration;
using System.IO;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Configuration
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
