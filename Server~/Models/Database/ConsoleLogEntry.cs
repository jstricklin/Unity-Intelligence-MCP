using System;
using UnityIntelligenceMCP.Core.Data.Contracts;

namespace UnityIntelligenceMCP.Models.Database
{
    public class ConsoleLogEntry : IDbWorkItem
    {
        public DateTime Timestamp { get; set; }
        public string LogType { get; set; } = "";
        public string Message { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public int Line { get; set; }
        public string StackTrace { get; set; } = "";
    }
}
