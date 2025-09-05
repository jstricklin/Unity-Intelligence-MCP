using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityIntelligenceMCP.Unity
{
    [InitializeOnLoad]
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

        public bool IsListening => _wsserver?.IsListening ?? false;

        public void Start(int port)
        {
            if (IsListening) return;
            try
            {
                _wsserver = new WebSocketServer($"ws://localhost:{port}");
                _wsserver.AddWebSocketService<UnityIntelligenceMCPSocketHandler>("/mcp-bridge");
                _wsserver.Start();
                Debug.Log($"Unity Intelligence MCP WebSocket server started on port {port}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start WebSocket server: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_wsserver == null) return;

            _wsserver?.Stop();
            _wsserver = null;
            // Debug.Log("Unity Intelligence MCP WebSocket server stopped");
        }

        public Task Send(string jsonPayload)
        {
            if (!IsListening) return Task.CompletedTask;

            _wsserver?.WebSocketServices["/mcp-bridge"].Sessions.Broadcast(jsonPayload);
            return Task.CompletedTask;
        }
    }
}
