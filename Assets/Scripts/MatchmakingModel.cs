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
    
    public static event Action<StartGameData> OnMatchFound;
    public static event Action<string> OnMatchmakingFailed;
    public static event Action<int, int> OnMatchmakingUpdated;

    private static string lastMode;
    private static int lastStake;
    private static string lastLobbyId;
    private static BestHTTP.SocketIO3.Socket lastSocket;

    public static void FindMatch(string mode, int stake, string lobbyId)
    {
        if (SocketManager.Instance == null || SocketManager.Instance.Socket == null)
        {
            Debug.LogError("❌ <color=#FF0000>[Socket ERROR] SocketManager is not ready!</color>");
            OnMatchmakingFailed?.Invoke("Connection to server lost.");
            return;
        }

        lastMode = mode;
        lastStake = stake;
        lastLobbyId = lobbyId;

        IsSearching = true;
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
            FindMatchPayload payload = new FindMatchPayload 
            { 
                mode = lastMode, 
                stake = lastStake,
                lobbyId = lastLobbyId
            };
            
            SocketManager.Instance.Socket.Emit("cancelMatchmaking", payload);
            Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'cancelMatchmaking' for Lobby: {lastLobbyId}</color>");
        }
        
        IsSearching = false;
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
                    
                    RecoveredGameData recovered = BestHTTP.JSON.LitJson.JsonMapper.ToObject<RecoveredGameData>(json);
                    
                    // Map RecoveredGameData to StartGameData
                    StartGameData data = new StartGameData();
                    data.roomId = recovered.roomId;
                    data.matrix = recovered.matrix;
                    data.duration = recovered.duration;
                    data.winConditionTarget = recovered.winConditionTarget;
                    data.playerSides = recovered.playerSides;
                    data.timeRemaining = recovered.timeRemaining;
                    data.gameplayStartsAt = recovered.gameplayStartsAt;
                    
                    // The server payload is missing stake, so we fallback to 0 (UI will just show 0 fee)
                    data.stake = 0f;
                    
                    data.playerAvatars = new System.Collections.Generic.Dictionary<string, PlayerAvatarInfo>();
                    if (recovered.playerAvatars != null)
                    {
                        foreach (var kvp in recovered.playerAvatars)
                        {
                            bool isBot = kvp.Key.StartsWith("bot");
                            data.playerAvatars[kvp.Key] = new PlayerAvatarInfo {
                                name = isBot ? "Bot" : "Player", // We don't have the real name in the payload, but the avatar is there
                                avatar = kvp.Value,
                                isBot = isBot
                            };
                        }
                    }
                    
                    data.gameState = new System.Collections.Generic.Dictionary<string, PlayerGameState>();
                    if (recovered.allLockedIndices != null)
                    {
                        foreach (var kvp in recovered.allLockedIndices)
                        {
                            data.gameState[kvp.Key] = new PlayerGameState {
                                lockedIndices = kvp.Value,
                                score = kvp.Value.Length * 50 // Rough score based on locked boxes
                            };
                        }
                    }
                    
                    // The user explicitly requested to rejoin, so we pull them into the room
                    SocketManager.Instance.Socket.Emit("joinRoom", new JoinRoomPayload { roomId = data.roomId });
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

    private static void HandleError(Error error)
    {
        string msg = error != null ? error.message : "Unknown Server Error";
        Debug.LogError($"❌ <color=#FF0000>[Socket ERROR] 'error' received: {msg}</color>");
        
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
        if (SocketManager.Instance?.Socket == null) return;
        
        EnsureListenersAdded();

        ReconnectMatchPayload payload = new ReconnectMatchPayload { activeMatchId = activeMatchId, activeRoomId = activeRoomId };
        SocketManager.Instance.Socket.Emit("reconnectMatch", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'reconnectMatch' for match {activeMatchId} in room {activeRoomId}</color>");
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
