using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models.Documentation;
using UnityIntelligenceMCP.Models.Database;

namespace UnityIntelligenceMCP.Models
{
    public class UnityDocumentationSource : IDocumentationSource
    {
        private readonly UnityDocumentationData _data;
        private readonly IDocumentChunker _chunker;
        private readonly IEnumerable<DocumentChunk>? _preEmbeddedChunks;

        public string SourceType => "scripting_api";

        public UnityDocumentationSource(UnityDocumentationData data, IDocumentChunker chunker, IEnumerable<DocumentChunk>? preEmbeddedChunks = null)
        {
            _data = data;
            _chunker = chunker;
            _preEmbeddedChunks = preEmbeddedChunks;
        }

        public Task<SemanticDocumentRecord> ToSemanticRecordAsync(IEmbeddingService _embeddingService)
        {
            // If pre-embedded chunks aren't provided, chunk the document now as a fallback.
            var chunks = _preEmbeddedChunks ?? _chunker.ChunkDocument(_data);
            var elements = CreateContentElementsFromChunks(chunks.ToList());

            var record = new SemanticDocumentRecord
            {
                DocKey = System.IO.Path.GetFileNameWithoutExtension(_data.FilePath),
                Title = _data.Title,
                Url = _data.FilePath,
                ConstructType = _data.ConstructType,
                Category = "Scripting API",
                UnityVersion = _data.UnityVersion,
                ContentHash = ComputeContentHash(),
                Embedding = _data.Embedding, // Use the pre-computed embedding.
                Metadata = new List<DocMetadata>
                {
                    new()
                    {
                        MetadataType = "scripting_api",
                        MetadataJson = JsonSerializer.Serialize(new { 
                            inherits = _data.InheritsFrom?.Title, 
                            implementedIn = _data.ImplementedIn?.Title, 
                            implementedInterfaces = _data.ImplementedInterfaces.Select(link => link.Title).ToList()
                            })
                    }
                },
                Elements = elements
            };
            return Task.FromResult(record);
        }

        private List<ContentElement> CreateContentElementsFromChunks(IReadOnlyList<DocumentChunk> chunks)
        {
            var elements = new List<ContentElement>(chunks.Count);
            foreach (var chunk in chunks)
            {
                // This assumes that the chunk's Embedding property has been populated beforehand.
                elements.Add(new ContentElement
                {
                    ElementType = chunk.Section,
                    Title = chunk.Title,
                    Content = chunk.Text,
                    Embedding = chunk.Embedding, // Directly use the pre-computed embedding
                    AttributesJson = JsonSerializer.Serialize(new
                    {
                        chunkIndex = chunk.Index,
                        startPosition = chunk.StartPosition,
                        endPosition = chunk.EndPosition
                    })
                });
            }
            return elements;
        }
        
        private string ComputeContentHash()
        {
            using var sha256 = SHA256.Create();
            // Serialize the entire data object to create a reliable hash.
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = false });
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hash);
        }
    }
}
