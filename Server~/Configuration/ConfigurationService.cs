using Microsoft.Extensions.Configuration;
using System;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Configuration
{
    public class ConfigurationService
    {
        public UnityAnalysisSettings UnitySettings { get; }

        public ConfigurationService()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var port = Environment.GetEnvironmentVariable("MCP_SERVER_PORT") ?? "5000";
            var installRoot = Environment.GetEnvironmentVariable("INSTALL_ROOT");
            var projectPath = Environment.GetEnvironmentVariable("PROJECT_PATH");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .Build();

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
