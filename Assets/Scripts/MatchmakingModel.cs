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

    public static void FindMatch(string mode, int stake)
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
            stake = stake 
        };

        SocketManager.Instance.Socket.Emit("findMatch", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'findMatch' for mode '{mode}' with stake {stake}...</color>");
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

        socket.On<StartGameData>("startGame", HandleStartGame);
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
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'startGame' received! Match ID: {data?.roomId}</color>");
        OnMatchFound?.Invoke(data);
    }

    private static void HandleError(Error error)
    {
        IsSearching = false;
        string msg = error != null ? error.message : "Unknown Server Error";
        Debug.LogError($"❌ <color=#FF0000>[Socket ERROR] 'error' received: {msg}</color>");
        OnMatchmakingFailed?.Invoke(msg);
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

    public static void RequestDiceRoll(string roomId)
    {
        if (SocketManager.Instance?.Socket == null) return;

        RollDiceRequestPayload payload = new RollDiceRequestPayload { roomId = roomId };
        SocketManager.Instance.Socket.Emit("rollDiceRequest", payload);
        Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'rollDiceRequest' for room {roomId}</color>");
    }

    private static void HandleDiceRolled(DiceRolledData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'diceRolled': sum={data.sum} by {data.userId}</color>");
        OnDiceRolledEvent?.Invoke(data);
    }

    private static void HandleNumberLocked(NumberLockedData data)
    {
        Debug.Log($"<color=#00FFFF>⬇️ [Socket ON] 'numberLocked': index={data.index} by {data.userId}</color>");
        OnNumberLockedEvent?.Invoke(data);
    }
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
