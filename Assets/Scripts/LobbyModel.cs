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
        // Start with empty or small default if needed, but FetchConfigurations will resize it.
    }

    private static void InitializeDefaults(int count)
    {
        Configurations = new LobbyConfig[count];
        for (int i = 0; i < count; i++)
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
