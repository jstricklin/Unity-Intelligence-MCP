using Microsoft.Extensions.Configuration;
using System;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Configuration
{
    public class ConfigurationService
    {
        public UnityAnalysisSettings UnitySettings { get; }

        public ConfigurationService(IConfiguration configuration)
        {
            UnitySettings = configuration.GetSection("UnityAnalysisSettings").Get<UnityAnalysisSettings>() ?? new UnityAnalysisSettings();
            Console.Error.WriteLine($"[Settings check] {UnitySettings.ProjectPath}");
        }

        public string GetConfiguredProjectPath()
        {
            var projectPath = UnitySettings.ProjectPath;
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new InvalidOperationException("Unity project path is not configured in appsettings.json (UnityAnalysisSettings:ProjectPath).");
            }
            return projectPath;
        }
    }
}
