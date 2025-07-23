using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class UnityDocumentationResource
    {
        private readonly UnityInstallationService _installationService;
        private readonly ILogger<UnityDocumentationResource> _logger;

        public UnityDocumentationResource(UnityInstallationService installationService, ILogger<UnityDocumentationResource> logger)
        {
            _installationService = installationService;
            _logger = logger;
        }

        [McpServerResource(Name = "unity_docs")]
        public Task<ResourceResult> GetDocumentationPage(
            [Description("The project path used to resolve the correct Unity version.")] string projectPath,
            [Description("The relative path to the HTML documentation file, e.g., 'ScriptReference/MonoBehaviour.html'")] string relativePath)
        {
            try
            {
                var docRoot = _installationService.GetDocumentationPath(projectPath);
                var fullPath = Path.GetFullPath(Path.Combine(docRoot, relativePath));

                // Security check to prevent path traversal attacks
                if (!fullPath.StartsWith(Path.GetFullPath(docRoot)))
                {
                    return Task.FromResult(ResourceResult.Error(403, "Forbidden path."));
                }

                if (!File.Exists(fullPath))
                {
                    return Task.FromResult(ResourceResult.NotFound());
                }

                var stream = File.OpenRead(fullPath);
                return Task.FromResult(ResourceResult.Success(new ResourceContent(stream, "text/html")));
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
