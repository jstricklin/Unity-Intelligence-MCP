using System;

namespace UnityIntelligenceMCP.Models.Database
{
    public class ContentElementRecord
    {
        public required long DocId { get; set; }       // Foreign key to unity_docs.id
        public required string ElementType { get; set; } // "Method", "Property", "Description", etc.
        public string? Title { get; set; }
        public required string Content { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string AttributesJson { get; set; } = "{}";
    }
}
