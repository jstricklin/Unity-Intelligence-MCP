namespace UnityIntelligenceMCP.Models
{
    public class UnityAnalysisSettings
    {
        public string? InstallRoot { get; set; }
        public string? EditorPath { get; set; }
        public string? ProjectPath { get; set; }
        public bool? ForceDocumentationReindex { get; set; }
        public string? ChromaDbUrl { get; set; } // Add this property
    }
}
