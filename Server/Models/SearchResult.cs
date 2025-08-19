namespace UnityIntelligenceMCP.Models
{
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string ElementType { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public float Similarity { get; set; }
    }
}
