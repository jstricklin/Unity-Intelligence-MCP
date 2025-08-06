using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models
{
    public class DocumentGroup
    {
        public long DocId { get; set; }
        public string Title { get; set; } = String.Empty;
        public string Url { get; set; } = String.Empty;
        public string Source { get; set; } = String.Empty;
        public double MaxRelevance { get; set; }
        public List<ChunkResult> TopChunks { get; set; } = new();
    }

    public class ChunkResult
    {
        public long ChunkId { get; set; }
        public string ContentSnippet { get; set; } = "";
        public double Relevance { get; set; }
        public string SectionType { get; set; } = "";
    }
}
