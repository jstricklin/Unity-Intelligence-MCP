using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityIntelligenceMCP.Configuration;

namespace UnityIntelligenceMCP.Core.IO
{
    public class UnityInstallationService
    {
        private readonly ConfigurationService _configurationService;
        private readonly ILogger<UnityInstallationService> _logger;
        private string? _editorPath;

        public UnityInstallationService(
            ConfigurationService configurationService,
            ILogger<UnityInstallationService> logger
            )
        {
            _configurationService = configurationService;
            _logger = logger;
            _editorPath = _configurationService.UnitySettings.EDITOR_PATH;
        }

        public string? GetEditorPath()
        {
            return _editorPath;
        }

        // private string? CalculateEditorPath(string projectPath)
        // {
        //     // Strategy 1: Explicit configuration
        //     // var installRoot = _configurationService.UnitySettings.INSTALL_ROOT;
        //     // var projectVersion = GetProjectVersion(projectPath);

        //     // if (!string.IsNullOrEmpty(installRoot) && !string.IsNullOrEmpty(projectVersion))
        //     // {
        //     //     var explicitPath = Path.Combine(installRoot, projectVersion);
        //     //     if (Directory.Exists(explicitPath))
        //     //     {
        //     //         _logger.LogInformation($"[INFO] Using config-specified installation: {explicitPath}");
        //     //         return explicitPath;
        //     //     }
        //     //     _logger.LogWarning($"[WARN] Configured path not found: {explicitPath}");
        //     // }

        //     // Strategy 2: Direct path override
        //     if (!string.IsNullOrEmpty(editorPath))
        //     {
        //         if (Directory.Exists(editorPath))
        //         {
        //             _logger.LogInformation($"[INFO] Using direct path override: {editorPath}");
        //             return editorPath;
        //         }
        //         _logger.LogWarning($"[WARN] Direct editor path not found: {editorPath}");
        //     }

        //     // Final fallback
        //     var errMsg = "Unable to resolve Unity path. Verify configuration in appsettings.json";
        //     _logger.LogError($"[ERROR] {errMsg}");
        //     throw new InvalidOperationException(errMsg);
        // }

        public string GetDocumentationPath(string docDomain = "ScriptReference")
        {
            string errorMsg;

            if (string.IsNullOrEmpty(_editorPath))
            {
                errorMsg = "Could not resolve Unity Editor path. Cannot find documentation.";
                _logger.LogError($"[ERROR] {errorMsg}");
                throw new DirectoryNotFoundException(errorMsg);
            }

            var potentialDocumentationPaths = new[]
            {
                Path.Combine(_editorPath, "Data", "Documentation"), // Windows/Linux
                Path.Combine(_editorPath, "Documentation"), // macOS inside Contents
                Path.Combine(_editorPath, "Contents", "Documentation"), // macOS for Unity.app path
                Path.Combine(_editorPath, "Unity.app", "Contents", "Documentation") // macOS Hub install
            };

            foreach (var docPath in potentialDocumentationPaths.Select(p => Path.GetFullPath(p)))
            {
                // Check for a known subdirectory to validate it's the correct documentation folder.
                if (Directory.Exists(docPath))
                {
                    string retVal = Path.Combine(docPath, "en", docDomain);
                    _logger.LogInformation($"[INFO] Found documentation at: {retVal}");
                    return retVal;
                }
            }

            errorMsg = $"Unable to find Unity Documentation folder for editor path: {_editorPath}";
            _logger.LogError($"[ERROR] {errorMsg}");
            throw new DirectoryNotFoundException(errorMsg);
        }

        public string? GetEditorVersion()
        {
            try
            {
                string? version = _editorPath?.Split('/').Last();
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Failed to read Unity version from Editor Path ({_editorPath}): {ex.Message}");
                return null;
            }
        }
    }
}
