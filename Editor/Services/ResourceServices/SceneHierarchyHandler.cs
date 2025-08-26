using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class SceneHierarchyHandler : IResourceHandler
    {
        public string ResourceURI => "unity://scene/hierarchy";
        public async Task<ToolResponse> HandleRequest(JObject parameters)
        {
            // TODO consider parameter to retrieve active scene only
            // var scene = SceneManager.GetActiveScene();
            // var hierarchy = scene
            //     .GetRootGameObjects()
            //     .Select(go => AddGameObjectNode(go));

            // return await Task.FromResult(ToolResponse.SuccessResponse("Scene hierarchy retrieved", new JObject {["scene"] = scene.name, ["hierarchy"] = new JArray(hierarchy) }));
            JArray scenes = new JArray();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var hierarchy = await Task.WhenAll(scene
                    .GetRootGameObjects()
                    .Select(async (go) => await AddGameObjectNode(go)));
                scenes.Add(new JObject {
                    ["scene"] = scene.name,
                    ["hierarchy"] = new JArray(hierarchy),
                });
            }
            return ToolResponse.SuccessResponse("Scene hierarchy retrieved", new JObject { ["sceneHierarchies"] = scenes });
        }

        async Task<JObject> AddGameObjectNode(GameObject go)
        {
            return new JObject
            {
                ["name"] = go.name,
                ["instanceId"] = go.GetInstanceID(),
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["tag"] = go.tag,
                ["layer"] = go.layer,
                ["activeSelf"] = go.activeSelf,
                ["children"] = await GetChildrenRecursive(go.transform)
            };
        }

        async Task<JArray> GetChildrenRecursive(Transform parent)
        {
            JArray children = new JArray();
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                children.Add(await AddGameObjectNode(child.gameObject));
            }
            return children;
        }
    }
}