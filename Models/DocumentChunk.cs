namespace UnityIntelligenceMCP.Models
{
    public class DocumentChunk
    {
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
    }

}
