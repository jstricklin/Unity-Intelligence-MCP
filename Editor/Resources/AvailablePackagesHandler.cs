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
    public class AvailablePackagesHandler : IResourceHandler
    {
        public string ResourceURI => "unity://packages/available";

        public Task<ResourceResponse> HandleRequest(JObject parameters)
        {
            var tcs = new TaskCompletionSource<ResourceResponse>();

            EditorApplication.delayCall += () =>
            {
                try
                {
                    tcs.TrySetResult(ResourceResponse.SuccessResponse(ResourceURI, UnityPackageService.GetAvailablePackages()));
                }
                catch (Exception ex)
                {
                    tcs.TrySetResult(ResourceResponse.ErrorResponse(ResourceURI, $"Failed to get available packages: {ex.ToString()}"));
                }
            };

            return tcs.Task;
        }
    }
}
