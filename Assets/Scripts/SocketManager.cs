using UnityEngine;
using System;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;

public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }
    
    private BestHTTP.SocketIO3.SocketManager ioManager;
    public BestHTTP.SocketIO3.Socket Socket => ioManager?.Socket;

    [Header("Server Settings")]
    [Tooltip("The Ngrok URL or Backend URL")]
    [SerializeField] private string serverUrl = "https://unclog-goggles-drastic.ngrok-free.dev";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Connection is no longer automatic on start.
        // It will be triggered by BackendAuthService after successful login.
    }

    public void ConnectToServer()
    {
        if (!BackendAuthService.HasStoredJWT())
        {
            Debug.LogWarning("[SocketManager] Cannot connect to Socket: No JWT token found.");
            return;
        }

        if (ioManager != null && ioManager.State == BestHTTP.SocketIO3.SocketManager.States.Open)
        {
            Debug.Log("[SocketManager] Socket is already connected.");
            return;
        }

        Uri serverUri = new Uri(serverUrl);

        SocketOptions options = new SocketOptions
        {
            ConnectWith = BestHTTP.SocketIO3.Transports.TransportTypes.WebSocket,
            AutoConnect = false, // Connecting manually
            Auth = (manager, socket) => new { 
                token = BackendAuthService.GetStoredJWT(),
                version = "1.0.1",
                clientVersion = "1.0.1",
                v = "1.0.1"
            }
        };

        Debug.Log("[SocketManager] Attempting Socket.IO Handshake...");
        
        ioManager = new BestHTTP.SocketIO3.SocketManager(serverUri, options);

        // 3. Listen for Socket.IO specific events
        ioManager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        ioManager.Socket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        ioManager.Socket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        ioManager.Open();
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

[System.Serializable]
public class FindMatchPayload
{
    public string mode;
    public int stake;
}

[System.Serializable]
public class StartGameData
{
    // Define properties here based on your backend payload. e.g.:
    public string matchId;
}

[System.Serializable]
public class MatchmakingUpdateData
{
    public int current;
    public int target;
}
