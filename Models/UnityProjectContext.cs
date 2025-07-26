using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public record ProjectContext(
        string RootPath,
        IReadOnlyList<ScriptInfo> Scripts,
        IReadOnlyList<DetectedUnityPattern> DetectedPatterns,
        UnityComponentGraph ComponentRelationships,
        DependencyGraph Dependencies
    );

    public record ScriptInfo(
        string Path,
        string ClassName,
        string BaseType
    );

    public record DetectedUnityPattern(
        string PatternName,
        string ScriptPath,
        string ClassName,
        float Confidence = 1.0f
    );

    public record ComponentRelationship(
        string TargetComponent,
        string RelationshipType
    );

    public class UnityComponentGraph
    {
        public Dictionary<string, List<ComponentRelationship>> Nodes { get; } = new();
        
        public void AddNode(string className, List<ComponentRelationship> relationships)
        {
            Nodes[className] = relationships;
        }
    }

    // Request models for tools
    public record UnityProjectAnalysisRequest(string ProjectPath);
}
