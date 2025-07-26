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
        private readonly object _cacheLock = new();
        private string _cachedEditorPath;

        public UnityInstallationService(ConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public string? ResolveUnityEditorPath(string projectPath)
        {
            lock (_cacheLock)
            {
                return _cachedEditorPath ??= CalculateEditorPath(projectPath);
            }
        }

        private string? CalculateEditorPath(string projectPath)
        {
            // Strategy 1: Explicit configuration
            var installRoot = _configurationService.UnitySettings.InstallRoot;
            var projectVersion = GetUnityVersionFromProject(projectPath);
            var directPath = _configurationService.UnitySettings.EditorPath;

            if (!string.IsNullOrEmpty(installRoot) && !string.IsNullOrEmpty(projectVersion))
            {
                var explicitPath = Path.Combine(installRoot, projectVersion);
                if (Directory.Exists(explicitPath))
                {
                    Console.Error.WriteLine($"[INFO] Using config-specified installation: {explicitPath}");
                    return explicitPath;
                }
                Console.Error.WriteLine($"[WARN] Configured path not found: {explicitPath}");
            }

            // Strategy 2: Direct path override
            if (!string.IsNullOrEmpty(directPath))
            {
                if (Directory.Exists(directPath))
                {
                    Console.Error.WriteLine($"[INFO] Using direct path override: {directPath}");
                    return directPath;
                }
                Console.Error.WriteLine($"[WARN] Direct editor path not found: {directPath}");
            }

            // Strategy 3: Automatic resolution
            if (!string.IsNullOrEmpty(projectVersion))
            {
                var commonPath = TryFindUnityInCommonLocations();
                if (commonPath != null)
                {
                    Console.Error.WriteLine($"[INFO] Using automatically detected path: {commonPath}");
                    return commonPath;
                }
            }

            // Final fallback
            var errMsg = "Unable to resolve Unity path. Verify configuration in appsettings.json";
            Console.Error.WriteLine($"[ERROR] {errMsg}");
            throw new InvalidOperationException(errMsg);
        }

        public string GetDocumentationPath(string projectPath, string docDomain = "ScriptReference")
        {
            var editorPath = ResolveUnityEditorPath(projectPath);

            var potentialDocumentationPaths = new[]
            {
                Path.Combine(editorPath, "Data", "Documentation"), // Windows/Linux
                Path.Combine(editorPath, "Documentation"), // macOS inside Contents
                Path.Combine(editorPath, "Contents", "Documentation"), // macOS for Unity.app path
                Path.Combine(editorPath, "Unity.app", "Contents", "Documentation") // macOS Hub install
            };

            foreach (var docPath in potentialDocumentationPaths.Select(p => Path.GetFullPath(p)))
            {
                // Check for a known subdirectory to validate it's the correct documentation folder.
                if (Directory.Exists(docPath))
                {
                    string retVal = Path.Combine(docPath, "en", docDomain);
                    Console.Error.WriteLine($"[INFO] Found documentation at: {retVal}");
                    return retVal;
                }
            }

            var errorMsg = $"Unable to find Unity Documentation folder for editor path: {editorPath}";
            Console.Error.WriteLine($"[ERROR] {errorMsg}");
            throw new DirectoryNotFoundException(errorMsg);
        }

        private string? GetUnityVersionFromProject(string projectPath)
        {
            try
            {
                var projectVersionPath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
                if (!File.Exists(projectVersionPath))
                {
                    Console.Error.WriteLine($"[ERROR] ProjectVersion.txt not found at {projectVersionPath}");
                    return null;
                }

                var content = File.ReadAllText(projectVersionPath);
                var versionLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("m_EditorVersion:"));
                    
                if (versionLine == null)
                {
                    Console.Error.WriteLine($"[ERROR] m_EditorVersion not found in ProjectVersion.txt at {projectVersionPath}");
                    return null;
                }

                var version = versionLine.Split(':', 2)[1].Trim();
                if (string.IsNullOrEmpty(version))
                {
                    Console.Error.WriteLine($"[ERROR] Empty version found in ProjectVersion.txt from line: {versionLine}");
                    return null;
                }

                Console.Error.WriteLine($"[INFO] Unity version detected from project {projectPath}: {version}");
                return version;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to read Unity version from project: {ex.Message}");
                return null;
            }
        }

        private string? TryFindUnityInCommonLocations()
        {
            var searchPaths = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                searchPaths.AddRange(new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Unity"),
                    @"C:\Program Files\Unity"
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                searchPaths.AddRange(new[]
                {
                    "/Applications/Unity",
                    "/Applications/Unity/Hub/Editor",
                    "~/Applications/Unity"
                });
            }
            else // Linux
            {
                searchPaths.AddRange(new[]
                {
                    "/opt/Unity",
                    "/usr/share/unity",
                    "~/Unity"
                });
            }

            foreach (var path in searchPaths.Select(Path.GetFullPath))
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }
    }
}
