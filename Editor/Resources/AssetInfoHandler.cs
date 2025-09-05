using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Editor.Resources.Contracts;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class AssetInfoHandler : IResourceHandler
    {
        public string ResourceURI => "unity://asset/info";

        public Task<ResourceResponse> HandleRequest(JObject parameters)
        {
            var filter = parameters?["filter"]?.Value<string>() ?? "t:Object";
            var searchInFolders = parameters?["searchInFolders"]?.ToObject<string[]>();

            string[] guids;
            if (searchInFolders != null && searchInFolders.Length > 0)
            {
                guids = AssetDatabase.FindAssets(filter, searchInFolders);
            }
            else
            {
                guids = AssetDatabase.FindAssets(filter);
            }

            var assetInfos = guids.Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) return null;

                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset == null) return null;

                return new
                {
                    name = asset.name,
                    type = asset.GetType().FullName,
                    path,
                    guid
                };
            }).Where(info => info != null).ToList();

            return Task.FromResult(ResourceResponse.SuccessResponse(ResourceURI, assetInfos));
        }
    }
}
