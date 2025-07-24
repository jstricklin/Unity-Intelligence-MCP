using UnityIntelligenceMCP.Models;
using System.Collections.Generic;
using HtmlAgilityPack;

public class UnityDocumentChunker : IDocumentChunker
{
    private record ExtractionConfig(string Selector, Func<string, string> Transform);
    // This below should extract values to build our document data object
    private static readonly Dictionary<string, ExtractionConfig> ExtractionRules = new ()
    {
        ["Title"] = new (".content h1",
        text => text?.Trim()),
        ["Description"] = new (".description, .summary",
        text => text?.Trim()),
        ["Namespace"] = new (".namespace",
        text => text?.Trim()),
        ["UnityVersion"] = new ("[data-unity-version], .unity-version",
        text => text?.Trim()),
        ["MainContent"] = new (".content .section-content",
        text => text?.Trim()),
    };

    private string ExtractByRule(string html, string ruleName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var config = ExtractionRules[ruleName];
        var node = doc.DocumentNode.SelectSingleNode(config.Selector);
        return config.Transform(node?.InnerText);
    }


    private async Task<UnityDocumentationData> ParseDocumentAsync(string filePath)
    {
        var html = await File.ReadAllTextAsync(filePath);
        
        // Parse Unity documentation structure
        // Unity docs have specific patterns we can extract - TODO: add commented out examples
        return new UnityDocumentationData
        {
            // FilePath = filePath,
            // Title = ExtractByRule(html, "Title"),
            // Description = ExtractByRule(html, "Description"),
            // Namespace = ExtractByRule(html, "Namespace"),
            // UnityVersion = ExtractByRule(html, "UnityVersion"),
            // MainContent = ExtractByRule(html, "MainContent")
            // Type = DetermineDocumentationType(html, "Type"),
            // CodeExamples = ExtractByRules(html),
            // Parameters = ExtractByRules(html)
        };
    }
    public List<DocumentChunk> ChunkDocument(UnityDocumentationData doc)
    {
        var chunks = new List<DocumentChunk>();
        
        // Chunk by semantic sections
        chunks.Add(new DocumentChunk 
        { 
            // Text = $"{doc.Title}\n{doc.Description}", 
            Section = "Overview" 
        });
        
        // Each parameter gets its own chunk
        // foreach (var param in doc.Parameters)
        // {
        //     chunks.Add(new DocumentChunk 
        //     { 
        //         Text = $"Parameter: {param.Name}\n{param.Description}", 
        //         Section = "Parameters" 
        //     });
        // }
        
        // Code examples as separate chunks
        // foreach (var example in doc.CodeExamples)
        // {
        //     chunks.Add(new DocumentChunk 
        //     { 
        //         Text = $"Example:\n{example.Code}\n{example.Explanation}", 
        //         Section = "Examples" 
        //     });
        // }
        
        return chunks;
    }
}