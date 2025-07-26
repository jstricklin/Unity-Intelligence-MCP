using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public record ScriptInfo(
        string Path,
        string ClassName,
        string BaseType,
        SemanticModel SemanticModel,
        SyntaxTree SyntaxTree,
        UnityScriptAnalysis UnityAnalysis
    );

    public record UnityScriptAnalysis(bool IsMonoBehaviour, List<UnityMessageInfo> UnityMessages);

    public record UnityMessageInfo(string MessageName);
}
