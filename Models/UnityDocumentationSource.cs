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

        public string SourceType => "scripting_api";

        public UnityDocumentationSource(UnityDocumentationData data, IDocumentChunker chunker)
        {
            _data = data;
            _chunker = chunker;
        }

        public async Task<SemanticDocumentRecord> ToSemanticRecordAsync(IEmbeddingService _embeddingService)
        {
            var chunks = _chunker.ChunkDocument(_data);
            var elements = CreateContentElementsFromChunks(chunks, _embeddingService);

            var record = new SemanticDocumentRecord
            {
                DocKey = System.IO.Path.GetFileNameWithoutExtension(_data.FilePath),
                Title = _data.Title,
                Url = _data.FilePath,
                DocType = "class",
                Category = "Scripting API",
                UnityVersion = _data.UnityVersion,
                ContentHash = ComputeContentHash(),
                Embedding = await _embeddingService.EmbedAsync(_data.Description),
                Metadata = new List<DocMetadata>
                    {
                        new()
                        {
                            MetadataType = "scripting_api",
                            MetadataJson = JsonSerializer.Serialize(new { inherits = _data.InheritsFrom?.Title })
                        }
                    },
                Elements = await elements
            };
            return await Task.FromResult(record);
        }

        private async Task<List<ContentElement>> CreateContentElementsFromChunks(List<DocumentChunk> chunks, IEmbeddingService _embeddingService)
        {
            var elements = new List<ContentElement>();
            foreach (var chunk in chunks)
            {
                elements.Add(new ContentElement
                {
                    ElementType = chunk.Section,
                    Title = chunk.Title,
                    Content = chunk.Text,
                    Embedding = await _embeddingService.EmbedAsync(_data.Description),
                    AttributesJson = JsonSerializer.Serialize(new
                    {
                        chunkIndex = chunk.Index,
                        startPosition = chunk.StartPosition,
                        endPosition = chunk.EndPosition
                    })
                });
            }
            return await Task.FromResult(elements);
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
