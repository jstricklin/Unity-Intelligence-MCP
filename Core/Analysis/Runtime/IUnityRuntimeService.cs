using System.Threading.Tasks;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Analysis.Runtime
{
    public interface IUnityRuntimeService
    {
        Task<PerformanceData> MonitorPerformanceAsync(PerformanceMonitorRequest request);
        Task<MemoryAnalysis> AnalyzeMemoryAsync(MemoryAnalysisRequest request);
        Task<RenderingAnalysis> AnalyzeRenderingAsync(RenderingAnalysisRequest request);
        Task<AssetAudit> AuditAssetsAsync(AssetAuditRequest request);
        Task<SystemCompatibility> CheckSystemCompatibilityAsync(SystemCompatibilityRequest request);
    }
}
