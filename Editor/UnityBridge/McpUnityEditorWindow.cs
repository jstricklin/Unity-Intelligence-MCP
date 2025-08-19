using System;
using McpUnity.Utils;
using UnityEngine;
using UnityEditor;

namespace McpUnity.Unity
{
    public class McpUnityEditorWindow : EditorWindow
    {
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _wrappedLabelStyle;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Server", "Help" };
        private Vector2 _helpTabScrollPosition = Vector2.zero;
        private Vector2 _serverTabScrollPosition = Vector2.zero;

        [MenuItem("Tools/MCP Unity/Server Window", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<McpUnityEditorWindow>("MCP Unity");
            window.minSize = new Vector2(600, 400);
        }

        private void OnGUI()
        {
            InitializeStyles();
            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.Space();
            WrappedLabel("MCP Unity", _headerStyle);
            EditorGUILayout.Space();

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();

            switch (_selectedTab)
            {
                case 0: // Server tab
                    DrawServerTab();
                    break;
                case 1: // Help tab
                    DrawHelpTab();
                    break;
            }

            // Version info at the bottom
            GUILayout.FlexibleSpace();
            WrappedLabel($"MCP Unity v0.1.0", EditorStyles.miniLabel, GUILayout.Width(150));
            EditorGUILayout.EndVertical();
        }

        #region Tab Drawing Methods

        private void DrawServerTab()
        {
            _serverTabScrollPosition = EditorGUILayout.BeginScrollView(_serverTabScrollPosition);
            EditorGUILayout.BeginVertical("box");

            McpUnitySettings settings = McpUnitySettings.Instance;
            McpUnityServer mcpUnityServer = McpUnityServer.Instance;

            // Server status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(120));
            string statusText = mcpUnityServer.IsListening ? "Server Online" : "Server Offline";
            Color statusColor = mcpUnityServer.IsListening ? Color.green : Color.red;
            GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
            statusStyle.normal.textColor = statusColor;
            EditorGUILayout.LabelField(statusText, statusStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Port configuration
            EditorGUILayout.BeginHorizontal();
            int newPort = EditorGUILayout.IntField("Connection Port", settings.Port);
            if (newPort < 1 || newPort > 65536)
            {
                newPort = settings.Port;
                Debug.LogError($"{newPort} is an invalid port number. Please enter a number between 1 and 65535.");
            }
            if (newPort != settings.Port)
            {
                settings.Port = newPort;
                settings.SaveSettings();
                if (mcpUnityServer.IsListening)
                {
                    mcpUnityServer.StopServer();
                    mcpUnityServer.StartServer(settings.Port);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Server controls
            EditorGUILayout.BeginHorizontal();
            if (!mcpUnityServer.IsListening)
            {
                if (GUILayout.Button("Start Server", GUILayout.Height(30)))
                {
                    mcpUnityServer.StartServer(settings.Port);
                }
            }
            else
            {
                if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
                {
                    mcpUnityServer.StopServer();
                }
            }
            
            if (GUILayout.Button("Send Test Message", GUILayout.Height(30)))
            {
                mcpUnityServer.SendToAllClients(
                    "{\"event\":\"test\", \"data\":\"Hello from Unity Editor\"}"
                );
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHelpTab()
        {
            _helpTabScrollPosition = EditorGUILayout.BeginScrollView(_helpTabScrollPosition);
            EditorGUILayout.BeginVertical("box");
            WrappedLabel("MCP Unity - Usage Guide", _subHeaderStyle);
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
