using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityCodeIntelligence.Core.Analysis;

public class RoslynAnalysisService
{
    // The existing method signature is correct.
    public async Task<CSharpCompilation> CreateUnityCompilationAsync(string projectPath)
    {
        // 1. Find all .cs files in the project's "Assets" directory, excluding "Library".
        var csFiles = Directory.EnumerateFiles(
            Path.Combine(projectPath, "Assets"),
            "*.cs",
            SearchOption.AllDirectories);

        // 2. Load each file into a Roslyn SyntaxTree.
        var syntaxTrees = new List<SyntaxTree>();
        foreach (var file in csFiles)
        {
            var sourceText = await File.ReadAllTextAsync(file);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(sourceText, path: file));
        }

        // 3. Get references to Unity and .NET DLLs.
        // This is a simplification. A robust implementation would parse .csproj files
        // or find the Unity editor installation path.
        var references = new List<MetadataReference>
        {
            // Basic .NET references
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),

            // Placeholder for Unity-specific references.
            // TODO: Dynamically locate these from the Unity project/editor install.
            // For now, these can be hardcoded paths if available during development.
            // Example: MetadataReference.CreateFromFile("path/to/UnityEngine.dll")
        };

        // 4. Create and return the compilation.
        return CSharpCompilation.Create(
            "UnityProjectCompilation",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    // Other analysis methods from the spec (e.g., AnalyzeMonoBehaviour)
    // should be stubbed out for now, as they are not required for Phase 1.
}
