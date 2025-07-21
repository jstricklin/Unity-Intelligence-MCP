using UnityCodeIntelligence.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnityCodeIntelligence.Analysis
{
    public class UnityComponentRelationshipAnalyzer
    {
        private readonly UnityRoslynAnalysisService _roslynService;

        public UnityComponentRelationshipAnalyzer(UnityRoslynAnalysisService roslynService)
        {
            _roslynService = roslynService;
        }

        public async Task<UnityComponentGraph> AnalyzeAsync(string projectPath, CancellationToken cancellationToken)
        {
            var compilation = await _roslynService.CreateUnityCompilationAsync(projectPath, cancellationToken);
            return AnalyzeMonoBehaviours(compilation, cancellationToken);
        }

        public UnityComponentGraph AnalyzeMonoBehaviours(Compilation compilation, CancellationToken cancellationToken)
        {
            var graph = new UnityComponentGraph();
            var monoBehaviourSymbol = compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");
            if (monoBehaviourSymbol == null) return graph; // MonoBehaviour not found

            var monoBehaviourClasses = new List<INamedTypeSymbol>();
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);
                foreach (var classDeclaration in tree.GetRoot(cancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is INamedTypeSymbol classSymbol &&
                        IsSubclassOf(classSymbol, monoBehaviourSymbol))
                    {
                        monoBehaviourClasses.Add(classSymbol);
                    }
                }
            }

            foreach (var classSymbol in monoBehaviourClasses)
            {
                var relationships = new HashSet<(string, string)>();
                foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
                {
                    var classNode = syntaxRef.GetSyntax(cancellationToken) as ClassDeclarationSyntax;
                    if (classNode == null) continue;

                    var semanticModel = compilation.GetSemanticModel(classNode.SyntaxTree);

                    // Analyze field and property declarations
                    foreach (var member in classNode.Members)
                    {
                        var typeSyntax = (member as FieldDeclarationSyntax)?.Declaration.Type ?? (member as PropertyDeclarationSyntax)?.Type;
                        if (typeSyntax == null) continue;

                        if (semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type is INamedTypeSymbol typeSymbol && IsSubclassOf(typeSymbol, monoBehaviourSymbol))
                        {
                            relationships.Add((typeSymbol.Name, "Reference (Field/Property)"));
                        }
                    }

                    // Analyze method bodies for GetComponent, AddComponent, etc.
                    foreach (var invocation in classNode.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol { IsGenericMethod: true } methodSymbol)
                        {
                            var relationshipType = GetRelationshipType(methodSymbol.Name);
                            if (relationshipType == null) continue;

                            if (methodSymbol.TypeArguments.FirstOrDefault() is INamedTypeSymbol typeArgSymbol && IsSubclassOf(typeArgSymbol, monoBehaviourSymbol))
                            {
                                relationships.Add((typeArgSymbol.Name, relationshipType));
                            }
                        }
                    }
                }
                graph.AddNode(classSymbol.Name, relationships.Select(r => new ComponentRelationship(r.Item1, r.Item2)).ToList());
            }

            return graph;
        }

        private bool IsSubclassOf(INamedTypeSymbol type, INamedTypeSymbol baseTypeSymbol)
        {
            var current = type.BaseType;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseTypeSymbol)) return true;
                current = current.BaseType;
            }
            return false;
        }

        private string GetRelationshipType(string methodName) => methodName switch
        {
            "GetComponent" or "GetComponents" or "GetComponentInChildren" or "GetComponentsInChildren" or "GetComponentInParent" or "GetComponentsInParent" or "FindObjectOfType" or "FindObjectsOfType" => $"Reference ({methodName})",
            "AddComponent" => $"Creation ({methodName})",
            _ => null
        };
    }
}
