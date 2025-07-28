using System.Collections.Generic;
using Microsoft.Extensions.VectorData;

namespace UnityIntelligenceMCP.Models.Documentation
{
    public class SemanticDocumentRecord
    {
        [VectorStoreKey]
        public string DocKey { get; set; } = string.Empty;

        [VectorStoreData]
        public string Title { get; set; } = string.Empty;
        
        [VectorStoreData]
        public string? Description { get; set; }

        [VectorStoreData]
        public string? Namespace { get; set; }

        [VectorStoreData]
        public string? Url { get; set; }

        [VectorStoreData]
        public string? DocType { get; set; }

        [VectorStoreData]
        public string? Category { get; set; }

        [VectorStoreData]
        public string? UnityVersion { get; set; }

        [VectorStoreData]
        public string? ContentHash { get; set; }
        
        [VectorStoreData]
        public string? InheritsFromJson { get; set; }

        [VectorStoreData]
        public string? ImplementedInJson { get; set; }

        // The Dimensions parameter should match the output of your embedding service.
        [VectorStoreVector(Dimensions: 1536)]
        public byte[]? TitleEmbedding { get; set; }

        // The Dimensions parameter should match the output of your embedding service.
        [VectorStoreVector(Dimensions: 1536)]
        public byte[]? SummaryEmbedding { get; set; }

        public List<DocMetadata> Metadata { get; set; } = new();
        public List<ContentElement> Elements { get; set; } = new();
    }
}
