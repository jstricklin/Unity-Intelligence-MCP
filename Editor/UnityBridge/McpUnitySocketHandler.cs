using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WebSocketSharp;

public class McpUnitySocketHandler : WebSocketBehavior
{
    private readonly Dictionary<string, WebSocketBehavior> connections;

    public McpUnitySocketHandler(Dictionary<string, WebSocketBehavior> connDict)
    {
        connections = connDict;
    }

    protected override void OnOpen()
    {
        connections[ID] = this;
        Debug.Log($"New MCP client connected: {ID}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        connections.Remove(ID);
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

    public new void Send(string data)
    {
        base.Send(data);
    }
}
