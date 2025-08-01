using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;
using System.Text.Json;
using UnityIntelligenceMCP.Utilities;
using UnityIntelligenceMCP.Models.Documentation;

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

        [McpServerResource(Name = "get_script_reference_page")]
        public Task<TextResourceContents> GetScriptReferencePage(
            [Description("The relative path to the HTML documentation file, e.g., 'MonoBehaviour.html'")] 
            string relativePath
            )
        {
            try
            {
                string projectPath = _configurationService.GetConfiguredProjectPath();
                string docRoot = _installationService.GetDocumentationPath(projectPath, "ScriptReference");
                string fullPath = Path.GetFullPath(Path.Combine(docRoot, relativePath));

                // Security check to prevent path traversal attacks
                if (!fullPath.StartsWith(Path.GetFullPath(docRoot)))
                {
                    throw new UnauthorizedAccessException("Forbidden path.");
                }

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("File Not Found", fullPath);
                }

                var parser = new UnityDocumentationParser();
                UnityDocumentationData docData = parser.Parse(fullPath);

                return Task.FromResult(new TextResourceContents { 
                    Text = JsonSerializer.Serialize(parser.Parse(fullPath)),
                    MimeType = "text/json" 
                    });
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
