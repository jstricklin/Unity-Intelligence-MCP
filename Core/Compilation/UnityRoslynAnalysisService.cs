using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityCodeIntelligence.Configuration;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Core.RoslynServices
{
    public class UnityRoslynAnalysisService
    {
        // Updated reference handling with caching
        private static readonly ConcurrentDictionary<string, MetadataReference> _referenceCache = new();

        public async Task<Compilation> CreateUnityCompilationAsync(string projectPath, SearchScope searchScope = SearchScope.AssetsAndPackages, CancellationToken cancellationToken = default)
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            // Try to resolve Unity Editor path using multiple strategies
            var unityEditorPath = ResolveUnityEditorPath(projectPath);

            // Replace manual loading with cached loader
            if (!string.IsNullOrEmpty(unityEditorPath))
            {
                LoadReferencesWithCaching(unityEditorPath, references);
            }

            var unityScriptAssembliesPath = Path.Combine(projectPath, "Library", "ScriptAssemblies");
            if (Directory.Exists(unityScriptAssembliesPath))
            {
                foreach (var dll in Directory.GetFiles(unityScriptAssembliesPath, "*.dll"))
                {
                    references.Add(MetadataReference.CreateFromFile(dll));
                }
            }

            var searchDirectories = new List<string>();
            switch (searchScope)
            {
                case SearchScope.Assets:
                    searchDirectories.Add(Path.Combine(projectPath, "Assets"));
                    break;
                case SearchScope.Packages:
                    searchDirectories.Add(Path.Combine(projectPath, "Packages"));
                    break;
                case SearchScope.AssetsAndPackages:
                    searchDirectories.Add(Path.Combine(projectPath, "Assets"));
                    searchDirectories.Add(Path.Combine(projectPath, "Packages"));
                    break;
            }

            var csFiles = new List<string>();
            foreach (var dir in searchDirectories.Where(Directory.Exists))
            {
                csFiles.AddRange(Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories));
            }

            var syntaxTrees = new List<SyntaxTree>();
            foreach (var file in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceText = await File.ReadAllTextAsync(file, cancellationToken);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: file, cancellationToken: cancellationToken);
                syntaxTrees.Add(syntaxTree);
            }

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                {
                    // Suppress errors about missing references to proceed with analysis.
                    { "CS1701", ReportDiagnostic.Suppress },
                    { "CS0012", ReportDiagnostic.Suppress },
                    { "CS0246", ReportDiagnostic.Suppress }
                });

            var compilation = CSharpCompilation.Create(
                "UnityProject",
                syntaxTrees,
                references,
                compilationOptions
            );

            return compilation;
        }

        private string? ResolveUnityEditorPath(string projectPath)
        {
            // Strategy 1: Explicit configuration
            var installRoot = ConfigurationService.UnitySettings.InstallRoot;
            var projectVersion = GetUnityVersionFromProject(projectPath);
            var directPath = ConfigurationService.UnitySettings.EditorPath;

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

        private void LoadReferencesWithCaching(string unityEditorPath, List<MetadataReference> references)
        {
            string[] managedPaths = {
                Path.Combine(unityEditorPath, "Data", "Managed"),
                Path.Combine(unityEditorPath, "Unity.app", "Contents", "Managed"),
                Path.Combine(unityEditorPath, "Contents", "Managed")
            };

            foreach (var managedPath in managedPaths)
            {
                if (!Directory.Exists(managedPath)) continue;
                
                foreach (var dll in Directory.GetFiles(managedPath, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    references.Add(_referenceCache.GetOrAdd(dll, path => 
                        MetadataReference.CreateFromFile(path)));
                }
            }
        }
    }
}
