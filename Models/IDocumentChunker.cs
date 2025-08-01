using UnityIntelligenceMCP.Models.Documentation;

namespace UnityIntelligenceMCP.Models
{
    public interface IDocumentChunker 
    {
        public List<DocumentChunk> ChunkDocument(UnityDocumentationData doc);
    }
}