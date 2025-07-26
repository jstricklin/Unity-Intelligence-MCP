using System.Collections.Generic;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Analysis.Runtime
{
    /// <summary>
    /// A mock implementation of the runtime service for demonstration and testing.
    /// A real implementation would communicate with a live Unity instance.
    /// </summary>
    public class MockUnityRuntimeService : IUnityRuntimeService
    {
        public Task<PerformanceData> MonitorPerformanceAsync(PerformanceMonitorRequest request)
        {
            var dummyMetrics = new Dictionary<string, object>
            {
                { "status", "Monitoring completed." },
                { "duration_seconds", request.Duration },
                { "sample_rate", request.SampleRate },
                { "metrics_tracked", request.Metrics ?? new List<string>() }
            };
            return Task.FromResult(new PerformanceData(dummyMetrics));
        }

        public Task<MemoryAnalysis> AnalyzeMemoryAsync(MemoryAnalysisRequest request) =>
            Task.FromResult(new MemoryAnalysis($"Memory analysis report for type '{request.AnalysisType}'."));

        public Task<RenderingAnalysis> AnalyzeRenderingAsync(RenderingAnalysisRequest request) =>
            Task.FromResult(new RenderingAnalysis($"Rendering analysis report for {request.FrameCount} frames."));

        public Task<AssetAudit> AuditAssetsAsync(AssetAuditRequest request) =>
            Task.FromResult(new AssetAudit($"Asset audit report for project: {request.ProjectPath}"));

        public Task<SystemCompatibility> CheckSystemCompatibilityAsync(SystemCompatibilityRequest request) =>
            Task.FromResult(new SystemCompatibility($"System compatibility report for platform: {request.TargetPlatform}"));
    }
}
