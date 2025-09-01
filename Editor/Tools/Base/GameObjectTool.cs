using System;
using System.Collections.Generic;
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
        protected virtual bool multiple { get; set; } = false;
        protected List<GameObject> targets = null;
        protected GameObject target = null;
        protected GameObjectTool() {}

        protected GameObjectTool(IGameObjectService gameObjectService, IComponentService componentService)
        {
            GameObjectService = gameObjectService;
            ComponentService = componentService;
        }

        public override async Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            if (findTarget && GameObjectService == null)
            {
                return ToolResponse.ErrorResponse("Editor Tool improperly configured - GameObjectService must be injected when 'findTarget' is true.");
            }
            if (findTarget)
            {
                if (multiple)
                {
                    if (!ToolValidator.TryFindTargets(parameters, GameObjectService, out targets, out var errorResponseMultiple))
                        return errorResponseMultiple;
                }
                else 
                {
                    if (!ToolValidator.TryFindTarget(parameters, GameObjectService, out target, out var errorResponse))
                        return errorResponse;
                }
            }
            // if (findTarget && !ToolValidator.TryFindTargets(parameters, GameObjectService, out targets, out var errorResponse))
            // {
            //     return errorResponse;
            // }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    ToolResponse response;
                    if (targets != null && targets.Count > 0)
                        response = ExecuteOnMainThread(targets, parameters);
                    else if (target != null)
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
        protected virtual ToolResponse ExecuteOnMainThread(List<GameObject> targets, JObject parameters) { throw new System.NotImplementedException(); }
    }
}
