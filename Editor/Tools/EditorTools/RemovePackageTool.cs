using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class RemovePackageTool : EditorTool
    {
        public override string CommandName => "remove_package";

        public override Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            var packageName = parameters?["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return Task.FromResult(ToolResponse.ErrorResponse("Package 'name' parameter is required."));
            }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                Debug.Log("starting package remove");
                RemoveRequest request = Client.Remove(packageName);
                EditorApplication.update += OnProgress;
                void OnProgress()
                {
                    if (request.Status != StatusCode.InProgress)
                        Debug.Log($"package remove complete. status: {request.Status.ToString()}");
                    if (request.Status == StatusCode.Success)
                    {
                        tcs.TrySetResult(ToolResponse.SuccessResponse($"Successfully removed package: {packageName}"));
                        EditorApplication.update -= OnProgress;
                    }
                    else if (request.Status == StatusCode.Failure)
                    {
                        tcs.TrySetResult(ToolResponse.ErrorResponse($"Failed to remove package '{packageName}': {request.Error.message}"));
                        EditorApplication.update -= OnProgress;
                    }
                }
            };

            return tcs.Task;
        }

    }
}
