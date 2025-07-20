using System.ComponentModel;
using ModelContextProtocol.Server;
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

        [McpServerTool(Name = "analyze_unity_project"), Description("Analyzes a Unity project structure, scripts, and diagnostics.")]
        public async Task<ProjectContext> AnalyzeUnityProject(
            [Description("The absolute path to the Unity project directory.")] string projectPath,
            CancellationToken cancellationToken)
        {
            var context = await _analyzer.AnalyzeProjectAsync(projectPath, cancellationToken);
            return context;
        }

    }
}
