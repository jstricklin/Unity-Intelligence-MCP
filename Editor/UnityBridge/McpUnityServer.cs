using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace McpUnity.Unity
{
    public class McpUnityServer : MonoBehaviour
    {
        private WebSocketServer _wsserver;
        private readonly Dictionary<string, IWebSocketSession> _sessions = new();

        public bool IsListening => _wsserver?.IsListening ?? false;

        public void StartServer(int port)
        {
            if (IsListening) return;

            try
            {
                _wsserver = new WebSocketServer(port);
                _wsserver.AddWebSocketService<McpUnitySocketHandler>("/mcp-bridge");
                _wsserver.Start();
                Debug.Log($"MCP Unity WebSocket server started on port {port}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start WebSocket server: {e.Message}");
            }
        }

        public void StopServer()
        {
            if (!IsListening) return;

            _wsserver?.Stop();
            _wsserver = null;
            _sessions.Clear();
            Debug.Log("MCP Unity WebSocket server stopped");
        }

        public void SendToAllClients(string jsonPayload)
        {
            if (!IsListening) return;

            _wsserver?.WebSocketServices.Broadcast(jsonPayload);
            Debug.Log($"Sent message to {_wsserver.WebSocketServices.Count} client(s)");
        }

        public void DisconnectClient(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Context.WebSocket.Close();
                _sessions.Remove(sessionId);
            }
        }
    }
}
