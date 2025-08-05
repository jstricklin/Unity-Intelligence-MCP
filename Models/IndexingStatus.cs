namespace UnityIntelligenceMCP.Models
{
    public class IndexingStatus
    {
        public string? UnityVersion { get; set; }
        public string Status { get; set; } = "Unknown";
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public bool IsReady => Status == "Complete";
    }
}
