using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityIntelligenceMCP.Core.RoslynServices;
using UnityIntelligenceMCP.Models;

namespace UnityIntelligenceMCP.Core.Analysis.Project
{
    public class UnityMessageAnalyzer : IUnityMessageAnalyzer
    {
        private readonly UnityRoslynAnalysisService _roslynService;
        private static readonly ImmutableHashSet<string> UnityMessageNames = new HashSet<string>
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
            "OnEnable", "OnDisable", "OnDestroy",
            "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
            "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit",
            "OnGUI", "OnRenderObject"
        }.ToImmutableHashSet();

        public UnityMessageAnalyzer(UnityRoslynAnalysisService roslynService)
        {
            _roslynService = roslynService;
        }

        public async Task<UnityMessagesAnalysisResult> AnalyzeMessagesAsync(string projectPath, IEnumerable<string> scriptPaths, CancellationToken cancellationToken)
        {
            var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, cancellationToken);
            var scriptAnalyses = new List<UnityScriptMessageAnalysis>();

            var absoluteScriptPaths = scriptPaths.Select(p => System.IO.Path.GetFullPath(p, projectPath)).ToHashSet();

            var monoBehaviourSymbol = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");
            if (monoBehaviourSymbol == null)
            {
                // Cannot perform analysis without MonoBehaviour type, return empty result.
                return new UnityMessagesAnalysisResult(new List<UnityScriptMessageAnalysis>());
            }

            foreach (var tree in compilation.SyntaxTrees.Where(t => absoluteScriptPaths.Contains(t.FilePath)))
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                var classNodes = tree.GetRoot(cancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classNode in classNodes)
                {
                    if (semanticModel.GetDeclaredSymbol(classNode, cancellationToken) is not INamedTypeSymbol classSymbol) continue;
                    if (!IsSubclassOf(classSymbol, monoBehaviourSymbol)) continue;

                    var messages = AnalyzeClassMethods(classSymbol);
                    if (messages.Any())
                    {
                        scriptAnalyses.Add(new UnityScriptMessageAnalysis(tree.FilePath, messages));
                    }
                }
            }

            return new UnityMessagesAnalysisResult(scriptAnalyses);
        }

        private List<UnityMessageInfo> AnalyzeClassMethods(INamedTypeSymbol classSymbol)
        {
            var messages = new List<UnityMessageInfo>();
            foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (!UnityMessageNames.Contains(member.Name)) continue;

                var syntaxReference = member.DeclaringSyntaxReferences.FirstOrDefault();
                if (syntaxReference?.GetSyntax() is not MethodDeclarationSyntax methodSyntax) continue;

                System.Enum.TryParse<UnityMessageType>(member.Name, out var messageType);
                var isEmpty = !methodSyntax.Body?.Statements.Any() ?? true;
                var hasPerformanceImplications = HasPerformanceImplications(methodSyntax, messageType);

                messages.Add(new UnityMessageInfo(
                    member.Name,
                    new MethodDetails(member.Name, member.ReturnType.ToDisplayString(), methodSyntax.GetLocation().GetMappedLineSpan()),
                    messageType,
                    isEmpty,
                    hasPerformanceImplications
                ));
            }
            return messages;
        }

        private bool IsSubclassOf(INamedTypeSymbol type, INamedTypeSymbol baseTypeSymbol)
        {
            var current = type;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseTypeSymbol))
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private bool HasPerformanceImplications(MethodDeclarationSyntax method, UnityMessageType type)
        {
            if (type != UnityMessageType.Update && type != UnityMessageType.FixedUpdate && type != UnityMessageType.LateUpdate)
            {
                return false;
            }
            // A simple check for expensive method calls inside Update loops.
            return method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(inv =>
                inv.ToString().Contains("GetComponent") ||
                inv.ToString().Contains("FindObject"));
        }
    }
}
