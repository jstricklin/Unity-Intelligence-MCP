using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public record UnityMessagesAnalysisResult(
        IReadOnlyList<UnityScriptMessageAnalysis> ScriptAnalyses
    );

    public record UnityScriptMessageAnalysis(
        string ScriptPath,
        IReadOnlyList<UnityMessageInfo> Messages
    );
}
