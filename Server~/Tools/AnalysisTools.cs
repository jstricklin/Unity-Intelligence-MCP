using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.Analysis;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools
{
    [McpServerToolType]
    public class AnalysisTools
    {
        private readonly IUnityStaticAnalysisService _staticAnalysisService;
        private readonly ConfigurationService _configurationService;

        public AnalysisTools(
            IUnityStaticAnalysisService staticAnalysisService,
            ConfigurationService configurationService)
        {
            _staticAnalysisService = staticAnalysisService;
            _configurationService = configurationService;
        }

        [McpServerTool(Name = "analyze_unity_project"), Description("Analyzes a Unity project structure, scripts, and diagnostics.")]
        public async Task<ProjectContext> AnalyzeUnityProject(
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.AnalyzeProjectAsync(projectPath, cancellationToken);
        }

        [McpServerTool(Name = "find_unity_patterns"), Description("Scans the Unity project for specific design patterns.")]
        public async Task<IEnumerable<DetectedPattern>> FindUnityPatterns(
            [Description("A list of pattern names to search for.")]
            List<string> patternTypes,
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.FindPatternsAsync(projectPath, patternTypes, cancellationToken);
        }

        [McpServerTool(Name = "analyze_component_relationships"), Description("Analyzes and returns a graph of MonoBehaviour component interactions.")]
        public async Task<UnityComponentGraph> AnalyzeComponentRelationships(
            [Description("The scope for analysis: 'Assets', 'Packages', 'or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            var context = await _staticAnalysisService.AnalyzeProjectAsync(projectPath, cancellationToken);
            return context.ComponentRelationships;
        }

        [McpServerTool(Name = "get_pattern_metrics"), Description("Provides quantitative data on the usage of design patterns throughout the project.")]
        public async Task<PatternMetrics> GetPatternMetrics(
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.GetMetricsAsync(projectPath, cancellationToken);
        }

        [McpServerTool(Name = "analyze_unity_messages"), Description("Analyzes one or more scripts for Unity message methods (e.g., Awake, Start, Update).")]
        public async Task<UnityMessagesAnalysisResult> AnalyzeUnityMessages(
            [Description("A request object containing a list of relative paths to the script files to be analyzed.")]
            UnityMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.AnalyzeMessagesAsync(projectPath, request.ScriptPaths, cancellationToken);
        }

    }
}
