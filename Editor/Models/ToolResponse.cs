using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UnityIntelligenceMCP.Editor.Models
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
        public object Error { get; private set; }

        [JsonProperty("execution_time", NullValueHandling = NullValueHandling.Ignore)]
        public float ExecutionTime { get; private set; }

        public static ToolResponse FinalizeResponse(ToolResponse response, float executeTime)
        {
            response.ExecutionTime = executeTime;
            return response;
        }

        public static ToolResponse SuccessResponse(string message, object data = null)
        {
            return new ToolResponse { Success = true, Message = message, Data = data };
        }

        public static ToolResponse ErrorResponse(string errorMessage)
        {
            return new ToolResponse { Success = false, Error = new { message = errorMessage } };
        }
    }
}
