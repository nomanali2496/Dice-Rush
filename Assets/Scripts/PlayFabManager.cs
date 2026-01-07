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
        Debug.Log($"[PlayFabManager] Attempting to register player: {playerName} with email: {email}");
        
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
        
        // Clear explicit logout flag since user has successfully registered/logged in
        ClearExplicitLogoutFlag();
        
        // Create initial player data
        currentPlayerData = new PlayerData(playerName, email, 1000, 100);
        
        // Save the initial player data
        SavePlayerData(() =>
        {
            Debug.Log("[PlayFabManager] Registration complete and data saved!");
            OnRegisterComplete?.Invoke(true);
        });
    }
    
    private void OnRegisterFailure(PlayFabError error)
    {
        string errorMessage = "Registration failed: " + error.GenerateErrorReport();
        Debug.LogError(errorMessage);
        
        // Provide specific error messages for common registration issues
        if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable || 
            error.ErrorMessage.Contains("EmailAddressNotAvailable"))
        {
            Debug.LogError("Email address is already in use. Please use a different email or try logging in instead.");
        }
        else if (error.Error == PlayFabErrorCode.UsernameNotAvailable ||
                 error.ErrorMessage.Contains("UsernameNotAvailable"))
        {
            Debug.LogError("Username is already taken. Please choose a different username.");
        }
        else if (error.ErrorMessage.Contains("Player creations have been disabled"))
        {
            Debug.LogError("Account creation is disabled in PlayFab settings. " +
                          "Please enable 'Allow Client to Create Players' in PlayFab Dashboard > Settings > API Features.");
        }
        else
        {
            Debug.LogError($"Registration failed: {error.Error} - {error.ErrorMessage}");
        }
        
        OnRegisterComplete?.Invoke(false);
    }
    
    /// <summary>
    /// Login with email and password
    /// </summary>
    public void LoginWithEmail(string email, string password, bool rememberMe = true)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }
    
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("[PlayFabManager] Login successful!");
        playFabId = result.PlayFabId;
        isLoggedIn = true;
        
        Debug.Log($"[PlayFabManager] PlayFabId: {playFabId}, isLoggedIn: {isLoggedIn}");
        
        // Clear explicit logout flag since user has successfully logged in
        ClearExplicitLogoutFlag();
        
        // Store session info if available
        if (result.SessionTicket != null)
        {
            Debug.Log("[PlayFabManager] Session ticket received - session will persist");
        }
        
        // Load player data after successful login
        LoadPlayerData();
        
        OnLoginComplete?.Invoke(true);
    }
    
    private void OnLoginFailure(PlayFabError error)
    {
        string errorMessage = "Login failed: " + error.GenerateErrorReport();
        Debug.LogError(errorMessage);
        
        // Provide specific error messages for login issues
        if (error.Error == PlayFabErrorCode.InvalidAccount)
        {
            Debug.LogError("Invalid email or password. Please check your credentials and try again.");
        }
        else if (error.Error == PlayFabErrorCode.AccountNotFound)
        {
            Debug.LogError("Account not found. Please check your email address or register a new account.");
        }
        else if (error.ErrorMessage.Contains("Player creations have been disabled"))
        {
            Debug.LogError("Guest account creation is disabled in PlayFab settings. " +
                          "Please enable 'Allow Client to Create Players' in PlayFab Dashboard > Settings > API Features.");
        }
        
        OnLoginComplete?.Invoke(false);
    }
    
    /// <summary>
    /// Login with custom ID (for guest/quick play)
    /// </summary>
    public void LoginWithCustomId(string customId, bool createAccount = true)
    {
        // First try to login without creating account (in case account already exists)
        var request = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = false,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        
        PlayFabClientAPI.LoginWithCustomID(request, 
            OnLoginSuccess, 
            error => OnCustomIdLoginFailure(error, customId, createAccount));
    }
    
    private void OnCustomIdLoginFailure(PlayFabError error, string customId, bool createAccount = true)
    {
        // If account doesn't exist, try to create it
        if (error.Error == PlayFabErrorCode.AccountNotFound && createAccount)
        {
            Debug.Log("Account not found, attempting to create new guest account...");
            var createRequest = new LoginWithCustomIDRequest
            {
                CustomId = customId,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true
                }
            };
            
            PlayFabClientAPI.LoginWithCustomID(createRequest, OnLoginSuccess, OnLoginFailure);
        }
        else
        {
            // For other errors (like account creation disabled), show the error
            OnLoginFailure(error);
        }
    }
    
    /// <summary>
    /// Login with Google account
    /// </summary>
    /// <param name="serverAuthCode">Server auth code from Google Sign-In (required for PlayFab)</param>
    public void LoginWithGoogle(string serverAuthCode)
    {
        Debug.Log("[PlayFabManager] Attempting to login with Google...");
        
        if (string.IsNullOrEmpty(serverAuthCode))
        {
            Debug.LogError("[PlayFabManager] Server auth code is required for Google login!");
            OnLoginFailure(new PlayFabError
            {
                Error = PlayFabErrorCode.UnknownError,
                ErrorMessage = "Server auth code is required"
            });
            return;
        }
        
        var request = new LoginWithGoogleAccountRequest
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true, // Create PlayFab account if it doesn't exist
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        
        PlayFabClientAPI.LoginWithGoogleAccount(request, OnLoginSuccess, OnLoginFailure);
    }
    
    /// <summary>
    /// Check if there's an existing PlayFab session
    /// </summary>
    public bool HasExistingSession()
    {
        return PlayFabClientAPI.IsClientLoggedIn();
    }
    
    /// <summary>
    /// Attempt to restore session by getting account info
    /// </summary>
    public void RestoreSession(Action<bool> onComplete = null)
    {
        if (!HasExistingSession())
        {
            Debug.Log("[PlayFabManager] No existing session found");
            onComplete?.Invoke(false);
            return;
        }
        
        Debug.Log("[PlayFabManager] Existing session found, restoring...");
        
        // Get account info to verify session and get PlayFabId
        var request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request,
            result =>
            {
                if (result.AccountInfo != null)
                {
                    playFabId = result.AccountInfo.PlayFabId;
                    isLoggedIn = true;
                    Debug.Log($"[PlayFabManager] Session restored! PlayFabId: {playFabId}");
                    
                    // Load player data
                    LoadPlayerData();
                    
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning("[PlayFabManager] Session exists but account info is null");
                    onComplete?.Invoke(false);
                }
            },
            error =>
            {
                Debug.LogWarning($"[PlayFabManager] Failed to restore session: {error.GenerateErrorReport()}");
                // Session might be invalid, clear it
                PlayFabClientAPI.ForgetAllCredentials();
                onComplete?.Invoke(false);
            });
    }
    
    /// <summary>
    /// Attempt auto-login with device ID (for guest accounts)
    /// </summary>
    public void AttemptAutoLoginWithDeviceId(Action<bool> onComplete = null)
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log($"[PlayFabManager] Attempting auto-login with device ID: {deviceId}");
        
        LoginWithCustomId(deviceId, true);
        
        // We'll use the existing OnLoginComplete event, but we need to track this is an auto-login
        // For now, we'll just let the normal flow handle it
        // The onComplete will be called via the OnLoginComplete event
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
            { "ImageUrl", currentPlayerData.imageUrl },
            { "SelectedAvatarIndex", currentPlayerData.selectedAvatarIndex.ToString() },
            { "SelectedBackgroundIndex", currentPlayerData.selectedBackgroundIndex.ToString() }
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
                float.TryParse(result.Data["Cash"].Value, out currentPlayerData.cash);
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
            
            if (result.Data.ContainsKey("SelectedAvatarIndex"))
            {
                int.TryParse(result.Data["SelectedAvatarIndex"].Value, out currentPlayerData.selectedAvatarIndex);
                Debug.Log($"[PlayFabManager] SelectedAvatarIndex: {currentPlayerData.selectedAvatarIndex}");
            }
            
            if (result.Data.ContainsKey("SelectedBackgroundIndex"))
            {
                int.TryParse(result.Data["SelectedBackgroundIndex"].Value, out currentPlayerData.selectedBackgroundIndex);
                Debug.Log($"[PlayFabManager] SelectedBackgroundIndex: {currentPlayerData.selectedBackgroundIndex}");
            }
            
            Debug.Log($"[PlayFabManager] Final data - Name: {currentPlayerData.playerName}, Cash: {currentPlayerData.cash}, Coins: {currentPlayerData.coins}, Avatar: {currentPlayerData.selectedAvatarIndex}, Background: {currentPlayerData.selectedBackgroundIndex}");
        }
        else
        {
            Debug.LogWarning("[PlayFabManager] No player data found in PlayFab, initializing with defaults.");
            // Initialize guest account with default values
            currentPlayerData = new PlayerData("Guest", "", 1000f, 100);
            // Save the initial data for guest accounts
            SavePlayerData();
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
    public void AddCash(float amount)
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
    public bool SpendCash(float amount)
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
        
        // Mark that user has explicitly logged out - prevent auto-login on next start
        PlayerPrefs.SetInt("UserExplicitlyLoggedOut", 1);
        PlayerPrefs.Save();
        Debug.Log("[PlayFabManager] User explicitly logged out - auto-login will be disabled on next start");
    }
    
    /// <summary>
    /// Check if user has explicitly logged out
    /// </summary>
    public bool HasUserExplicitlyLoggedOut()
    {
        return PlayerPrefs.GetInt("UserExplicitlyLoggedOut", 0) == 1;
    }
    
    /// <summary>
    /// Clear the explicit logout flag (called when user successfully logs in)
    /// </summary>
    private void ClearExplicitLogoutFlag()
    {
        PlayerPrefs.DeleteKey("UserExplicitlyLoggedOut");
        PlayerPrefs.Save();
        Debug.Log("[PlayFabManager] Cleared explicit logout flag - auto-login enabled");
    }
    
    #endregion
}

