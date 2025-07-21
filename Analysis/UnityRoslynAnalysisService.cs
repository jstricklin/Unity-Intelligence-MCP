using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityCodeIntelligence.Analysis
{
    public class UnityRoslynAnalysisService
    {
        public async Task<Compilation> CreateUnityCompilationAsync(string projectPath, CancellationToken cancellationToken = default)
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
            };

            var unityScriptAssembliesPath = Path.Combine(projectPath, "Library", "ScriptAssemblies");
            if (Directory.Exists(unityScriptAssembliesPath))
            {
                foreach (var dll in Directory.GetFiles(unityScriptAssembliesPath, "*.dll"))
                {
                    references.Add(MetadataReference.CreateFromFile(dll));
                }
            }

            var syntaxTrees = new List<SyntaxTree>();
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in csFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceText = await File.ReadAllTextAsync(file, cancellationToken);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: file, cancellationToken: cancellationToken);
                syntaxTrees.Add(syntaxTree);
            }

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
                {
                    // Suppress errors about missing references to proceed with analysis.
                    { "CS1701", ReportDiagnostic.Suppress },
                    { "CS0012", ReportDiagnostic.Suppress },
                    { "CS0246", ReportDiagnostic.Suppress }
                });

            var compilation = CSharpCompilation.Create(
                "UnityProject",
                syntaxTrees,
                references,
                compilationOptions
            );

            return compilation;
        }
    }
}