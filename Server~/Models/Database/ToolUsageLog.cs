namespace UnityIntelligenceMCP.Models.Database
{
    public class ToolUsageLog
    {
        public string ToolName { get; set; }
        public string ParametersJson { get; set; }
        public string ResultSummaryJson { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool WasSuccessful { get; set; }
        public long? PeakProcessMemoryMb { get; set; }
    }
}
