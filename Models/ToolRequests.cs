using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public record PatternSearchRequest(string ProjectPath, List<string> PatternTypes);

    public record DetectedPattern(string PatternName, string FilePath, string ClassName);

    public record ComponentRequest(string ProjectPath);

    public record PatternMetricRequest(string ProjectPath);

    public record PatternMetrics(Dictionary<string, int> PatternCounts);
}
