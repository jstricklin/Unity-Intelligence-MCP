using System.Collections.Generic;
using UnityIntelligenceMCP.Core.Data;

namespace UnityIntelligenceMCP.Models.Documentation
{
    public class SemanticDocumentRecord : IDbWorkItem
    {
        public string DocKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }

        public string? Namespace { get; set; }

        public string? Url { get; set; }

        public string? DocType { get; set; }

        public string? Category { get; set; }

        public string? UnityVersion { get; set; }

        public string? ContentHash { get; set; }
        
        public string? InheritsFromJson { get; set; }

        public string? ImplementedInJson { get; set; }

        public List<DocMetadata> Metadata { get; set; } = new();
        public List<ContentElement> Elements { get; set; } = new();
    }
}
