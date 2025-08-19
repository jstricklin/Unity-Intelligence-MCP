using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class UnityIntelligenceMCPSocketHandler : WebSocketBehavior
{
    [System.ComponentModel.Browsable(false)]
    public new WebSocketServiceManager WebSocketServices => base.WebSocketServices;

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
        UnityEditor.EditorApplication.delayCall += () => 
        {
            Debug.Log($"Received message from MCP server: {e.Data}");
            // Placeholder: Will add request processing
        };
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"WebSocket error: {e.Message}");
    }
}
