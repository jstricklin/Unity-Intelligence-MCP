using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityIntelligenceMCP.Models
{
    public record PatternSearchRequest(string ProjectPath, List<string> PatternTypes);

    public record DetectedPattern(string PatternName, string FilePath, string ClassName);

    public record ComponentRequest(string ProjectPath);

    public record PatternMetricRequest(string ProjectPath);

    public record PatternMetrics(Dictionary<string, int> PatternCounts);
    public record UnityProjectAnalysisRequest(string ProjectPath);

    public record UnityMessageRequest
    {
        [JsonPropertyName("script_paths")]
        public required List<string> ScriptPaths { get; init; }
    }
}
