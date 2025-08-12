using System.Collections.Generic;
using System.Linq;
using UnityIntelligenceMCP.Models;
using UnityIntelligenceMCP.Models.Documentation;

public class UnityDocumentChunker : IDocumentChunker
{
    private const int MaxTokens = 400; // A conservative token limit to avoid errors
    private const int CharsPerToken = 3;
    private const int TargetChars = MaxTokens * CharsPerToken; // ~1000 chars
    private const int OverlapTokens = 50;
    private const int OverlapChars = OverlapTokens * CharsPerToken; // ~200 chars

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

        var subChunks = SplitText(text, TargetChars);
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
            var combinedLength = (example.Description?.Length ?? 0) + (example.Code?.Length ?? 0) + 2;
            if (combinedLength <= TargetChars)
            {
                var fullText = $"{example.Description}\n\n{example.Code}";
                chunks.Add(new DocumentChunk
                {
                    Index = currentIndex++,
                    Title = example.Description,
                    Text = fullText,
                    Section = section,
                    StartPosition = 0,
                    EndPosition = fullText.Length
                });
            }
            else
            {
                // Description might need chunking
                AddTextChunks(chunks, example.Description, example.Description, section, ref currentIndex);
                
                // Chunk the code separately using the new method
                var codeChunks = SplitCode(example.Code, TargetChars);
                foreach (var codeChunk in codeChunks)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Index = currentIndex++,
                        Title = example.Description, // Use description as context title
                        Text = codeChunk,
                        Section = section,
                        StartPosition = 0,
                        EndPosition = codeChunk.Length
                    });
                }
            }
        }
    }

    private static List<string> SplitText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return new List<string> { text };
        }
    
        var chunks = new List<string>();
        int startIndex = 0;
    
        while (startIndex < text.Length)
        {
            int endIndex = System.Math.Min(startIndex + maxLength, text.Length);
    
            // If not the last chunk, find a natural boundary near the end of the chunk
            if (endIndex < text.Length)
            {
                // Search backwards from the end of the chunk for a sentence terminator
                int lastSentenceEnd = text.LastIndexOfAny(new[] { '.', '!', '?' }, endIndex - 1, endIndex - startIndex);
    
                if (lastSentenceEnd > startIndex)
                {
                    endIndex = lastSentenceEnd + 1; // Split after the punctuation
                }
            }
    
            chunks.Add(text.Substring(startIndex, endIndex - startIndex).Trim());
    
            if (endIndex >= text.Length)
            {
                break;
            }
    
            // Set the start of the next chunk to create an overlap
            startIndex = System.Math.Max(startIndex + 1, endIndex - OverlapChars);
        }
    
        return chunks;
    }

    private static List<string> SplitCode(string code, int maxLength)
    {
        if (string.IsNullOrEmpty(code) || code.Length <= maxLength)
        {
            return new List<string> { code };
        }
    
        const int overlapLines = 3;
        var lines = code.Split('\n');
        var chunks = new List<string>();
        int currentLineIndex = 0;
    
        while (currentLineIndex < lines.Length)
        {
            var chunkBuilder = new System.Text.StringBuilder();
            int endLineIndex = currentLineIndex;
    
            // Build chunk until it's full
            for (int i = currentLineIndex; i < lines.Length; i++)
            {
                if (chunkBuilder.Length + lines[i].Length + 1 > maxLength && chunkBuilder.Length > 0)
                {
                    break; // Stop before adding the line that would overflow
                }
                chunkBuilder.AppendLine(lines[i]);
                endLineIndex = i;
            }
    
            // Try to find a better breaking point by looking backwards from the last line included
            int finalEndLine = endLineIndex;
            if (endLineIndex < lines.Length - 1) // Only look for better breaks if not the very last chunk
            {
                for (int i = endLineIndex; i > currentLineIndex && i > endLineIndex - 5; i--) // Look back up to 5 lines
                {
                    var trimmedLine = lines[i].Trim();
                    if (trimmedLine == "}" || trimmedLine == "};" || string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        finalEndLine = i;
                        break;
                    }
                }
            }
            
            // Re-build the chunk with the final determined end line
            var finalChunk = new System.Text.StringBuilder();
            for (int i = currentLineIndex; i <= finalEndLine; i++)
            {
                finalChunk.AppendLine(lines[i]);
            }
            chunks.Add(finalChunk.ToString().TrimEnd());
            
            if (finalEndLine >= lines.Length - 1)
            {
                break; // We've processed all lines
            }
    
            // Set the start for the next chunk, ensuring overlap
            currentLineIndex = System.Math.Max(0, finalEndLine + 1 - overlapLines);
        }
        return chunks;
    }
}
