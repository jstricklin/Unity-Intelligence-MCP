using Microsoft.CodeAnalysis;
using System.IO;
using UnityCodeIntelligence.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Analysis.Patterns;
using UnityCodeIntelligence.Core.Analysis.Relationships;
using UnityCodeIntelligence.Core.Compilation;

namespace UnityCodeIntelligence.Core.Analysis.Project
{
    public class UnityProjectAnalyzer
    {
        private readonly UnityRoslynAnalysisService _roslynService;
        private readonly PatternDetectorRegistry _patternDetectors;
        private readonly UnityComponentRelationshipAnalyzer _relationshipAnalyzer;
        
        public UnityProjectAnalyzer(
            UnityRoslynAnalysisService roslynService,
            PatternDetectorRegistry patternDetectors,
            UnityComponentRelationshipAnalyzer relationshipAnalyzer)
        {
            _roslynService = roslynService;
            _patternDetectors = patternDetectors;
            _relationshipAnalyzer = relationshipAnalyzer;
        }

        public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath, SearchScope searchScope, CancellationToken cancellationToken = default)
        {
            var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, searchScope, cancellationToken);
            
            var scripts = compilation.SyntaxTrees.Select(st => new ScriptInfo(
                st.FilePath,
                Path.GetFileNameWithoutExtension(st.FilePath),
                "MonoBehaviour" // TODO: Replace with actual base type from Roslyn analysis
            )).ToList();
            
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
            var relationships = _relationshipAnalyzer.AnalyzeMonoBehaviours(compilation, cancellationToken);
            
            return new ProjectContext(
                projectPath,
                scripts,
                patterns,
                relationships
            );
        }
    }
}
