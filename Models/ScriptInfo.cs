using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityIntelligenceMCP.Models
{
    public record ScriptInfo(
        string Path,
        string ClassName,
        string BaseType,
        [property: JsonIgnore] SemanticModel SemanticModel,
        [property: JsonIgnore] SyntaxTree SyntaxTree,
        UnityScriptAnalysis UnityAnalysis
    );

    public record UnityScriptAnalysis(bool IsMonoBehaviour, List<UnityMessageInfo> UnityMessages);

}
