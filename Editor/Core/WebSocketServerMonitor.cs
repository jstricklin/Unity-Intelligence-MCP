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
    // public bool enabled => UnityIntelligenceMCPSettings.Instance.MonitorServer;
    static WebSocketServerMonitor _monitor = null;

    void OnEnable()
    {
        hideFlags = HideFlags.DontUnloadUnusedAsset;
        EditorApplication.update += MonitorServerStatus;
    }

    public static void Initialize()
    {
        if (InitializeMonitor())
        {
            _monitor.monitorStartTime = EditorApplication.timeSinceStartup;
        }
        _startTime = _monitor.monitorStartTime;
    }

    static bool InitializeMonitor()
    {
        _monitor = AssetDatabase.LoadAssetAtPath<WebSocketServerMonitor>(_path);
        bool initialize = _monitor == null;
        if (initialize)
        {
            _monitor = ScriptableObject.CreateInstance<WebSocketServerMonitor>();
            AssetDatabase.CreateAsset(_monitor, _path);
            AssetDatabase.SaveAssets();
            EditorApplication.quitting += Dispose;
        }
        return initialize;
    }

    public static void Dispose()
    {
        _startTime = 0f;
        UpTimeSeconds = 0f;
        EditorApplication.update -= MonitorServerStatus;
        if (_monitor != null || AssetDatabase.LoadAssetAtPath<WebSocketServerMonitor>(_path) != null)
        {
            AssetDatabase.DeleteAsset(_path);
            AssetDatabase.SaveAssets();
        }
        EditorApplication.quitting -= Dispose;
    }

    static void MonitorServerStatus()
    {
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