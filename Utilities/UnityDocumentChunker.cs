using UnityIntelligenceMCP.Models;
using System.Collections.Generic;

public class UnityDocumentChunker : IDocumentChunker
{
    private async Task<UnityDocumentationData> ParseDocumentAsync(string filePath)
    {
        var html = await File.ReadAllTextAsync(filePath);
        
        // Parse Unity documentation structure
        // Unity docs have specific patterns we can extract
        return new UnityDocumentationData
        {
            FilePath = filePath,
            Title = ExtractTitle(html),
            Description = ExtractDescription(html),
            Type = DetermineDocumentationType(html),
            Namespace = ExtractNamespace(html),
            UnityVersion = ExtractUnityVersion(html),
            MainContent = ExtractMainContent(html),
            CodeExamples = ExtractCodeExamples(html),
            Parameters = ExtractParameters(html)
        };
    }
    public List<DocumentChunk> ChunkDocument(UnityDocumentationData doc)
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