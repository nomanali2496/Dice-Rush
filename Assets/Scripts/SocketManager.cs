using UnityEngine;
using System;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;

public class SocketManager : MonoBehaviour
{
    private BestHTTP.SocketIO3.SocketManager ioManager;

    [Header("Server Settings")]
    [Tooltip("The Ngrok URL or Backend URL")]
    [SerializeField] private string serverUrl = "https://unclog-goggles-drastic.ngrok-free.dev";

    void Start()
    {
        // 1. Point to the Ngrok URL. Removed query params because they crash the backend's Node.js message parser.
        Uri serverUri = new Uri(serverUrl);

        // 2. Set up options to match your backend's requirements
        SocketOptions options = new SocketOptions
        {
            ConnectWith = BestHTTP.SocketIO3.Transports.TransportTypes.WebSocket,
            AutoConnect = true,
            // Pass the token and inject the version strings to bypass the "Version Mismatch" block
            Auth = (manager, socket) => new { 
                token = BackendAuthService.HasStoredJWT() ? BackendAuthService.GetStoredJWT() : "",
                version = "1.0.1",
                clientVersion = "1.0.1",
                v = "1.0.1"
            }
        };

        Debug.Log("Attempting Socket.IO Handshake...");
        
        ioManager = new BestHTTP.SocketIO3.SocketManager(serverUri, options);

        // 3. Listen for Socket.IO specific events
        ioManager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        ioManager.Socket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        ioManager.Socket.On<Error>(SocketIOEventTypes.Error, OnSocketError);
    }

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("🎉 Socket.IO Connected successfully! The backend accepted the connection.");
    }

    private void OnSocketDisconnected()
    {
        Debug.LogWarning("Socket.IO Disconnected from server.");
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError("Socket.IO Error: " + (err != null ? err.message : "Unknown Error"));
    }

    private void OnDestroy()
    {
        if (ioManager != null)
        {
            ioManager.Close();
        }
    }
}
