using UnityEditor;
using UnityEngine;

namespace UnityIntelligenceMCP.Unity
{
    public class UnityIntelligenceMCPSettings
    {
        public static string PackageName = "com.jstricklin.unity-intelligence-mcp";
        private const string SettingsKey = "UnityIntelligenceMCP.Settings";
        private const int DefaultPort = 5000;
        private const string DefaultServerUrl = "ws://localhost";
        private const bool DefaultAnalyzeProjectCode = true;
        private const bool DefaultEmbeddUnityDocs = true;

        private static UnityIntelligenceMCPSettings _instance;
        public static UnityIntelligenceMCPSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnityIntelligenceMCPSettings();
                    _instance.LoadSettings();
                }
                return _instance;
            }
        }

        public int Port = DefaultPort;
        public string ServerUrl = DefaultServerUrl;
        public bool AnalyzeProjectCode = DefaultAnalyzeProjectCode;
        public bool EmbeddUnityDocs = DefaultEmbeddUnityDocs;

        public void SaveSettings()
        {
            string json = JsonUtility.ToJson(this, true);
            EditorPrefs.SetString(SettingsKey, json);
        }

        private void LoadSettings()
        {
            if (EditorPrefs.HasKey(SettingsKey))
            {
                string json = EditorPrefs.GetString(SettingsKey);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }
    }
}
