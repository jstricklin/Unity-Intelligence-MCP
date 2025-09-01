using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Utils;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;
using UnityIntelligenceMCP.Unity.Services.Contracts;

namespace UnityIntelligenceMCP.Tools.GameObjects
{
    public class GetGameObjectInfoTool : GameObjectTool
    {
        public override string CommandName => "fetch_gameobject_info";
        protected override bool findTarget { get; set; } = true;

        public GetGameObjectInfoTool(IGameObjectService gameObjectService, IComponentService componentService)
        {
            GameObjectService = gameObjectService;
            ComponentService = componentService;
        }

        // TODO consider multiple here
        protected override ToolResponse ExecuteOnMainThread(GameObject target, JObject parameters)
        {

            var results = new JArray();
            var options = parameters["options"] as JObject ?? new JObject();

            // foreach (var target in targets)
            // {
            //     results.Add(BuildGameObjectInfo(target, options));
            // }
            // var responseData = new JObject
            // {
            //     ["success"] = true,
            //     ["gameObjectData"] = results
            //     ["count"] = targets.Count
            // };
            var responseData = new JObject
            {
                ["success"] = true,
                ["gameObjectData"] = BuildGameObjectInfo(target, options)
                // ["count"] = targets.Count
            };
            
            return ToolResponse.SuccessResponse($"Found {target.name}.", responseData);
            // return ToolResponse.SuccessResponse($"Found {targets.Count} matching GameObjects.", responseData);
        }
        
        private JObject BuildGameObjectInfo(GameObject go, JObject options)
        {
            var info = new JObject();
            
            info["basic"] = new JObject
            {
                ["name"] = go.name,
                ["guid"] = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString(),
                ["instanceId"] = go.GetInstanceID(),
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["layerName"] = LayerMask.LayerToName(go.layer),
                ["active"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["scene"] = go.scene.name
            };

            if (options.Value<bool?>("includeTransform") ?? true)
            {
                info["transform"] = new JObject
                {
                    ["position"] = VectorParser.JObjectFromVector3(go.transform.position),
                    // ["localPosition"] = VectorParser.JObjectFromVector3(go.transform.localPosition),
                    ["rotation"] = VectorParser.JObjectFromQuaternion(go.transform.rotation),
                    // ["eulerAngles"] = VectorParser.JObjectFromVector3(go.transform.eulerAngles),
                    ["localScale"] = VectorParser.JObjectFromVector3(go.transform.localScale),
                    // ["lossyScale"] = VectorParser.JObjectFromVector3(go.transform.lossyScale)
                };
            }

            if (options.Value<bool?>("includeComponents") ?? true)
            {
                var componentFilter = options["componentFilter"]?.Values<string>().ToList();
                var components = go.GetComponents<Component>();
                var componentArray = new JArray();

                foreach (var component in components)
                {
                    if (component == null) continue;
                    var typeName = component.GetType().FullName;
                    if (componentFilter == null || !componentFilter.Any() || componentFilter.Contains(typeName) || componentFilter.Contains(component.GetType().Name))
                    {
                        var componentInfo = new JObject
                        {
                            ["type"] = component.GetType().Name,
                            ["typeName"] = typeName,
                            ["guid"] = GlobalObjectId.GetGlobalObjectIdSlow(component).ToString()
                        };
                        componentArray.Add(componentInfo);
                    }
                }
                info["components"] = componentArray;
            }
            
            if (options.Value<bool?>("includeHierarchy") ?? false)
            {
                var hierarchy = new JObject
                {
                    ["childCount"] = go.transform.childCount,
                    ["siblingIndex"] = go.transform.GetSiblingIndex()
                };

                if (go.transform.parent != null)
                {
                    hierarchy["parent"] = new JObject
                    {
                        ["name"] = go.transform.parent.name,
                        ["guid"] = GlobalObjectId.GetGlobalObjectIdSlow(go.transform.parent.gameObject).ToString()
                    };
                }

                var children = new JArray();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i);
                    children.Add(new JObject
                    {
                        ["name"] = child.name,
                        ["guid"] = GlobalObjectId.GetGlobalObjectIdSlow(child.gameObject).ToString(),
                        ["active"] = child.gameObject.activeSelf,
                        ["childCount"] = child.childCount
                    });
                }
                hierarchy["children"] = children;
                info["hierarchy"] = hierarchy;
            }

            return info;
        }
    }
}
