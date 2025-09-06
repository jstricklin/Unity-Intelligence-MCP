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
    static WebSocketServerMonitor _monitor = null;
    static bool _disposing;

    void EditorStartup()
    {
        MonitorServerStatus();
    }

    void OnEnable()
    {
        EditorApplication.update += MonitorServerStatus;
        EditorApplication.quitting += Dispose;
        hideFlags = HideFlags.DontUnloadUnusedAsset;
    }

    void OnDestroy()
    {
        _disposing = false;
    }

    public static void Initialize()
    {
        _monitor = AssetDatabase.LoadAssetAtPath<WebSocketServerMonitor>(_path);
        if (_monitor == null)
        {
            _monitor = ScriptableObject.CreateInstance<WebSocketServerMonitor>();
            _monitor.monitorStartTime = EditorApplication.timeSinceStartup;
            AssetDatabase.CreateAsset(_monitor, _path);
            AssetDatabase.SaveAssets();
        }
        _startTime = _monitor.monitorStartTime;
    }

    public static void Dispose()
    {
        _disposing = true;
        _startTime = 0f;
        UpTimeSeconds = 0f;
        EditorApplication.update -= MonitorServerStatus;
        EditorApplication.quitting -= Dispose;
        AssetDatabase.DeleteAsset(_path);
        AssetDatabase.SaveAssets();
    }

    static void MonitorServerStatus()
    {
        if (_disposing) return;
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