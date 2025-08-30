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

        protected GameObjectTool() {}

        protected GameObjectTool(IGameObjectService gameObjectService)
        {
            GameObjectService = gameObjectService;
        }

        public override async Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (!ToolValidator.TryFindTarget(parameters, GameObjectService, out var target, out var errorResponse))
            {
                return errorResponse;
            }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    var response = ExecuteOnMainThread(target, parameters);
                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            return await tcs.Task;
        }

        protected abstract ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters);
    }
}
