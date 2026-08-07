using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global data model for Lobby Configurations.
/// Connects the Backend API to the Game UI.
/// </summary>
public static class LobbyModel
{
    public static LobbyConfig[] Configurations { get; private set; } = new LobbyConfig[0];
    public static bool IsLoaded { get; private set; } = false;
    
    // Event fired whenever the lobby configurations update from the server
    public static event Action OnConfigurationsUpdated;

    // Static constructor
    static LobbyModel()
    {
        InitializeDefaults(12);
    }

    public static void InitializeDefaults(int count = 12)
    {
        Configurations = new LobbyConfig[count];
        int[] playerCounts = new int[] { 3, 5, 8, 10 };
        int[] entryFees = new int[] { 100, 250, 500, 1000, 2500, 5000, 10000, 25000, 50000, 100000, 250000, 500000 };

        string[] defaultLobbyIds = new string[] { "combo-dice", "dice-island", "dice-race", "dice-royale", "golden-throw", "lucky-rollers" };

        for (int i = 0; i < count; i++)
        {
            int pCount = playerCounts[i % playerCounts.Length];
            int fee = entryFees[i % entryFees.Length];
            int reward = fee * pCount;

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
            cfg.lobbyName = $"Board {i + 1}";
            cfg.lobbyId = defaultLobbyIds[i % defaultLobbyIds.Length];
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
            InitializeDefaults(12);
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
                        InitializeDefaults(12);
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
                InitializeDefaults(12);
            }
            OnConfigurationsUpdated?.Invoke();
        }
    }

    private static void UpdateConfigurations(List<BackendAuthService.LobbyApiData> apiData)
    {
        // Resize the configuration array to match exactly what the server returned
        Configurations = new LobbyConfig[apiData.Count];

        for (int i = 0; i < apiData.Count; i++)
        {
            var apiConfig = apiData[i];
            
            // Use the loop index (i) as the SSOT index.
            int indexToUse = i;

            if (indexToUse >= 0 && indexToUse < Configurations.Length)
            {
                LobbyConfig newConfig = new LobbyConfig(
                    indexToUse,
                    apiConfig.playerCount,
                    apiConfig.entryFeeCash,
                    apiConfig.entryFeeCoins,
                    apiConfig.firstPlaceCash,
                    apiConfig.firstPlaceCoins,
                    apiConfig.secondPlaceCash,
                    apiConfig.secondPlaceCoins,
                    apiConfig.thirdPlaceCash,
                    apiConfig.thirdPlaceCoins,
                    apiConfig.gameTimer
                );
                
                newConfig.lobbyName = apiConfig.name;
                newConfig.lobbyId = apiConfig.id; // Map the database slug
                newConfig.mode = apiConfig.mode; // Map the database mode ("2p", "5p", etc)
                newConfig.imageUrl = apiConfig.image; // Map the remote image path
                
                Configurations[indexToUse] = newConfig;
            }
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
