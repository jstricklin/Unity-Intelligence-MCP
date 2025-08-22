using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Analysis
{
    public interface IUnityStaticAnalysisService
    {
        Task<ProjectContext> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken);
        Task<IEnumerable<DetectedPattern>> FindPatternsAsync(string projectPath, List<string> patternTypes, CancellationToken cancellationToken);
        Task<PatternMetrics> GetMetricsAsync(string projectPath, CancellationToken cancellationToken);
        Task<UnityMessagesAnalysisResult> AnalyzeMessagesAsync(string projectPath, IEnumerable<string> scriptPaths, CancellationToken cancellationToken);
    }
}
