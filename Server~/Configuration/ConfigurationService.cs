using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Configuration
{
    public class ConfigurationService
    {
        public UnityAnalysisSettings UnitySettings { get; }
        ILogger<ConfigurationService> _logger;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            UnitySettings = configuration.Get<UnityAnalysisSettings>() ?? new UnityAnalysisSettings();
            _logger.LogInformation($"Project Path: {UnitySettings.PROJECT_PATH}");
        }

        public string GetConfiguredProjectPath()
        {
            var projectPath = UnitySettings.PROJECT_PATH;
            if (string.IsNullOrEmpty(projectPath))
            {
                _logger.LogWarning("Unity project path is not configured. Project Analysis tools disabled. (PROJECT_PATH).");
            }
            return projectPath ?? "";
        }
    }
}
