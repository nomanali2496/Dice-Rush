using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global data model for Lobby Configurations.
/// Connects the Backend API to the Game UI.
/// </summary>
public static class LobbyModel
{
    public static readonly string[] StandardBoardNames = new string[] {
        "Monaco Riviera", "Atlantic Boardwalk", "Macau Dragon", "Vegas Neon", 
        "Singapore Marina", "London Royal", "Player's Arena"
    };

    public static int StandardBoardCount => StandardBoardNames.Length;

    public static LobbyConfig[] Configurations { get; private set; } = new LobbyConfig[0];
    public static bool IsLoaded { get; private set; } = false;
    
    // Event fired whenever the lobby configurations update from the server
    public static event Action OnConfigurationsUpdated;

    // Static constructor
    static LobbyModel()
    {
        InitializeDefaults(7);
    }

    public static void InitializeDefaults(int count = 7)
    {
        Configurations = new LobbyConfig[count];
        
        // Match Entry & Economy from FRD
        int[] entryFees = new int[] { 100, 300, 500, 1000, 2500, 5000, 10000, 25000, 50000, 100000, 250000, 500000 };
        string[] lobbyNames = StandardBoardNames;
        // Small = 3, Medium = 5, Large = 10
        int[] playerCounts = new int[] { 3, 3, 5, 5, 10, 10, 10, 10, 10, 10, 10, 10 };

        for (int i = 0; i < count; i++)
        {
            int pCount = playerCounts[i % playerCounts.Length];
            int fee = entryFees[i % entryFees.Length];
            // Prize pool is roughly 95%, we'll just set firstChips to a dummy value here as backend controls real payout
            int reward = (int)(fee * pCount * 0.95f);

            LobbyConfig cfg = new LobbyConfig(
                index: i,
                players: pCount,
                entryCash: 0f,
                entryChips: fee,
                firstCash: 0f,
                firstChips: reward,
                secondCash: 0f,
                secondChips: 0,
                thirdCash: 0f,
                thirdChips: 0,
                duration: 60f
            );
            cfg.lobbyName = lobbyNames[i % lobbyNames.Length];
            cfg.lobbyId = lobbyNames[i % lobbyNames.Length].ToLower().Replace(" ", "-");
            cfg.mode = $"{pCount}p";
            Configurations[i] = cfg;
        }
        IsLoaded = true;
    }

