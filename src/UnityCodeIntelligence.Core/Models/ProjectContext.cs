namespace UnityCodeIntelligence.Core.Models;

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

// These are defined here as they are part of the ProjectContext data contract.
public record UnityVersion(string Version);
public record AssemblyInfo(string Name);
public record SceneInfo(string Path);
public record PrefabInfo(string Path);
public record AssetInfo(string Path);
public record DependencyGraph;
public record FieldInfo;
public record MethodInfo;
public record UsagePattern;

public record ScriptInfo(
    string Path,
    string ClassName,
    string? Namespace,
    string BaseType,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<FieldInfo> PublicFields,
    IReadOnlyList<MethodInfo> PublicMethods,
    IReadOnlyList<UsagePattern> UsagePatterns,
    IReadOnlyList<string> SemanticTags
)
{
    public ISymbol? Symbol { get; init; }
    public SyntaxTree? SyntaxTree { get; init; }
    public SemanticModel? SemanticModel { get; init; }
}

public record ProjectContext(
    string RootPath,
    UnityVersion UnityVersion,
    IReadOnlyList<AssemblyInfo> Assemblies,
    IReadOnlyList<SceneInfo> Scenes,
    IReadOnlyList<PrefabInfo> Prefabs,
    IReadOnlyList<ScriptInfo> Scripts,
    IReadOnlyList<AssetInfo> Assets,
    DependencyGraph Dependencies
);
