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
    
    // UI Events
    public static event Action<StartGameData> OnMatchFound;
    public static event Action<string> OnMatchmakingFailed;
    public static event Action<int, int> OnMatchmakingUpdated;

    private static bool listenersAdded = false;

    public static void FindMatch(string mode, int stake, string lobbyId)
    {
        if (SocketManager.Instance == null || SocketManager.Instance.Socket == null)
        {
            Debug.LogError("❌ <color=#FF0000>[Socket ERROR] SocketManager is not ready!</color>");
            OnMatchmakingFailed?.Invoke("Connection to server lost.");
            return;
        }

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
        IsSearching = false;
        // Optionally emit a cancel event if your backend supports it
        // SocketManager.Instance?.Socket?.Emit("cancelMatch");
    }

    private static void EnsureListenersAdded()
    {
        if (listenersAdded) return;
        if (SocketManager.Instance == null || SocketManager.Instance.Socket == null) return;

        var socket = SocketManager.Instance.Socket;
        
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
        socket.On<DiceRolledData>("diceRolled", HandleDiceRolled);
        socket.On<NumberLockedData>("numberLocked", HandleNumberLocked);
        socket.On<string>("gameOver", (data) => Debug.Log("<color=#00FFFF>⬇️ [Socket ON] 'gameOver': " + data + "</color>"));

        listenersAdded = true;
    }

    private static void HandleStartGame(StartGameData data)
    {
        IsSearching = false;
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'startGame' received! Match ID: {data?.roomId}, Matrix Size: {data?.matrix?.Length}</color>");
        
        // Reset sequence for new match
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
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'numberLocked': index={data.index} by {data.userId}, val={data.val}</color>");
        OnNumberLockedEvent?.Invoke(data);
    }
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
    public int val;
}
