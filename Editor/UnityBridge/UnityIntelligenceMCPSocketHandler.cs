using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using WebSocketSharp;
using System.Threading.Tasks;
using UnityIntelligenceMCP.Editor.Services.ResourceServices;
using WebSocketSharp.Server;
using UnityIntelligenceMCP.Editor.Models;
using UnityIntelligenceMCP.Unity;

public class UnityIntelligenceMCPSocketHandler : WebSocketBehavior
{
    public static List<string> Connections = new List<string>();
    protected override void OnOpen()
    {
        Debug.Log($"New MCP client Started: {ID}");
        Connections.Add(ID);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log($"MCP client disconnected: {ID}");
        Connections.Remove(ID);
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        UnityEditor.EditorApplication.delayCall += async () =>
        {
            Debug.Log($"Request received: {e.Data}");
            
            try
            {
                var message = JsonConvert.DeserializeObject<JObject>(e.Data);
            
                string type = message?["type"]?.ToString();
                string requestId = message?["request_id"]?.ToString();
            
                switch (type)
                {
                    case "resource":
                        await HandleResource(message, requestId);
                        break;
                    case "command":
                        await HandleCommand(message, requestId);
                        break;
                    default:
                        SendError("Unsupported message type", requestId);
                        break;
                }
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

    private async Task HandleResource(JObject message, string requestId)
    {
        string resourceUri = message["resource_uri"]?.ToString();
        JObject parameters = message["parameters"] as JObject;

        if (string.IsNullOrEmpty(resourceUri))
        {
            SendError("Missing resource_uri", requestId);
            return;
        }

        var response = await UnityIntelligenceMCPController.Instance
            .HandleResource(resourceUri, parameters);
        var responseObject = JObject.FromObject(response);
        if (!string.IsNullOrEmpty(requestId))
        {
            responseObject["request_id"] = requestId;
        }
        Send(JsonConvert.SerializeObject(responseObject));
    }

    private async Task HandleCommand(JObject message, string requestId)
    {
        string command = message["command"]?.Value<string>();
        JObject parameters = message["parameters"] as JObject;
                
        if (string.IsNullOrEmpty(command) || parameters == null)
        {
            SendError("Invalid format: must have 'command' and 'parameters'", requestId);
            return;
        }
                
        var response = await UnityIntelligenceMCPController.Instance
            .ExecuteTool(command, parameters);
        
        var responseObject = JObject.FromObject(response);
        if (!string.IsNullOrEmpty(requestId))
        {
            responseObject["request_id"] = requestId;
        }
        Send(JsonConvert.SerializeObject(responseObject));
    }
    
    private void SendError(string errorMessage, string requestId)
    {
        var errorResponse = ToolResponse.ErrorResponse(errorMessage);
        var responseObject = JObject.FromObject(errorResponse);
        if (!string.IsNullOrEmpty(requestId))
        {
            responseObject["request_id"] = requestId;
        }
        Send(JsonConvert.SerializeObject(responseObject));
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error ({ID}): {e.Message}");
    }
}
