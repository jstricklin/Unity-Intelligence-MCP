using System;
using Newtonsoft.Json;
namespace UnityIntelligenceMCP.Editor.Models
{
    [Serializable]
    public class UnityPackageData
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }
        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }
        [JsonProperty("installed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Installed { get; set; }
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }
    }
}