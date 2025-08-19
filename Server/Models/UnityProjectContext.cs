using System.Collections.Generic;
using UnityIntelligenceMCP.Models.Analysis;

namespace UnityIntelligenceMCP.Models
{
    public record ProjectContext(
        string RootPath,
        IReadOnlyList<ScriptInfo> Scripts,
        IReadOnlyList<DetectedUnityPattern> DetectedPatterns,
        UnityComponentGraph ComponentRelationships,
        DependencyGraph Dependencies
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
