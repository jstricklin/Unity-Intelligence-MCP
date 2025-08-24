using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools
{
    public static class ToolValidator
    {
        public static bool TryFindTarget(
            JObject parameters,
            IGameObjectService service,
            out GameObject target,
            out ToolResponse errorResponse)
        {
            target = null;
            errorResponse = null;

            parameters.TryGetValue("target", out var targetToken);
            var targetValue = targetToken?.Value<string>();

            parameters.TryGetValue("instanceId", out var instanceIdToken);
            var instanceIdValue = instanceIdToken?.Value<string>();

            if (string.IsNullOrEmpty(targetValue) && string.IsNullOrEmpty(instanceIdValue))
            {
                errorResponse = ToolResponse.ErrorResponse("At least one of 'target' or 'instanceId' must be provided.");
                return false;
            }
            
            target = service.Find(targetValue, instanceIdValue);
            
            if (target == null)
            {
                errorResponse = ToolResponse.ErrorResponse("GameObject not found with the provided criteria.");
                return false;
            }

            return true;
        }
    }
}