    /// <summary>
    /// Fetches lobby configurations from the API.
    /// This is the connecting layer between the raw HTTP service and the UI.
    /// </summary>
    public static void FetchConfigurations()
    {
        if (Configurations == null || Configurations.Length == 0)
        {
            InitializeDefaults(7);
        }

        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.GetLobbyConfigurations((success, response) =>
            {
                if (success && response != null && response.data != null && response.data.Count > 0)
                {
                    UpdateConfigurations(response.data);
                }
                else
                {
                    Debug.LogWarning("[LobbyModel] Failed to fetch configurations from API. Using local defaults.");
                    if (Configurations == null || Configurations.Length == 0)
                    {
                        InitializeDefaults(7);
                    }
                    OnConfigurationsUpdated?.Invoke();
                }
            });
        }
        else
        {
            Debug.LogWarning("[LobbyModel] BackendAuthService not found! Using local default configurations.");
            if (Configurations == null || Configurations.Length == 0)
            {
                InitializeDefaults(7);
            }
            OnConfigurationsUpdated?.Invoke();
        }
    }

    private static void UpdateConfigurations(List<BackendAuthService.LobbyApiData> apiData)
    {
        // ✅ FIX: Filter out disabled lobbies BEFORE building the config array.
        // This prevents players from clicking on and submitting findMatch for lobbies
        // that the backend has marked as inactive (enabled: false).
        var enabledData = apiData.FindAll(d => d.enabled);

        Debug.Log($"[LobbyModel] Backend returned {apiData.Count} lobbies. {enabledData.Count} are enabled and will be shown.");

        // Resize the configuration array to match only enabled lobbies
        Configurations = new LobbyConfig[enabledData.Count];

        for (int i = 0; i < enabledData.Count; i++)
        {
            var apiConfig = enabledData[i];

            // Assign standard board index 0..11 if matching, otherwise fallback to array index
            int boardIdx = Array.IndexOf(StandardBoardNames, apiConfig.name);
            int assignedLobbyIndex = boardIdx >= 0 ? boardIdx : i;

            LobbyConfig newConfig = new LobbyConfig(
                assignedLobbyIndex,
                apiConfig.playerCount,
                apiConfig.entryFeeCash > 0 ? apiConfig.entryFeeCash : apiConfig.stake,
                apiConfig.entryFeeCoins > 0 ? apiConfig.entryFeeCoins : apiConfig.stakeCoins,
                apiConfig.firstPlaceCash > 0 ? apiConfig.firstPlaceCash : apiConfig.prizePool,
                apiConfig.firstPlaceCoins > 0 ? apiConfig.firstPlaceCoins : apiConfig.prizePoolCoins,
                apiConfig.secondPlaceCash,
                apiConfig.secondPlaceCoins,
                apiConfig.thirdPlaceCash,
                apiConfig.thirdPlaceCoins,
                apiConfig.gameTimer,
                apiConfig.isArenaPreset,
                apiConfig.arenaStakeMin,
                apiConfig.arenaStakeMax
            );

            newConfig.lobbyName = apiConfig.name;
            newConfig.lobbyId = apiConfig.id;
            newConfig.mode = apiConfig.mode;
            newConfig.imageUrl = apiConfig.image;
            newConfig.enabled = true; // Already filtered, so always true here

            Configurations[i] = newConfig;
        }

        IsLoaded = true;
        OnConfigurationsUpdated?.Invoke();
    }
    
    public static LobbyConfig GetConfig(int index)
    {
        if (index >= 0 && index < Configurations.Length)
        {
            return Configurations[index];
        }
        return null;
    }

    /// <summary>
    /// Gets the configuration for a standard board (0..11) given the desired player count (3, 5, 8, 10).
    /// Filters to non-arena presets and finds the exact variant matching the player count.
    /// </summary>
    public static LobbyConfig GetBoardConfig(int boardIndex, int playerCount)
    {
        if (boardIndex < 0 || boardIndex >= StandardBoardNames.Length) return null;
        string boardName = StandardBoardNames[boardIndex];

        // SPECIAL CASE: Private Table (Player's Arena) doesn't have 3p/5p/10p variants and is an Arena preset.
        // So we just return it exactly as is, regardless of the selected player count.
        if (boardName == "Player's Arena")
        {
            if (Configurations != null)
            {
                for (int i = 0; i < Configurations.Length; i++)
                {
                    if (Configurations[i] != null && Configurations[i].lobbyName == boardName)
                    {
                        return Configurations[i];
                    }
                }
            }
        }

        // 8 players maps to 10 players (Large board) on the backend
        int targetPlayers = (playerCount <= 3) ? 3 : (playerCount <= 5 ? 5 : 10);

        if (Configurations != null && Configurations.Length > 0)
        {
            // 1. Exact match on non-arena board name + target player count
            for (int i = 0; i < Configurations.Length; i++)
            {
                var cfg = Configurations[i];
                if (cfg != null && !cfg.isArenaPreset && cfg.lobbyName == boardName && cfg.numberOfPlayers == targetPlayers)
                {
                    return cfg;
                }
            }

            // 2. Fallback: match non-arena board name
            for (int i = 0; i < Configurations.Length; i++)
            {
                var cfg = Configurations[i];
                if (cfg != null && !cfg.isArenaPreset && cfg.lobbyName == boardName)
                {
                    return cfg;
                }
            }
        }

        return GetConfig(boardIndex);
    }

    /// <summary>
    /// Middle Layer method to request a sprite for a specific lobby.
    /// Decouples the UI from the Backend Service.
    /// </summary>
    public static void LoadLobbySprite(string imageUrl, Action<Sprite> onComplete)
    {
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.LoadRemoteSprite(imageUrl, onComplete);
        }
        else
        {
            onComplete?.Invoke(null);
        }
    }
}
