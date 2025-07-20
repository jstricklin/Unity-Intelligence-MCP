namespace UnityCodeIntelligence.Models;

public record ProjectContext(
    string RootPath,
    IReadOnlyList<ScriptInfo> Scripts
);

public record ScriptInfo(
    string Path,
    string ClassName
);

// Request models for tools
public record UnityProjectAnalysisRequest(string ProjectPath);
