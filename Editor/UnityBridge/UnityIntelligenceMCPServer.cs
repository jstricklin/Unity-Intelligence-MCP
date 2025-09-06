using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Threading.Tasks;
using UnityEditor;
using System.Collections;

namespace UnityIntelligenceMCP.Unity
{
    public static class UnityIntelligenceMCPServer 
    {
        private static string _url => UnityIntelligenceMCPSettings.Instance.ServerUrl;
        private static int _port => UnityIntelligenceMCPSettings.Instance.Port;
        private static WebSocketServer _wsserver;
        public static bool IsListening => _wsserver?.IsListening ?? false;
        
        public static void Start()
        {
            if (IsListening) return;
            
            try
            {
                _wsserver = new WebSocketServer($"{_url}:{_port}");
                _wsserver.AddWebSocketService<UnityIntelligenceMCPSocketHandler>("/mcp-bridge");
                _wsserver.Start();
                WebSocketServerMonitor.Initialize();
                Debug.Log($"Unity Intelligence MCP WebSocket server started on port {_port}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start WebSocket server: {ex.Message}");
            }
        }
        
        public static void Stop()
        {
            WebSocketServerMonitor.Dispose();
            _wsserver?.Stop();
            _wsserver = null;
            Debug.Log("Unity Intelligence MCP WebSocket server stopped");
        }
        
        public static Task Send(string jsonPayload)
        {
            if (!IsListening) return Task.CompletedTask;
            _wsserver?.WebSocketServices["/mcp-bridge"].Sessions.Broadcast(jsonPayload);
            
            return Task.CompletedTask;
        }
    }
}