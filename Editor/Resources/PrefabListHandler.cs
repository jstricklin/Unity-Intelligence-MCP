using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Resources.Contracts;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class PrefabListHandler : IResourceHandler
    {
        public string ResourceURI => "unity://prefabs";

        public Task<ResourceResponse> HandleRequest(JObject parameters)
        {
            var searchPath = parameters?["search_path"]?.Value<string>()?.Trim();
            if (string.IsNullOrEmpty(searchPath))
            {
                searchPath = "Assets";
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
            var prefabs = guids.Select(guid => {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                return new {
                    name = prefab.name,
                    path = path,
                    guid = guid,
                    isVariant = PrefabUtility.IsPartOfVariantPrefab(prefab),
                    parentPath = GetVariantParentPath(prefab)
                };
            }).ToArray();

            var data = new { prefabs, totalCount = prefabs.Length, searchPath };
            return Task.FromResult(ResourceResponse.SuccessResponse(ResourceURI, data));
        }

        private string GetVariantParentPath(GameObject prefab)
        {
            if (!PrefabUtility.IsPartOfVariantPrefab(prefab))
                return null;

            var parentObject = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            return parentObject != null ? AssetDatabase.GetAssetPath(parentObject) : null;
        }
    }
}
