using UnityIntelligenceMCP.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityIntelligenceMCP.Models.Analysis;

namespace UnityIntelligenceMCP.Core.Analysis.Relationships
{
    public class UnityComponentRelationshipAnalyzer
    {
        public UnityComponentGraph AnalyzeRelationships(IEnumerable<ScriptInfo> scripts)
        {
            var graph = new UnityComponentGraph();
            var firstScript = scripts.FirstOrDefault(s => s.SemanticModel != null);
            if (firstScript == null) return graph;

            // All scripts share the same compilation, so we can get MonoBehaviour from the first one.
            var monoBehaviourSymbol = firstScript.SemanticModel.Compilation.GetTypeByMetadataName("UnityEngine.MonoBehaviour");
            if (monoBehaviourSymbol == null) return graph;

            foreach (var script in scripts.Where(s => s.UnityAnalysis.IsMonoBehaviour))
            {
                var relationships = ExtractUnityRelationships(script, monoBehaviourSymbol);
                graph.AddComponent(script.ClassName, relationships, script.UnityAnalysis);
            }
            return graph;
        }

        private List<ComponentRelationship> ExtractUnityRelationships(ScriptInfo script, INamedTypeSymbol monoBehaviourSymbol)
        {
            var walker = new UnityComponentUsageWalker(script.SemanticModel, monoBehaviourSymbol);
            walker.Visit(script.SyntaxTree.GetRoot());
            return walker.Relationships.ToList();
        }

        private class UnityComponentUsageWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly INamedTypeSymbol _monoBehaviourSymbol;
            public readonly HashSet<ComponentRelationship> Relationships = new();

            public UnityComponentUsageWalker(SemanticModel semanticModel, INamedTypeSymbol monoBehaviourSymbol)
            {
                _semanticModel = semanticModel;
                _monoBehaviourSymbol = monoBehaviourSymbol;
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (_semanticModel.GetTypeInfo(node.Declaration.Type).Type is INamedTypeSymbol typeSymbol && IsSubclassOf(typeSymbol, _monoBehaviourSymbol))
                {
                    Relationships.Add(new ComponentRelationship(typeSymbol.Name, "Reference (Field/Property)"));
                }
                base.VisitFieldDeclaration(node);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (_semanticModel.GetTypeInfo(node.Type).Type is INamedTypeSymbol typeSymbol && IsSubclassOf(typeSymbol, _monoBehaviourSymbol))
                {
                    Relationships.Add(new ComponentRelationship(typeSymbol.Name, "Reference (Field/Property)"));
                }
                base.VisitPropertyDeclaration(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (_semanticModel.GetSymbolInfo(node).Symbol is IMethodSymbol { IsGenericMethod: true } methodSymbol)
                {
                    var relationshipType = GetRelationshipType(methodSymbol.Name);
                    if (relationshipType != null)
                    {
                        if (methodSymbol.TypeArguments.FirstOrDefault() is INamedTypeSymbol typeArgSymbol && IsSubclassOf(typeArgSymbol, _monoBehaviourSymbol))
                        {
                            Relationships.Add(new ComponentRelationship(typeArgSymbol.Name, relationshipType));
                        }
                    }
                }
                base.VisitInvocationExpression(node);
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

            private string? GetRelationshipType(string methodName) => methodName switch
            {
                "GetComponent" or "GetComponents" or "GetComponentInChildren" or "GetComponentsInChildren" or "GetComponentInParent" or "GetComponentsInParent" or "FindObjectOfType" or "FindObjectsOfType" => $"Reference ({methodName})",
                "AddComponent" => $"Creation ({methodName})",
                _ => null
            };
        }
    }
}
