using UnityEngine;
using WebSocketSharp;

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
        private WebSocket _wsclient;

        public bool IsConnected => _wsclient?.ReadyState == WebSocketState.Open;

        public void Connect(string url)
        {
            if (IsConnected) return;

            try
            {
                _wsclient = new WebSocket(url);
                _wsclient.OnOpen += (sender, e) => Debug.Log("WebSocket connection opened.");
                _wsclient.OnMessage += (sender, e) => Debug.Log($"WebSocket message received: {e.Data}");
                _wsclient.OnError += (sender, e) => Debug.LogError($"WebSocket error: {e.Message}");
                _wsclient.OnClose += (sender, e) => Debug.Log("WebSocket connection closed.");
                _wsclient.Connect();
                Debug.Log($"Unity Intelligence MCP WebSocket connecting to {url}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to connect WebSocket: {e.Message}");
            }
        }

        public void Disconnect()
        {
            if (_wsclient == null) return;

            _wsclient?.Close();
            _wsclient = null;
            Debug.Log("Unity Intelligence MCP WebSocket disconnected");
        }

        public void Send(string jsonPayload)
        {
            if (!IsConnected) return;

            _wsclient?.Send(jsonPayload);
        }
    }
}
