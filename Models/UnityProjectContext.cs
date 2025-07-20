using Microsoft.CodeAnalysis;

namespace UnityCodeIntelligence.Models;

public record ProjectContext(
    string RootPath,
    IReadOnlyList<ScriptInfo> Scripts,
    IReadOnlyList<UnityDiagnostic> UnityDiagnostics
);

public record ScriptInfo(
    string Path,
    string ClassName
);

public record UnityDiagnostic(
    string Id,
    string Message,
    DiagnosticSeverity Severity,
    string FilePath,
    int Line,
    int Character
);

// Request models for tools
public record UnityProjectAnalysisRequest(string ProjectPath);
public record PerformanceAnalysisRequest(string ProjectPath);
