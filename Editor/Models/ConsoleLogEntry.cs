using System;

namespace UnityIntelligenceMCP.Editor.Models
{
    [Serializable]
    public class ConsoleLogEntry
    {
        public DateTime Timestamp;
        public string LogType;
        public string Message;
        public string SourceFile;
        public int Line;
        public string StackTrace;
    }
}
