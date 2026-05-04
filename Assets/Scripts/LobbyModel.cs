using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global data model for Lobby Configurations.
/// Connects the Backend API to the Game UI.
/// </summary>
public static class LobbyModel
{
    public static LobbyConfig[] Configurations { get; private set; } = new LobbyConfig[12];
    public static bool IsLoaded { get; private set; } = false;
    
    // Event fired whenever the lobby configurations update from the server
    public static event Action OnConfigurationsUpdated;

    // Static constructor sets up default fallback values
    static LobbyModel()
    {
        InitializeDefaults();
    }

    private static void InitializeDefaults()
    {
        for (int i = 0; i < 12; i++)
        {
            Configurations[i] = new LobbyConfig(
                index: i,
                players: 2,
                entryCash: 0f,
                entryChips: 10,
                firstCash: 0f,
                firstChips: 20,
                secondCash: 0f,
                secondChips: 10,
                thirdCash: 0f,
                thirdChips: 5,
                duration: 60f
            );
        }
    }

    /// <summary>
    /// Fetches lobby configurations from the API.
    /// This is the connecting layer between the raw HTTP service and the UI.
    /// </summary>
    public static void FetchConfigurations()
    {
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.GetLobbyConfigurations((success, response) =>
            {
                if (success && response != null && response.data != null)
                {
                    UpdateConfigurations(response.data);
                }
                else
                {
                    Debug.LogWarning("[LobbyModel] Failed to fetch configurations from API. Using local defaults.");
                }
            });
        }
        else
        {
            Debug.LogWarning("[LobbyModel] BackendAuthService not found! Cannot fetch configurations.");
        }
    }

    private static void UpdateConfigurations(List<BackendAuthService.LobbyApiData> apiData)
    {
        foreach (var apiConfig in apiData)
        {
            if (apiConfig.lobbyIndex >= 0 && apiConfig.lobbyIndex < Configurations.Length)
            {
                LobbyConfig newConfig = new LobbyConfig(
                    apiConfig.lobbyIndex,
                    apiConfig.playerCount,
                    apiConfig.entryFeeCash,
                    apiConfig.entryFeeChips,
                    apiConfig.firstPlaceCash,
                    apiConfig.firstPlaceChips,
                    apiConfig.secondPlaceCash,
                    apiConfig.secondPlaceChips,
                    apiConfig.thirdPlaceCash,
                    apiConfig.thirdPlaceChips,
                    apiConfig.gameTimer
                );
                
                newConfig.lobbyName = apiConfig.name;
                Configurations[apiConfig.lobbyIndex] = newConfig;
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
}
