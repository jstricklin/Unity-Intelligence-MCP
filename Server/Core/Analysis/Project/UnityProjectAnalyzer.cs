using Microsoft.CodeAnalysis;
using System.IO;
using UnityIntelligenceMCP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityIntelligenceMCP.Core.Analysis.Dependencies;
using UnityIntelligenceMCP.Core.Analysis.Patterns;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Core.RoslynServices;
using UnityIntelligenceMCP.Models.Analysis;
using UnityIntelligenceMCP.Core.Data.Contracts;
using UnityIntelligenceMCP.Models.Database;
using System.Text.Json;
using System.Diagnostics;

namespace UnityIntelligenceMCP.Core.Analysis.Project
{
    public class UnityProjectAnalyzer
    {
        private readonly UnityRoslynAnalysisService _roslynService;
        private readonly PatternDetectorRegistry _patternDetectors;
        private readonly UnityComponentRelationshipAnalyzer _relationshipAnalyzer;
        private readonly UnityDependencyGraphAnalyzer _dependencyGraphAnalyzer;
        private readonly IToolUsageLogger _usageLogger;
        
        public UnityProjectAnalyzer(
            UnityRoslynAnalysisService roslynService,
            PatternDetectorRegistry patternDetectors,
            UnityComponentRelationshipAnalyzer relationshipAnalyzer,
            UnityDependencyGraphAnalyzer dependencyGraphAnalyzer,
            IToolUsageLogger usageLogger)
        {
            _roslynService = roslynService;
            _patternDetectors = patternDetectors;
            _relationshipAnalyzer = relationshipAnalyzer;
            _dependencyGraphAnalyzer = dependencyGraphAnalyzer;
            _usageLogger = usageLogger;
        }

        public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath, SearchScope searchScope, CancellationToken cancellationToken = default)
        {
            var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, searchScope, cancellationToken);
            var monoBehaviourSymbol = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");

            var scripts = new List<ScriptInfo>();
            var stopwatch = Stopwatch.StartNew();
            bool wasSuccessful = false;
            try
            {
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxTree, true);
                    var classNode = syntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    if (classNode == null || semanticModel.GetDeclaredSymbol(classNode, cancellationToken) is not INamedTypeSymbol classSymbol) continue;

                    var isMonoBehaviour = IsSubclassOf(classSymbol, monoBehaviourSymbol);
                    // TODO: Expand script type identification for ScriptableObject and Editor classes.
                    
                    // TODO: Populate UnityMessages by analyzing class methods.
                    var unityAnalysis = new UnityScriptAnalysis(isMonoBehaviour, new List<UnityMessageInfo>());

                    // TODO: Extract public methods and serialized fields into dedicated models.
                    // TODO: Capture the class namespace and implemented interfaces.
                    scripts.Add(new ScriptInfo(
                        syntaxTree.FilePath,
                        classSymbol.Name,
                        classSymbol.BaseType?.Name ?? "object",
                        semanticModel,
                        syntaxTree,
                        unityAnalysis
                    ));
                }
                
                // Run pattern detection
                var patterns = new List<DetectedUnityPattern>();
                foreach (var script in scripts)
                {
                    foreach (var detector in _patternDetectors.GetAllDetectors())
                    {
                        if (await detector.DetectAsync(script, cancellationToken))
                        {
                            patterns.Add(new DetectedUnityPattern(
                                detector.PatternName,
                                script.Path,
                                script.ClassName,
                                detector.Confidence
                            ));
                        }
                    }
                }
                
                // Analyze component relationships
                var relationships = _relationshipAnalyzer.AnalyzeRelationships(scripts);
                
                // Build dependency graph
                var dependencies = await _dependencyGraphAnalyzer.BuildGraphAsync(projectPath, searchScope, compilation);
                wasSuccessful = true;
                return new ProjectContext(
                    projectPath,
                    scripts,
                    patterns,
                    relationships,
                    dependencies
                );
            }
            finally
            {
                stopwatch.Stop();
                var process = Process.GetCurrentProcess();
                process.Refresh();
                var peakMemoryMb = process.PeakWorkingSet64 / (1024 * 1024);

                var parameters = new { projectPath, searchScope };
                var resultSummary = new { MimeType = "Binary", TextLength = "N/A" };

                await _usageLogger.LogAsync(new ToolUsageLog
                {
                    ToolName = "analyze_unity_project",
                    ParametersJson = JsonSerializer.Serialize(parameters),
                    ResultSummaryJson = JsonSerializer.Serialize(resultSummary),
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    WasSuccessful = wasSuccessful,
                    PeakProcessMemoryMb = peakMemoryMb
                });
            }
        }

        private bool IsSubclassOf(INamedTypeSymbol? type, INamedTypeSymbol? baseTypeSymbol)
        {
            if (baseTypeSymbol == null) return false;
            var current = type;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseTypeSymbol)) return true;
                current = current.BaseType;
            }
            return false;
        }
    }
}
