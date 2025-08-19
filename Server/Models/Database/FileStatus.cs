namespace UnityIntelligenceMCP.Models.Database
{
    public enum DocumentState
    {
        Pending,
        Processing,
        Processed,
        Failed,
        Deprecated
    }

    public class FileStatus
    {
        public string FilePath { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public DocumentState State { get; set; } = DocumentState.Pending;
    }
}
