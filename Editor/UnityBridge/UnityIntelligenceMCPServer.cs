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

        public bool IsListening => _wsserver?.IsListening ?? false;

        public void Start(int port)
        {
            if (IsListening) return;

            if (Application.isEditor)
            {
                Application.runInBackground = true;
            }

            try
            {
                _wsserver = new WebSocketServer($"ws://localhost:{port}");
                _wsserver.AddWebSocketService<UnityIntelligenceMCPSocketHandler>("/mcp-bridge");
                _wsserver.Start();
                Debug.Log($"Unity Intelligence MCP WebSocket server started on port {port}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start WebSocket server: {e.Message}");
            }
        }

        public void Stop()
        {
            if (_wsserver == null) return;

            _wsserver?.Stop();
            _wsserver = null;
            Debug.Log("Unity Intelligence MCP WebSocket server stopped");
        }

        public void Send(string jsonPayload)
        {
            if (!IsListening) return;

            _wsserver?.WebSocketServices["/mcp-bridge"].Sessions.Broadcast(jsonPayload);
        }
    }
}
