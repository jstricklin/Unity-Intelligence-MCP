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

            if (!parameters.TryGetValue("target", out var targetToken))
            {
                errorResponse = ToolResponse.ErrorResponse("Missing 'target' parameter");
                return false;
            }
            if (!parameters.TryGetValue("searchBy", out var searchByToken))
            {
                errorResponse = ToolResponse.ErrorResponse("Missing 'searchBy' parameter. Use 'name' or 'instanceId'.");
                return false;
            }

            var targetValue = targetToken.Value<string>();
            target = service.Find(targetValue, searchByToken.Value<string>());
            
            if (target == null)
            {
                errorResponse = ToolResponse.ErrorResponse($"GameObject '{targetValue}' not found");
                return false;
            }

            return true;
        }
    }
}
