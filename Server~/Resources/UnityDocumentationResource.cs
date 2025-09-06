using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.IO;
using ModelContextProtocol.Protocol;
using UnityIntelligenceMCP.Models.Documentation;
using System.Text.Json;
using UnityIntelligenceMCP.Utilities;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class UnityDocumentationResource
    {
        private readonly UnityInstallationService _installationService;
        private readonly ILogger<UnityDocumentationResource> _logger;
        private readonly ConfigurationService _configurationService;
        private readonly IMCPUsageLogger _usageLogger;

        public UnityDocumentationResource(
            UnityInstallationService installationService, 
            ILogger<UnityDocumentationResource> logger, 
            ConfigurationService configurationService, 
            IMCPUsageLogger usageLogger)
        {
            _installationService = installationService;
            _logger = logger;
            _configurationService = configurationService;
            _usageLogger = usageLogger;
        }

        [McpServerResource(Name = "get_script_reference_page")]
        public async Task<string> GetScriptReferencePage(
            [Description("The relative path to the HTML documentation file, e.g., 'MonoBehaviour.html'")] 
            string relativePath
            )
        {
            // TextResourceContents? result = null;
            try
            {
                string projectPath = _configurationService.GetConfiguredProjectPath();
                string docRoot = _installationService.GetDocumentationPath("ScriptReference");
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
                UnityDocumentationData docData = await Task.FromResult(Task.Run(() => parser.Parse(fullPath))).Result;

                var jsonResponse = JsonSerializer.Serialize(new { success = true, data = docData });
                return ResourceParser.ParseTextResourceContents(jsonResponse);
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
