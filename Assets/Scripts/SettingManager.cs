using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game settings including music toggle on/off
/// Handles saving and loading settings using PlayerPrefs (account-based)
/// </summary>
public class SettingManager : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private Toggle musicToggle; // Toggle UI for music on/off
    [SerializeField] private AudioSource backgroundMusicSource; // AudioSource component that plays background music
    
    [Header("Debug Info")]
    [SerializeField] private string currentUserId = ""; // Current user's PlayFab ID (for account-specific storage)
    [SerializeField] private string storageKey = "N/A"; // PlayerPrefs key being used for storage
    
    // PlayerPrefs key prefix for music setting (account-specific)
    private const string MUSIC_ENABLED_KEY_PREFIX = "MusicEnabled_";
    
    private bool isMusicEnabled = true; // Default music state (enabled)
    
    // Reference to AuthenticationManager for login events
    private AuthenticationManager authManager;
    
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
        
        // Check if user is already logged in (in case Start() runs after login)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            OnUserLoggedIn();
        }
        else
        {
            // User not logged in yet - load device-based setting
            LoadMusicSetting();
        }
        
        // Setup toggle listener
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            // Set toggle to match current setting
            musicToggle.isOn = isMusicEnabled;
        }
        else
        {
            Debug.LogWarning("[SettingManager] Music toggle not assigned! Please assign it in the Inspector.");
        }
        
        // Apply music setting on start
        ApplyMusicSetting();
    }
    
    /// <summary>
    /// Called when user successfully logs in - gets the user ID for account-specific storage
    /// </summary>
    private void OnUserLoggedIn()
    {
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
        
        // Load account-specific music setting
        LoadMusicSetting();
        
        // Update toggle to match loaded setting
        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicEnabled;
        }
        
        // Apply music setting
        ApplyMusicSetting();
    }
    
    /// <summary>
    /// Called when user logs out - clears current user data
    /// </summary>
    public void OnUserLoggedOut()
    {
        Debug.Log("[SettingManager] User logged out, clearing user data");
        currentUserId = "";
        UpdateStorageKeyDisplay();
        // Reload device-based setting
        LoadMusicSetting();
        
        // Update toggle to match loaded setting
        if (musicToggle != null)
        {
            musicToggle.isOn = isMusicEnabled;
        }
        
        // Apply music setting
        ApplyMusicSetting();
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
    /// Applies the current music setting to the AudioSource
    /// </summary>
    private void ApplyMusicSetting()
    {
        if (backgroundMusicSource != null)
        {
            if (isMusicEnabled)
            {
                // Enable music
                backgroundMusicSource.mute = false;
                // If not playing, start playing
                if (!backgroundMusicSource.isPlaying)
                {
                    backgroundMusicSource.Play();
                }
            }
            else
            {
                // Disable music
                backgroundMusicSource.mute = true;
            }
        }
        else
        {
            Debug.LogWarning("[SettingManager] Background music AudioSource not assigned! Please assign it in the Inspector.");
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
    }
}
