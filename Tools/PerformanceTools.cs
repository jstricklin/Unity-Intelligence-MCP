using System.Threading.Tasks;
using System.ComponentModel;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.Analysis.Runtime;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools
{
    [McpServerToolType]
    public class PerformanceTools
    {
        private readonly IUnityRuntimeAnalysisService _runtimeService;
        private readonly ConfigurationService _configurationService;

        public PerformanceTools(IUnityRuntimeAnalysisService runtimeService, ConfigurationService configurationService)
        {
            _runtimeService = runtimeService;
            _configurationService = configurationService;
        }

        [McpServerTool(Name = "unity_performance_monitor"), Description("Continuously monitor Unity application performance metrics")]
        public async Task<PerformanceData> UnityPerformanceMonitor(PerformanceMonitorRequest request)
            => await _runtimeService.MonitorPerformanceAsync(request);

        [McpServerTool(Name = "unity_memory_analyzer"), Description("Deep dive into memory usage patterns and detect memory leaks")]
        public async Task<MemoryAnalysis> UnityMemoryAnalyzer(MemoryAnalysisRequest request)
            => await _runtimeService.AnalyzeMemoryAsync(request);

        [McpServerTool(Name = "unity_rendering_analyzer"), Description("Analyze rendering performance and identify bottlenecks")]
        public async Task<RenderingAnalysis> UnityRenderingAnalyzer(RenderingAnalysisRequest request)
            => await _runtimeService.AnalyzeRenderingAsync(request);

        [McpServerTool(Name = "unity_asset_auditor"), Description("Audit assets for performance impact and optimization opportunities")]
        public async Task<AssetAudit> UnityAssetAuditor(AssetAuditRequest request)
        {
            // Use configured project path if not provided in the request.
            var pathAwareRequest = request with
            {
                ProjectPath = request.ProjectPath ?? _configurationService.GetConfiguredProjectPath()
            };
            return await _runtimeService.AuditAssetsAsync(pathAwareRequest);
        }

        [McpServerTool(Name = "unity_system_checker"), Description("Analyze system capabilities and recommend performance settings")]
        public async Task<SystemCompatibility> UnitySystemChecker(SystemCompatibilityRequest request)
            => await _runtimeService.CheckSystemCompatibilityAsync(request);
    }
}
