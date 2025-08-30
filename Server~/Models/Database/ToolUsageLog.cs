namespace UnityIntelligenceMCP.Models.Database
{
    public class ToolUsageLog
    {
        public string? UsageType { get; set; }
        public string? OperationName { get; set; }
        public string? ParametersJson { get; set; }
        public string? ResultSummaryJson { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool WasSuccessful { get; set; }
        public string? ResourceUri { get; set; }
    }
}
