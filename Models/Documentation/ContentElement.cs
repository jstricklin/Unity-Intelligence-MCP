using System;
using System.Text.Json.Serialization;

namespace UnityIntelligenceMCP.Models.Documentation
{
    public class ContentElement
    {
        [JsonIgnore]
        public long Id { get; set; }
        public string ElementType { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? AttributesJson { get; set; }
    }
}
