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
        public Task<ResourceResult> GetDocumentationPage(
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
                    return Task.FromResult(ResourceResult.Error(403, "Forbidden path."));
                }

                if (!File.Exists(fullPath))
                {
                    return Task.FromResult(ResourceResult.Error(404, "File Not Found"));
                }

                var stream = File.OpenRead(fullPath);
                // return Task.FromResult(ResourceResult.Success(new ResourceContent(stream, typeof(UnityDocumentationData))));
                return Task.FromResult(ResourceResult.Success(typeof(string), "SUCCESFUL RESOURCE!"));
            }
            catch (DirectoryNotFoundException ex)
            {
                 _logger.LogError(ex, "Documentation directory not found.");
                return Task.FromResult(ResourceResult.Error(500, $"Configuration error: {ex.Message}"));
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read documentation file.");
                return Task.FromResult(ResourceResult.Error(500, $"File access error: {ex.Message}"));
            }
        }
    }
}
