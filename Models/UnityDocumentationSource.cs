using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Core.Semantics;
using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Models
{
    public class UnityDocumentationSource : IDocumentationSource
    {
        private readonly UnityDocumentationData _data;

        public UnityDocumentationSource(UnityDocumentationData data)
        {
            _data = data;
        }

        public string SourceType => "scripting_api";

        public async Task<SemanticDocumentRecord> ToSemanticRecordAsync(IEmbeddingService embeddingService)
        {
            var record = new SemanticDocumentRecord
            {
                DocKey = Path.GetFileNameWithoutExtension(_data.FilePath),
                Title = _data.Title,
                DocType = "class",
                Category = "Scripting API",
                ContentHash = ComputeContentHash()
            };

            var titleContext = $"Unity Scripting API Class: {_data.Title}";
            var summaryContext = $"Description for {_data.Title}: {_data.Description}";

            var titleEmbedding = await embeddingService.EmbedAsync(titleContext);
            var summaryEmbedding = await embeddingService.EmbedAsync(summaryContext);

            record.TitleEmbedding = titleEmbedding;
            record.SummaryEmbedding = summaryEmbedding;

            var metadata = new DocMetadata
            {
                MetadataType = SourceType,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    description = _data.Description,
                    class_name = _data.Title,
                    @namespace = _data.Namespace
                }, new JsonSerializerOptions { WriteIndented = false })
            };
            record.Metadata.Add(metadata);

            record.Elements.AddRange(await CreateContentElements(_data.PublicMethods, "public_method", embeddingService));
            record.Elements.AddRange(await CreateContentElements(_data.Properties, "property", embeddingService));
            record.Elements.AddRange(await CreateContentElements(_data.StaticMethods, "static_method", embeddingService));
            record.Elements.AddRange(await CreateContentElements(_data.Messages, "message", embeddingService));

            return record;
        }

        private async Task<List<ContentElement>> CreateContentElements(IEnumerable<DocumentationLink> links, string elementType, IEmbeddingService embeddingService)
        {
            var elements = new List<ContentElement>();
            foreach (var link in links)
            {
                var elementContext = $"Unity {elementType.Replace('_', ' ')} '{link.Title}' in class {_data.Title}: {link.Description}";
                var embedding = await embeddingService.EmbedAsync(elementContext);

                elements.Add(new ContentElement
                {
                    ElementType = elementType,
                    Title = link.Title,
                    Content = link.Description,
                    ElementEmbedding = embedding?.ToArray(),
                    AttributesJson = JsonSerializer.Serialize(new { link.RelativePath })
                });
            }
            return elements;
        }

        private string ComputeContentHash()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(_data.Title).Append(_data.Description);
            _data.Properties.ForEach(p => stringBuilder.Append(p.Title).Append(p.Description));
            _data.PublicMethods.ForEach(p => stringBuilder.Append(p.Title).Append(p.Description));

            return stringBuilder.ToString().GetHashCode().ToString("X");
        }

        private static byte[]? FloatArrayToByteArray(IReadOnlyCollection<float> floats)
        {
            if (floats == null || floats.Count == 0) return null;
            var byteArray = new byte[floats.Count * sizeof(float)];
            Buffer.BlockCopy(floats.ToArray(), 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}
