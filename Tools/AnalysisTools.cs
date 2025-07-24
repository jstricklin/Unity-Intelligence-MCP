using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.Analysis.Patterns;
using UnityIntelligenceMCP.Core.Analysis.Project;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools
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
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")] SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = ConfigurationService.GetConfiguredProjectPath();
            return _projectAnalyzer.AnalyzeProjectAsync(projectPath, searchScope, cancellationToken);
        }

        [McpServerTool(Name = "find_unity_patterns"), Description("Scans the Unity project for specific design patterns.")]
        public Task<IEnumerable<DetectedPattern>> FindUnityPatterns(
            [Description("A list of pattern names to search for.")] List<string> patternTypes,
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")] SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = ConfigurationService.GetConfiguredProjectPath();
            return _patternAnalyzer.FindPatternsAsync(projectPath, patternTypes, searchScope, cancellationToken);
        }

        [McpServerTool(Name = "analyze_component_relationships"), Description("Analyzes and returns a graph of MonoBehaviour component interactions.")]
        public Task<UnityComponentGraph> AnalyzeComponentRelationships(
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")] SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = ConfigurationService.GetConfiguredProjectPath();
            return _componentAnalyzer.AnalyzeAsync(projectPath, searchScope, cancellationToken);
        }

        [McpServerTool(Name = "get_pattern_metrics"), Description("Provides quantitative data on the usage of design patterns throughout the project.")]
        public Task<PatternMetrics> GetPatternMetrics(
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")] SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = ConfigurationService.GetConfiguredProjectPath();
            return _patternMetricsAnalyzer.GetMetricsAsync(projectPath, searchScope, cancellationToken);
        }

    }
}
