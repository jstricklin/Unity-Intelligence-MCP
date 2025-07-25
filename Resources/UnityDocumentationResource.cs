using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class UnityDocumentationResource
    {
        private readonly UnityInstallationService _installationService;
        private readonly ILogger<UnityDocumentationResource> _logger;
        private readonly ConfigurationService _configurationService;

        public UnityDocumentationResource(UnityInstallationService installationService, ILogger<UnityDocumentationResource> logger, ConfigurationService configurationService)
        {
            _installationService = installationService;
            _logger = logger;
            _configurationService = configurationService;
        }

        [McpServerResource(Name = "get_unity_documentation_page")]
        public Task<ResourceContent> GetDocumentationPage(
            [Description("The relative path to the HTML documentation file, e.g., 'ScriptReference/MonoBehaviour.html'")] string relativePath)
        {
            try
            {
                string projectPath = _configurationService.GetConfiguredProjectPath();
                var docRoot = _installationService.GetDocumentationPath(projectPath);
                var fullPath = Path.GetFullPath(Path.Combine(docRoot, relativePath));

                // Security check to prevent path traversal attacks
                if (!fullPath.StartsWith(Path.GetFullPath(docRoot)))
                {
                    throw new UnauthorizedAccessException("Forbidden path.");
                }

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("File Not Found", fullPath);
                }

                var stream = File.OpenRead(fullPath);
                return Task.FromResult(new ResourceContent(stream, typeof(UnityDocumentationData)));
            }
            catch (DirectoryNotFoundException ex)
            {
                 _logger.LogError(ex, "Documentation directory not found.");
                throw new InvalidOperationException($"Configuration error: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read documentation file.");
                throw new InvalidOperationException($"File access error: {ex.Message}", ex);
            }
        }
    }
}
