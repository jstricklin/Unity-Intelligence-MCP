using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UnityIntelligenceMCP.Editor.Models
{
    public class ResourceResponse
    {
        [JsonProperty("success")]
        public bool Success { get; private set; }
        [JsonProperty("resource_uri")]
        public string Uri { get; private set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; private set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; private set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public object Error { get; private set; }

        public static ResourceResponse SuccessResponse(string uri, object data = null)
        {
            return new ResourceResponse { Success = true, Uri = uri, Data = data };
        }

        public static ResourceResponse ErrorResponse(string uri, string errorMessage)
        {
            return new ResourceResponse { Success = false, Uri = uri, Error = new { error = errorMessage } };
        }
    }
}
