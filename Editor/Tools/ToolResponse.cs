using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UnityIntelligenceMCP.Tools
{
    public class ToolResponse
    {
        [JsonProperty("success")]
        public bool Success { get; private set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; private set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; private set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public string Error { get; private set; }

        public static ToolResponse SuccessResponse(string message, object data = null)
        {
            return new ToolResponse { Success = true, Message = message, Data = data };
        }

        public static ToolResponse ErrorResponse(string errorMessage)
        {
            return new ToolResponse { Success = false, Error = errorMessage };
        }
    }
}
