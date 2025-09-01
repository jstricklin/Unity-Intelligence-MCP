using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityIntelligenceMCP.Editor.Models;
using UnityEngine;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;
using UnityEditor;

namespace UnityIntelligenceMCP.Tools.GameObjects
{
    public class FindGameObjectsTool : GameObjectTool
    {
        public override string CommandName => "find_gameobjects";
        protected override bool multiple { get; set; } = true;

        public FindGameObjectsTool(IGameObjectService service)
        {
            GameObjectService = service;
        }

        protected override ToolResponse ExecuteOnMainThread(List<GameObject> targets, JObject parameters)
        {
            Selection.objects = targets.ToArray();
            return ToolResponse.SuccessResponse($"Found {targets.Count} GameObjects.", new { instanceIds = targets.Select(go => go.GetInstanceID()) });
        }
    }
}
