using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Analysis.Dependencies
{
    /// <summary>
    /// Analyzes project assets to build a dependency graph.
    /// It maps both script-to-script references and asset-to-script usages (e.g., scenes using a component).
    /// </summary>
    public class UnityDependencyGraphAnalyzer
    {
        // Pre-compile regexes for performance and define as static to create only once.
        private static readonly Regex MetaGuidRegex = new(@"guid: ([a-f0-9]{32})", RegexOptions.Compiled);
        private static readonly Regex AssetGuidRegex = new(@"m_Script: {fileID: \d+, guid: ([a-f0-9]{32}), type: \d+}", RegexOptions.Compiled);

        /// <summary>
        /// Builds the complete dependency graph for the project.
        /// </summary>
        /// <param name="projectPath">The root path of the Unity project.</param>
        /// <param name="searchScope">The scope to search within (e.g., just the Assets folder).</param>
        /// <param name="compilation">The Roslyn compilation of the project's scripts.</param>
        /// <returns>A DependencyGraph instance representing project-wide relationships.</returns>
        public async Task<DependencyGraph> BuildGraphAsync(string projectPath, SearchScope searchScope, Compilation compilation)
        {
            var graph = new DependencyGraph();
            var searchPattern = Path.Combine(projectPath, searchScope == SearchScope.Assets ? "Assets" : "Packages");

            // Run asset and script analysis concurrently.
            await AnalyzeAssetToScriptDependenciesAsync(searchPattern, graph);
            AnalyzeScriptToScriptDependencies(compilation, graph);

            return graph;
        }

        /// <summary>
        /// Uses Roslyn to find direct dependencies between scripts, processing files in parallel.
        /// </summary>
        private void AnalyzeScriptToScriptDependencies(Compilation compilation, DependencyGraph graph)
        {
            Parallel.ForEach(compilation.SyntaxTrees, syntaxTree =>
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var walker = new ScriptDependencyWalker(semanticModel, compilation, graph, syntaxTree.FilePath);
                walker.Visit(syntaxTree.GetRoot());
            });
        }

        /// <summary>
        /// Scans asset files (.unity, .prefab) to find which scripts they reference.
        /// This process is parallelized and memory-optimized for large projects.
        /// </summary>
        private async Task AnalyzeAssetToScriptDependenciesAsync(string searchPath, DependencyGraph graph)
        {
            // 1. Build a thread-safe map from a script's GUID to its file path in parallel.
            var guidToScriptPath = new ConcurrentDictionary<string, string>();
            var metaFiles = Directory.EnumerateFiles(searchPath, "*.cs.meta", SearchOption.AllDirectories);

            var metaFileTasks = metaFiles.Select(async metaFile =>
            {
                var content = await File.ReadAllTextAsync(metaFile);
                var match = MetaGuidRegex.Match(content);
                if (match.Success)
                {
                    var scriptPath = Path.ChangeExtension(metaFile, null);
                    guidToScriptPath.TryAdd(match.Groups[1].Value, scriptPath);
                }
            });
            await Task.WhenAll(metaFileTasks);

            // 2. Scan asset files in parallel and read them line-by-line for memory efficiency.
            var assetFiles = Directory.EnumerateFiles(searchPath, "*.unity", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(searchPath, "*.prefab", SearchOption.AllDirectories));

            var assetFileTasks = assetFiles.Select(async assetFile =>
            {
                using var reader = new StreamReader(assetFile);
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var match = AssetGuidRegex.Match(line);
                    if (match.Success)
                    {
                        var guid = match.Groups[1].Value;
                        if (guidToScriptPath.TryGetValue(guid, out var scriptPath))
                        {
                            graph.AddDependency(assetFile, scriptPath);
                        }
                    }
                }
            });
            await Task.WhenAll(assetFileTasks);
        }

        /// <summary>
        /// A Roslyn syntax walker that finds type references within a script
        /// to identify dependencies on other scripts.
        /// </summary>
        private class ScriptDependencyWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly Compilation _compilation;
            private readonly DependencyGraph _graph;
            private readonly string _sourceFilePath;

            public ScriptDependencyWalker(SemanticModel semanticModel, Compilation compilation, DependencyGraph graph, string sourceFilePath)
            {
                _semanticModel = semanticModel;
                _compilation = compilation;
                _graph = graph;
                _sourceFilePath = sourceFilePath;
            }

            /// <summary>
            /// This method is called for every identifier (e.g., a class name) found in the code.
            /// </summary>
            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(node);
                var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                // We are interested in named types (classes, structs, interfaces, enums).
                if (symbol is INamedTypeSymbol typeSymbol)
                {
                    // Check if the type is defined within our own project source code.
                    if (typeSymbol.DeclaringSyntaxReferences.Any())
                    {
                        var dependencyPath = typeSymbol.DeclaringSyntaxReferences.First().SyntaxTree.FilePath;
                        _graph.AddDependency(_sourceFilePath, dependencyPath);
                    }
                }
                base.VisitIdentifierName(node);
            }
        }
    }
}
