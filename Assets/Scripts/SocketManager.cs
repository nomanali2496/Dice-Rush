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
        if (focus)
        {
            Debug.Log("[SocketManager] App focused. Checking socket state and syncing profile...");

            // Fetch latest profile to sync active match state and auto-reconnect socket if needed
            if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
            {
                BackendAuthService.Instance.GetProfile((success, message) => 
                {
                    if (success && !string.IsNullOrEmpty(PlayerModel.ActiveRoomId) && !string.IsNullOrEmpty(PlayerModel.ActiveMatchId))
                    {
                        // We do not auto-reconnect here anymore. 
                        // PlayerModel.OnDataUpdated will fire and MatchRecoveryUIManager will show the prompt if appropriate.
                    }
                });
            }
            else if (ioManager != null && ioManager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
            {
                Debug.Log("[SocketManager] Socket not open on focus. Reconnecting...");
                ConnectToServer();
            }
        }
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
            Debug.Log("<color=#00FF00>✅ [SocketManager] Socket is already connected.</color>");
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
            ioManager.Close();
            ioManager = null;
            Debug.Log("<color=#FFA500>⬇️ [SocketManager] Disconnected and cleared.</color>");
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

[System.Serializable]
public class RecoveredGameData
{
    public string roomId;
    public int[] matrix;
    public int duration;
    public int winConditionTarget;
    public int timeRemaining;
    public long gameplayStartsAt;
    public int currentSum;
    public bool hasPendingRoll;
    
    public System.Collections.Generic.Dictionary<string, string> playerSides;
    public System.Collections.Generic.Dictionary<string, string> playerAvatars;
    public System.Collections.Generic.Dictionary<string, int[]> allLockedIndices;
}

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
