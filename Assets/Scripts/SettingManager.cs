using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game settings including music toggle on/off
/// Handles saving and loading settings using PlayerPrefs (account-based)
/// </summary>

public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;
    [Header("Music Settings")]
    [SerializeField] private Toggle musicToggle; // Toggle UI for music on/off
    
    [Header("Sound Settings")]
    [SerializeField] private Toggle soundToggle; // Toggle UI for sound effects on/off

    [Header("Game Reference")]
    [SerializeField] private GameObject mainMenu; // Reference to Main Menu / Lobby GameObject
    [SerializeField] private GameObject gameplayScreen; // Reference to Gameplay GameObject
    
    [Header("Debug Info")]
    [SerializeField] private string currentUserId = ""; // Current user ID (for account-specific storage)
    [SerializeField] private string storageKey = "N/A"; // PlayerPrefs key being used for storage
    
    // PlayerPrefs keys for music and sound (device-wide)
    private const string MUSIC_ENABLED_KEY = "MusicEnabled";
    private const string SOUND_ENABLED_KEY = "SoundEnabled";
    
    private bool isMusicEnabled = true; // Default music state (enabled)
    private bool isSoundEnabled = true; // Default sound state (enabled)
    private bool hasUserLoggedIn = false; // Track if user has logged in
    
    // Reference to AuthenticationManager for login events
    private AuthenticationManager authManager;
      void Awake()
    {
        instance = this;
    }

    private bool lastMainMenuActiveState = false; // Track previous state to detect changes
    private bool lastGameplayActiveState = false; // Track previous gameplay state
    private bool isCountdownActive = false; // Track if countdown is currently running (to delay gameplay music)

    private void Start()
    {
        // Find AuthenticationManager in scene
        authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess += OnUserLoggedIn;
        }

        // Subscribe to clean logic events for music shifting
        MatchmakingModel.OnMatchFound += HandleMatchFound;
        MatchmakingModel.OnMatchmakingFailed += HandleMatchmakingFailed;
        GameManager.OnGameStarted += HandleGameStarted;
        GameManager.OnGameEnded += HandleGameEnded;
        
        // Ensure music is stopped initially (before login)
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopAllMusic();
        }
        
        // Check if user is already logged in (Server JWT)
        if (BackendAuthService.HasStoredJWT())
        {
            OnUserLoggedIn();
        }
        
        // Initialize state tracker
        if (mainMenu != null)
        {
            lastMainMenuActiveState = mainMenu.activeInHierarchy;
        }
        
        // Setup toggles
        if (musicToggle != null) musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
        if (soundToggle != null) soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
    }

    private void Update()
    {
        // Check if Main Menu just became active (e.g., after Splash Screen finishes)
        if (mainMenu != null)
        {
            bool isMainMenuActive = mainMenu.activeInHierarchy;
            if (isMainMenuActive != lastMainMenuActiveState)
            {
                lastMainMenuActiveState = isMainMenuActive;
                if (isMainMenuActive)
                {
                    Debug.Log("[SettingManager] Main Menu became active. Checking music...");
                    ApplyMusicSetting();
                }
            }
        }
    }

    #region Event Handlers
    
    private void HandleMatchFound(StartGameData data)
    {
        // Stop lobby music as soon as match is found (during loading/transition)
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopBackgroundMusic();
        }
    }

    private void HandleMatchmakingFailed(string error)
    {
        // Resume lobby music if match fails
        ApplyMusicSetting();
    }

    private void HandleGameStarted()
    {
        // Play gameplay music when countdown is done
        if (isMusicEnabled && SoundManager.instance != null)
        {
            // HARD STOP for background music to ensure no overlap
            SoundManager.instance.StopBackgroundMusic();
            SoundManager.instance.PlayGameplayMusic();
        }
    }

    private void HandleGameEnded()
    {
        // Return to lobby music when results screen appears
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopGameplayMusic();
            ApplyMusicSetting(); // Re-applies lobby music if enabled
        }
    }
    
    #endregion
    
    /// <summary>
    /// Called when user successfully logs in - gets the user ID for account-specific storage
    /// </summary>
    private void OnUserLoggedIn()
    {
        hasUserLoggedIn = true;
        
        // Get the current user's ID for account-specific storage (if needed later)
        currentUserId = PlayerModel.Id;
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("[SettingManager] User ID is empty, using device-based storage");
        }
        else
        {
            Debug.Log($"[SettingManager] User logged in with ID: {currentUserId}");
        }
        
        // Update storage key immediately
        UpdateStorageKeyDisplay();
        
        // Load account-specific settings
        LoadMusicSetting();
        LoadSoundSetting();
        
        // Update toggles to match loaded settings
        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicEnabled;
        }
        if (soundToggle != null)
        {
            soundToggle.isOn = isSoundEnabled;
        }
        
        // Apply settings (music will start playing now if enabled)
        ApplyMusicSetting();
        Debug.Log("[SettingManager] Settings loaded and applied after user login");
    }
    
    /// <summary>
    /// Called when user logs out - clears current user data and stops music
    /// </summary>
    public void OnUserLoggedOut()
    {
        hasUserLoggedIn = false;
        Debug.Log("[SettingManager] User logged out, clearing user data and stopping music");
        
        // Stop music when user logs out
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopAllMusic();
        }
        
        currentUserId = "";
        UpdateStorageKeyDisplay();
        
        // Reset to defaults (settings will be loaded when new user logs in)
        isMusicEnabled = true;
        isSoundEnabled = true;
        
        // Update toggles to defaults
        if (musicToggle != null)
        {
            musicToggle.isOn = true;
        }
        if (soundToggle != null)
        {
            soundToggle.isOn = true;
        }
    }
    
    /// <summary>
    /// Updates the storage key display in the editor
    /// </summary>
    private void UpdateStorageKeyDisplay()
    {
        storageKey = GetMusicEnabledKey();
    }
    
    /// <summary>
    /// Called when music toggle value changes
    /// </summary>
    private void OnMusicToggleChanged(bool isOn)
    {
        isMusicEnabled = isOn;
        SaveMusicSetting();
        ApplyMusicSetting();
        Debug.Log($"[SettingManager] Music toggle changed to: {(isMusicEnabled ? "ON" : "OFF")}");
    }
    
    /// <summary>
    /// Called when sound toggle value changes
    /// </summary>
    private void OnSoundToggleChanged(bool isOn)
    {
        isSoundEnabled = isOn;
        SaveSoundSetting();
        Debug.Log($"[SettingManager] Sound toggle changed to: {(isSoundEnabled ? "ON" : "OFF")}");
    }
    
    /// <summary>
    /// Applies the current music setting to the AudioSource
    /// Logic: 
    /// 1. If Game Active -> Play Gameplay Music, Stop Normal
    /// 2. If Game Not Active (Lobby/Winner) -> Play Normal Music, Stop Gameplay
    /// </summary>
    public void ApplyMusicSetting()
    {
        Debug.Log($"[SettingManager] Applying Music Setting... Enabled: {isMusicEnabled}");

        // 1. Check Login Status (Custom Server JWT)
        if (BackendAuthService.Instance == null || !BackendAuthService.HasStoredJWT())
        {
            Debug.Log("[SettingManager] Not logged in to server yet. Stopping music.");
            StopAllMusic();
            return;
        }

        // 2. Check if Main Menu is actually visible
        bool isMenuActive = (mainMenu != null && mainMenu.activeInHierarchy);
        Debug.Log($"[SettingManager] Menu Active: {isMenuActive}");

        // 3. Apply Settings
        if (isMusicEnabled)
        {
            if (isMenuActive)
            {
                if (SoundManager.instance == null) SoundManager.instance = FindObjectOfType<SoundManager>();
                if (SoundManager.instance != null) SoundManager.instance.PlayBackgroundMusic();
            }
            // (Note: Gameplay music is handled by its own event-driven logic)
        }
        else
        {
            // Music disabled globally - stop EVERYTHING
            Debug.Log("[SettingManager] Music disabled. Stopping all music sources.");
            StopAllMusic();
        }
    }

    private void StopAllMusic()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.StopAllMusic();
        }
    }
    
    /// <summary>
    /// Gets the PlayerPrefs key for storing music setting
    /// </summary>
    private string GetMusicEnabledKey()
    {
        return MUSIC_ENABLED_KEY;
    }
    
    /// <summary>
    /// Gets the PlayerPrefs key for storing sound setting
    /// </summary>
    private string GetSoundEnabledKey()
    {
        return SOUND_ENABLED_KEY;
    }
    
    /// <summary>
    /// Loads music setting from PlayerPrefs (account-specific)
    /// Defaults to true (enabled) if key doesn't exist
    /// </summary>
    private void LoadMusicSetting()
    {
        string key = GetMusicEnabledKey();
        
        // Default to true (enabled) if key doesn't exist
        if (PlayerPrefs.HasKey(key))
        {
            isMusicEnabled = PlayerPrefs.GetInt(key, 1) == 1;
            Debug.Log($"[SettingManager] Music setting loaded from account: {(isMusicEnabled ? "ON" : "OFF")} (Key: {key})");
        }
        else
        {
            isMusicEnabled = true; // Default enabled
            SaveMusicSetting(); // Save default value
            Debug.Log($"[SettingManager] New account/device detected - defaulting music to ON (Key: {key})");
        }
    }
    
    /// <summary>
    /// Saves music setting to PlayerPrefs (account-specific)
    /// </summary>
    private void SaveMusicSetting()
    {
        string key = GetMusicEnabledKey();
        PlayerPrefs.SetInt(key, isMusicEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[SettingManager] Music setting saved to account: {(isMusicEnabled ? "ON" : "OFF")} (Key: {key})");
    }
    
    /// <summary>
    /// Loads sound setting from PlayerPrefs (account-specific)
    /// Defaults to true (enabled) if key doesn't exist
    /// </summary>
    private void LoadSoundSetting()
    {
        string key = GetSoundEnabledKey();
        
        // Default to true (enabled) if key doesn't exist
        if (PlayerPrefs.HasKey(key))
        {
            isSoundEnabled = PlayerPrefs.GetInt(key, 1) == 1;
            Debug.Log($"[SettingManager] Sound setting loaded from account: {(isSoundEnabled ? "ON" : "OFF")} (Key: {key})");
        }
        else
        {
            isSoundEnabled = true; // Default enabled
            SaveSoundSetting(); // Save default value
            Debug.Log($"[SettingManager] New account/device detected - defaulting sound to ON (Key: {key})");
        }
    }
    
    /// <summary>
    /// Saves sound setting to PlayerPrefs (account-specific)
    /// </summary>
    private void SaveSoundSetting()
    {
        string key = GetSoundEnabledKey();
        PlayerPrefs.SetInt(key, isSoundEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[SettingManager] Sound setting saved to account: {(isSoundEnabled ? "ON" : "OFF")} (Key: {key})");
    }
    
    /// <summary>
    /// Gets the current music enabled state
    /// </summary>
    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }
    
    /// <summary>
    /// Sets music enabled state programmatically
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        
        // Update toggle if assigned
        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicEnabled;
        }
        
        SaveMusicSetting();
        ApplyMusicSetting();
    }
    
    /// <summary>
    /// Gets the current sound enabled state
    /// </summary>
    public bool IsSoundEnabled()
    {
        return isSoundEnabled;
    }
    
    /// <summary>
    /// Sets sound enabled state programmatically
    /// </summary>
    public void SetSoundEnabled(bool enabled)
    {
        isSoundEnabled = enabled;
        
        // Update toggle if assigned
        if (soundToggle != null)
        {
            soundToggle.isOn = isSoundEnabled;
        }
        
        SaveSoundSetting();
    }
    
    /// <summary>
    /// Called by GameManager when countdown completes - allows gameplay music to start
    /// </summary>
    public void OnCountdownComplete()
    {
        isCountdownActive = false;
        ApplyMusicSetting();
        Debug.Log("[SettingManager] Countdown complete - gameplay music can now play");
    }
    


    private void OnDestroy()
    {
        // Unsubscribe from events
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess -= OnUserLoggedIn;
        }

        MatchmakingModel.OnMatchFound -= HandleMatchFound;
        MatchmakingModel.OnMatchmakingFailed -= HandleMatchmakingFailed;
        GameManager.OnGameStarted -= HandleGameStarted;
        GameManager.OnGameEnded -= HandleGameEnded;
        
        // Unsubscribe from toggle events
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(OnMusicToggleChanged);
        }
        
        if (soundToggle != null)
        {
            soundToggle.onValueChanged.RemoveListener(OnSoundToggleChanged);
        }
    }
}
