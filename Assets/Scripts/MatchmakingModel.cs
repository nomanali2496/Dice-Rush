using System;
using UnityEngine;
using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;

/// <summary>
/// Middle Layer connecting UI buttons to the SocketManager for Matchmaking.
/// </summary>
public static class MatchmakingModel
{
    public static bool IsSearching { get; private set; }
    public static bool ReconnectPending { get { return reconnectPending; } set { reconnectPending = value; } }
    
    public static event Action<StartGameData> OnMatchFound;
    public static event Action<string> OnMatchmakingFailed;
    public static event Action<int, int> OnMatchmakingUpdated;

    private static string lastMode;
    private static int lastStake;
    private static string lastLobbyId;
    private static BestHTTP.SocketIO3.Socket lastSocket;
    private static bool localForfeitPending;
    private static bool reconnectPending;

    public static void FindMatch(string mode, int stake, string lobbyId)
    {
        lastMode = mode;
        lastStake = stake;
        lastLobbyId = lobbyId;
        IsSearching = true;

        if (SocketManager.Instance == null) return;

        // If not connected, connect first. The connection callback will handle the emit via sync logic.
        if (SocketManager.Instance.Socket == null || SocketManager.Instance.Socket.Manager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
        {
            Debug.Log("<color=#FFA500>🔄 [MatchmakingModel] Socket not connected. Triggering connection before search...</color>");
            SocketManager.Instance.ConnectToServer();
            return;
        }

        EnsureListenersAdded();

        FindMatchPayload payload = new FindMatchPayload 
        { 
            mode = mode, 
            stake = stake,
            lobbyId = lobbyId
        };

        SocketManager.Instance.Socket.Emit("findMatch", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'findMatch' for mode '{mode}' with stake {stake} (Lobby: {lobbyId})...</color>");
    }

    public static void CancelSearch()
    {
        if (SocketManager.Instance != null && SocketManager.Instance.Socket != null && IsSearching)
        {
            // Only send lobbyId — that's all the server needs to remove the player from the queue
            CancelMatchmakingPayload payload = new CancelMatchmakingPayload 
            { 
                lobbyId = lastLobbyId
            };
            
            SocketManager.Instance.Socket.Emit("cancelMatchmaking", payload);
            Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'cancelMatchmaking' for Lobby: {lastLobbyId}</color>");
            
            // DO NOT disconnect the socket here.
            // Disconnecting immediately drops the cancel event before the server processes it,
            // leaving activeMatchId on the user in the DB. When the user clicks Play again,
            // the reconnect triggers "Already in a match" → recovery panel appears.
            // Keeping the socket alive ensures cancel reaches the server cleanly.
        }
        
        IsSearching = false;
        lastMode = string.Empty;
        lastStake = 0;
        lastLobbyId = string.Empty;
    }

    public static void EnsureListenersAdded()
    {
        var socket = SocketManager.Instance.Socket;
        if (socket == null || lastSocket == socket) return;

        Debug.Log("<color=#00FFFF>🔄 [MatchmakingModel] Adding socket listeners to new socket instance...</color>");
        lastSocket = socket;
        
        // Use JsonData as the generic type. This is the most stable way to handle 
        // complex JSON in BestHTTP without triggering 'constructor not found' errors.
        socket.On<BestHTTP.JSON.LitJson.JsonData>("startGame", (jsonData) => {
            try {
                if (jsonData != null) {
                    // Convert the raw JsonData to our typed class
                    string json = jsonData.ToJson();
                    StartGameData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<StartGameData>(json);
                    HandleStartGame(data);
                }
            } catch (Exception e) {
                Debug.LogError($"[MatchmakingModel] JsonData parse failed for startGame: {e.Message}");
            }
        });
        
        socket.On<Error>("error", HandleError);
        socket.On<MatchmakingUpdateData>("matchmakingUpdate", HandleMatchmakingUpdate);
        
        // Gameplay events
        socket.On<BestHTTP.JSON.LitJson.JsonData>("diceRolled", (jsonData) => {
            if (jsonData != null) {
                string json = jsonData.ToJson();
                Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'diceRolled' raw: " + json + "</color>");
                DiceRolledData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<DiceRolledData>(json);
                HandleDiceRolled(data);
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("numberLocked", (jsonData) => {
            if (jsonData != null) {
                string json = jsonData.ToJson();
                Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'numberLocked' raw: " + json + "</color>");
                NumberLockedData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<NumberLockedData>(json);
                HandleNumberLocked(data);
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("tapResult", (jsonData) => {
            if (jsonData != null) {
                string json = jsonData.ToJson();
                Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'tapResult' raw: " + json + "</color>");
                TapResultData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<TapResultData>(json);
                HandleTapResult(data);
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("iconActivated", (jsonData) => {
            if (jsonData != null) {
                string json = jsonData.ToJson();
                Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'iconActivated' raw: " + json + "</color>");
                IconActivatedData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<IconActivatedData>(json);
                OnIconActivatedEvent?.Invoke(data);
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("iconExpired", (jsonData) => {
            if (jsonData != null) {
                string json = jsonData.ToJson();
                Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'iconExpired' raw: " + json + "</color>");
                IconExpiredData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<IconExpiredData>(json);
                OnIconExpiredEvent?.Invoke(data);
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("playerMilestone", (jsonData) => {
            if (jsonData != null) {
                string json = jsonData.ToJson();
                Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'playerMilestone' raw: " + json + "</color>");
                PlayerMilestoneData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<PlayerMilestoneData>(json);
                OnPlayerMilestoneEvent?.Invoke(data);
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("gameOver", (jsonData) => {
            try {
                if (jsonData != null) {
                    string json = jsonData.ToJson();
                    Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'gameOver' received: " + json + "</color>");
                    
                    GameOverData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<GameOverData>(json);
                    OnGameOverEvent?.Invoke(data);
                }
            } catch (Exception e) {
                Debug.LogError($"[MatchmakingModel] JsonData parse failed for gameOver: {e.Message}");
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("matchRecovered", (jsonData) => {
            try {
                if (jsonData != null) {
                    string json = jsonData.ToJson();
                    Debug.Log("<color=#FF00FF>🔥 [AUTO-RECOVERY] 'matchRecovered' packet received!</color>");
                    Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'matchRecovered' payload: " + json + "</color>");
                    
                    // Directly parse into StartGameData as the payload now matches the structure perfectly
                    StartGameData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<StartGameData>(json);
                    
                    // The user explicitly requested to rejoin, so we pull them into the room
                    SocketManager.Instance.Socket.Emit("joinRoom", new JoinRoomPayload { roomId = data.roomId });
                    
                    IsSearching = false; // Ensure search state is cleared on recovery
                    OnMatchFound?.Invoke(data);
                }
            } catch (Exception e) {
                Debug.LogError($"[MatchmakingModel] JsonData parse failed for matchRecovered: {e.Message}");
            }
        });

        socket.On<BestHTTP.JSON.LitJson.JsonData>("countdown", (jsonData) => {
            try {
                if (jsonData != null) {
                    string json = jsonData.ToJson();
                    Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'countdown' received: " + json + "</color>");
                    CountdownData data = BestHTTP.JSON.LitJson.JsonMapper.ToObject<CountdownData>(json);
                    OnCountdownEvent?.Invoke(data);
                }
            } catch (Exception e) {
                Debug.LogError($"[MatchmakingModel] JsonData parse failed for countdown: {e.Message}");
            }
        });
    }

    private static void HandleStartGame(StartGameData data)
    {
        IsSearching = false;
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'startGame' received! Match ID: {data?.roomId}, Matrix Size: {data?.matrix?.Length}</color>");
        
        // Fix: Emit 'joinRoom' so the server adds this socket to the match room
        if (SocketManager.Instance != null && SocketManager.Instance.Socket != null && data != null)
        {
            SocketManager.Instance.Socket.Emit("joinRoom", new JoinRoomPayload { roomId = data.roomId });
            Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'joinRoom' for room {data.roomId}</color>");
        }

        if (OnMatchFound == null)
        {
            Debug.LogError("❌ [MatchmakingModel] OnMatchFound has NO SUBSCRIBERS! The LobbyManager is not listening.");
        }
        else
        {
            Debug.Log($"[MatchmakingModel] Invoking OnMatchFound for {OnMatchFound.GetInvocationList().Length} subscribers.");
        }

        OnMatchFound?.Invoke(data);
    }

    private static void HandleError(BestHTTP.SocketIO3.Error err)
    {
        if (err == null) return;
        string msg = err.message ?? "Unknown Server Error";
        Debug.LogError($"❌ <color=#FF0000>[Socket ERROR] 'error' received: {msg}</color>");

        // Player backgrounded while a match was being found — server already placed them in a match.
        // Fetch the active match IDs from the profile and reconnect directly.
        if (msg.Contains("Already in an active match") || msg.Contains("Already in a match"))
        {
            Debug.Log("<color=#00FFFF>🔄 [MatchmakingModel] Already in active match — fetching profile to reconnect...</color>");
            IsSearching = false;

            if (BackendAuthService.Instance != null)
            {
                BackendAuthService.Instance.GetProfile((success, error) => {
                    if (!success) { Debug.LogWarning("[MatchmakingModel] Profile fetch failed during active-match recovery."); return; }

                    string matchId = PlayerModel.ActiveMatchId;
                    string roomId  = PlayerModel.ActiveRoomId;

                    if (!string.IsNullOrEmpty(matchId) && !string.IsNullOrEmpty(roomId))
                    {
                        Debug.Log($"<color=#00FF00>✅ [MatchmakingModel] Reconnecting to active match {matchId} / room {roomId}</color>");
                        ReconnectMatch(matchId, roomId);
                    }
                    else
                    {
                        Debug.LogWarning("[MatchmakingModel] Active match IDs not found on profile — user may need to rejoin manually.");
                        OnMatchmakingFailed?.Invoke("Match found but reconnect info missing. Please restart.");
                    }
                });
            }
            return;
        }
        
        // Only trigger matchmaking failure if we were actually searching
        if (IsSearching)
        {
            IsSearching = false;
            OnMatchmakingFailed?.Invoke(msg);
        }
        
        OnSocketErrorEvent?.Invoke(msg);
    }

    private static void HandleMatchmakingUpdate(MatchmakingUpdateData data)
    {
        if (data != null)
        {
            Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'matchmakingUpdate' Queue: {data.current}/{data.target}</color>");
            OnMatchmakingUpdated?.Invoke(data.current, data.target);
        }
    }

    /// <summary>
    /// Re-triggers the matchmaking search if the app was resumed during a pending search.
    /// Since the socket disconnected, the server cleared the old search.
    /// </summary>
    public static void ReTriggerSearchAfterReconnect()
    {
        if (IsSearching && !string.IsNullOrEmpty(lastMode))
        {
            Debug.Log("<color=#FFA500>🔄 [MatchmakingModel] App resumed during search. Re-triggering 'findMatch'...</color>");
            FindMatch(lastMode, lastStake, lastLobbyId);
        }
    }

    // --- GAMEPLAY EVENTS ---
    public static event Action<DiceRolledData> OnDiceRolledEvent;
    public static event Action<NumberLockedData> OnNumberLockedEvent;
    public static event Action<TapResultData> OnTapResultEvent;
    public static event Action<IconActivatedData> OnIconActivatedEvent;
    public static event Action<IconExpiredData> OnIconExpiredEvent;
    public static event Action<PlayerMilestoneData> OnPlayerMilestoneEvent;
    public static event Action<GameOverData> OnGameOverEvent;
    public static event Action<CountdownData> OnCountdownEvent;
    public static event Action<string> OnSocketErrorEvent;

    public static void RequestDiceRoll(string roomId)
    {
        if (SocketManager.Instance?.Socket == null) return;

        RollDiceRequestPayload payload = new RollDiceRequestPayload { roomId = roomId };
        SocketManager.Instance.Socket.Emit("rollDiceRequest", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'rollDiceRequest' for room {roomId}</color>");
    }

    private static void HandleDiceRolled(DiceRolledData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'diceRolled': d1={data.d1}, d2={data.d2}, sum={data.sum} by {data.userId}</color>");
        OnDiceRolledEvent?.Invoke(data);
    }

    public static void EmitTapNumber(string roomId, int index, int val, int sequence)
    {
        if (SocketManager.Instance?.Socket == null) return;

        TapNumberPayload payload = new TapNumberPayload
        {
            roomId = roomId,
            index = index,
            val = val,
            sequence = sequence
        };
        SocketManager.Instance.Socket.Emit("tapNumber", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'tapNumber' for room {roomId} | index={index}, val={val}, seq={sequence}</color>");
    }

    private static void HandleNumberLocked(NumberLockedData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'numberLocked': index={data.index} by {data.userId}, cellValue={data.cellValue}, score={data.score}</color>");
        OnNumberLockedEvent?.Invoke(data);
    }

    private static void HandleTapResult(TapResultData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'tapResult': correct={data.correct}, delta={data.scoreDelta}, total={data.score}</color>");
        OnTapResultEvent?.Invoke(data);
    }

    private static void HandleIconActivated(IconActivatedData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'iconActivated': index={data.index}, duration={data.duration}</color>");
        OnIconActivatedEvent?.Invoke(data);
    }

    public static void ReconnectMatch(string activeMatchId, string activeRoomId)
    {
        if (SocketManager.Instance == null) return;

        // If not connected, connect first. The connection callback will handle the sync.
        if (SocketManager.Instance.Socket == null || SocketManager.Instance.Socket.Manager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
        {
            Debug.Log("<color=#FFA500>🔄 [MatchmakingModel] Socket not connected for recovery. Triggering connection...</color>");
            reconnectPending = true;
            SocketManager.Instance.ConnectToServer();
            return;
        }

        EnsureListenersAdded();

        ReconnectMatchPayload payload = new ReconnectMatchPayload { activeMatchId = activeMatchId, activeRoomId = activeRoomId };
        SocketManager.Instance.Socket.Emit("reconnectMatch", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'reconnectMatch' for match {activeMatchId} in room {activeRoomId}</color>");
    }

    public static void ForfeitMatch(string roomId, bool joinRoomFirst = false)
    {
        if (SocketManager.Instance == null || string.IsNullOrEmpty(roomId)) return;

        localForfeitPending = true;
        lastLobbyId = roomId; // Reuse lastLobbyId to store roomId for pending forfeit

        // If not connected, connect first. The connection callback will handle the forfeit via sync logic.
        if (SocketManager.Instance.Socket == null || SocketManager.Instance.Socket.Manager.State != BestHTTP.SocketIO3.SocketManager.States.Open)
        {
            Debug.Log("<color=#FFA500>🔄 [MatchmakingModel] Socket not connected for forfeit. Triggering connection...</color>");
            SocketManager.Instance.ConnectToServer();
            return;
        }

        EnsureListenersAdded();

        if (joinRoomFirst)
        {
            SocketManager.Instance.Socket.Emit("joinRoom", new JoinRoomPayload { roomId = roomId });
            Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'joinRoom' before forfeit for room {roomId}</color>");
        }

        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'forfeitMatch' for Room: {roomId}</color>");
        SocketManager.Instance.Socket.Emit("forfeitMatch", new ForfeitMatchPayload { roomId = roomId });
    }

    public static bool ConsumeLocalForfeitPending()
    {
        if (!localForfeitPending) return false;

        localForfeitPending = false;
        return true;
    }

    public static bool IsForfeitPending() => localForfeitPending;
    public static string GetPendingForfeitRoomId() => lastLobbyId;

    public static bool IsReconnectPending() => reconnectPending;

    public static void ClearLocalForfeitPending()
    {
        localForfeitPending = false;
    }

    private static void HandleIconExpired(IconExpiredData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'iconExpired': index={data.index}</color>");
        OnIconExpiredEvent?.Invoke(data);
    }
}

[Serializable]
public class ReconnectMatchPayload
{
    public string activeMatchId;
    public string activeRoomId;
}

[Serializable]
public class TapNumberPayload
{
    public string roomId;
    public int index;
    public int val;
    public int sequence;
}

[Serializable]
public class RollDiceRequestPayload
{
    public string roomId;
}

[Serializable]
public class DiceRolledData
{
    public int d1;
    public int d2;
    public int sum;
    public string userId;
}

[Serializable]
public class NumberLockedData
{
    public int index;
    public string userId;
    public int cellValue;
    public int score;
}

[Serializable]
public class TapResultData
{
    public bool correct;
    public int scoreDelta;
    public int score;
}

[Serializable]
public class IconActivatedData
{
    public int index;
    public int duration;
}

[Serializable]
public class IconExpiredData
{
    public int index;
}

[Serializable]
public class PlayerMilestoneData
{
    public string userId;
    public bool reachedWinTarget;
    public int score;
}

[Serializable]
public class GameOverData
{
    public string winnerId;
    public string reason;
    public double stake;
    public double prizePool;
    public System.Collections.Generic.Dictionary<string, int> scores;
    public System.Collections.Generic.Dictionary<string, int> totalLocks;
    public System.Collections.Generic.Dictionary<string, double> earnings;
    public System.Collections.Generic.Dictionary<string, double> coinsEarnings;
    public System.Collections.Generic.Dictionary<string, PlayerAvatarInfo> players;
}

[Serializable]
public class JoinRoomPayload
{
    public string roomId;
}

[Serializable]
public class CountdownData
{
    public int count;
}

[Serializable]
public class ForfeitMatchPayload
{
    public string roomId;
}

[Serializable]
public class CancelMatchmakingPayload
{
    public string lobbyId;
}
