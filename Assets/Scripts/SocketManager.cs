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
    private bool isQuitting = false;

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
        Application.quitting += () => isQuitting = true;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
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
            // Reconnect if we are in gameplay, searching, OR browsing the lobby.
            // Only stay disconnected when the user is on the home/main screen.
            bool isGameplayActive = GameManager.Instance != null && GameManager.Instance.IsGameplayScreenActive;
            bool isLobbyActive = HorizontalLobbyManager.IsLobbyActive;
            bool shouldConnect = isGameplayActive || MatchmakingModel.IsSearching || isLobbyActive;

            if (shouldConnect)
            {
                if (ioManager == null || ioManager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
                {
                    Debug.Log("<color=#98FB98>⬆️ [SocketManager] App resumed (Gameplay/Search/Lobby active). Triggering reconnection...</color>");
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
                Debug.Log("<color=#AFEEEE>[SocketManager] App resumed on home screen. Staying disconnected.</color>");
            }
        }
    }

    /// <summary>
    /// Checks if the player is currently in a match and triggers the sync if so.
    /// Used for seamless resume without showing the recovery panel.
    /// </summary>
    private void CheckAndTriggerMatchSync()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsActivelyPlaying)
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
                Debug.Log("[SocketManager] Sync: Gameplay active state checked, no match/room ID cached yet.");
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

    // Fired when server credits coins from a rewarded ad S2S callback.
    // Only arrives if the socket is already open (e.g. user is in lobby).
    public static event System.Action<int, int> OnAdRewardGranted; // (newTotal, coinsAwarded)

    // Fired after match settlement and after ad reward — always update local state from this.
    public static event System.Action<UserStatsPayload> OnUserStatsUpdated;

    private void OnSocketConnected(ConnectResponse resp)
    {
        Debug.Log("<color=#00FF00>🎉 ⬇️ [Socket ON] Connected successfully! The backend accepted the connection.</color>");

        // Wire wallet/reward events every (re-)connect.
        ioManager.Socket.On<BestHTTP.JSON.LitJson.JsonData>("adRewardGranted", OnAdRewardGrantedRaw);
        ioManager.Socket.On<BestHTTP.JSON.LitJson.JsonData>("userStatsUpdated", OnUserStatsUpdatedRaw);

        CheckAndTriggerMatchSync();
    }

    private void OnAdRewardGrantedRaw(BestHTTP.JSON.LitJson.JsonData json)
    {
        try
        {
            int newTotal    = (int)json["coins"];
            int coinsEarned = (int)json["coinsAwarded"];
            Debug.Log($"<color=#00FF00>⬇️ [Socket ON] adRewardGranted — +{coinsEarned} coins (total: {newTotal})</color>");

            // Update PlayerModel immediately from the authoritative socket value.
            PlayerModel.UpdateData(
                PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
                PlayerModel.Wins, PlayerModel.Losses,
                PlayerModel.Balance, (int)PlayerModel.CashBalance, PlayerModel.BonusBalance, PlayerModel.WinningBalance, newTotal,
                PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level, PlayerModel.Role,
                PlayerModel.SelectedAvatarIndex, PlayerModel.ActiveMatchId, PlayerModel.ActiveRoomId);

            OnAdRewardGranted?.Invoke(newTotal, coinsEarned);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SocketManager] adRewardGranted parse error: {e.Message}");
        }
    }

    private void OnUserStatsUpdatedRaw(BestHTTP.JSON.LitJson.JsonData json)
    {
        try
        {
            string raw = json.ToJson();
            UserStatsPayload payload = BestHTTP.JSON.LitJson.JsonMapper.ToObject<UserStatsPayload>(raw);
            Debug.Log($"<color=#00FF00>⬇️ [Socket ON] userStatsUpdated — coins:{payload.coins} cash:{payload.cashBalance}</color>");

            PlayerModel.UpdateData(
                PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
                payload.wins, payload.losses,
                PlayerModel.Balance, (int)payload.cashBalance, (int)payload.bonusBalance, (int)payload.winningBalance, payload.coins,
                PlayerModel.Avatar, payload.xp, payload.level, PlayerModel.Role,
                PlayerModel.SelectedAvatarIndex, PlayerModel.ActiveMatchId, PlayerModel.ActiveRoomId);

            OnUserStatsUpdated?.Invoke(payload);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SocketManager] userStatsUpdated parse error: {e.Message}");
        }
    }

    private void OnSocketDisconnected()
    {
        if (isQuitting) return;
        Debug.LogWarning("⚠️ <color=#FFA500>⬇️ [Socket ON] Disconnected from server.</color>");
        TriggerSilentRecoveryIfInGame();
    }

    private void TriggerSilentRecoveryIfInGame()
    {
        if (isQuitting) return;
        if (GameManager.Instance != null && GameManager.Instance.IsActivelyPlaying)
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
        
        while (!isQuitting)
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
            
            // Safety: Stop monitoring if we are no longer actively playing (e.g., game ended or winner screen showing)
            if (GameManager.Instance == null || !GameManager.Instance.IsActivelyPlaying)
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
        if (isQuitting) return;
        string msg = err != null ? err.message : "Unknown Error";
        
        // Filter out harmless exceptions and expected closures
        if (msg.Contains("Aborted") || 
            msg.Contains("Closed") || 
            msg.Contains("WSACancelBlockingCall"))
        {
            Debug.Log($"[SocketManager] Socket info: {msg}");
            return;
        }

        Debug.LogError("❌ <color=#FF0000>⬇️ [Socket ERROR] " + msg + "</color>");
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
    public float stake;
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
    public string frame; // Added for profile frame support
    public bool isBot;
    public int rank;
}

[System.Serializable]
public class MatchmakingUpdateData
{
    public int current;
    public int target;
}

[System.Serializable]
public class UserStatsPayload
{
    public float balance;
    public float cashBalance;
    public float bonusBalance;
    public float winningBalance;
    public int   coins;
    public int   wins;
    public int   losses;
    public int   xp;
    public int   level;
    public string frame; // Added for profile frame support
}
