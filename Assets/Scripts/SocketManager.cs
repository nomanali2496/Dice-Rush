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

    private void OnApplicationFocus(bool focus)
    {
        Debug.Log($"<color=#AFEEEE>[SocketManager] OnApplicationFocus: {focus}</color>");
        if (focus) HandleAppResume();
    }

    private void OnApplicationPause(bool pause)
    {
        Debug.Log($"<color=#AFEEEE>[SocketManager] OnApplicationPause: {pause}</color>");
        if (pause)
        {
            Debug.Log("<color=#FF6347>⚠️ [SocketManager] App backgrounded. Triggering disconnect...</color>");
            Disconnect();
        }
        else
        {
            HandleAppResume();
        }
    }

    private void HandleAppResume()
    {
        if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            if (ioManager == null || ioManager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
            {
                Debug.Log("<color=#98FB98>⬆️ [SocketManager] App resumed. Triggering reconnection...</color>");
                ConnectToServer();
                BackendAuthService.Instance.GetProfile((s, m) => { });
            }
        }
    }


    public void ConnectToServer()
    {
        Debug.Log("<color=#98FB98>🔄 [SocketManager] Starting ConnectToServer flow...</color>");

        if (!BackendAuthService.HasStoredJWT())
        {
            Debug.LogError("[SocketManager] Cannot connect: No JWT token found in storage.");
            return;
        }

        if (ioManager != null && ioManager.State == BestHTTP.SocketIO3.SocketManager.States.Open)
        {
            Debug.Log("<color=#00FF00>✅ [SocketManager] Socket already connected. Skipping handshake.</color>");
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

        Debug.Log("<color=#FFA500>⬆️ [Socket EMIT] Attempting Socket.IO Handshake...</color>");
        
        ioManager = new BestHTTP.SocketIO3.SocketManager(serverUri, options);

        // 3. Listen for Socket.IO specific events
        ioManager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnSocketConnected);
        ioManager.Socket.On(SocketIOEventTypes.Disconnect, OnSocketDisconnected);
        ioManager.Socket.On<Error>(SocketIOEventTypes.Error, OnSocketError);

        Debug.Log("<color=#FFA500>⬆️ [Socket EMIT] Handshake initialized. Waiting for 'connect' event...</color>");

        ioManager.Open();
    }

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("<color=#00FF00>🎉 ⬇️ [Socket ON] Connected successfully! The backend accepted the connection.</color>");
        
        // We no longer auto-emit reconnectMatch here. The MatchRecoveryUIManager handles asking the player first.
    }

    private void OnSocketDisconnected()
    {
        Debug.LogWarning("⚠️ <color=#FFA500>⬇️ [Socket ON] Disconnected from server.</color>");
    }

    public void Disconnect()
    {
        if (ioManager != null)
        {
            Debug.Log("<color=#FFA500>⬇️ [SocketManager] Closing Socket.IO connection and clearing manager...</color>");
            ioManager.Close();
            ioManager = null;
            Debug.Log("<color=#00FFFF>✅ [SocketManager] Socket successfully DISCONNECTED.</color>");
        }
        else
        {
            Debug.Log("[SocketManager] Disconnect called but ioManager was already null.");
        }
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError("❌ <color=#FF0000>⬇️ [Socket ERROR] " + (err != null ? err.message : "Unknown Error") + "</color>");
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
    public string lobbyId;
}

[System.Serializable]
public class StartGameData
{
    public string roomId;
    public int[] matrix;
    public int duration;
    public float stake;
    public int winConditionTarget; // Added from your JSON
    
    public System.Collections.Generic.Dictionary<string, string> playerSides;
    public System.Collections.Generic.Dictionary<string, PlayerAvatarInfo> playerAvatars;
    
    // Recovery fields
    public int timeRemaining; // Seconds left in match
    public long gameplayStartsAt; // Unix ms timestamp when gameplay begins
    public int countdown; // Seconds of countdown before gameplay
    public System.Collections.Generic.Dictionary<string, PlayerGameState> gameState; // Current state of each player
}

// RecoveredGameData removed. Use StartGameData instead as it matches the server payload.

[System.Serializable]
public class PlayerGameState
{
    public int currentSum;
    public int[] lockedIndices;
    public int score;
    public bool reachedWinTarget;
    public double reachedWinTargetTime;
    public string side;
}

[System.Serializable]
public class PlayerAvatarInfo
{
    public string name;
    public string avatar;
    public bool isBot;
}

[System.Serializable]
public class MatchmakingUpdateData
{
    public int current;
    public int target;
}
