using System;
using UnityIntelligenceMCP.Core.Data.Contracts;

namespace UnityIntelligenceMCP.Models.Database
{
    public class ConsoleLogEntryBatch
    {
        public List<ConsoleLogEntry> logs { get; set; } = new();
    }
}
