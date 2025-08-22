using System;
// using UnityIntelligenceMCP.Utils;
using UnityEngine;
using UnityEditor;

namespace UnityIntelligenceMCP.Unity
{
    public class UnityIntelligenceMCPEditorWindow : EditorWindow
    {
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _wrappedLabelStyle;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Server", "Configuration", "Help" };
        private Vector2 _helpTabScrollPosition = Vector2.zero;
        private Vector2 _serverTabScrollPosition = Vector2.zero;
        private Vector2 _configurationTabScrollPosition = Vector2.zero;
        private UnityIntelligenceMCPController _controller;

        [MenuItem("Tools/Unity Intelligence MCP/Server Window", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityIntelligenceMCPEditorWindow>("Unity Intelligence MCP");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            _controller = new UnityIntelligenceMCPController();
        }

        private void OnGUI()
        {
            InitializeStyles();
            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.Space();
            WrappedLabel("Unity Intelligence MCP", _headerStyle);
            EditorGUILayout.Space();

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();

            switch (_selectedTab)
            {
                case 0: // Server tab
                    DrawServerTab();
                    break;
                case 1: // Configuration tab
                    DrawConfigurationTab();
                    break;
                case 2: // Help tab
                    DrawHelpTab();
                    break;
            }

            // Version info at the bottom
            GUILayout.FlexibleSpace();
            WrappedLabel($"Unity Intelligence MCP v0.1.0", EditorStyles.miniLabel, GUILayout.Width(150));
            EditorGUILayout.EndVertical();
        }

        #region Tab Drawing Methods

        private void DrawServerTab()
        {
            _serverTabScrollPosition = EditorGUILayout.BeginScrollView(_serverTabScrollPosition);
            EditorGUILayout.BeginVertical("box");

            UnityIntelligenceMCPSettings settings = UnityIntelligenceMCPSettings.Instance;
            UnityIntelligenceMCPServer mcpUnityServer = UnityIntelligenceMCPServer.Instance;

            // Server status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(120));
            string statusText = mcpUnityServer.IsListening ? "Server Online" : "Server Offline";
            Color statusColor = mxpUnityServer.IsListening ? Color.green : Color.red;
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField(statusText, statusStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();


            // Server controls
            EditorGUILayout.BeginHorizontal();
            if (!mcpUnityServer.IsConnected)
            {
                if (GUILayout.Button("Start Server", GUILayout.Height(30)))
                {
                    _controller.StartServer();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
                {
                    _controller.StopServer();
                }
            }

            if (GUILayout.Button("Send Test Message", GUILayout.Height(30)))
            {
                _controller.SendTestMessage();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigurationTab()
        {
            UnityIntelligenceMCPSettings settings = UnityIntelligenceMCPSettings.Instance;
            _configurationTabScrollPosition = EditorGUILayout.BeginScrollView(_configurationTabScrollPosition);
            EditorGUILayout.BeginVertical("box");

            WrappedLabel("IDE Integration", _subHeaderStyle);
            EditorGUILayout.Space();

            if (GUILayout.Button("Copy to Clipboard"))
            {
                _controller.CopyMCPConfigToClipboard();
            }

            EditorGUILayout.LabelField("Preview of mcp.json:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(_controller.GetMCPConfigJson(), EditorStyles.textArea, GUILayout.Height(250));

            EditorGUILayout.Space();

            GUILayout.Label("MCP Server Settings", EditorStyles.boldLabel);
            // Port configuration
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newPort = EditorGUILayout.IntField("Connection Port", settings.Port, GUILayout.Width(250));
            if (EditorGUI.EndChangeCheck())
            {
                _controller.ChangePort(newPort);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            string newScriptsDir = EditorGUILayout.TextField("Scripts Directory", settings.ScriptsDir, GUILayout.Width(250));
            if (EditorGUI.EndChangeCheck())
            {
                _controller.ChangeScriptsDir(newScriptsDir);
            }

            EditorGUILayout.Space();

            // GUILayout.Label("Tool Settings", EditorStyles.boldLabel);
            // Simple checkboxes
            // settings.AnalyzeProjectCode = EditorGUILayout.Toggle("Enable Code Analysis", settings.AnalyzeProjectCode);
            // settings.EmbeddUnityDocs = EditorGUILayout.Toggle("Build Unity RAG (~2GB)", settings.EmbeddUnityDocs);

            EditorGUILayout.Space();

            if (GUILayout.Button("Configure VSCode (CoPilot)", GUILayout.Height(30)))
            {
                _controller.ConfigureVSCode();
            }
            if (GUILayout.Button("Configure Roo Code", GUILayout.Height(30)))
            {
                _controller.ConfigureRooCode();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Add PackageCache to Workspace", GUILayout.Height(30)))
            {
                _controller.AddPackageCacheToWorkspace();
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHelpTab()
        {
            _helpTabScrollPosition = EditorGUILayout.BeginScrollView(_helpTabScrollPosition);
            EditorGUILayout.BeginVertical("box");
            WrappedLabel("Unity Intelligence MCP - Usage Guide", _subHeaderStyle);
            EditorGUILayout.Space();
            WrappedLabel("This tool creates a bridge between Unity Editor and the MCP server. Start the server and have your MCP client connect to the specified port.");
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Utility Methods

        private void InitializeStyles()
        {
            if (_headerStyle != null) return;

            // Header style
            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 0, 0)
            };

            // Subheader style
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                padding = new RectOffset(0, 0, 3, 3)
            };

            // Wrapped label style
            _wrappedLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true
            };
        }

        private void WrappedLabel(string text, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
            {
                EditorGUILayout.LabelField(text, _wrappedLabelStyle, options);
                return;
            }

            GUIStyle wrappedStyle = new GUIStyle(style)
            {
                wordWrap = true
            };
            EditorGUILayout.LabelField(text, wrappedStyle, options);
        }

        #endregion
    }
}
