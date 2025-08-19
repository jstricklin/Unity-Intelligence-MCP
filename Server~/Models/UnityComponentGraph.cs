using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnityIntelligenceMCP.Models.Analysis;

namespace UnityIntelligenceMCP.Models
{
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
