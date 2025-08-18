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

namespace UnityIntelligenceMCP.Resources
{
    [McpServerResourceType]
    public class UnityDocumentationResource
    {
        private readonly UnityInstallationService _installationService;
        private readonly ILogger<UnityDocumentationResource> _logger;
        private readonly ConfigurationService _configurationService;
        private readonly IToolUsageLogger _usageLogger;

        public UnityDocumentationResource(UnityInstallationService installationService, ILogger<UnityDocumentationResource> logger, ConfigurationService configurationService, IToolUsageLogger usageLogger)
        {
            _installationService = installationService;
            _logger = logger;
            _configurationService = configurationService;
            _usageLogger = usageLogger;
        }

        [McpServerResource(Name = "get_script_reference_page")]
        public async Task<TextResourceContents> GetScriptReferencePage(
            [Description("The relative path to the HTML documentation file, e.g., 'MonoBehaviour.html'")] 
            string relativePath
            )
        {
            var stopwatch = Stopwatch.StartNew();
            bool wasSuccessful = false;
            TextResourceContents result = null;
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

                result = new TextResourceContents
                {
                    Text = JsonSerializer.Serialize(docData),
                    MimeType = "text/json"
                };
                wasSuccessful = true;
                return result;
            }
            catch (DirectoryNotFoundException ex)
            {
                wasSuccessful = false;
                 _logger.LogError(ex, "Documentation directory not found.");
                throw new InvalidOperationException($"Configuration error: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                wasSuccessful = false;
                _logger.LogError(ex, "Failed to read documentation file.");
                throw new InvalidOperationException($"File access error: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                var process = Process.GetCurrentProcess();
                process.Refresh();
                var peakMemoryMb = process.PeakWorkingSet64 / (1024 * 1024);

                var parameters = new { relativePath };
                var resultSummary = new { MimeType = result?.MimeType, TextLength = result?.Text.Length ?? 0 };

                await _usageLogger.LogAsync(new ToolUsageLog
                {
                    ToolName = "get_script_reference_page",
                    ParametersJson = JsonSerializer.Serialize(parameters),
                    ResultSummaryJson = JsonSerializer.Serialize(resultSummary),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    WasSuccessful = wasSuccessful,
                    PeakProcessMemoryMb = peakMemoryMb
                });
            }
        }
    }
}
