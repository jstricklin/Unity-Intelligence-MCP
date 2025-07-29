using System.Collections.Generic;
using UnityIntelligenceMCP.Models;

public interface IDocumentChunker
{
    List<DocumentChunk> ChunkDocument(UnityDocumentationData doc);
}
