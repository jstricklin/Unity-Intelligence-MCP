using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class GenericCodeChunker
{
    public class CodeChunk
    {
        public string Content { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
    }
    
    public static List<CodeChunk> ChunkCode(string code, int maxTokens = 400, int overlapLines = 3)
    {
        var chunks = new List<CodeChunk>();
        var lines = code.Split('\n').Select(l => l.TrimEnd()).ToArray();
        
        int currentStart = 0;
        
        while (currentStart < lines.Length)
        {
            var chunk = CreateChunk(lines, currentStart, maxTokens, overlapLines);
            if (chunk != null && !string.IsNullOrWhiteSpace(chunk.Content))
            {
                chunks.Add(chunk);
                currentStart = chunk.EndLine - overlapLines + 1; // Start next chunk with overlap
            }
            else
            {
                break;
            }
        }
        
        return chunks;
    }
    
    static CodeChunk CreateChunk(string[] lines, int startIndex, int maxTokens, int overlapLines)
    {
        if (startIndex >= lines.Length) return null;
        
        var chunkLines = new List<string>();
        int currentTokens = 0;
        int endIndex = startIndex;
        
        // Strategy: Build chunk until we hit a good breaking point near maxTokens
        for (int i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineTokens = EstimateTokens(line);
            
            // Always include at least one line
            if (i == startIndex || currentTokens + lineTokens <= maxTokens)
            {
                chunkLines.Add(line);
                currentTokens += lineTokens;
                endIndex = i;
            }
            else
            {
                // We've exceeded maxTokens, look for a good breaking point
                var breakPoint = FindBreakingPoint(lines, i, maxTokens - currentTokens);
                if (breakPoint > i)
                {
                    // Found a good break point, include lines up to it
                    for (int j = i; j <= breakPoint && j < lines.Length; j++)
                    {
                        chunkLines.Add(lines[j]);
                        endIndex = j;
                    }
                }
                break;
            }
        }
        
        return new CodeChunk
        {
            Content = string.Join("\n", chunkLines),
            StartLine = startIndex + 1, // 1-based line numbers
            EndLine = endIndex + 1
        };
    }
    
    static int FindBreakingPoint(string[] lines, int currentIndex, int remainingTokens)
    {
        // Look ahead for good breaking points within remaining token budget
        int lookAhead = Math.Min(5, lines.Length - currentIndex - 1); // Max 5 lines ahead
        
        for (int i = 0; i < lookAhead; i++)
        {
            int lineIndex = currentIndex + i;
            if (lineIndex >= lines.Length) break;
            
            var line = lines[lineIndex].Trim();
            var lineTokens = EstimateTokens(lines[lineIndex]);
            
            if (lineTokens > remainingTokens) break; // Would exceed budget
            
            // Priority 1: End of blocks (closing braces)
            if (IsBlockEnd(line))
            {
                return lineIndex;
            }
        }
        
        // Priority 2: Empty lines (natural breaks)
        for (int i = 0; i < lookAhead; i++)
        {
            int lineIndex = currentIndex + i;
            if (lineIndex >= lines.Length) break;
            
            if (EstimateTokens(lines[lineIndex]) > remainingTokens) break;
            
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                return lineIndex;
            }
        }
        
        // Priority 3: End of statements (semicolons, specific keywords)
        for (int i = 0; i < lookAhead; i++)
        {
            int lineIndex = currentIndex + i;
            if (lineIndex >= lines.Length) break;
            
            if (EstimateTokens(lines[lineIndex]) > remainingTokens) break;
            
            var line = lines[lineIndex].Trim();
            if (IsStatementEnd(line))
            {
                return lineIndex;
            }
        }
        
        // No good break point found, return current position
        return currentIndex;
    }
    
    static bool IsBlockEnd(string line)
    {
        var trimmed = line.Trim();
        
        // Common block endings across languages
        return trimmed == "}" ||           // C-style languages
               trimmed == "};" ||          // C-style with semicolon
               trimmed.EndsWith("};") ||   // Object/array endings
               trimmed == "end" ||         // Ruby, VB, etc.
               trimmed == "fi" ||          // Shell scripts
               trimmed == "done" ||        // Shell loops
               trimmed.StartsWith("</") || // XML/HTML closing tags
               (trimmed.StartsWith("}") && (trimmed.Contains("catch") || trimmed.Contains("finally"))); // try-catch blocks
    }
    
    static bool IsStatementEnd(string line)
    {
        var trimmed = line.Trim();
        
        return trimmed.EndsWith(";") ||                    // Most C-style statements
               trimmed.EndsWith(":") ||                    // Python, YAML
               (trimmed.StartsWith("//") && trimmed.Length < 50) ||  // Short comments
               (trimmed.StartsWith("#") && trimmed.Length < 50) ||   // Python/shell comments
               (trimmed.StartsWith("*") && trimmed.Length < 50) ||   // Documentation comments
               trimmed.EndsWith("*/") ||                   // End of block comment
               trimmed.StartsWith("import ") ||            // Python imports
               trimmed.StartsWith("using ") ||             // C# using
               trimmed.StartsWith("include ") ||           // C++ includes
               trimmed.StartsWith("require ");             // Ruby/Node requires
    }
    
    static int EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        
        // Simple token estimation for code:
        // - Whitespace and common separators
        // - Account for code being more token-dense than natural language
        var words = text.Split(new char[] { ' ', '\t', '(', ')', '{', '}', '[', ']', ',', ';', '.' }, 
                              StringSplitOptions.RemoveEmptyEntries);
        
        return Math.Max(1, words.Length + 2); // +2 buffer for code density
    }
    
    // Helper method to preview chunks
    public static void PreviewChunks(string code, int maxTokens = 400, int overlapLines = 3)
    {
        var chunks = ChunkCode(code, maxTokens, overlapLines);
        
        Console.WriteLine($"Code split into {chunks.Count} chunks:\n");
        
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var tokenCount = EstimateTokens(chunk.Content);
            
            Console.WriteLine($"--- Chunk {i + 1} (Lines {chunk.StartLine}-{chunk.EndLine}, ~{tokenCount} tokens) ---");
            Console.WriteLine(chunk.Content);
            Console.WriteLine();
        }
    }
    
    public static void Main()
    {
        // Test with a sample C# code
        string sampleCode = @"using UnityEngine;
using System.Collections;

public class RaycastExample : MonoBehaviour
{
    public LayerMask layerMask;
    public float maxDistance = 100f;
    
    void Start()
    {
        // Initialize raycast parameters
        layerMask = LayerMask.GetMask(""Wall"", ""Character"");
        Debug.Log(""Raycast system initialized"");
    }
    
    void FixedUpdate()
    {
        PerformRaycast();
    }
    
    void PerformRaycast()
    {
        RaycastHit hit;
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        
        if (Physics.Raycast(origin, direction, out hit, maxDistance, layerMask))
        {
            Debug.DrawRay(origin, direction * hit.distance, Color.red);
            ProcessHit(hit);
        }
        else
        {
            Debug.DrawRay(origin, direction * maxDistance, Color.green);
            Debug.Log(""No collision detected"");
        }
    }
    
    void ProcessHit(RaycastHit hit)
    {
        Debug.Log(""Hit: "" + hit.collider.name);
        Debug.Log(""Distance: "" + hit.distance);
        Debug.Log(""Point: "" + hit.point);
        
        // Additional hit processing logic here
        if (hit.collider.CompareTag(""Enemy""))
        {
            Debug.Log(""Enemy detected!"");
        }
    }
}";

        // Preview the chunking
        PreviewChunks(sampleCode, maxTokens: 150, overlapLines: 2);
    }
}
