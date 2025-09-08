using UnityEditor;
using UnityEngine;
using UnityIntelligenceMCP.Unity;
using WebSocketSharp;
using WebSocketSharp.Server;

[InitializeOnLoad]
internal class WebSocketServerMonitor : ScriptableObject 
{
    public double monitorStartTime { get; set; }
    public static double UpTimeSeconds = 0f;
    static string _path = "Packages/com.jstricklin.unity-intelligence-mcp/Editor/WebSocketServerMonitor.asset";
    static double _startTime = 0f;
    static double _lastUpdate = 0f;
    static float _refreshRate = 0.2f;
    public bool enabled => UnityIntelligenceMCPSettings.Instance.MonitorServer;
    static WebSocketServerMonitor _monitor = null;

    void OnEnable()
    {
        hideFlags = HideFlags.DontUnloadUnusedAsset;
        MonitorServerStatus();
    }

    public static void Initialize()
    {
        if(_monitor == null)
            InitializeMonitor();
        if (!UnityIntelligenceMCPSettings.Instance.MonitorServer)
        {
            UnityIntelligenceMCPSettings.Instance.MonitorServer = true;
            UnityIntelligenceMCPSettings.Instance.SaveSettings();
            _monitor.monitorStartTime = EditorApplication.timeSinceStartup;
        }
        EditorApplication.update += MonitorServerStatus;
        EditorApplication.quitting += Dispose;
        _startTime = _monitor.monitorStartTime;
    }

    static void InitializeMonitor()
    {
        _monitor = AssetDatabase.LoadAssetAtPath<WebSocketServerMonitor>(_path);
        if (_monitor == null)
        {
            _monitor = ScriptableObject.CreateInstance<WebSocketServerMonitor>();
            AssetDatabase.CreateAsset(_monitor, _path);
            AssetDatabase.SaveAssets();
        }
    }

    public static void Dispose()
    {
        UnityIntelligenceMCPSettings.Instance.MonitorServer = false;
        UnityIntelligenceMCPSettings.Instance.SaveSettings();
        _startTime = 0f;
        UpTimeSeconds = 0f;
        EditorApplication.update -= MonitorServerStatus;
        EditorApplication.quitting -= Dispose;
    }

    static void MonitorServerStatus()
    {
        if (_monitor == null)
            InitializeMonitor();
        if (!_monitor.enabled) return;
        if (!UnityIntelligenceMCPServer.IsListening)
            UnityIntelligenceMCPServer.Start();
        else
        {
            if (_lastUpdate + _refreshRate < EditorApplication.timeSinceStartup)
            {
                UpTimeSeconds = EditorApplication.timeSinceStartup - _startTime;
                _lastUpdate = EditorApplication.timeSinceStartup;
            }
        }
    }
}