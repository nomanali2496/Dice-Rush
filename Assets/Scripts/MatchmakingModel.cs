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
            Debug.LogError("[MatchmakingModel] SocketManager is not ready!");
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
        Debug.Log($"[MatchmakingModel] Emitted 'findMatch' for mode '{mode}' with stake {stake}...");
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
        
        // Setup empty placeholders for other events shown in Postman
        socket.On<string>("diceRolled", (data) => Debug.Log("[MatchmakingModel] diceRolled: " + data));
        socket.On<string>("numberLocked", (data) => Debug.Log("[MatchmakingModel] numberLocked: " + data));
        socket.On<string>("gameOver", (data) => Debug.Log("[MatchmakingModel] gameOver: " + data));

        listenersAdded = true;
    }

    private static void HandleStartGame(StartGameData data)
    {
        IsSearching = false;
        Debug.Log($"[MatchmakingModel] Match Found! ID: {data?.matchId}");
        OnMatchFound?.Invoke(data);
    }

    private static void HandleError(Error error)
    {
        IsSearching = false;
        string msg = error != null ? error.message : "Unknown Server Error";
        Debug.LogError("[MatchmakingModel] Server Error: " + msg);
        OnMatchmakingFailed?.Invoke(msg);
    }

    private static void HandleMatchmakingUpdate(MatchmakingUpdateData data)
    {
        if (data != null)
        {
            Debug.Log($"[MatchmakingModel] Queue Update: {data.current}/{data.target}");
            OnMatchmakingUpdated?.Invoke(data.current, data.target);
        }
    }
}
