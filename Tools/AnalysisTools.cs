using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityCodeIntelligence.Analysis;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Tools
{
    [McpServerToolType]
    public class AnalysisTools
    {
        private readonly UnityProjectAnalyzer _projectAnalyzer;
        private readonly UnityPatternAnalyzer _patternAnalyzer;
        private readonly UnityComponentRelationshipAnalyzer _componentAnalyzer;
        private readonly PatternMetricsAnalyzer _patternMetricsAnalyzer;

        public AnalysisTools(
            UnityProjectAnalyzer projectAnalyzer,
            UnityPatternAnalyzer patternAnalyzer,
            UnityComponentRelationshipAnalyzer componentAnalyzer,
            PatternMetricsAnalyzer patternMetricsAnalyzer)
        {
            _projectAnalyzer = projectAnalyzer;
            _patternAnalyzer = patternAnalyzer;
            _componentAnalyzer = componentAnalyzer;
            _patternMetricsAnalyzer = patternMetricsAnalyzer;
        }

        [McpServerTool(Name = "analyze_unity_project"), Description("Analyzes a Unity project structure, scripts, and diagnostics.")]
        public Task<ProjectContext> AnalyzeUnityProject(
            [Description("The absolute path to the Unity project directory.")] string projectPath,
            CancellationToken cancellationToken)
        {
            return _projectAnalyzer.AnalyzeProjectAsync(projectPath, cancellationToken);
        }

        [McpServerTool(Name = "find_unity_patterns"), Description("Scans the Unity project for specific design patterns.")]
        public Task<IEnumerable<DetectedPattern>> FindUnityPatterns(
            PatternSearchRequest request,
            CancellationToken cancellationToken)
        {
            return _patternAnalyzer.FindPatternsAsync(request.ProjectPath, request.PatternTypes, cancellationToken);
        }

        [McpServerTool(Name = "analyze_component_relationships"), Description("Analyzes and returns a graph of MonoBehaviour component interactions.")]
        public Task<UnityComponentGraph> AnalyzeComponentRelationships(
            ComponentRequest request,
            CancellationToken cancellationToken)
        {
            return _componentAnalyzer.AnalyzeAsync(request.ProjectPath, cancellationToken);
        }

        [McpServerTool(Name = "get_pattern_metrics"), Description("Provides quantitative data on the usage of design patterns throughout the project.")]
        public Task<PatternMetrics> GetPatternMetrics(
            PatternMetricRequest request,
            CancellationToken cancellationToken)
        {
            return _patternMetricsAnalyzer.GetMetricsAsync(request.ProjectPath, cancellationToken);
        }
    }
}
