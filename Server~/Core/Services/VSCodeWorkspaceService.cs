using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.IO;

namespace UnityIntelligenceMCP.Core.Services
{
    public class VSCodeWorkspaceService : IVSCodeWorkspaceService
    {
        private readonly UnityInstallationService _installationService;

        public VSCodeWorkspaceService(UnityInstallationService installationService)
        {
            _installationService = installationService;
        }

        public async Task<string> GenerateWorkspaceAsync(string projectPath, string workspaceName = "project-workspace")
        {
            try
            {
                var (projectType, dependencyFolders) = DetectProjectDependencies(projectPath);

                var config = new VSCodeWorkspaceConfig
                {
                    Folders = new List<WorkspaceFolder>
                    {
                        new() { Name = "Project Root", Path = projectPath }
                    },
                    Settings = new Dictionary<string, object>
                    {
                        ["search.exclude"] = new Dictionary<string, bool>(),
                        ["files.exclude"] = new Dictionary<string, bool>(),
                        ["typescript.preferences.includePackageJsonAutoImports"] = "on"
                    },
                    Extensions = new WorkspaceExtensions
                    {
                        Recommendations = new List<string>()
                    }
                };

                foreach (var folder in dependencyFolders)
                {
                    config.Folders.Add(new WorkspaceFolder { Name = folder.Name, Path = folder.Path });
                    
                    if (folder.Type.Contains("cache"))
                    {
                        ((Dictionary<string, bool>)config.Settings["search.exclude"])
                            [$"**/{Path.GetFileName(folder.Path)}/**"] = true;
                    }
                }

                if (projectType == "unity")
                {
                    config.Extensions.Recommendations.AddRange(new[] {
                        "ms-dotnettools.csharp",
                        "kleber-swf.unity-code-snippets",
                        "tobiah.unity-tools"
                    });
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var workspacePath = Path.Combine(projectPath, $"{workspaceName}.code-workspace");
                await File.WriteAllTextAsync(workspacePath, json);
                
                return workspacePath;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Workspace generation failed: {ex.Message}");
                throw;
            }
        }

        private (string projectType, List<(string Path, string Type, string Name)>) 
            DetectProjectDependencies(string projectPath)
        {
            var type = DetectProjectType(projectPath);
            var folders = new List<(string Path, string Type, string Name)>();

            if (type == "unity")
            {
                var packageCachePath = Path.Combine(projectPath, "Library", "PackageCache");
                if (Directory.Exists(packageCachePath))
                {
                    folders.Add((packageCachePath, "unity-package-cache", "Unity Packages Cache"));
                }
            }

            return (type, folders);
        }

        private string DetectProjectType(string projectPath)
        {
            if (Directory.Exists(Path.Combine(projectPath, "Assets")) ||
                Directory.Exists(Path.Combine(projectPath, "ProjectSettings")))
            {
                return "unity";
            }
            return "unknown";
        }

        private class VSCodeWorkspaceConfig
        {
            public List<WorkspaceFolder> Folders { get; set; }
            public Dictionary<string, object> Settings { get; set; }
            public WorkspaceExtensions Extensions { get; set; }
        }

        private class WorkspaceFolder
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        private class WorkspaceExtensions
        {
            public List<string> Recommendations { get; set; }
        }
    }
}
