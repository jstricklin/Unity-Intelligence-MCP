## the purpose of this example script is to return chunks of unity docs to be processed into a vector DB for improved semantic search of unity documentation
public class UnityDocumentChunker : IDocumentChunker
{
    public List<DocumentChunk> ChunkDocument(ParsedDocument doc)
    {
        var chunks = new List<DocumentChunk>();
        
        // Chunk by semantic sections
        chunks.Add(new DocumentChunk 
        { 
            Text = $"{doc.Title}\n{doc.Description}", 
            Section = "Overview" 
        });
        
        // Each parameter gets its own chunk
        foreach (var param in doc.Parameters)
        {
            chunks.Add(new DocumentChunk 
            { 
                Text = $"Parameter: {param.Name}\n{param.Description}", 
                Section = "Parameters" 
            });
        }
        
        // Code examples as separate chunks
        foreach (var example in doc.CodeExamples)
        {
            chunks.Add(new DocumentChunk 
            { 
                Text = $"Example:\n{example.Code}\n{example.Explanation}", 
                Section = "Examples" 
            });
        }
        
        return chunks;
    }
}
