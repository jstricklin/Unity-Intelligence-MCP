using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public record ProjectContext(
        string RootPath,
        IReadOnlyList<ScriptInfo> Scripts,
        IReadOnlyList<DetectedUnityPattern> DetectedPatterns,
        UnityComponentGraph ComponentRelationships
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

}
