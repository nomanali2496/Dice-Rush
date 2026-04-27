using UnityEngine;
using System;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;

public class SimpleSocketClient : MonoBehaviour
{
    private SocketManager ioManager;

    void Start()
    {
        // 1. Point to the Ngrok URL. Removed query params because they crash the backend's Node.js message parser.
        Uri serverUri = new Uri("https://unclog-goggles-drastic.ngrok-free.dev");

        // 2. Set up options to match your backend's requirements
        SocketOptions options = new SocketOptions
        {
            ConnectWith = BestHTTP.SocketIO3.Transports.TransportTypes.WebSocket,
            AutoConnect = true,
            // Pass the token and inject the version strings to bypass the "Version Mismatch" block
            Auth = (manager, socket) => new { 
                token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY5ZWY1N2VmYzc5YTg0NDNlMDM1NjZiMiIsInNlc3Npb25JZCI6IjY2ZTgyNTgwLTdiMDctNDk1NS05OGU0LTFjNWM5YjNhNTdmZCIsImlhdCI6MTc3NzMwMDk4MywiZXhwIjoxNzc5ODkyOTgzfQ.ehH_awxUNtiy9B-ebmnXsCIG0QRc2JZn4k_BotIQx8M",
                version = "1.0.1",
                clientVersion = "1.0.1",
                v = "1.0.1"
            }
        };

        Debug.Log("Attempting Socket.IO Handshake...");
        
        ioManager = new SocketManager(serverUri, options);

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
