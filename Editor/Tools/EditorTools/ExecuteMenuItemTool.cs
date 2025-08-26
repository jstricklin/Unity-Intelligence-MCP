using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Editor.Models;

namespace UnityIntelligenceMCP.Tools.EditorTools
{
    public class ExecuteMenuItemTool : ITool
    {
        public string CommandName => "execute_menu_item";

        public async Task<ToolResponse> ExecuteAsync(JObject parameters)
        {
            var path = parameters?["path"]?.ToString();
            if (string.IsNullOrWhiteSpace(path))
            {
                return ToolResponse.ErrorResponse("Menu item 'path' parameter is required.");
            }

            var tcs = new TaskCompletionSource<(bool success, System.Collections.Generic.List<string> opened, System.Collections.Generic.List<string> closed)>();

            // Unity Editor APIs must be called from the main thread.
            // We use delayCall to schedule the work on the next editor update tick.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var windowsBefore = Resources.FindObjectsOfTypeAll<EditorWindow>().Select(w => w.titleContent.text).ToList();
                    var success = EditorApplication.ExecuteMenuItem(path);
                    var windowsAfter = Resources.FindObjectsOfTypeAll<EditorWindow>().Select(w => w.titleContent.text).ToList();

                    var openedWindows = windowsAfter.Except(windowsBefore).ToList();
                    var closedWindows = windowsBefore.Except(windowsAfter).ToList();

                    tcs.SetResult((success, openedWindows, closedWindows));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            var result = await tcs.Task;

            if (!result.success)
            {
                return ToolResponse.ErrorResponse($"Failed to execute menu item: '{path}'. It may not exist or is currently disabled.");
            }

            return ToolResponse.SuccessResponse(
                $"Successfully executed menu item: '{path}'.",
                new
                {
                    opened_windows = result.opened,
                    closed_windows = result.closed
                }
            );
        }
    }
}
