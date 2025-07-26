using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    // These models represent the complex data returned from a live Unity instance.
    // A full implementation would define their properties in detail.
    public record PerformanceData(Dictionary<string, object> Metrics);
    public record MemoryAnalysis(string Report);
    public record RenderingAnalysis(string Report);
    public record AssetAudit(string Report);
    public record SystemCompatibility(string Report);
}
