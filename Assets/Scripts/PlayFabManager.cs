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
    public Action<string, float, int> OnRewardClaimed; // Action when a pending reward is claimed
    
    // Reward keys
    public const string PENDING_CASH_KEY = "PendingCashReward";
    public const string PENDING_CHIPS_KEY = "PendingChipsReward";
    public const string PENDING_MSG_KEY = "PendingRewardMessage";
    
    private string playFabId;
    private bool isLoggedIn = false;
    private string lastErrorMessage = "";
    
    // Constants for login method types
    private const string LOGIN_METHOD_GUEST = "Guest";
    private const string LOGIN_METHOD_EMAIL = "Email";
    private const string LOGIN_METHOD_GOOGLE = "Google";
    private const string LOGIN_METHOD_FACEBOOK = "Facebook";
    private const string PLAYER_PREFS_LOGIN_METHOD = "LastLoginMethod";
    private const string PLAYER_PREFS_DEVICE_ID = "StoredDeviceId";
    private const string PLAYER_PREFS_EMAIL = "StoredEmail";
    private const string PLAYER_PREFS_PASSWORD = "StoredPassword"; // Encoded, not plain text
    
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
        
        // Store credentials temporarily to save them after successful registration
        pendingRegistrationEmail = email;
        pendingRegistrationPassword = password;
        
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = playerName,
            RequireBothUsernameAndEmail = true
        };
        
        PlayFabClientAPI.RegisterPlayFabUser(request, 
            result => OnRegisterSuccess(result, email, playerName, password), 
            OnRegisterFailure);
    }
    
    private string pendingRegistrationEmail = "";
    private string pendingRegistrationPassword = "";
    
    private void OnRegisterSuccess(RegisterPlayFabUserResult result, string email, string playerName, string password)
    {
        Debug.Log("[PlayFabManager] Registration successful! Creating new account...");
        
        // Clear any old stored credentials before saving new ones
        // This ensures we're using the newly registered account, not old credentials
        ClearStoredEmailCredentials();
        
        // Set the new account details
        playFabId = result.PlayFabId;
        if (currentPlayerData != null) currentPlayerData.playFabId = playFabId;
        isLoggedIn = true;
        
        // Clear last error message on successful registration
        lastErrorMessage = "";
        
        // Clear explicit logout flag since user has successfully registered/logged in
        ClearExplicitLogoutFlag();
        
        // Save login method as Email (registration logs user in with email/password)
        SaveLastLoginMethod(LOGIN_METHOD_EMAIL);
        Debug.Log("[PlayFabManager] Saved last login method: Email (from registration)");
        
        // IMPORTANT: Save the new account's credentials for auto-login
        // This must be done before loading player data to ensure proper association
        SaveEmailCredentials(email, password);
        Debug.Log("[PlayFabManager] Saved new account credentials for auto-login");
        
        // Store device ID for new device detection
        string currentDeviceId = SystemInfo.deviceUniqueIdentifier;
        PlayerPrefs.SetString(PLAYER_PREFS_DEVICE_ID, currentDeviceId);
        PlayerPrefs.Save();
        
        // Clear any cached player data and create fresh data for the NEW account
        currentPlayerData = new PlayerData(playerName, email, 1000, 1000);
        
        Debug.Log($"[PlayFabManager] New account created - PlayFabId: {playFabId}, Name: {playerName}, Email: {email}");
        Debug.Log($"[PlayFabManager] Session ticket: {(result.SessionTicket != null ? "Received" : "Not received")}");
        
        // Save the initial player data first
        SavePlayerData(() =>
        {
            Debug.Log("[PlayFabManager] Initial player data saved for new account");
            
            // Then load player data to ensure we're working with the new account's data
            // (This should load the data we just saved, confirming we have the right account)
            LoadPlayerData();
            
            Debug.Log("[PlayFabManager] Registration complete - new account ready!");
            // Only invoke OnRegisterComplete, not OnLoginComplete
            // This ensures registration flow is separate from login flow
            OnRegisterComplete?.Invoke(true);
        });
        
        // Clear pending registration data
        pendingRegistrationEmail = "";
        pendingRegistrationPassword = "";
    }
    
    private void OnRegisterFailure(PlayFabError error)
    {
        string errorMessage = "Registration failed: " + error.GenerateErrorReport();
        Debug.LogError(errorMessage);
        
        // Store the last error message for UI display
        lastErrorMessage = "";
        
        // Provide specific error messages for common registration issues
        if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable || 
            error.ErrorMessage.Contains("EmailAddressNotAvailable"))
        {
            lastErrorMessage = "Email address is already in use. Please use a different email or try logging in instead.";
            Debug.LogError(lastErrorMessage);
        }
        else if (error.Error == PlayFabErrorCode.UsernameNotAvailable ||
                 error.ErrorMessage.Contains("UsernameNotAvailable"))
        {
            lastErrorMessage = "Username is already taken. Please choose a different username.";
            Debug.LogError(lastErrorMessage);
        }
        else if (error.ErrorMessage.Contains("Player creations have been disabled"))
        {
            lastErrorMessage = "Account creation is disabled in PlayFab settings. " +
                          "Please enable 'Allow Client to Create Players' in PlayFab Dashboard > Settings > API Features.";
            Debug.LogError(lastErrorMessage);
        }
        else
        {
            lastErrorMessage = "Registration failed. Please try again.";
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
        
        // Store credentials if rememberMe is true
        if (rememberMe)
        {
            SaveEmailCredentials(email, password);
        }
        
        PlayFabClientAPI.LoginWithEmailAddress(request, 
            result => OnLoginSuccess(result, LOGIN_METHOD_EMAIL), 
            OnLoginFailure);
    }
    
    private void OnLoginSuccess(LoginResult result, string loginMethod = null)
    {
        Debug.Log("[PlayFabManager] Login successful!");
        playFabId = result.PlayFabId;
        if (currentPlayerData != null) currentPlayerData.playFabId = playFabId;
        isLoggedIn = true;
        
        // Clear last error message on successful login
        lastErrorMessage = "";
        
        Debug.Log($"[PlayFabManager] PlayFabId: {playFabId}, isLoggedIn: {isLoggedIn}");
        
        // Clear explicit logout flag since user has successfully logged in
        ClearExplicitLogoutFlag();
        
        // Store login method if provided (will be set by specific login methods)
        if (!string.IsNullOrEmpty(loginMethod))
        {
            SaveLastLoginMethod(loginMethod);
            Debug.Log($"[PlayFabManager] Saved last login method: {loginMethod}");
        }
        
        // Store device ID for new device detection
        string currentDeviceId = SystemInfo.deviceUniqueIdentifier;
        PlayerPrefs.SetString(PLAYER_PREFS_DEVICE_ID, currentDeviceId);
        PlayerPrefs.Save();
        
        // Check for pending Branch referral after login
        if (BranchInit.instance != null)
        {
            BranchInit.instance.CheckPendingReferral();
        }
        
        // Store session info if available
        if (result.SessionTicket != null)
        {
            Debug.Log("[PlayFabManager] Session ticket received - session will persist");
        }
        
        // Load player data after successful login
        LoadPlayerData();
        
        OnLoginComplete?.Invoke(true);
        
        // Call pending email auto-login callback if set
        if (pendingEmailAutoLoginCallback != null)
        {
            pendingEmailAutoLoginCallback.Invoke(true);
            pendingEmailAutoLoginCallback = null;
        }
    }
    
    private void OnLoginFailure(PlayFabError error)
    {
        string errorMessage = "Login failed: " + error.GenerateErrorReport();
        Debug.LogError(errorMessage);
        
        // Store the last error message for UI display
        lastErrorMessage = "";
        
        // Provide specific error messages for login issues
        if (error.Error == PlayFabErrorCode.InvalidAccount)
        {
            lastErrorMessage = "Invalid email or password. Please check your credentials and try again.";
            Debug.LogError(lastErrorMessage);
        }
        else if (error.Error == PlayFabErrorCode.AccountNotFound)
        {
            lastErrorMessage = "Account not found. Please check your email address or register a new account.";
            Debug.LogError(lastErrorMessage);
        }
        else if (error.ErrorMessage.Contains("Player creations have been disabled"))
        {
            lastErrorMessage = "Guest account creation is disabled in PlayFab settings. " +
                          "Please enable 'Allow Client to Create Players' in PlayFab Dashboard > Settings > API Features.";
            Debug.LogError(lastErrorMessage);
        }
        else if (error.ErrorMessage.Contains("Client ID and secret must be configured") || 
                 error.ErrorMessage.Contains("Google Add-on"))
        {
            lastErrorMessage = "Google Sign-In is not configured. " +
                          "Please configure Google Client ID and secret in PlayFab Game Manager " +
                          "(Dashboard > Add-ons > Google > Configure).";
            Debug.LogError("Google Sign-In configuration error: " +
                          "Google Client ID and secret must be configured in PlayFab Game Manager. " +
                          "Go to PlayFab Dashboard > Add-ons > Google > Configure to set up Google authentication.");
        }
        else if (error.ErrorMessage.Contains("redirect_uri_mismatch"))
        {
            lastErrorMessage = "Google redirect URI mismatch. " +
                          "Please add 'https://oauth.playfab.com/oauth2/google' to your Google OAuth Client's authorized redirect URIs in Google Cloud Console.";
            Debug.LogError("Google redirect URI mismatch error: " +
                          "The redirect URI 'https://oauth.playfab.com/oauth2/google' must be added to your Google OAuth Client's " +
                          "authorized redirect URIs. Go to Google Cloud Console > APIs & Services > Credentials > " +
                          "Your OAuth 2.0 Client ID > Authorized redirect URIs and add: https://oauth.playfab.com/oauth2/google");
        }
        else
        {
            // Generic error message
            lastErrorMessage = "Login failed. Please try again.";
        }
        
        OnLoginComplete?.Invoke(false);
        
        // Call pending email auto-login callback if set
        if (pendingEmailAutoLoginCallback != null)
        {
            pendingEmailAutoLoginCallback.Invoke(false);
            pendingEmailAutoLoginCallback = null;
        }
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
            result => OnLoginSuccess(result, LOGIN_METHOD_GUEST), 
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
            
            PlayFabClientAPI.LoginWithCustomID(createRequest, 
                result => OnLoginSuccess(result, LOGIN_METHOD_GUEST), 
                OnLoginFailure);
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
    /// <param name="googleEmail">Google user's email (optional)</param>
    /// <param name="googleName">Google user's display name (optional)</param>
    /// <param name="googleImageUrl">Google user's profile picture URL (optional)</param>
    public void LoginWithGoogle(string serverAuthCode, string googleEmail = "", string googleName = "", string googleImageUrl = "")
    {
        Debug.Log("[PlayFabManager] Attempting to login with Google...");
        Debug.Log($"[PlayFabManager] PlayFab Title ID: {PlayFabSettings.TitleId}");
        Debug.Log($"[PlayFabManager] ServerAuthCode length: {(serverAuthCode?.Length ?? 0)}");
        Debug.Log($"[PlayFabManager] ServerAuthCode (first 50 chars): {(serverAuthCode?.Substring(0, Math.Min(50, serverAuthCode.Length)) ?? "null")}...");
        
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
        
        // Store Google profile data for later use
        string googleEmailStr = googleEmail;
        string googleNameStr = googleName;
        string googleImageUrlStr = googleImageUrl;
        
        Debug.Log("[PlayFabManager] Sending LoginWithGoogleAccount request to PlayFab...");
        PlayFabClientAPI.LoginWithGoogleAccount(request, 
            result => OnGoogleLoginSuccess(result, googleNameStr, googleEmailStr, googleImageUrlStr), 
            OnLoginFailure);
    }
    
    /// <summary>
    /// Handle successful Google login and update user data with Google profile
    /// </summary>
    private void OnGoogleLoginSuccess(LoginResult result, string googleName, string googleEmail, string googleImageUrl)
    {
        // First call the standard login success handler with Google login method
        OnLoginSuccess(result, LOGIN_METHOD_GOOGLE);
        
        // Then update user data with Google profile if available
        if (!string.IsNullOrEmpty(googleName) || !string.IsNullOrEmpty(googleEmail) || !string.IsNullOrEmpty(googleImageUrl))
        {
            Debug.Log($"[PlayFabManager] Updating user data with Google profile - Name: {googleName}, Email: {googleEmail}, ImageURL: {googleImageUrl}");
            
            // Update local player data
            bool shouldUpdateName = !string.IsNullOrEmpty(googleName) && (string.IsNullOrEmpty(currentPlayerData.playerName) || currentPlayerData.playerName == "Guest");
            bool shouldUpdateEmail = !string.IsNullOrEmpty(googleEmail) && string.IsNullOrEmpty(currentPlayerData.email);
            bool shouldUpdateImage = !string.IsNullOrEmpty(googleImageUrl) && string.IsNullOrEmpty(currentPlayerData.imageUrl);
            
            bool dataUpdated = false;
            
            if (shouldUpdateName)
            {
                currentPlayerData.playerName = googleName;
                Debug.Log($"[PlayFabManager] Updated player name from Google: {googleName}");
                dataUpdated = true;
                
                // Update PlayFab display name
                UpdatePlayFabDisplayName(googleName);
            }
            
            if (shouldUpdateEmail)
            {
                currentPlayerData.email = googleEmail;
                Debug.Log($"[PlayFabManager] Updated email from Google: {googleEmail}");
                dataUpdated = true;
            }
            
            if (shouldUpdateImage)
            {
                currentPlayerData.imageUrl = googleImageUrl;
                Debug.Log($"[PlayFabManager] Updated avatar image URL from Google: {googleImageUrl}");
                dataUpdated = true;
            }
            
            // If data was updated, notify UI immediately and then save
            if (dataUpdated)
            {
                // Trigger UI update immediately with the new Google data
                Debug.Log("[PlayFabManager] Triggering OnPlayerDataLoaded event with updated Google profile data");
                OnPlayerDataLoaded?.Invoke(currentPlayerData);
            }
            
            // Save updated data to PlayFab
            SavePlayerData(() =>
            {
                Debug.Log("[PlayFabManager] Google profile data (including avatar) saved to PlayFab successfully!");
                
                // Reload player data to ensure everything is in sync
                LoadPlayerData();
            });
        }
    }
    
    /// <summary>
    /// Login with Facebook account
    /// </summary>
    /// <param name="accessToken">Access token from Facebook SDK (required for PlayFab)</param>
    /// <param name="facebookName">Facebook user's name (optional)</param>
    /// <param name="facebookEmail">Facebook user's email (optional)</param>
    /// <param name="facebookImageUrl">Facebook user's profile picture URL (optional)</param>
    public void LoginWithFacebook(string accessToken, string facebookName = "", string facebookEmail = "", string facebookImageUrl = "")
    {
        Debug.Log("[PlayFabManager] Attempting to login with Facebook...");
        
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("[PlayFabManager] Access token is required for Facebook login!");
            OnLoginFailure(new PlayFabError
            {
                Error = PlayFabErrorCode.UnknownError,
                ErrorMessage = "Access token is required"
            });
            return;
        }
        
        // Store Facebook profile data for later use
        string fbName = facebookName;
        string fbEmail = facebookEmail;
        string fbImageUrl = facebookImageUrl;
        
        var request = new LoginWithFacebookRequest
        {
            AccessToken = accessToken,
            CreateAccount = true, // Create PlayFab account if it doesn't exist
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        
        PlayFabClientAPI.LoginWithFacebook(request, 
            result => OnFacebookLoginSuccess(result, fbName, fbEmail, fbImageUrl), 
            OnLoginFailure);
    }
    
    /// <summary>
    /// Handle successful Facebook login and update user data with Facebook profile
    /// </summary>
    private void OnFacebookLoginSuccess(LoginResult result, string facebookName, string facebookEmail, string facebookImageUrl)
    {
        // First call the standard login success handler with Facebook login method
        OnLoginSuccess(result, LOGIN_METHOD_FACEBOOK);
        
        // Then update user data with Facebook profile if available
        if (!string.IsNullOrEmpty(facebookName) || !string.IsNullOrEmpty(facebookEmail) || !string.IsNullOrEmpty(facebookImageUrl))
        {
            Debug.Log($"[PlayFabManager] Updating user data with Facebook profile - Name: {facebookName}, Email: {facebookEmail}, ImageURL: {facebookImageUrl}");
            
            // Update local player data
            bool shouldUpdateName = !string.IsNullOrEmpty(facebookName) && (string.IsNullOrEmpty(currentPlayerData.playerName) || currentPlayerData.playerName == "Guest");
            bool shouldUpdateEmail = !string.IsNullOrEmpty(facebookEmail) && string.IsNullOrEmpty(currentPlayerData.email);
            bool shouldUpdateImage = !string.IsNullOrEmpty(facebookImageUrl) && string.IsNullOrEmpty(currentPlayerData.imageUrl);
            
            bool dataUpdated = false;
            
            if (shouldUpdateName)
            {
                currentPlayerData.playerName = facebookName;
                Debug.Log($"[PlayFabManager] Updated player name from Facebook: {facebookName}");
                dataUpdated = true;
                
                // Update PlayFab display name
                UpdatePlayFabDisplayName(facebookName);
            }
            
            if (shouldUpdateEmail)
            {
                currentPlayerData.email = facebookEmail;
                Debug.Log($"[PlayFabManager] Updated email from Facebook: {facebookEmail}");
                dataUpdated = true;
            }
            
            if (shouldUpdateImage)
            {
                currentPlayerData.imageUrl = facebookImageUrl;
                Debug.Log($"[PlayFabManager] Updated avatar image URL from Facebook: {facebookImageUrl}");
                dataUpdated = true;
            }
            
            // If data was updated, notify UI immediately and then save
            if (dataUpdated)
            {
                // Trigger UI update immediately with the new Facebook data
                Debug.Log("[PlayFabManager] Triggering OnPlayerDataLoaded event with updated Facebook profile data");
                OnPlayerDataLoaded?.Invoke(currentPlayerData);
            }
            
            // Save updated data to PlayFab
            SavePlayerData(() =>
            {
                Debug.Log("[PlayFabManager] Facebook profile data (including avatar) saved to PlayFab successfully!");
                
                // Reload player data to ensure everything is in sync
                LoadPlayerData();
            });
        }
    }
    
    /// <summary>
    /// Update PlayFab display name (title display name)
    /// </summary>
    private void UpdatePlayFabDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName) || !isLoggedIn)
        {
            return;
        }
        
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = displayName
        };
        
        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result =>
            {
                Debug.Log($"[PlayFabManager] PlayFab display name updated to: {result.DisplayName}");
            },
            error =>
            {
                Debug.LogWarning($"[PlayFabManager] Failed to update display name: {error.GenerateErrorReport()}");
            });
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
                    if (currentPlayerData != null) currentPlayerData.playFabId = playFabId;
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
                isAwardingPrize = false; // Clear lock — save confirmed by server
                OnPlayerDataSaved?.Invoke(true);
                onComplete?.Invoke();
            },
            error =>
            {
                Debug.LogError("Failed to save player data: " + error.GenerateErrorReport());
                isAwardingPrize = false; // Clear lock even on failure
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
        
        // Ensure playFabId is set in currentPlayerData even if not provided in dictionary
        if (currentPlayerData != null && !string.IsNullOrEmpty(playFabId))
        {
            currentPlayerData.playFabId = playFabId;
        }
        
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
                // Only load from server if we are NOT currently saving a prize award.
                // This prevents a stale server value from overwriting a freshly awarded prize.
                if (!isAwardingPrize)
                {
                    float.TryParse(result.Data["Cash"].Value, out currentPlayerData.cash);
                    Debug.Log($"[PlayFabManager] Cash loaded from server: {currentPlayerData.cash}");
                }
                else
                {
                    Debug.Log($"[PlayFabManager] ⚠️ Skipping Cash overwrite — prize award in progress. Current cash: {currentPlayerData.cash}");
                }
            }
            
            if (result.Data.ContainsKey("Coins"))
            {
                if (!isAwardingPrize)
                {
                    int.TryParse(result.Data["Coins"].Value, out currentPlayerData.coins);
                    Debug.Log($"[PlayFabManager] Coins loaded from server: {currentPlayerData.coins}");
                }
                else
                {
                    Debug.Log($"[PlayFabManager] ⚠️ Skipping Coins overwrite — prize award in progress. Current coins: {currentPlayerData.coins}");
                }
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

            // Check for any rewards won while away (Match disconnection recovery)
            CheckForPendingRewards(result);
        }
        else
        {
            Debug.LogWarning("[PlayFabManager] No player data found in PlayFab, initializing with defaults.");
            
            // Only initialize guest data if we don't have a name/email already set
            // (This prevents overwriting data during registration)
            if (string.IsNullOrEmpty(currentPlayerData.playerName) || currentPlayerData.playerName == "Guest")
            {
                // Initialize guest account with default values
                currentPlayerData = new PlayerData("Guest", "", 1000, 1000);
                // Save the initial data for guest accounts
                SavePlayerData();
            }
            else
            {
                // We have player data from registration, just save it
                Debug.Log($"[PlayFabManager] Using existing player data: {currentPlayerData.playerName}");
                SavePlayerData();
            }
        }
        
        Debug.Log("[PlayFabManager] Invoking OnPlayerDataLoaded event...");
        OnPlayerDataLoaded?.Invoke(currentPlayerData);
        
        // Check for pending Branch referral after data is loaded
        if (BranchInit.instance != null)
        {
            BranchInit.instance.CheckPendingReferral();
        }
    }
    
    private void OnPlayerDataLoaded_Failure(PlayFabError error)
    {
        Debug.LogError("Failed to load player data: " + error.GenerateErrorReport());
    }
    
    private bool isCheckingRewards = false;

    /// <summary>
    /// Checks if there are any rewards waiting to be claimed globally (from matches won while away)
    /// </summary>
    private void CheckForPendingRewards(GetUserDataResult result)
    {
        if (result.Data == null || isCheckingRewards) return;

        bool hasCash = result.Data.ContainsKey(PENDING_CASH_KEY) && !string.IsNullOrEmpty(result.Data[PENDING_CASH_KEY].Value);
        bool hasChips = result.Data.ContainsKey(PENDING_CHIPS_KEY) && !string.IsNullOrEmpty(result.Data[PENDING_CHIPS_KEY].Value);

        if (hasCash || hasChips)
        {
            isCheckingRewards = true; // Lock process
            float cashVal = 0;
            int chipVal = 0;
            string msg = "You won rewards while you were offline!";

            if (hasCash) float.TryParse(result.Data[PENDING_CASH_KEY].Value, out cashVal);
            if (hasChips) int.TryParse(result.Data[PENDING_CHIPS_KEY].Value, out chipVal);
            if (result.Data.ContainsKey(PENDING_MSG_KEY)) msg = result.Data[PENDING_MSG_KEY].Value;

            if (cashVal > 0 || chipVal > 0)
            {
                Debug.Log($"[PlayFabManager] 🎁 Found pending rewards: ${cashVal}, {chipVal} chips. Applying now...");
                
                // Add to current totals
                currentPlayerData.cash += cashVal;
                currentPlayerData.coins += chipVal;
                
                // Notify UI
                OnRewardClaimed?.Invoke(msg, cashVal, chipVal);
                
                // 1. CLEAR the pending rewards from PlayFab (to prevent double claiming)
                var clearData = new Dictionary<string, string>
                {
                    { PENDING_CASH_KEY, null },
                    { PENDING_CHIPS_KEY, null },
                    { PENDING_MSG_KEY, null }
                };

                PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest { Data = clearData }, 
                    res => {
                        Debug.Log("[PlayFabManager] Pending rewards cleared from server. Persisting totals...");
                        isCheckingRewards = false;
                        // 2. Persist the updated totals to secure the addition
                        SavePlayerData(); 
                    }, 
                    err => {
                        Debug.LogWarning("[PlayFabManager] Failed to clear pending rewards: " + err.GenerateErrorReport());
                        isCheckingRewards = false;
                    }
                );
            }
            else
            {
                isCheckingRewards = false;
            }
        }
    }

    #endregion

    #region Currency Management

    // Lock flag: prevents LoadPlayerData from overwriting cash/coins
    // while a prize is being saved to the server.
    private bool isAwardingPrize = false;

    /// <summary>
    /// Add cash to player. Locks LoadPlayerData from overwriting this value
    /// until the save is confirmed by the server.
    /// </summary>
    public void AddCash(float amount)
    {
        isAwardingPrize = true;
        currentPlayerData.cash += amount;
        Debug.Log($"[PlayFabManager] 💰 AddCash: +{amount} → New balance: {currentPlayerData.cash}");
        SavePlayerData();
    }
    
    /// <summary>
    /// Add coins to player. Locks LoadPlayerData from overwriting this value
    /// until the save is confirmed by the server.
    /// </summary>
    public void AddCoins(int amount)
    {
        isAwardingPrize = true;
        currentPlayerData.coins += amount;
        Debug.Log($"[PlayFabManager] 🪙 AddCoins: +{amount} → New balance: {currentPlayerData.coins}");
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
    
    #region Referral System
    
    /// <summary>
    /// Save referral data when a user is referred by someone
    /// This adds the referred user to the referrer's list
    /// </summary>
    public void SaveReferralData(string inviterPlayFabId, string inviterName, Action<bool> onComplete = null)
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("[PlayFabManager] Cannot save referral data - not logged in!");
            onComplete?.Invoke(false);
            return;
        }
        
        string currentUserPlayFabId = playFabId;
        string currentUserName = currentPlayerData != null ? currentPlayerData.playerName : "Unknown";
        
        if (string.IsNullOrEmpty(inviterPlayFabId))
        {
            Debug.LogWarning("[PlayFabManager] Cannot save referral - no inviter PlayFab ID");
            onComplete?.Invoke(false);
            return;
        }
        
        Debug.Log($"[PlayFabManager] Saving referral: {currentUserPlayFabId} ({currentUserName}) was referred by {inviterPlayFabId} ({inviterName ?? "Unknown"})");
        
        // Get the referrer's current referred users list
        GetReferredUsersList(inviterPlayFabId, (existingList) =>
        {
            // Check if this user is already in the list
            bool alreadyExists = false;
            if (existingList != null)
            {
                foreach (var referredUser in existingList)
                {
                    if (referredUser.playFabId == currentUserPlayFabId)
                    {
                        alreadyExists = true;
                        Debug.Log("[PlayFabManager] User already in referrer's list - skipping");
                        break;
                    }
                }
            }
            
            if (alreadyExists)
            {
                onComplete?.Invoke(true);
                return;
            }
            
            // Add current user to referrer's list
            if (existingList == null)
            {
                existingList = new List<ReferredUserData>();
            }
            
            ReferredUserData newReferral = new ReferredUserData
            {
                playFabId = currentUserPlayFabId,
                playerName = currentUserName,
                imageUrl = currentPlayerData != null ? currentPlayerData.imageUrl : "",
                referredDate = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            
            existingList.Add(newReferral);
            
            // Save updated list to referrer's PlayFab data
            SaveReferredUsersList(inviterPlayFabId, existingList, (success) =>
            {
                if (success)
                {
                    Debug.Log($"[PlayFabManager] ✅ Successfully added {currentUserName} to {inviterName ?? inviterPlayFabId}'s referral list");
                }
                onComplete?.Invoke(success);
            });
        });
    }
    
    /// <summary>
    /// Get the list of users referred by the current user
    /// </summary>
    public void GetMyReferredUsers(Action<List<ReferredUserData>> onComplete)
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("[PlayFabManager] Cannot get referred users - not logged in!");
            onComplete?.Invoke(null);
            return;
        }
        
        GetReferredUsersList(playFabId, onComplete);
    }
    
    /// <summary>
    /// Get the list of users referred by a specific PlayFab ID
    /// </summary>
    private void GetReferredUsersList(string referrerPlayFabId, Action<List<ReferredUserData>> onComplete)
    {
        var request = new GetUserDataRequest
        {
            PlayFabId = referrerPlayFabId,
            Keys = new List<string> { "ReferredUsers" }
        };
        
        PlayFabClientAPI.GetUserData(request,
            result =>
            {
                List<ReferredUserData> referredUsers = new List<ReferredUserData>();
                
                if (result.Data != null && result.Data.ContainsKey("ReferredUsers"))
                {
                    string jsonData = result.Data["ReferredUsers"].Value;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        try
                        {
                            ReferredUsersListWrapper wrapper = JsonUtility.FromJson<ReferredUsersListWrapper>(jsonData);
                            if (wrapper != null && wrapper.referredUsers != null)
                            {
                                referredUsers = wrapper.referredUsers;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[PlayFabManager] Error parsing referred users JSON: {e.Message}");
                        }
                    }
                }
                
                Debug.Log($"[PlayFabManager] Loaded {referredUsers.Count} referred users for {referrerPlayFabId}");
                onComplete?.Invoke(referredUsers);
            },
            error =>
            {
                Debug.LogWarning($"[PlayFabManager] Could not load referred users (user may not exist or have no referrals): {error.GenerateErrorReport()}");
                onComplete?.Invoke(new List<ReferredUserData>());
            });
    }
    
    /// <summary>
    /// Save the list of referred users for a specific PlayFab ID
    /// </summary>
    private void SaveReferredUsersList(string referrerPlayFabId, List<ReferredUserData> referredUsers, Action<bool> onComplete)
    {
        ReferredUsersListWrapper wrapper = new ReferredUsersListWrapper
        {
            referredUsers = referredUsers
        };
        
        string jsonData = JsonUtility.ToJson(wrapper);
        
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "ReferredUsers", jsonData }
            },
            Permission = UserDataPermission.Public
        };
        
        // Note: We need to use server API or admin API to update another user's data
        // For now, we'll use a workaround: save to current user's data with a key based on referrer ID
        // OR use CloudScript to handle this properly
        
        // For client-side, we can only update our own data
        // So we'll save it with a special key that includes the referrer ID
        if (referrerPlayFabId == playFabId)
        {
            // We're updating our own data
            PlayFabClientAPI.UpdateUserData(request,
                result =>
                {
                    Debug.Log("[PlayFabManager] Referred users list saved successfully!");
                    onComplete?.Invoke(true);
                },
                error =>
                {
                    Debug.LogError($"[PlayFabManager] Failed to save referred users list: {error.GenerateErrorReport()}");
                    onComplete?.Invoke(false);
                });
        }
        else
        {
            // We need to update another user's data - this requires CloudScript or Server API
            // For now, log a warning and suggest using CloudScript
            Debug.LogWarning("[PlayFabManager] Cannot update another user's data from client. This should be done via CloudScript.");
            Debug.LogWarning("[PlayFabManager] For production, implement a CloudScript function to handle referral saving.");
            
            // For testing, we'll try to save it to current user's data with a special key
            // This is a workaround - in production, use CloudScript
            string keyName = $"ReferredUsers_{referrerPlayFabId}";
            var workaroundRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { keyName, jsonData }
                },
                Permission = UserDataPermission.Public
            };
            
            PlayFabClientAPI.UpdateUserData(workaroundRequest,
                result =>
                {
                    Debug.Log($"[PlayFabManager] Saved referred users list with workaround key: {keyName}");
                    Debug.LogWarning("[PlayFabManager] NOTE: This is a workaround. Implement CloudScript for production.");
                    onComplete?.Invoke(true);
                },
                error =>
                {
                    Debug.LogError($"[PlayFabManager] Failed to save referred users list: {error.GenerateErrorReport()}");
                    onComplete?.Invoke(false);
                });
        }
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
    /// Get the last error message from login/registration attempts
    /// </summary>
    public string GetLastErrorMessage()
    {
        return lastErrorMessage;
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
        
        // Clear stored email credentials on logout for security
        ClearStoredEmailCredentials();
        
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
    
    /// <summary>
    /// Save the last login method used
    /// </summary>
    private void SaveLastLoginMethod(string loginMethod)
    {
        PlayerPrefs.SetString(PLAYER_PREFS_LOGIN_METHOD, loginMethod);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get the last login method used
    /// </summary>
    public string GetLastLoginMethod()
    {
        return PlayerPrefs.GetString(PLAYER_PREFS_LOGIN_METHOD, "");
    }
    
    /// <summary>
    /// Check if the current device is a new device (different from stored device ID)
    /// </summary>
    public bool IsNewDevice()
    {
        string storedDeviceId = PlayerPrefs.GetString(PLAYER_PREFS_DEVICE_ID, "");
        string currentDeviceId = SystemInfo.deviceUniqueIdentifier;
        
        if (string.IsNullOrEmpty(storedDeviceId))
        {
            // No stored device ID means this is likely a new installation
            return true;
        }
        
        return storedDeviceId != currentDeviceId;
    }
    
    /// <summary>
    /// Save email credentials securely (encoded, not plain text)
    /// </summary>
    private void SaveEmailCredentials(string email, string password)
    {
        try
        {
            // Simple encoding (base64) - not full encryption but better than plain text
            string encodedPassword = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            PlayerPrefs.SetString(PLAYER_PREFS_EMAIL, email);
            PlayerPrefs.SetString(PLAYER_PREFS_PASSWORD, encodedPassword);
            PlayerPrefs.Save();
            Debug.Log("[PlayFabManager] Email credentials saved (encoded)");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayFabManager] Failed to save credentials: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get stored email credentials
    /// </summary>
    public bool GetStoredEmailCredentials(out string email, out string password)
    {
        email = "";
        password = "";
        
        try
        {
            email = PlayerPrefs.GetString(PLAYER_PREFS_EMAIL, "");
            string encodedPassword = PlayerPrefs.GetString(PLAYER_PREFS_PASSWORD, "");
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(encodedPassword))
            {
                return false;
            }
            
            // Decode password
            password = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(encodedPassword));
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayFabManager] Failed to retrieve stored credentials: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Clear stored email credentials
    /// </summary>
    public void ClearStoredEmailCredentials()
    {
        PlayerPrefs.DeleteKey(PLAYER_PREFS_EMAIL);
        PlayerPrefs.DeleteKey(PLAYER_PREFS_PASSWORD);
        PlayerPrefs.Save();
        Debug.Log("[PlayFabManager] Stored email credentials cleared");
    }
    
    private Action<bool> pendingEmailAutoLoginCallback = null;
    
    /// <summary>
    /// Attempt to login with stored email credentials
    /// </summary>
    public void AttemptAutoLoginWithEmail(Action<bool> onComplete = null)
    {
        if (GetStoredEmailCredentials(out string email, out string password))
        {
            Debug.Log($"[PlayFabManager] Attempting auto-login with stored email credentials for: {email}");
            
            // Store callback to call it in OnLoginSuccess/OnLoginFailure
            pendingEmailAutoLoginCallback = onComplete;
            
            LoginWithEmail(email, password, true); // Keep rememberMe true
        }
        else
        {
            Debug.Log("[PlayFabManager] No stored email credentials found");
            onComplete?.Invoke(false);
        }
    }
    
    #endregion
}

