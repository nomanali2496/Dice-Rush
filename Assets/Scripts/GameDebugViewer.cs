using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A helper script to view static Model data in the Unity Editor Inspector.
/// Attach this to any GameObject in your scene (like GameManager or Canvas) 
/// to monitor Player and Lobby data in real-time.
/// </summary>
public class GameDebugViewer : MonoBehaviour
{
    [Header("Player Data (Live)")]
    public string playerName;
    public int cashBalance;
    public int coins;
    public int level;
    public int xp;

    [Header("Lobby Data (Live)")]
    public bool isLobbyDataLoaded;
    [Tooltip("The 12 live lobby configurations downloaded from the server.")]
    public List<LobbyConfig> lobbyConfigurations = new List<LobbyConfig>();
    
    [Header("Socket.IO Status (Live)")]
    public string socketState = "Not Initialized";
    public string activeMatchId;
    public string activeRoomId;
    public string serverUrl;
    public StartGameData currentMatch;

    private void Update()
    {
        // Update Player Data
        playerName = PlayerModel.Name;
        cashBalance = PlayerModel.CashBalance;
        coins = PlayerModel.Coins;
        level = PlayerModel.Level;
        xp = PlayerModel.Xp;

        // Update Socket Status
        if (SocketManager.Instance != null)
        {
            serverUrl = SocketManager.Instance.GetServerUrl();
            var socket = SocketManager.Instance.Socket;
            if (socket == null)
            {
                socketState = "Socket Null";
            }
            else
            {
                socketState = socket.Manager.State.ToString();
            }
        }
        
        activeMatchId = PlayerModel.ActiveMatchId;
        activeRoomId = PlayerModel.ActiveRoomId;

        // Update Lobby Data
        isLobbyDataLoaded = LobbyModel.IsLoaded;
        
        // Ensure the list has the correct number of elements for viewing
        if (lobbyConfigurations.Count != 12)
        {
            lobbyConfigurations.Clear();
            for (int i = 0; i < 12; i++)
            {
                lobbyConfigurations.Add(null);
            }
        }

        // Copy array data to list for Inspector viewing
        if (LobbyModel.Configurations != null)
        {
            for (int i = 0; i < LobbyModel.Configurations.Length && i < 12; i++)
            {
                lobbyConfigurations[i] = LobbyModel.Configurations[i];
            }
        }
        
        // Update Match Data
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            currentMatch = gm.GetCurrentMatchData();
        }
    }
}
