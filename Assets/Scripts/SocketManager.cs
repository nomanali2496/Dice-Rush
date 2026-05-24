using UnityEngine;
using System;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;

public class SocketManager : MonoBehaviour
{
    public static SocketManager Instance { get; private set; }
    
    private BestHTTP.SocketIO3.SocketManager ioManager;
    public BestHTTP.SocketIO3.Socket Socket => ioManager?.Socket;

    private Coroutine internetMonitorCoroutine;

    [Header("Server Settings")]
    [Tooltip("Backend environment config (assign BackendConfig asset, or place one in Resources/BackendConfig.asset)")]
    [SerializeField] private BackendConfig backendConfig;

    private string serverUrl => (backendConfig != null ? backendConfig : BackendConfig.Load())?.SocketUrl ?? "https://unclog-goggles-drastic.ngrok-free.dev";

    public string GetServerUrl() => serverUrl;

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
            // ONLY connect if we are in gameplay or searching
            bool isGameplayActive = GameManager.Instance != null && GameManager.Instance.IsGameplayScreenActive;
            bool shouldConnect = isGameplayActive || MatchmakingModel.IsSearching;

            if (shouldConnect)
            {
                if (ioManager == null || ioManager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
                {
                    Debug.Log("<color=#98FB98>⬆️ [SocketManager] App resumed (Gameplay/Search active). Triggering reconnection...</color>");
                    ConnectToServer();
                }
                else
                {
                    // Already connected, just ensure match sync if in-game
                    CheckAndTriggerMatchSync();
                }
            }
            else
            {
                Debug.Log("<color=#AFEEEE>[SocketManager] App resumed but no active match/search. Staying disconnected.</color>");
            }
        }
    }

    /// <summary>
    /// Checks if the player is currently in a match and triggers the sync if so.
    /// Used for seamless resume without showing the recovery panel.
    /// </summary>
    private void CheckAndTriggerMatchSync()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayScreenActive)
        {
            string matchId = PlayerModel.ActiveMatchId;
            string roomId = PlayerModel.ActiveRoomId;

            // Fallback to GameManager's active data if PlayerModel cache is empty (silent recovery case)
            if (string.IsNullOrEmpty(matchId)) matchId = GameManager.Instance.GetCurrentRoomId();
            if (string.IsNullOrEmpty(roomId)) roomId = GameManager.Instance.GetCurrentRoomId();

            if (!string.IsNullOrEmpty(matchId) && !string.IsNullOrEmpty(roomId))
            {
                Debug.Log($"<color=#FFA500>🔄 [SocketManager] Auto-triggering match sync for Match:{matchId} Room:{roomId}</color>");
                MatchmakingModel.ReconnectMatch(matchId, roomId);
            }
            else
            {
                Debug.LogWarning("[SocketManager] Sync: Gameplay active but no Match/Room ID found! Reconnection aborted.");
            }
        }
        else if (MatchmakingModel.IsReconnectPending())
        {
            // If we connected because the user clicked "Rejoin" from the lobby prompt
            string matchId = PlayerModel.ActiveMatchId;
            string roomId = PlayerModel.ActiveRoomId;
            
            if (!string.IsNullOrEmpty(matchId) && !string.IsNullOrEmpty(roomId))
            {
                Debug.Log($"<color=#FFA500>🔄 [SocketManager] Rejoin Pending: Triggering match sync for Match:{matchId} Room:{roomId}</color>");
                MatchmakingModel.ReconnectMatch(matchId, roomId);
            }
            // Clear the flag after triggering
            MatchmakingModel.ReconnectPending = false;
        }
        else if (MatchmakingModel.IsForfeitPending())
        {
            // If we connected just to forfeit, trigger it now
            string roomId = MatchmakingModel.GetPendingForfeitRoomId();
            Debug.Log($"<color=#FFA500>🔄 [SocketManager] Triggering pending forfeit for Room:{roomId}</color>");
            MatchmakingModel.ForfeitMatch(roomId, true);
        }
        else if (MatchmakingModel.IsSearching)
        {
            // If we were searching but not in a game yet, re-trigger the search
            MatchmakingModel.ReTriggerSearchAfterReconnect();
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

        if (ioManager != null && (ioManager.State == BestHTTP.SocketIO3.SocketManager.States.Open || ioManager.State == BestHTTP.SocketIO3.SocketManager.States.Opening))
        {
            Debug.Log("<color=#00FF00>✅ [SocketManager] Socket already connected or connecting. Skipping.</color>");
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
        
        // Seamlessly sync match data if the player resumed directly into the gameplay screen
        CheckAndTriggerMatchSync();
    }

    private void OnSocketDisconnected()
    {
        Debug.LogWarning("⚠️ <color=#FFA500>⬇️ [Socket ON] Disconnected from server.</color>");
        TriggerSilentRecoveryIfInGame();
    }

    private void TriggerSilentRecoveryIfInGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameplayScreenActive)
        {
            if (internetMonitorCoroutine == null)
            {
                internetMonitorCoroutine = StartCoroutine(MonitorInternetAndReconnect());
            }
        }
    }

    private System.Collections.IEnumerator MonitorInternetAndReconnect()
    {
        Debug.Log("<color=#00FFFF>🔍 [SocketManager] Silent internet monitor started...</color>");
        
        while (true)
        {
            // 1. Check if internet is physically reachable
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                // 2. Only attempt connection if not already open or opening
                if (ioManager == null || (ioManager.State != BestHTTP.SocketIO3.SocketManager.States.Open && ioManager.State != BestHTTP.SocketIO3.SocketManager.States.Opening))
                {
                    Debug.Log("<color=#98FB98>🌐 [SocketManager] Internet restored! Attempting silent reconnect...</color>");
                    ConnectToServer();
                }
                
                // 3. Give it a moment to connect
                yield return new WaitForSeconds(3f);
                
                // 4. If we are now open, we can stop the monitor (OnSocketConnected will handle match sync)
                if (ioManager != null && ioManager.State == BestHTTP.SocketIO3.SocketManager.States.Open)
                {
                    Debug.Log("<color=#00FF00>✅ [SocketManager] Silent reconnect successful! Monitor stopping.</color>");
                    internetMonitorCoroutine = null;
                    yield break;
                }
            }
            
            // Check every 2 seconds
            yield return new WaitForSeconds(2f);
            
            // Safety: Stop monitoring if we are no longer in gameplay (e.g., game ended while offline)
            if (GameManager.Instance == null || !GameManager.Instance.IsGameplayScreenActive)
            {
                Debug.Log("<color=#FFA500>⏹️ [SocketManager] Gameplay ended. Stopping internet monitor.</color>");
                internetMonitorCoroutine = null;
                yield break;
            }
        }
    }

    public void Disconnect()
    {
        if (ioManager != null)
        {
            Debug.Log("<color=#FF6347>⚠️ [SocketManager] Disconnecting and clearing ioManager...</color>");
            ioManager.Close();
            ioManager = null;
        }
        else
        {
            Debug.Log("[SocketManager] Disconnect called but ioManager was already null.");
        }
    }

    private void OnSocketError(Error err)
    {
        Debug.LogError("❌ <color=#FF0000>⬇️ [Socket ERROR] " + (err != null ? err.message : "Unknown Error") + "</color>");
        TriggerSilentRecoveryIfInGame();
    }

    /// <summary>
    /// Waits for a specified duration before disconnecting.
    /// Useful for ensuring 'forfeit' or 'leave' messages are sent before the socket closes.
    /// </summary>
    public void DisconnectDelayed(float delay)
    {
        StartCoroutine(DisconnectCoroutine(delay));
    }

    private System.Collections.IEnumerator DisconnectCoroutine(float delay)
    {
        Debug.Log($"<color=#FFD700>⏳ [SocketManager] Delayed disconnect triggered ({delay}s)...</color>");
        yield return new WaitForSeconds(delay);
        Disconnect();
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
