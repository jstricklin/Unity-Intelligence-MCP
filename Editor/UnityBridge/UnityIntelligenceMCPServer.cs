using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace UnityIntelligenceMCP.Unity
{
    public class UnityIntelligenceMCPServer 
    {
        private static UnityIntelligenceMCPServer _instance;
        public static UnityIntelligenceMCPServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnityIntelligenceMCPServer();
                }
                return _instance;
            }
        }
        private WebSocketServer _wsserver;
        private readonly Dictionary<string, IWebSocketSession> _sessions = new();

        public bool IsListening => _wsserver?.IsListening ?? false;

        public void StartServer(int port)
        {
            if (IsListening) return;

            try
            {
                _wsserver = new WebSocketServer(port);
                _wsserver.AddWebSocketService<UnityIntelligenceMCPSocketHandler>("/mcp-bridge");
                _wsserver.Start();
                Debug.Log($"Unity Intelligence MCP WebSocket server started on port {port}");
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
            Debug.Log("Unity Intelligence MCP WebSocket server stopped");
        }

        public void SendToAllClients(string jsonPayload)
        {
            if (!IsListening) return;

            // _wsserver?.Send(jsonPayload);
            // Debug.Log($"Sent message to {_wsserver.Server.Count} client(s)");
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
