using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityCodeIntelligence.Core.Models;

namespace UnityCodeIntelligence.Core.Analysis;

public class UnityProjectAnalyzer
{
    private readonly RoslynAnalysisService _roslynService;
    private readonly DependencyGraphBuilder _dependencyGraphBuilder;

    public UnityProjectAnalyzer(RoslynAnalysisService roslynService, DependencyGraphBuilder dependencyGraphBuilder)
    {
        _roslynService = roslynService;
        _dependencyGraphBuilder = dependencyGraphBuilder;
    }

    public async Task<ProjectContext> AnalyzeProjectAsync(string projectPath)
    {
        // 1. Create the Roslyn compilation.
        var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath);

        // 2. Analyze scripts from the compilation.
        var scripts = await AnalyzeScriptsAsync(compilation);

        // 3. Asset/Scene analysis is out of scope for Phase 1. Return empty lists.
        var scenes = new List<SceneInfo>();
        var prefabs = new List<PrefabInfo>();
        var assets = new List<AssetInfo>();

        // 4. Build dependency graph based on script analysis.
        var dependencies = _dependencyGraphBuilder.BuildFromScripts(scripts);

        // 5. Populate and return the ProjectContext.
        return new ProjectContext(
            RootPath: projectPath,
            UnityVersion: new UnityVersion("Unknown"), // Placeholder
            Assemblies: new List<AssemblyInfo>(),      // Placeholder
            Scenes: scenes,
            Prefabs: prefabs,
            Scripts: scripts,
            Assets: assets,
            Dependencies: dependencies
        );
    }

    private async Task<IReadOnlyList<ScriptInfo>> AnalyzeScriptsAsync(CSharpCompilation compilation)
    {
        var scripts = new List<ScriptInfo>();
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();
            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classNode == null) continue;

            var classSymbol = semanticModel.GetDeclaredSymbol(classNode);
            if (classSymbol == null) continue;

            scripts.Add(new ScriptInfo(
                Path: syntaxTree.FilePath,
                ClassName: classSymbol.Name,
                Namespace: classSymbol.ContainingNamespace?.ToDisplayString(),
                BaseType: classSymbol.BaseType?.Name,
                Interfaces: classSymbol.Interfaces.Select(i => i.Name).ToList(),
                Dependencies: new List<string>(), // To be populated by a separate pass if needed.
                PublicFields: new List<FieldInfo>(), // Out of scope for Phase 1
                PublicMethods: new List<MethodInfo>(), // Out of scope for Phase 1
                UsagePatterns: new List<UsagePattern>(), // Out of scope for Phase 1
                SemanticTags: new List<string>(), // Out of scope for Phase 1
                Symbol = classSymbol,
                SyntaxTree = syntaxTree,
                SemanticModel = semanticModel
            ));
        }
        return scripts;
    }
}
