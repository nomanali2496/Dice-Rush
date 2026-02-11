using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game settings including music toggle on/off
/// Handles saving and loading settings using PlayerPrefs (account-based)
/// </summary>

[RequireComponent(typeof(AudioSource))]
public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;
    [SerializeField] AudioClip tapClip;
    [SerializeField] AudioSource source;
    [Header("Music Settings")]
    [SerializeField] private Toggle musicToggle; // Toggle UI for music on/off
    [SerializeField] private AudioSource backgroundMusicSource; // Main Menu / Normal Background Music
    [SerializeField] private AudioSource gameplayMusicSource; // Gameplay Background Music
    
    [Header("Sound Settings")]
    [SerializeField] private Toggle soundToggle; // Toggle UI for sound effects on/off

    [Header("Game Reference")]
    [SerializeField] private GameObject mainMenu; // Reference to Main Menu / Lobby GameObject
    [SerializeField] private GameObject gameplayScreen; // Reference to Gameplay GameObject
    
    [Header("Debug Info")]
    [SerializeField] private string currentUserId = ""; // Current user's PlayFab ID (for account-specific storage)
    [SerializeField] private string storageKey = "N/A"; // PlayerPrefs key being used for storage
    
    // PlayerPrefs key prefix for music setting (account-specific)
    private const string MUSIC_ENABLED_KEY_PREFIX = "MusicEnabled_";
    
    // PlayerPrefs key prefix for sound setting (account-specific)
    private const string SOUND_ENABLED_KEY_PREFIX = "SoundEnabled_";
    
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
            Debug.Log("[SettingManager] Successfully subscribed to AuthenticationManager events");
        }
        else
        {
            Debug.LogWarning("[SettingManager] AuthenticationManager not found! Settings will use device-based storage.");
        }
        
        // Initialize state tracker
        if (mainMenu != null)
        {
            lastMainMenuActiveState = mainMenu.activeInHierarchy;
        }
        if (gameplayScreen != null)
        {
            lastGameplayActiveState = gameplayScreen.activeInHierarchy;
        }
        
        // Ensure music is stopped initially (before login)
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.mute = true;
        }
        if (gameplayMusicSource != null)
        {
            gameplayMusicSource.Stop();
            gameplayMusicSource.mute = true;
        }
        Debug.Log("[SettingManager] All music muted until user logs in");
        
        // Check if user is already logged in (in case Start() runs after login)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            OnUserLoggedIn();
        }
        else
        {
            // User not logged in yet - don't load or apply settings yet
            // Settings will be loaded when user logs in
            Debug.Log("[SettingManager] User not logged in - music will start after login");
        }
        
        // Setup music toggle listener
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            // Set toggle to default (will be updated after login)
            musicToggle.isOn = true; // Default, will be updated after login
        }
        else
        {
            Debug.LogWarning("[SettingManager] Music toggle not assigned! Please assign it in the Inspector.");
        }
        
        // Setup sound toggle listener
        if (soundToggle != null)
        {
            soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
            // Set toggle to default (will be updated after login)
            soundToggle.isOn = true; // Default, will be updated after login
        }
        else
        {
            Debug.LogWarning("[SettingManager] Sound toggle not assigned! Please assign it in the Inspector.");
        }
        
        // Initialize AudioSource if not assigned
        if (source == null)
        {
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogWarning("[SettingManager] AudioSource not found! Sound effects may not work.");
            }
        }
    }

    private void Update()
    {
        // 1. Check Main Menu state changes
        if (mainMenu != null)
        {
            bool isMainMenuActive = mainMenu.activeInHierarchy;
            if (isMainMenuActive != lastMainMenuActiveState)
            {
                lastMainMenuActiveState = isMainMenuActive;
                Debug.Log($"[SettingManager] Main Menu active state changed to: {isMainMenuActive}. Refreshing music.");
                ApplyMusicSetting();
            }
        }

        // 2. Check Gameplay state changes
        if (gameplayScreen != null)
        {
            bool isGameplayActive = gameplayScreen.activeInHierarchy;
            if (isGameplayActive != lastGameplayActiveState)
            {
                lastGameplayActiveState = isGameplayActive;
                Debug.Log($"[SettingManager] Gameplay active state changed to: {isGameplayActive}. Refreshing music.");
                
                // If gameplay just became active, set countdown flag to delay music
                if (isGameplayActive)
                {
                    isCountdownActive = true;
                    Debug.Log("[SettingManager] Gameplay activated - waiting for countdown to complete before starting music");
                }
                else
                {
                    // Gameplay deactivated, apply music setting immediately (stop gameplay music)
                    isCountdownActive = false;
                    ApplyMusicSetting();
                }
            }
        }
    }
    
    /// <summary>
    /// Called when user successfully logs in - gets the user ID for account-specific storage
    /// </summary>
    private void OnUserLoggedIn()
    {
        hasUserLoggedIn = true;
        
        // Get the current user's PlayFab ID for account-specific storage
        if (PlayFabManager.Instance != null)
        {
            currentUserId = PlayFabManager.Instance.GetPlayFabId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("[SettingManager] PlayFabId is empty, using device-based storage (not account-specific)");
            }
            else
            {
                Debug.Log($"[SettingManager] User logged in with PlayFabId: {currentUserId}");
            }
        }
        else
        {
            Debug.LogWarning("[SettingManager] PlayFabManager not found, using device-based storage (not account-specific)");
            currentUserId = ""; // Fallback to device-based storage
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
        if (backgroundMusicSource != null) backgroundMusicSource.Stop();
        if (gameplayMusicSource != null) gameplayMusicSource.Stop();
        
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
        // 1. Check Login Status
        bool isLoggedIn = hasUserLoggedIn;
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            isLoggedIn = true;
        }

        if (!isLoggedIn)
        {
            StopAllMusic();
            return;
        }

        // 2. Determine Logic based on Screens
        bool isGameplayActive = (gameplayScreen != null && gameplayScreen.activeInHierarchy);
        bool isMainMenuActive = (mainMenu != null && mainMenu.activeInHierarchy);
        
        // "Gameplay" music plays ONLY if Gameplay is active AND countdown is NOT active
        bool shouldPlayGameplay = isGameplayActive && !isCountdownActive;
        
        // "Normal" music plays ONLY if Gameplay is NOT active AND Main Menu IS active
        bool shouldPlayNormal = !isGameplayActive && isMainMenuActive;

        // 3. Apply Settings (Toggle Check)
        if (isMusicEnabled)
        {
            // --- GAMEPLAY MUSIC ---
            if (gameplayMusicSource != null)
            {
                if (shouldPlayGameplay)
                {
                    gameplayMusicSource.mute = false;
                    if (!gameplayMusicSource.isPlaying) gameplayMusicSource.Play();
                }
                else
                {
                    // Stop gameplay music if not in gameplay or countdown is active
                    if (gameplayMusicSource.isPlaying) gameplayMusicSource.Stop();
                }
            }

            // --- NORMAL / MAIN MENU MUSIC ---
            if (backgroundMusicSource != null)
            {
                if (shouldPlayNormal)
                {
                    backgroundMusicSource.mute = false;
                    if (!backgroundMusicSource.isPlaying) backgroundMusicSource.Play();
                }
                else
                {
                    // Stop normal music if in gameplay
                    if (backgroundMusicSource.isPlaying) backgroundMusicSource.Stop();
                }
            }
        }
        else
        {
            // Music Disabled Globally
            StopAllMusic();
        }
    }

    private void StopAllMusic()
    {
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.mute = true;
            if (backgroundMusicSource.isPlaying) backgroundMusicSource.Stop();
        }
        if (gameplayMusicSource != null)
        {
            gameplayMusicSource.mute = true;
            if (gameplayMusicSource.isPlaying) gameplayMusicSource.Stop();
        }
    }
    
    /// <summary>
    /// Gets the account-specific PlayerPrefs key for storing music setting
    /// </summary>
    private string GetMusicEnabledKey()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            // Fallback to device-based storage if no user ID
            return MUSIC_ENABLED_KEY_PREFIX + "Device";
        }
        return MUSIC_ENABLED_KEY_PREFIX + currentUserId;
    }
    
    /// <summary>
    /// Gets the account-specific PlayerPrefs key for storing sound setting
    /// </summary>
    private string GetSoundEnabledKey()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            // Fallback to device-based storage if no user ID
            return SOUND_ENABLED_KEY_PREFIX + "Device";
        }
        return SOUND_ENABLED_KEY_PREFIX + currentUserId;
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
    
    /// <summary>
    /// Plays tap/click sound effect (only if sound is enabled)
    /// </summary>
    public void PlayTapSound()
    {
        // Check if sound is enabled before playing
        if (!isSoundEnabled)
        {
            return; // Sound is disabled, don't play
        }
        
        // Check if source and clip are assigned
        if (source == null)
        {
            Debug.LogWarning("[SettingManager] AudioSource not assigned for tap sound!");
            return;
        }
        
        if (tapClip == null)
        {
            Debug.LogWarning("[SettingManager] Tap clip not assigned!");
            return;
        }
        
        source.PlayOneShot(tapClip);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess -= OnUserLoggedIn;
        }
        
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
