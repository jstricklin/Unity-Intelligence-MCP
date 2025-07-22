using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityCodeIntelligence.Core.Analysis.Project;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Core.Analysis.Patterns
{
    public class PatternMetricsAnalyzer
    {
        private readonly UnityProjectAnalyzer _projectAnalyzer;
        private readonly PatternDetectorRegistry _registry;

        public PatternMetricsAnalyzer(UnityProjectAnalyzer projectAnalyzer, PatternDetectorRegistry registry)
        {
            _projectAnalyzer = projectAnalyzer;
            _registry = registry;
        }

        public async Task<PatternMetrics> GetMetricsAsync(string projectPath, SearchScope searchScope, CancellationToken cancellationToken)
        {
            var context = await _projectAnalyzer.AnalyzeProjectAsync(projectPath, searchScope, cancellationToken);
            var detectors = _registry.GetAllDetectors();
            var patternCounts = new Dictionary<string, int>();

            foreach (var detector in detectors)
            {
                patternCounts[detector.PatternName] = 0;
            }

            foreach (var script in context.Scripts)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                foreach (var detector in detectors)
                {
                    if (await detector.DetectAsync(script, cancellationToken))
                    {
                        patternCounts[detector.PatternName]++;
                    }
                }
            }

            return new PatternMetrics(patternCounts);
        }
    }
}
