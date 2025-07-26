using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public class UnityComponentGraph
    {
        public Dictionary<string, List<ComponentRelationship>> Nodes { get; } = new();
        
        public void AddNode(string className, List<ComponentRelationship> relationships)
        {
            Nodes[className] = relationships;
        }
    }
}
