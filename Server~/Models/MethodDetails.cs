using Microsoft.CodeAnalysis;

namespace UnityIntelligenceMCP.Models
{
    public record MethodDetails(
        string Name,
        string ReturnType,
        FileLinePositionSpan Location
    );
}
