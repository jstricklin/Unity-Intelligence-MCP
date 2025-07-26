using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UnityIntelligenceMCP.Models
{
    public record PerformanceMonitorRequest(
        [property: JsonPropertyName("duration")] int Duration = 60,
        [property: JsonPropertyName("sample_rate")] int SampleRate = 10,
        [property: JsonPropertyName("metrics")] List<string>? Metrics = null
    );

    public record MemoryAnalysisRequest(
        [property: JsonPropertyName("analysis_type")] string AnalysisType,
        [property: JsonPropertyName("duration")] int? Duration,
        [property: JsonPropertyName("threshold_mb")] int ThresholdMb = 512
    );

    public record RenderingAnalysisRequest(
        [property: JsonPropertyName("frame_count")] int FrameCount = 300,
        [property: JsonPropertyName("include_frame_debugger")] bool IncludeFrameDebugger = false,
        [property: JsonPropertyName("target_fps")] int TargetFps = 60
    );

    public record AssetAuditRequest(
        [property: JsonPropertyName("asset_types")] List<string>? AssetTypes,
        [property: JsonPropertyName("project_path")] string? ProjectPath,
        [property: JsonPropertyName("size_threshold_mb")] int SizeThresholdMb = 10
    );

    public record SystemCompatibilityRequest(
        [property: JsonPropertyName("target_platform")] string TargetPlatform,
        [property: JsonPropertyName("quality_preset")] string QualityPreset = "medium"
    );
}
