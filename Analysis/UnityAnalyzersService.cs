using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers;
using UnityCodeIntelligence.Models;

namespace UnityCodeIntelligence.Analysis;

public class UnityAnalyzersService
{
    private readonly IReadOnlyList<DiagnosticAnalyzer> _unityAnalyzers;

    public UnityAnalyzersService()
    {
        // A subset of available analyzers for this phase.
        _unityAnalyzers = new DiagnosticAnalyzer[]
        {
            new EmptyUnityMessageAnalyzer(),
            new InefficientCameraMainUsageAnalyzer(),
            new UnityObjectNullComparisonAnalyzer(),
        };
    }

    public async Task<IEnumerable<UnityDiagnostic>> AnalyzeWithUnityRulesAsync(
        Compilation compilation, CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<UnityDiagnostic>();
        
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            _unityAnalyzers.ToImmutableArray(), 
            cancellationToken: cancellationToken);
            
        var analyzerDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
        
        diagnostics.AddRange(analyzerDiagnostics.Select(d => {
            var lineSpan = d.Location.GetMappedLineSpan();
            return new UnityDiagnostic(
                d.Id,
                d.GetMessage(),
                d.Severity,
                lineSpan.Path,
                lineSpan.StartLinePosition.Line,
                lineSpan.StartLinePosition.Character
            );
        }));
        
        return diagnostics;
    }
}
