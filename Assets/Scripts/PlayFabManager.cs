using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages PlayFab authentication and player data
/// </summary>
public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }
    
    [Header("Player Data")]
    public PlayerData currentPlayerData;
    
    [Header("Events")]
    public Action<bool> OnLoginComplete;
    public Action<bool> OnRegisterComplete;
    public Action<PlayerData> OnPlayerDataLoaded;
    public Action<bool> OnPlayerDataSaved;
    
    private string playFabId;
    private bool isLoggedIn = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlayFabManager] Instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Debug.Log("[PlayFabManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
        
        currentPlayerData = new PlayerData();
        Debug.Log("[PlayFabManager] Initialized with empty PlayerData");
    }
    
    #region Authentication
    
    /// <summary>
    /// Register a new player account
    /// </summary>
    public void RegisterPlayer(string email, string password, string playerName)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = playerName,
            RequireBothUsernameAndEmail = true
        };
        
        PlayFabClientAPI.RegisterPlayFabUser(request, 
            result => OnRegisterSuccess(result, email, playerName), 
            OnRegisterFailure);
    }
    
    private void OnRegisterSuccess(RegisterPlayFabUserResult result, string email, string playerName)
    {
        Debug.Log("Registration successful!");
        playFabId = result.PlayFabId;
        isLoggedIn = true;
        
        // Create initial player data
        currentPlayerData = new PlayerData(playerName, email, 1000, 100);
        
        // Save the initial player data
        SavePlayerData(() =>
        {
            OnRegisterComplete?.Invoke(true);
        });
    }
    
    private void OnRegisterFailure(PlayFabError error)
    {
        Debug.LogError("Registration failed: " + error.GenerateErrorReport());
        OnRegisterComplete?.Invoke(false);
    }
    
    /// <summary>
    /// Login with email and password
    /// </summary>
    public void LoginWithEmail(string email, string password)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password
        };
        
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }
    
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("[PlayFabManager] Login successful!");
        playFabId = result.PlayFabId;
        isLoggedIn = true;
        
        Debug.Log($"[PlayFabManager] PlayFabId: {playFabId}, isLoggedIn: {isLoggedIn}");
        
        // Load player data after successful login
        LoadPlayerData();
        
        OnLoginComplete?.Invoke(true);
    }
    
    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError("Login failed: " + error.GenerateErrorReport());
        OnLoginComplete?.Invoke(false);
    }
    
    /// <summary>
    /// Login with custom ID (for guest/quick play)
    /// </summary>
    public void LoginWithCustomId(string customId)
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };
        
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }
    
    #endregion
    
    #region Player Data Management
    
    /// <summary>
    /// Save player data to PlayFab
    /// </summary>
    public void SavePlayerData(Action onComplete = null)
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("Cannot save player data: Not logged in!");
            OnPlayerDataSaved?.Invoke(false);
            onComplete?.Invoke();
            return;
        }
        
        var data = new Dictionary<string, string>
        {
            { "PlayerName", currentPlayerData.playerName },
            { "Email", currentPlayerData.email },
            { "Cash", currentPlayerData.cash.ToString() },
            { "Coins", currentPlayerData.coins.ToString() },
            { "ImageUrl", currentPlayerData.imageUrl }
        };
        
        var request = new UpdateUserDataRequest
        {
            Data = data,
            Permission = UserDataPermission.Public
        };
        
        PlayFabClientAPI.UpdateUserData(request, 
            result =>
            {
                Debug.Log("Player data saved successfully!");
                OnPlayerDataSaved?.Invoke(true);
                onComplete?.Invoke();
            },
            error =>
            {
                Debug.LogError("Failed to save player data: " + error.GenerateErrorReport());
                OnPlayerDataSaved?.Invoke(false);
                onComplete?.Invoke();
            });
    }
    
    /// <summary>
    /// Load player data from PlayFab
    /// </summary>
    public void LoadPlayerData()
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("Cannot load player data: Not logged in!");
            return;
        }
        
        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId
        };
        
        PlayFabClientAPI.GetUserData(request, OnPlayerDataLoaded_Success, OnPlayerDataLoaded_Failure);
    }
    
    private void OnPlayerDataLoaded_Success(GetUserDataResult result)
    {
        Debug.Log("[PlayFabManager] Player data loaded successfully from PlayFab!");
        
        if (result.Data != null && result.Data.Count > 0)
        {
            Debug.Log($"[PlayFabManager] Received {result.Data.Count} data fields");
            
            // Parse the data
            if (result.Data.ContainsKey("PlayerName"))
            {
                currentPlayerData.playerName = result.Data["PlayerName"].Value;
                Debug.Log($"[PlayFabManager] PlayerName: {currentPlayerData.playerName}");
            }
            
            if (result.Data.ContainsKey("Email"))
            {
                currentPlayerData.email = result.Data["Email"].Value;
                Debug.Log($"[PlayFabManager] Email: {currentPlayerData.email}");
            }
            
            if (result.Data.ContainsKey("Cash"))
            {
                int.TryParse(result.Data["Cash"].Value, out currentPlayerData.cash);
                Debug.Log($"[PlayFabManager] Cash: {currentPlayerData.cash}");
            }
            
            if (result.Data.ContainsKey("Coins"))
            {
                int.TryParse(result.Data["Coins"].Value, out currentPlayerData.coins);
                Debug.Log($"[PlayFabManager] Coins: {currentPlayerData.coins}");
            }
            
            if (result.Data.ContainsKey("ImageUrl"))
            {
                currentPlayerData.imageUrl = result.Data["ImageUrl"].Value;
                Debug.Log($"[PlayFabManager] ImageUrl: {currentPlayerData.imageUrl}");
            }
            
            Debug.Log($"[PlayFabManager] Final data - Name: {currentPlayerData.playerName}, Cash: {currentPlayerData.cash}, Coins: {currentPlayerData.coins}");
        }
        else
        {
            Debug.LogWarning("[PlayFabManager] No player data found in PlayFab, using defaults.");
            currentPlayerData = new PlayerData();
        }
        
        Debug.Log("[PlayFabManager] Invoking OnPlayerDataLoaded event...");
        OnPlayerDataLoaded?.Invoke(currentPlayerData);
    }
    
    private void OnPlayerDataLoaded_Failure(PlayFabError error)
    {
        Debug.LogError("Failed to load player data: " + error.GenerateErrorReport());
    }
    
    #endregion
    
    #region Currency Management
    
    /// <summary>
    /// Add cash to player
    /// </summary>
    public void AddCash(int amount)
    {
        currentPlayerData.cash += amount;
        SavePlayerData();
    }
    
    /// <summary>
    /// Add coins to player
    /// </summary>
    public void AddCoins(int amount)
    {
        currentPlayerData.coins += amount;
        SavePlayerData();
    }
    
    /// <summary>
    /// Spend cash
    /// </summary>
    public bool SpendCash(int amount)
    {
        if (currentPlayerData.cash >= amount)
        {
            currentPlayerData.cash -= amount;
            SavePlayerData();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Spend coins
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (currentPlayerData.coins >= amount)
        {
            currentPlayerData.coins -= amount;
            SavePlayerData();
            return true;
        }
        return false;
    }
    
    #endregion
    
    #region Profile Image
    
    /// <summary>
    /// Update player profile image URL
    /// </summary>
    public void UpdateProfileImage(string imageUrl)
    {
        currentPlayerData.imageUrl = imageUrl;
        SavePlayerData();
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Check if player is logged in
    /// </summary>
    public bool IsLoggedIn()
    {
        return isLoggedIn;
    }
    
    /// <summary>
    /// Get PlayFab ID
    /// </summary>
    public string GetPlayFabId()
    {
        return playFabId;
    }
    
    /// <summary>
    /// Logout player
    /// </summary>
    public void Logout()
    {
        isLoggedIn = false;
        playFabId = "";
        currentPlayerData = new PlayerData();
        PlayFabClientAPI.ForgetAllCredentials();
    }
    
    #endregion
}

