using System.Collections.Generic;
using System.Linq;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

public class UnityDocumentChunker : IDocumentChunker
{
    private const int MaxChunkLength = 2000;

    public List<DocumentChunk> ChunkDocument(UnityDocumentationData doc)
    {
        var chunks = new List<DocumentChunk>();
        int chunkIndex = 0;

        AddTextChunks(chunks, doc.Title, doc.Description, "Overview", ref chunkIndex);

        AddLinkChunks(chunks, "Properties", doc.Properties, ref chunkIndex);
        AddLinkChunks(chunks, "Public Methods", doc.PublicMethods, ref chunkIndex);
        AddLinkChunks(chunks, "Static Methods", doc.StaticMethods, ref chunkIndex);
        AddLinkChunks(chunks, "Messages", doc.Messages, ref chunkIndex);
        AddLinkChunks(chunks, "Inherited Properties", doc.InheritedProperties, ref chunkIndex);
        AddLinkChunks(chunks, "Inherited Public Methods", doc.InheritedPublicMethods, ref chunkIndex);
        AddLinkChunks(chunks, "Inherited Static Methods", doc.InheritedStaticMethods, ref chunkIndex);
        AddLinkChunks(chunks, "Inherited Operators", doc.InheritedOperators, ref chunkIndex);
        AddCodeExampleChunks(chunks, "Examples", doc.Examples, ref chunkIndex);

        foreach (var overload in doc.Overloads)
        {
            AddTextChunks(chunks, overload.Declaration, overload.Description, "MethodOverload.Description", ref chunkIndex);
            AddCodeExampleChunks(chunks, $"MethodOverload.{overload.Declaration}", overload.Examples, ref chunkIndex);
            foreach (var param in overload.Parameters)
            {
                AddTextChunks(chunks, param.Name, param.Description, "MethodOverload.Parameter", ref chunkIndex);
            }
        }
        
        return chunks;
    }

    private void AddTextChunks(List<DocumentChunk> chunks, string title, string text, string section, ref int currentIndex)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var subChunks = SplitText(text, MaxChunkLength);
        foreach (var subChunk in subChunks)
        {
            chunks.Add(new DocumentChunk
            {
                Index = currentIndex++,
                Title = title,
                Text = subChunk,
                Section = section,
                StartPosition = 0,
                EndPosition = subChunk.Length
            });
        }
    }
    
    private void AddLinkChunks(List<DocumentChunk> chunks, string section, List<DocumentationLink> links, ref int currentIndex)
    {
        if (links == null || links.Count == 0) return;

        foreach (var link in links)
        {
            AddTextChunks(chunks, link.Title, link.Description, section, ref currentIndex);
        }
    }

    private void AddCodeExampleChunks(List<DocumentChunk> chunks, string section, List<CodeExample> examples, ref int currentIndex)
    {
        if (examples == null || !examples.Any()) return;
        
        foreach (var example in examples)
        {
            var fullText = $"{example.Description}\n\n{example.Code}";
            AddTextChunks(chunks, example.Description, fullText, section, ref currentIndex);
        }
    }

    private static List<string> SplitText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<string>();
        }
        
        if (text.Length <= maxLength)
        {
            return new List<string> { text };
        }

        var chunks = new List<string>();
        var remainingText = text;
        while (remainingText.Length > 0)
        {
            var length = System.Math.Min(maxLength, remainingText.Length);
            chunks.Add(remainingText.Substring(0, length));
            remainingText = remainingText.Substring(length);
        }
        return chunks;
    }
}
