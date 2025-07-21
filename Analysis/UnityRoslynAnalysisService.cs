using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityCodeIntelligence.Analysis
{
    public class UnityRoslynAnalysisService
    {
        public async Task<Compilation> CreateUnityCompilationAsync(string projectPath, CancellationToken cancellationToken = default)
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            // Try to resolve Unity Editor path using multiple strategies
            var unityEditorPath = ResolveUnityEditorPath(projectPath);
            
            if (!string.IsNullOrEmpty(unityEditorPath))
            {
                Console.Error.WriteLine($"[DEBUG] Using Unity Editor path: {unityEditorPath}");
                
                var coreModuleDll = GetUnityEngineCorePath(unityEditorPath);
                if (File.Exists(coreModuleDll))
                {
                    references.Add(MetadataReference.CreateFromFile(coreModuleDll));
                    Console.Error.WriteLine($"[DEBUG] Added Unity CoreModule reference: {coreModuleDll}");
                }
                else
                {
                    Console.Error.WriteLine($"[ERROR] Unity CoreModule not found at {coreModuleDll}");
                }
            }
            else
            {
                Console.Error.WriteLine("[ERROR] Unity Editor path could not be resolved");
            }

            var unityScriptAssembliesPath = Path.Combine(projectPath, "Library", "ScriptAssemblies");
            if (Directory.Exists(unityScriptAssembliesPath))
            {
                foreach (var dll in Directory.GetFiles(unityScriptAssembliesPath, "*.dll"))
                {
                    references.Add(MetadataReference.CreateFromFile(dll));
                }
            }

            var syntaxTrees = new List<SyntaxTree>();
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

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
            // Strategy 1: Explicit configuration (Production Priority)
            var installRoot = Environment.GetEnvironmentVariable("UNITY_INSTALL_ROOT");
            var projectVersion = GetUnityVersionFromProject(projectPath);
            
            if (!string.IsNullOrEmpty(installRoot) && !string.IsNullOrEmpty(projectVersion))
            {
                var explicitPath = Path.Combine(installRoot, projectVersion);
                if (Directory.Exists(explicitPath))
                {
                    Console.Error.WriteLine($"[INFO] Using Unity Editor path from UNITY_INSTALL_ROOT + project version: {explicitPath}");
                    return explicitPath;
                }
                else
                {
                    Console.Error.WriteLine($"[ERROR] Unity Editor not found at expected location: {explicitPath} (InstallRoot: {installRoot}, ProjectVersion: {projectVersion})");
                    // Don't fallback - fail fast in production
                    throw new DirectoryNotFoundException($"Unity Editor not found at: {explicitPath}");
                }
            }

            // Strategy 2: Direct override (for development/testing)
            var directPath = Environment.GetEnvironmentVariable("UNITY_EDITOR_PATH");
            if (!string.IsNullOrEmpty(directPath))
            {
                if (Directory.Exists(directPath))
                {
                    Console.Error.WriteLine($"[INFO] Using direct Unity Editor path override: {directPath}");
                    return directPath;
                }
                else
                {
                    Console.Error.WriteLine($"[ERROR] Direct Unity Editor path does not exist: {directPath}");
                    throw new DirectoryNotFoundException($"Unity Editor not found at: {directPath}");
                }
            }

            // Strategy 3: Configuration missing - fail with helpful message
            var missingConfig = new List<string>();
            if (string.IsNullOrEmpty(installRoot)) missingConfig.Add("UNITY_INSTALL_ROOT");
            if (string.IsNullOrEmpty(projectVersion)) missingConfig.Add("Unity project version");

            var errorMessage = $"Unity Editor path cannot be resolved. Missing: {string.Join(", ", missingConfig)}";
            Console.Error.WriteLine($"[ERROR] {errorMessage}");
            
            throw new InvalidOperationException(errorMessage);
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

        private string? TryCommonUnityPaths()
        {
            var commonPaths = GetPlatformSpecificUnityPaths();
            
            foreach (var basePath in commonPaths)
            {
                if (Directory.Exists(basePath))
                {
                    // Look for Unity installations in common patterns
                    var possiblePaths = new[]
                    {
                        basePath, // Direct path
                        Path.Combine(basePath, "Hub", "Editor"), // Unity Hub structure
                        Path.Combine(basePath, "Editor") // Alternative structure
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (Directory.Exists(path))
                        {
                            // Find the most recent version directory
                            var versionDirs = Directory.GetDirectories(path)
                                .Where(d => Path.GetFileName(d).Contains('.'))
                                .OrderByDescending(d => Path.GetFileName(d))
                                .ToArray();

                            if (versionDirs.Length > 0)
                            {
                                Console.Error.WriteLine($"[DEBUG] Found Unity installation via common paths: {versionDirs[0]}");
                                return versionDirs[0];
                            }
                        }
                    }
                }
            }

            Console.Error.WriteLine("[ERROR] No Unity installation found in common paths");
            return null;
        }

        private string[] GetPlatformSpecificUnityPaths()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.MacOSX => new[]
                {
                    "/Applications/Unity",
                    "/Applications/Unity Hub/Editor"
                },
                PlatformID.Unix => new[]
                {
                    "/opt/Unity",
                    "~/Unity",
                    Environment.ExpandEnvironmentVariables("$HOME/Unity")
                },
                _ => new[] // Windows
                {
                    @"C:\Program Files\Unity",
                    @"C:\Program Files\Unity Hub\Editor",
                    Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Unity Hub\Editor")
                }
            };
        }

        private string GetUnityEngineCorePath(string unityEditorPath)
        {
            // Try different possible locations for UnityEngine.CoreModule.dll
            var possiblePaths = new[]
            {
                Path.Combine(unityEditorPath, "Data", "Managed", "UnityEngine", "UnityEngine.CoreModule.dll"), // Windows/Linux
                Path.Combine(unityEditorPath, "Unity.app", "Contents", "Managed", "UnityEngine", "UnityEngine.CoreModule.dll"), // macOS App Bundle
                Path.Combine(unityEditorPath, "Contents", "Managed", "UnityEngine", "UnityEngine.CoreModule.dll"), // macOS alternative
                Path.Combine(unityEditorPath, "Editor", "Data", "Managed", "UnityEngine", "UnityEngine.CoreModule.dll") // Hub structure
            };

            return possiblePaths.FirstOrDefault(File.Exists) ?? possiblePaths[0];
        }
    }
}
