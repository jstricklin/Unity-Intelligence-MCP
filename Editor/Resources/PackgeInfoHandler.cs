using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityIntelligenceMCP.Editor.Resources.Contracts;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;
using System;

namespace UnityIntelligenceMCP.Editor.Services.ResourceServices
{
    public class PackageInfoHandler : IResourceHandler
    {
        // TODO update resource URI template parsing to properly match incoming template URIs
        public string ResourceURI => "unity://packages/info/{package_name}";

        public Task<ResourceResponse> HandleRequest(JObject parameters)
        {
            var packageName = parameters?["package_name"]?.Value<string>()?.Trim();
            if (string.IsNullOrEmpty(packageName))
            {
                return Task.FromResult(ResourceResponse.ErrorResponse(ResourceURI, "Required 'package_name' parameter missing from request."));
            }
            var tcs = new TaskCompletionSource<ResourceResponse>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    tcs.TrySetResult(ResourceResponse.SuccessResponse(ResourceURI, UnityPackageService.GetPackageInfo(packageName)));
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(ResourceResponse.ErrorResponse(ResourceURI, $"Failed to get package info: {ex.ToString()}"));
                }
            };

            return tcs.Task;
        }
    }
}
