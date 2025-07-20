using System.ComponentModel;
using ModelContextProtocol;
using UnityCodeIntelligence.Analysis;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Tools 
{
    [McpServerToolType]
    public class AnalysisTools
    {
        private readonly UnityProjectAnalyzer _analyzer;

        public AnalysisTools(UnityProjectAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        [McpServerTool("analyze_unity_project"), Description("Analyzes a Unity project structure, scripts, and diagnostics.")]
        public async Task<ProjectContext> AnalyzeUnityProject(
            [Description("The absolute path to the Unity project directory.")] string projectPath,
            CancellationToken cancellationToken)
        {
            var context = await _analyzer.AnalyzeProjectAsync(projectPath, cancellationToken);
            return context;
        }

        [McpServerTool("find_unity_performance_issues"), Description("Finds performance issues in a Unity project using Microsoft.Unity.Analyzers.")]
        public async Task<IEnumerable<UnityDiagnostic>> FindUnityPerformanceIssues(
            [Description("The absolute path to the Unity project directory.")] string projectPath,
            CancellationToken cancellationToken)
        {
            var context = await _analyzer.AnalyzeProjectAsync(projectPath, cancellationToken);
            // A simple implementation could filter diagnostics by category.
            // The Unity Analyzers have specific IDs for performance. e.g., UNT0006 for Camera.main
            return context.UnityDiagnostics.Where(d => d.Id is "UNT0006");
        }

        [McpServerResource("unity://project-diagnostics/{**projectPath}")]
        [Description("Gets all Unity-specific diagnostics for a project.")]
        public async Task<IEnumerable<UnityDiagnostic>> GetUnityDiagnostics(
            string projectPath,
            CancellationToken cancellationToken)
        {
            // The path from the URI will be URL-encoded.
            var decodedPath = System.Net.WebUtility.UrlDecode(projectPath);
            var context = await _analyzer.AnalyzeProjectAsync(decodedPath, cancellationToken);
            return context.UnityDiagnostics;
        }
    }
}
