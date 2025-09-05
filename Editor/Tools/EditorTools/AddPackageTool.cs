using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Tools.Base;

namespace UnityIntelligenceMCP.Tools.Editor
{
    public class AddPackageTool : EditorTool
    {
        public override string CommandName => "add_package";

        public override Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            var packageId = parameters?["identifier"]?.ToString();
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return Task.FromResult(ToolResponse.ErrorResponse("Package 'identifier' parameter is required."));
            }

            var tcs = new TaskCompletionSource<ToolResponse>();

            EditorApplication.delayCall += () =>
            {
                Debug.Log("starting package add");
                AddRequest request = Client.Add(packageId);
                EditorApplication.update += OnProgress;
                void OnProgress()
                {
                    if (request.Status != StatusCode.InProgress)
                        Debug.Log($"add package complete. status: {request.Status.ToString()}");
                    if (request.Status == StatusCode.Success)
                    {
                        tcs.TrySetResult(ToolResponse.SuccessResponse($"Successfully added package: {request.Result.displayName}"));
                        EditorApplication.update -= OnProgress;
                    }
                    else if (request.Status == StatusCode.Failure)
                    {
                        tcs.TrySetResult(ToolResponse.ErrorResponse($"Failed to add package '{packageId}': {request.Error.message}"));
                        EditorApplication.update -= OnProgress;
                    }
                }
            };

            return tcs.Task;
        }
    }
}
