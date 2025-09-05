using System;
namespace UnityIntelligenceMCP.Editor.Models
{
    [Serializable]
    public class ConsoleLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string LogType { get; set; }
        public string Message { get; set; }
        public string SourceFile { get; set; }
        public int Line { get; set; }
        public string StackTrace { get; set; }
    }
}
