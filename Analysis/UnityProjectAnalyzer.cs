using Microsoft.CodeAnalysis;
using System.IO;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Analysis
{
    public class UnityProjectAnalyzer
    {
        private readonly UnityRoslynAnalysisService _roslynService;
        public UnityProjectAnalyzer(UnityRoslynAnalysisService roslynService)
        {
            _roslynService = roslynService;
        }

        public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default)
        {
            var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, cancellationToken);
            
            var scripts = compilation.SyntaxTrees.Select(st => new ScriptInfo(
                st.FilePath,
                Path.GetFileNameWithoutExtension(st.FilePath)
            )).ToList();
            
            return new ProjectContext(projectPath, scripts);
        }
    }
}
