namespace UnityIntelligenceMCP.Unity
{
    public class UnityIntelligenceMCPController
    {
        private readonly UnityIntelligenceMCPServer _server;
        private readonly UnityIntelligenceMCPSettings _settings;

        public UnityIntelligenceMCPController()
        {
            _server = UnityIntelligenceMCPServer.Instance;
            _settings = UnityIntelligenceMCPSettings.Instance;
        }

        public void StartServer()
        {
            _server.StartServer(_settings.Port);
        }

        public void StopServer()
        {
            _server.StopServer();
        }

        public void ChangePort(int newPort)
        {
            if (newPort < 1 || newPort > 65535)
            {
                UnityEngine.Debug.LogError($"{newPort} is an invalid port number. Please enter a number between 1 and 65535.");
                return;
            }

            if (newPort != _settings.Port)
            {
                _settings.Port = newPort;
                _settings.SaveSettings();
                if (_server.IsListening)
                {
                    StopServer();
                    StartServer();
                }
            }
        }

        public void SendTestMessage()
        {
            _server.SendToAllClients("{\"event\":\"test\", \"data\":\"Hello from Unity Editor\"}");
        }
    }
}
