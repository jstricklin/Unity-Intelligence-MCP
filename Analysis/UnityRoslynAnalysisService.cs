using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityCodeIntelligence.Analysis
{
    public class UnityRoslynAnalysisService
    {
        // A more robust implementation would locate Unity-specific assemblies.
        // For now, we'll use a basic set of references to enable analysis.
        private static readonly IReadOnlyList<MetadataReference> References = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        public async Task<Compilation> CreateUnityCompilationAsync(string projectPath, CancellationToken cancellationToken = default)
        {
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
                References,
                compilationOptions
            );

            return compilation;
        }
    }
}
