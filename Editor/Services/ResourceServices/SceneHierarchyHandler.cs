using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIntelligenceMCP.Tools;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class SceneHierarchyHandler : IResourceHandler
    {
        public string ResourceURI => "unity://scene/hierarchy";
        public ToolResponse HandleRequest(JObject parameters)
        {
            var scene = SceneManager.GetActiveScene();
            var hierarchy = scene
                .GetRootGameObjects()
                .Select(go => AddGameObjectNode(go));

            return ToolResponse.SuccessResponse("Scene hierarchy retrieved", new JObject {["scene"] = scene.name, ["hierarchy"] = new JArray(hierarchy) });
            
            // JArray scenes = new JArray();
            // for (int i = 0; i < SceneManager.sceneCount; i++)
            // {
            //     var scene = SceneManager.GetSceneAt(i);
            //     var hierarchy = scene
            //         .GetRootGameObjects()
            //         .Select(go => AddGameObjectNode(go));
            //     scenes.Add(new JObject {
            //         ["scene"] = scene.name,
            //         ["hierarchy"] = new JArray(hierarchy),
            //     });
            // }

            // return ToolResponse.SuccessResponse("Scene hierarchy retrieved", new JObject { ["sceneHierarchies"] = scenes });
        }

        JObject AddGameObjectNode(GameObject go)
        {
            return new JObject
            {
                ["name"] = go.name,
                ["instanceId"] = go.GetInstanceID(),
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["activeSelf"] = go.activeSelf,
                ["children"] = GetChildrenRecursive(go.transform)
            };
        }

        JArray GetChildrenRecursive(Transform parent)
        {
            JArray children = new JArray();
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                children.Add(AddGameObjectNode(child.gameObject));
            }
            return children;
        }
    }
}