namespace UnityIntelligenceMCP.Models
{
    public class UnityAnalysisSettings
    {
        public string? INSTALL_ROOT { get; set; }
        public string? EDITOR_PATH { get; set; }
        public string? PROJECT_PATH { get; set; }
        public bool? FORCE_REINDEX { get; set; }
        public int MCP_SERVER_PORT { get; set; } = 5000;
    }
}
