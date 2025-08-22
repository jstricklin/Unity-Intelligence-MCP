using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Analysis.Patterns;
using UnityIntelligenceMCP.Core.Analysis.Project;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Analysis
{
    public class UnityStaticAnalysisService : IUnityStaticAnalysisService
    {
        private readonly UnityProjectAnalyzer _projectAnalyzer;
        private readonly UnityPatternAnalyzer _patternAnalyzer;
        private readonly PatternMetricsAnalyzer _patternMetricsAnalyzer;
        private readonly IUnityMessageAnalyzer _messageAnalyzer;

        public UnityStaticAnalysisService(
            UnityProjectAnalyzer projectAnalyzer,
            UnityPatternAnalyzer patternAnalyzer,
            PatternMetricsAnalyzer patternMetricsAnalyzer,
            IUnityMessageAnalyzer messageAnalyzer)
        {
            _projectAnalyzer = projectAnalyzer;
            _patternAnalyzer = patternAnalyzer;
            _patternMetricsAnalyzer = patternMetricsAnalyzer;
            _messageAnalyzer = messageAnalyzer;
        }

        public Task<ProjectContext> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken) =>
            _projectAnalyzer.AnalyzeProjectAsync(projectPath, cancellationToken);

        public Task<IEnumerable<DetectedPattern>> FindPatternsAsync(string projectPath, List<string> patternTypes, CancellationToken cancellationToken) =>
            _patternAnalyzer.FindPatternsAsync(projectPath, patternTypes, cancellationToken);

        public Task<PatternMetrics> GetMetricsAsync(string projectPath, CancellationToken cancellationToken) =>
            _patternMetricsAnalyzer.GetMetricsAsync(projectPath, cancellationToken);

        public Task<UnityMessagesAnalysisResult> AnalyzeMessagesAsync(string projectPath, IEnumerable<string> scriptPaths, CancellationToken cancellationToken) =>
            _messageAnalyzer.AnalyzeMessagesAsync(projectPath, scriptPaths, cancellationToken);
    }
}
