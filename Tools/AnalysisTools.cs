using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityIntelligenceMCP.Configuration;
using UnityIntelligenceMCP.Core.Analysis;
using UnityIntelligenceMCP.Core.Analysis.Relationships;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Tools
{
    [McpServerToolType]
    public class AnalysisTools
    {
        private readonly IUnityStaticAnalysisService _staticAnalysisService;
        private readonly ConfigurationService _configurationService;
        private readonly SemanticSearchService _semanticSearchService;

        public AnalysisTools(
            IUnityStaticAnalysisService staticAnalysisService,
            ConfigurationService configurationService,
            SemanticSearchService semanticSearchService) // Add this parameter
        {
            _staticAnalysisService = staticAnalysisService;
            _configurationService = configurationService;
            _semanticSearchService = semanticSearchService; // Add this line
        }

        [McpServerTool(Name = "analyze_unity_project"), Description("Analyzes a Unity project structure, scripts, and diagnostics.")]
        public async Task<ProjectContext> AnalyzeUnityProject(
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.AnalyzeProjectAsync(projectPath, searchScope, cancellationToken);
        }

        [McpServerTool(Name = "find_unity_patterns"), Description("Scans the Unity project for specific design patterns.")]
        public async Task<IEnumerable<DetectedPattern>> FindUnityPatterns(
            [Description("A list of pattern names to search for.")]
            List<string> patternTypes,
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.FindPatternsAsync(projectPath, patternTypes, searchScope, cancellationToken);
        }

        [McpServerTool(Name = "analyze_component_relationships"), Description("Analyzes and returns a graph of MonoBehaviour component interactions.")]
        public async Task<UnityComponentGraph> AnalyzeComponentRelationships(
            [Description("The scope for analysis: 'Assets', 'Packages', 'or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            var context = await _staticAnalysisService.AnalyzeProjectAsync(projectPath, searchScope, cancellationToken);
            return context.ComponentRelationships;
        }

        [McpServerTool(Name = "get_pattern_metrics"), Description("Provides quantitative data on the usage of design patterns throughout the project.")]
        public async Task<PatternMetrics> GetPatternMetrics(
            [Description("The scope for analysis: 'Assets', 'Packages', or 'AssetsAndPackages'. Defaults to 'Assets'.")]
            SearchScope searchScope = SearchScope.Assets,
            CancellationToken cancellationToken = default)
        {
            var projectPath = _configurationService.GetConfiguredProjectPath();
            return await _staticAnalysisService.GetMetricsAsync(projectPath, searchScope, cancellationToken);
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

        [McpServerTool(Name = "semantic_search_docs"), Description("Performs a semantic search on the indexed Unity documentation.")]
        public async Task<IEnumerable<SearchResult>> SemanticSearch(
            [Description("The natural language query to search for.")]
            string query,
            [Description("The maximum number of results to return.")]
            int maxResults = 10
            )
        {
            return await _semanticSearchService.SearchAsync(query, maxResults);
        }

    }
}
