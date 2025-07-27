using System;

namespace UnityIntelligenceMCP.Models.Documentation
{
    public class ContentElement
    {
        public string ElementType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public ReadOnlyMemory<float>? ElementEmbedding { get; set; }
        public string? AttributesJson { get; set; }
    }
}
