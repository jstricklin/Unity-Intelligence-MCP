using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Configuration
{
    public class ConfigurationService
    {
        public UnityAnalysisSettings UnitySettings { get; }

        public ConfigurationService()
        {
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            UnitySettings = configuration.Get<UnityAnalysisSettings>() ?? new UnityAnalysisSettings();
            Console.Error.WriteLine($"[Settings check] Project Path: {UnitySettings.PROJECT_PATH}\nPort: {UnitySettings.MCP_SERVER_PORT}");
        }

        public string GetConfiguredProjectPath()
        {
            var projectPath = UnitySettings.PROJECT_PATH;
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new InvalidOperationException("Unity project path is not configured. (PROJECT_PATH).");
            }
            return projectPath;
        }
    }
}
