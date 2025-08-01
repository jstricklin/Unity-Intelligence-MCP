using System.Collections.Generic;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

public class UnityDocumentChunker : IDocumentChunker
{
    public List<DocumentChunk> ChunkDocument(UnityDocumentationData doc)
    {
        var chunks = new List<DocumentChunk>();
        int chunkIndex = 0;

        if (!string.IsNullOrWhiteSpace(doc.Description))
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex++,
                Title = "Description",
                Text = doc.Description,
                Section = "Overview",
                StartPosition = 0,
                EndPosition = doc.Description.Length
            });
        }

        chunkIndex = AddLinkChunks(chunks, "Properties", doc.Properties, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Public Methods", doc.PublicMethods, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Static Methods", doc.StaticMethods, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Messages", doc.Messages, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Inherited Properties", doc.InheritedProperties, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Inherited Public Methods", doc.InheritedPublicMethods, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Inherited Static Methods", doc.InheritedStaticMethods, chunkIndex);
        chunkIndex = AddLinkChunks(chunks, "Inherited Operators", doc.InheritedOperators, chunkIndex);

        return chunks;
    }

    private int AddLinkChunks(List<DocumentChunk> chunks, string section, List<DocumentationLink> links, int currentIndex)
    {
        if (links == null || links.Count == 0) return currentIndex;

        foreach (var link in links)
        {
            var text = link.Description ?? string.Empty;
            chunks.Add(new DocumentChunk
            {
                Index = currentIndex++,
                Title = link.Title,
                Text = text,
                Section = section,
                StartPosition = 0,
                EndPosition = text.Length
            });
        }
        return currentIndex;
    }
}
