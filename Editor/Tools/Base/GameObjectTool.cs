using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.Base
{
    public abstract class GameObjectTool : EditorTool
    {
        protected IGameObjectService GameObjectService;
        protected IComponentService ComponentService;
        protected virtual bool findTarget { get; set; } = true;
        protected GameObject target = null;
        protected GameObjectTool() {}

        protected GameObjectTool(IGameObjectService gameObjectService, IComponentService componentService)
        {
            GameObjectService = gameObjectService;
            ComponentService = componentService;
        }

        public override async Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (findTarget && !ToolValidator.TryFindTarget(parameters, GameObjectService, out target, out var errorResponse))
            {
                return errorResponse;
            }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    ToolResponse response;
                    if (target != null)
                        response = ExecuteOnMainThread(target, parameters);
                    else
                        response = ExecuteOnMainThread(parameters);
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            return await tcs.Task;
        }

        protected virtual ToolResponse ExecuteOnMainThread(JObject parameters) { throw new System.NotImplementedException(); }
        protected virtual ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters) { throw new System.NotImplementedException(); }
    }
}
