using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityIntelligenceMCP.Core.IO;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.RoslynServices
{
    public class UnityRoslynAnalysisService
    {
        private readonly UnityInstallationService _unityInstallationService;
        // Updated reference handling with caching
        private static readonly ConcurrentDictionary<string, MetadataReference> _referenceCache = new();

        public UnityRoslynAnalysisService(UnityInstallationService unityInstallationService)
        {
            _unityInstallationService = unityInstallationService;
        }

        public async Task<Compilation> CreateUnityCompilationAsync(string projectPath, SearchScope searchScope = SearchScope.AssetsAndPackages, CancellationToken cancellationToken = default)
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            // Try to resolve Unity Editor path using multiple strategies
            var unityEditorPath = _unityInstallationService.ResolveUnityEditorPath(projectPath);

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
