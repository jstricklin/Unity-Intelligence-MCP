using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityIntelligenceMCP.Editor.Models;
using UnityEngine;

namespace UnityIntelligenceMCP.Tools
{
    public static class ToolValidator
    {
        public static bool TryFindTargets(
            JObject parameters,
            IGameObjectService service,
            out List<GameObject> targets,
            out ToolResponse errorResponse)
        {
            targets = new List<GameObject>();
            var foundTargets = new HashSet<GameObject>();
            errorResponse = null;

            parameters.TryGetValue("targets", out var targetToken);
            var targetValues = targetToken?.Value<string>();

            parameters.TryGetValue("instanceIds", out var instanceIdToken);
            var instanceIdValues = instanceIdToken?.Value<string>();

            if (string.IsNullOrEmpty(targetValues) && string.IsNullOrEmpty(instanceIdValues))
            {
                errorResponse = ToolResponse.ErrorResponse("At least one of 'targets' or 'instanceIds' must be provided.");
                return false;
            }
            
            foreach (var instanceId in instanceIdValues.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                if (service.Find(null, instanceId) is GameObject go)
                    foundTargets.Add(go);
            }
            foreach (var target in targetValues.Split(",", StringSplitOptions.RemoveEmptyEntries))
            {
                if (service.Find(target, null) is GameObject go)
                    foundTargets.Add(go);
            }
            
            if (foundTargets.Count == 0)
            {
                errorResponse = ToolResponse.ErrorResponse("No GameObjects found.");
                return false;
            } else
                targets.AddRange(foundTargets);

            return true;
        }

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
                errorResponse = ToolResponse.ErrorResponse("GameObject not found.");
                return false;
            }

            return true;
        }
    }
}
