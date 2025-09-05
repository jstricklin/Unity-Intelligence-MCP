using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

internal class UnityServerContainer : ScriptableObject 
{
    public static WebSocketServer Server { get; private set; }
    public static void InitializeServerContainer(WebSocketServer server)
    {
        Server = server;
    }
}