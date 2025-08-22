namespace UnityIntelligenceMCP.Models
{
    public class UnityAnalysisSettings
    {
        public string? EDITOR_PATH { get; set; }
        public string? SCRIPTS_DIR { get; set; }
        public string? PROJECT_PATH { get; set; }
        public bool? FORCE_REINDEX { get; set; }
        public int MCP_SERVER_PORT { get; set; } = 5000;
    }
}
