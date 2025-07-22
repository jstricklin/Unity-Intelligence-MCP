using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Analysis.Project;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Core.Analysis.Patterns
{
    public class UnityPatternAnalyzer
    {
        private readonly UnityProjectAnalyzer _projectAnalyzer;
        private readonly PatternDetectorRegistry _registry;

        public UnityPatternAnalyzer(UnityProjectAnalyzer projectAnalyzer, PatternDetectorRegistry registry)
        {
            _projectAnalyzer = projectAnalyzer;
            _registry = registry;
        }

        public async Task<IEnumerable<DetectedPattern>> FindPatternsAsync(string projectPath, List<string> patternTypes, SearchScope searchScope, CancellationToken cancellationToken)
        {
            var context = await _projectAnalyzer.AnalyzeProjectAsync(projectPath, searchScope, cancellationToken);
            var detectors = _registry.GetAllDetectors();

            var requestedDetectors = detectors;
            if (patternTypes != null && patternTypes.Count > 0)
            {
                var patternSet = new HashSet<string>(patternTypes, System.StringComparer.OrdinalIgnoreCase);
                requestedDetectors = detectors.Where(d => patternSet.Contains(d.PatternName));
            }

            var detectedPatterns = new List<DetectedPattern>();

            foreach (var script in context.Scripts)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                foreach (var detector in requestedDetectors)
                {
                    if (await detector.DetectAsync(script, cancellationToken))
                    {
                        detectedPatterns.Add(new DetectedPattern(detector.PatternName, script.Path, script.ClassName));
                    }
                }
            }
            return detectedPatterns;
        }
    }
}
