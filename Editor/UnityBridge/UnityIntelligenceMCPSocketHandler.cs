using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using UnityIntelligenceMCP.Tools;
using UnityIntelligenceMCP.Unity;

public class UnityIntelligenceMCPSocketHandler : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log($"New MCP client connected: {ID}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log($"MCP client disconnected: {ID}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        UnityEditor.EditorApplication.delayCall += async () => 
        {
            Debug.Log($"Request received: {e.Data}");
            
            try
            {
                // Parse JSON message
                var message = JsonConvert.DeserializeObject<JObject>(e.Data);
                string command = message["command"]?.Value<string>();
                JObject parameters = message["parameters"] as JObject;
                
                if (string.IsNullOrEmpty(command) || parameters == null)
                {
                    Send(JsonConvert.SerializeObject(
                        ToolResponse.ErrorResponse("Invalid format: must have 'command' and 'parameters'")
                    ));
                    return;
                }
                
                // Execute command through controller
                var response = await UnityIntelligenceMCPController.Instance
                    .ExecuteTool(command, parameters);
                
                Send(JsonConvert.SerializeObject(response));
            }
            catch (JsonException ex)
            {
                Send(JsonConvert.SerializeObject(
                    ToolResponse.ErrorResponse($"JSON parse error: {ex.Message}")
                ));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Processing error: {ex.Message}\n{ex.StackTrace}");
                Send(JsonConvert.SerializeObject(
                    ToolResponse.ErrorResponse($"Internal error: {ex.Message}")
                ));
            }
        };
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error ({ID}): {e.Message}");
    }
}
