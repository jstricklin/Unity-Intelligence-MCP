using Microsoft.CodeAnalysis;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Analysis;

public class UnityProjectAnalyzer
{
    private readonly UnityRoslynAnalysisService _roslynService;
    private readonly UnityAnalyzersService _unityAnalyzers;

    public UnityProjectAnalyzer(UnityRoslynAnalysisService roslynService, UnityAnalyzersService unityAnalyzers)
    {
        _roslynService = roslynService;
        _unityAnalyzers = unityAnalyzers;
    }

    public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, cancellationToken);
        
        var scripts = compilation.SyntaxTrees.Select(st => new ScriptInfo(
            st.FilePath,
            Path.GetFileNameWithoutExtension(st.FilePath)
        )).ToList();
        
        var unityDiagnostics = await _unityAnalyzers.AnalyzeWithUnityRulesAsync(compilation, cancellationToken);

        return new ProjectContext(projectPath, scripts, unityDiagnostics.ToList());
    }
}
