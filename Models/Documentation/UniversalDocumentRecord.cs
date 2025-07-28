using System.Collections.Generic;

namespace UnityIntelligenceMCP.Models.Documentation
{
    public class SemanticDocumentRecord
    {
        public string DocKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? DocType { get; set; }
        public string? Category { get; set; }
        public string? UnityVersion { get; set; }
        public string? ContentHash { get; set; }
        public byte[]? TitleEmbedding { get; set; }
        public byte[]? SummaryEmbedding { get; set; }
        public List<DocMetadata> Metadata { get; set; } = new();
        public List<ContentElement> Elements { get; set; } = new();
    }
}
