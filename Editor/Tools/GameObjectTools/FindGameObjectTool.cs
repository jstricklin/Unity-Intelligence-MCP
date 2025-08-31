using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityEngine;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.GameObjects
{
    public class FindGameObjectTool : GameObjectTool
    {
        public override string CommandName => "find_gameobject";

        public FindGameObjectTool(IGameObjectService service)
        {
            GameObjectService = service;
        }

        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {
            return ToolResponse.SuccessResponse(
                $"Found {target.name}",
                new
                {
                    instanceId = target.GetInstanceID(),
                    position = new
                    {
                        x = target.transform.position.x,
                        y = target.transform.position.y,
                        z = target.transform.position.z
                    },
                    scale = new
                    {
                        x = target.transform.localScale.x,
                        y = target.transform.localScale.y,
                        z = target.transform.localScale.z
                    },
                    rotation = new
                    {
                        x = target.transform.rotation.x,
                        y = target.transform.rotation.y,
                        z = target.transform.rotation.z,
                        w = target.transform.rotation.w
                    }
                }
            );
        }
    }
}
