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

namespace UnityIntelligenceMCP.Core.Analysis.Project
{
    public class UnityProjectAnalyzer
    {
        private readonly UnityRoslynAnalysisService _roslynService;
        private readonly PatternDetectorRegistry _patternDetectors;
        private readonly UnityComponentRelationshipAnalyzer _relationshipAnalyzer;
        private readonly UnityDependencyGraphAnalyzer _dependencyGraphAnalyzer;
        
        public UnityProjectAnalyzer(
            UnityRoslynAnalysisService roslynService,
            PatternDetectorRegistry patternDetectors,
            UnityComponentRelationshipAnalyzer relationshipAnalyzer,
            UnityDependencyGraphAnalyzer dependencyGraphAnalyzer)
        {
            _roslynService = roslynService;
            _patternDetectors = patternDetectors;
            _relationshipAnalyzer = relationshipAnalyzer;
            _dependencyGraphAnalyzer = dependencyGraphAnalyzer;
        }

        public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath, SearchScope searchScope, CancellationToken cancellationToken = default)
        {
            var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, searchScope, cancellationToken);
            var monoBehaviourSymbol = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");

            var scripts = new List<ScriptInfo>();
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree, true);
                var classNode = syntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classNode == null || semanticModel.GetDeclaredSymbol(classNode, cancellationToken) is not INamedTypeSymbol classSymbol) continue;

                var isMonoBehaviour = IsSubclassOf(classSymbol, monoBehaviourSymbol);
                // TODO: Populate UnityMessages by analyzing methods
                var unityAnalysis = new UnityScriptAnalysis(isMonoBehaviour, new List<UnityMessageInfo>());

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

            return new ProjectContext(
                projectPath,
                scripts,
                patterns,
                relationships,
                dependencies
            );
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
