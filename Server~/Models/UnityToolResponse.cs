using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnityIntelligenceMCP.Models 
{
    [Serializable]
    class UnityToolResponse 
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string? Message { get; set; } = null;
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Error { get; set; }
        public static string ParseResponse(string toolResponse)
        {
            var responseData = JsonSerializer.Deserialize<UnityToolResponse>(toolResponse);
            return JsonSerializer.Serialize(responseData);
        }
    }
}