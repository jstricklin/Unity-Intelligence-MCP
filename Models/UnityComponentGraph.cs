using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UnityIntelligenceMCP.Models
{
    // Placeholder records based on the spec, allowing the new analyzer to compile.
    // A more complete implementation of these will be part of a larger refactor.
    public record ScriptInfo(
        string ClassName,
        SemanticModel SemanticModel,
        SyntaxTree SyntaxTree,
        UnityScriptAnalysis UnityAnalysis
    );
    public record UnityScriptAnalysis(bool IsMonoBehaviour, List<UnityMessageInfo> UnityMessages);
    public record UnityMessageInfo(string MessageName);

    public class ComponentNode
    {
        public List<ComponentRelationship> Relationships { get; }
        public UnityScriptAnalysis Analysis { get; }

        public ComponentNode(List<ComponentRelationship> relationships, UnityScriptAnalysis analysis)
        {
            Relationships = relationships;
            Analysis = analysis;
        }
    }

    public class UnityComponentGraph
    {
        public Dictionary<string, ComponentNode> Nodes { get; } = new();
        
        public void AddComponent(string className, List<ComponentRelationship> relationships, UnityScriptAnalysis unityAnalysis)
        {
            Nodes[className] = new ComponentNode(relationships, unityAnalysis);
        }
    }
}
