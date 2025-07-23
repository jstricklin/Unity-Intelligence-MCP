namespace UnityIntelligenceMCP.Models;
public class DocumentChunk
{
    public int Index { get; set; }
    public string Text { get; set; }
    public string Section { get; set; } // "Description", "Parameters", "Example", etc.
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}