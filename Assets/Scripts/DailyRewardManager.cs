using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Daily reward data structure
/// </summary>
[System.Serializable]
public class DailyRewardData
{
    public int day;
    public RewardType rewardType;
    public float chipsAmount;
    public float cashAmount;
    public bool isCollected;
    
    public DailyRewardData(int day, RewardType rewardType, float chipsAmount = 0, float cashAmount = 0)
    {
        this.day = day;
        this.rewardType = rewardType;
        this.chipsAmount = chipsAmount;
        this.cashAmount = cashAmount;
        this.isCollected = false;
    }
}

/// <summary>
/// Reward type enum
/// </summary>
public enum RewardType
{
    Chips,
    Cash,
    ChipsAndCash
}

/// <summary>
/// Manages daily rewards system with 7 days of rewards
/// Shows rewards one by one in a week and handles reward collection
/// </summary>
public class DailyRewardManager : MonoBehaviour
{
    [Header("Daily Rewards UI")]
    [SerializeField] private Image[] dayBackgrounds = new Image[7]; // Background images for each day (0-6 for days 1-7)
    [SerializeField] private Sprite[] selectedBackgroundSprites = new Sprite[7]; // Background sprites for selected/current day (one for each day 1-7)
    [SerializeField] private Sprite[] normalBackgroundSprites = new Sprite[7]; // Background sprites for normal/unselected days (one for each day 1-7)
    
    [Header("Panel")]
    [SerializeField] private GameObject dailyRewardPanel; // Daily reward panel GameObject (optional - for showing/hiding)
    
    [Header("Collect Button")]
    [SerializeField] private Button collectButton; // Button to collect the current day's reward
    [SerializeField] private Button closeButton; // Close button for the panel (optional)
    
    [Header("Reward Amounts")]
    [SerializeField] private float[] chipsRewards = new float[7]; // Chips amount for each day
    [SerializeField] private float[] cashRewards = new float[7]; // Cash amount for each day
    
    [Header("Settings")]
    [SerializeField] private int currentDay = 1; // Current day (1-7)
    [SerializeField] private bool resetOnNewWeek = true; // Reset rewards when new week starts
    [SerializeField] private float autoCloseDelay = 0.5f; // Delay before auto-closing panel after collecting reward
    
    [Header("Debug Info")]
    [SerializeField] private string currentUserId = ""; // Current user's PlayFab ID (for account-specific storage)
    [SerializeField] private string lastShownDate = "N/A"; // Last date when panel was shown
    [SerializeField] private string currentTime = "N/A"; // Current system time
    [SerializeField] private string storageKey = "N/A"; // PlayerPrefs key being used for storage
    [SerializeField] private bool hasShownToday = false; // Whether panel has been shown today
    [SerializeField] private bool isPanelAssigned = false; // Whether dailyRewardPanel GameObject is assigned
    [SerializeField] private bool isPanelActive = false; // Whether dailyRewardPanel is currently active
    
    /// <summary>
    /// Internal representation of daily rewards data
    /// </summary>
    private DailyRewardData[] dailyRewards = new DailyRewardData[7];
    
    // PlayerPrefs key prefix for tracking last shown date (account-specific)
    private const string LAST_SHOWN_DATE_KEY_PREFIX = "DailyReward_LastShownDate_";
    
    // PlayerPrefs key prefix for tracking current day (account-specific)
    private const string CURRENT_DAY_KEY_PREFIX = "DailyReward_CurrentDay_";
    
    // Reference to AuthenticationManager for login events
    private AuthenticationManager authManager;
    
    // Events for reward collection and panel management (optional - can be used by other scripts)
    public System.Action<int, float, float> OnRewardCollected; // day, chips, cash
    public System.Action OnDailyRewardPanelClosed; // Fired when the daily reward panel is closed
    
    private void Start()
    {
        InitializeDailyRewards();
        SetupCloseButton();
        
        // Find AuthenticationManager in scene
        authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess += OnUserLoggedIn;
            Debug.Log("[DailyRewardManager] Successfully subscribed to AuthenticationManager events");
        }
        else
        {
            Debug.LogWarning("[DailyRewardManager] AuthenticationManager not found! Daily reward tracking will use device-based storage.");
        }
        
        // Check if user is already logged in (in case Start() runs after login)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            OnUserLoggedIn();
        }
        else
        {
            // User not logged in yet - initialize with default day 1
            currentDay = 1;
            UpdateUI();
        }
        
        // Start updating debug info (for editor debugging)
        StartCoroutine(UpdateDebugInfo());
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess -= OnUserLoggedIn;
        }
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
                Debug.LogWarning("[DailyRewardManager] PlayFabId is empty, using device-based storage (not account-specific)");
            }
            else
            {
                Debug.Log($"[DailyRewardManager] User logged in with PlayFabId: {currentUserId}");
            }
        }
        else
        {
            Debug.LogWarning("[DailyRewardManager] PlayFabManager not found, using device-based storage (not account-specific)");
            currentUserId = ""; // Fallback to device-based storage
        }
        
        // Update storage key immediately
        UpdateStorageKeyDisplay();
        
        // Load account-specific current day (defaults to 1 for new accounts)
        LoadCurrentDay();
    }
    
    /// <summary>
    /// Called when user logs out - clears current user data
    /// </summary>
    public void OnUserLoggedOut()
    {
        Debug.Log("[DailyRewardManager] User logged out, clearing user data");
        currentUserId = "";
        UpdateStorageKeyDisplay();
        // Reset currentDay to default (will be loaded when new user logs in)
        currentDay = 1;
    }
    
    /// <summary>
    /// Updates the storage key display in the editor
    /// </summary>
    private void UpdateStorageKeyDisplay()
    {
        storageKey = GetLastShownDateKey();
    }
    
    /// <summary>
    /// Gets the account-specific PlayerPrefs key for storing current day
    /// </summary>
    private string GetCurrentDayKey()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            // Fallback to device-based storage if no user ID
            return CURRENT_DAY_KEY_PREFIX + "Device";
        }
        return CURRENT_DAY_KEY_PREFIX + currentUserId;
    }
    
    /// <summary>
    /// Loads the current day from PlayerPrefs (account-specific)
    /// Defaults to 1 if not found (new account)
    /// </summary>
    private void LoadCurrentDay()
    {
        string key = GetCurrentDayKey();
        if (PlayerPrefs.HasKey(key))
        {
            currentDay = PlayerPrefs.GetInt(key, 1);
            currentDay = Mathf.Clamp(currentDay, 1, 7);
            Debug.Log($"[DailyRewardManager] Loaded current day {currentDay} for user {currentUserId}");
        }
        else
        {
            // New account - start at day 1
            currentDay = 1;
            SaveCurrentDay();
            Debug.Log($"[DailyRewardManager] New account detected - starting at day 1 for user {currentUserId}");
        }
        UpdateUI();
    }
    
    /// <summary>
    /// Saves the current day to PlayerPrefs (account-specific)
    /// </summary>
    private void SaveCurrentDay()
    {
        string key = GetCurrentDayKey();
        PlayerPrefs.SetInt(key, currentDay);
        PlayerPrefs.Save();
        Debug.Log($"[DailyRewardManager] Saved current day {currentDay} for user {currentUserId}");
    }
    
    /// <summary>
    /// Gets the account-specific PlayerPrefs key for storing last shown date
    /// </summary>
    private string GetLastShownDateKey()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            // Fallback to device-based storage if no user ID
            return LAST_SHOWN_DATE_KEY_PREFIX + "Device";
        }
        return LAST_SHOWN_DATE_KEY_PREFIX + currentUserId;
    }
    
    /// <summary>
    /// Sets up the close button if provided
    /// </summary>
    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }
    
    /// <summary>
    /// Initializes the daily rewards data structure
    /// </summary>
    private void InitializeDailyRewards()
    {
        // Day 1, 2, 3, 5: Chips only
        dailyRewards[0] = new DailyRewardData(1, RewardType.Chips, chipsRewards[0], 0);
        dailyRewards[1] = new DailyRewardData(2, RewardType.Chips, chipsRewards[1], 0);
        dailyRewards[2] = new DailyRewardData(3, RewardType.Chips, chipsRewards[2], 0);
        dailyRewards[4] = new DailyRewardData(5, RewardType.Chips, chipsRewards[4], 0);
        
        // Day 4, 6: Cash only
        dailyRewards[3] = new DailyRewardData(4, RewardType.Cash, 0, cashRewards[3]);
        dailyRewards[5] = new DailyRewardData(6, RewardType.Cash, 0, cashRewards[5]);
        
        // Day 7: Cash and Chips
        dailyRewards[6] = new DailyRewardData(7, RewardType.ChipsAndCash, chipsRewards[6], cashRewards[6]);
        
        // Setup collect button
        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(CollectCurrentDayReward);
        }
    }
    
    /// <summary>
    /// Updates the UI to reflect current state
    /// </summary>
    private void UpdateUI()
    {
        // Update background images
        for (int i = 0; i < 7; i++)
        {
            if (dayBackgrounds[i] == null) continue;
            
            int dayNumber = i + 1; // Day 1-7
            
            if (dayNumber == currentDay)
            {
                // Current/selected day - use selected sprite for this day
                if (selectedBackgroundSprites != null && i < selectedBackgroundSprites.Length && selectedBackgroundSprites[i] != null)
                {
                    dayBackgrounds[i].sprite = selectedBackgroundSprites[i];
                }
            }
            else
            {
                // Normal/unselected day - use normal sprite for this day
                if (normalBackgroundSprites != null && i < normalBackgroundSprites.Length && normalBackgroundSprites[i] != null)
                {
                    dayBackgrounds[i].sprite = normalBackgroundSprites[i];
                }
            }
        }
        
        // Update collect button state
        if (collectButton != null)
        {
            bool canCollect = currentDay >= 1 && currentDay <= 7 && 
                             dailyRewards[currentDay - 1] != null && 
                             !dailyRewards[currentDay - 1].isCollected;
            collectButton.interactable = canCollect;
        }
    }
    
    /// <summary>
    /// Collects the reward for the current day
    /// </summary>
    public void CollectCurrentDayReward()
    {
        if (currentDay < 1 || currentDay > 7)
        {
            Debug.LogWarning("[DailyRewardManager] Invalid current day!");
            return;
        }
        
        int dayIndex = currentDay - 1; // Convert to 0-based index
        
        if (dailyRewards[dayIndex] == null)
        {
            Debug.LogWarning($"[DailyRewardManager] No reward data for day {currentDay}!");
            return;
        }
        
        if (dailyRewards[dayIndex].isCollected)
        {
            Debug.LogWarning($"[DailyRewardManager] Day {currentDay} reward already collected!");
            return;
        }
        
        // Mark as collected
        dailyRewards[dayIndex].isCollected = true;
        
        // Get reward amounts
        float chipsAmount = dailyRewards[dayIndex].chipsAmount;
        float cashAmount = dailyRewards[dayIndex].cashAmount;
        
        // Store the day that was collected before advancing
        int collectedDay = currentDay;
        
        // Add rewards (you can connect this to your game's currency system)
        AddRewards(chipsAmount, cashAmount);
        
        // Trigger event
        OnRewardCollected?.Invoke(collectedDay, chipsAmount, cashAmount);
        
        // Advance to next day after collecting
        AdvanceToNextDay();
        
        Debug.Log($"[DailyRewardManager] Collected Day {collectedDay} reward: {chipsAmount} Chips, {cashAmount} Cash. Advanced to day {currentDay}");
        
        // Auto-close panel after delay
        StartCoroutine(AutoClosePanelAfterDelay());
    }
    
    /// <summary>
    /// Automatically closes the panel after a delay
    /// </summary>
    private IEnumerator AutoClosePanelAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        ClosePanel();
        Debug.Log("[DailyRewardManager] Panel auto-closed after collecting reward");
    }
    
    /// <summary>
    /// Adds rewards to player's account
    /// Connects to PlayFabManager to add currency and updates UI
    /// </summary>
    private void AddRewards(float chips, float cash)
    {
        // Check if PlayFabManager is available
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[DailyRewardManager] PlayFabManager.Instance is null! Cannot add rewards.");
            return;
        }
        
        // Add cash to player
        if (cash > 0f)
        {
            PlayFabManager.Instance.AddCash(cash);
            Debug.Log($"[DailyRewardManager] Added {cash} cash to player");
        }
        
        // Add chips (coins) to player - convert float to int
        if (chips > 0f)
        {
            int chipsInt = Mathf.RoundToInt(chips);
            PlayFabManager.Instance.AddCoins(chipsInt);
            Debug.Log($"[DailyRewardManager] Added {chipsInt} chips (coins) to player");
        }
        
        // Update UI displays if PlayerProfileUI is available
        PlayerProfileUI playerProfileUI = FindObjectOfType<PlayerProfileUI>();
        if (playerProfileUI != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            PlayerData data = PlayFabManager.Instance.currentPlayerData;
            playerProfileUI.UpdateAllCurrencyDisplays(data.coins, data.cash);
            Debug.Log("[DailyRewardManager] Updated UI displays with new currency values");
        }
        else
        {
            Debug.LogWarning("[DailyRewardManager] PlayerProfileUI not found or player data is null. UI may not update automatically.");
        }
    }
    
    /// <summary>
    /// Sets the current day (1-7) and saves it account-specifically
    /// </summary>
    public void SetCurrentDay(int day)
    {
        currentDay = Mathf.Clamp(day, 1, 7);
        SaveCurrentDay();
        UpdateUI();
    }
    
    /// <summary>
    /// Gets the current day
    /// </summary>
    public int GetCurrentDay()
    {
        return currentDay;
    }
    
    /// <summary>
    /// Advances to the next day and saves it account-specifically
    /// </summary>
    public void AdvanceToNextDay()
    {
        if (currentDay < 7)
        {
            currentDay++;
        }
        else if (resetOnNewWeek)
        {
            // Reset all rewards for new week
            ResetWeeklyRewards();
            currentDay = 1;
        }
        else
        {
            currentDay = 1; // Loop back to day 1
        }
        
        SaveCurrentDay();
        UpdateUI();
    }
    
    /// <summary>
    /// Resets all weekly rewards (marks all as not collected)
    /// </summary>
    public void ResetWeeklyRewards()
    {
        for (int i = 0; i < dailyRewards.Length; i++)
        {
            if (dailyRewards[i] != null)
            {
                dailyRewards[i].isCollected = false;
            }
        }
        UpdateUI();
        Debug.Log("[DailyRewardManager] Weekly rewards reset");
    }
    
    /// <summary>
    /// Checks if a specific day's reward is collected
    /// </summary>
    public bool IsDayCollected(int day)
    {
        if (day < 1 || day > 7) return false;
        int dayIndex = day - 1;
        return dailyRewards[dayIndex] != null && dailyRewards[dayIndex].isCollected;
    }
    
    /// <summary>
    /// Gets reward data for a specific day
    /// </summary>
    public DailyRewardData GetDayRewardData(int day)
    {
        if (day < 1 || day > 7) return null;
        int dayIndex = day - 1;
        return dailyRewards[dayIndex];
    }
    
    /// <summary>
    /// Shows the daily reward panel (only once per day)
    /// If already shown today, immediately fires OnDailyRewardPanelClosed event so other panels can proceed
    /// </summary>
    public void ShowPanel()
    {
        // Check if panel has already been shown today
        if (HasPanelBeenShownToday())
        {
            Debug.Log("[DailyRewardManager] Daily reward panel already shown today. Skipping and notifying that panel is 'closed' so offer panels can show.");
            // Immediately fire the closed event so offer panels can proceed
            OnDailyRewardPanelClosed?.Invoke();
            return;
        }
        
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(true);
            
            // Mark as shown today
            MarkPanelAsShownToday();
            
            Debug.Log("[DailyRewardManager] Daily reward panel shown");
        }
        else
        {
            Debug.LogError("[DailyRewardManager] Cannot show panel! dailyRewardPanel GameObject is not assigned in the Inspector!");
            // Still fire the closed event so offer panels can proceed
            OnDailyRewardPanelClosed?.Invoke();
        }
    }
    
    /// <summary>
    /// Checks if the panel has been shown today (account-specific)
    /// </summary>
    private bool HasPanelBeenShownToday()
    {
        string key = GetLastShownDateKey();
        string lastShownDateValue = PlayerPrefs.GetString(key, "");
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Update debug fields
        lastShownDate = string.IsNullOrEmpty(lastShownDateValue) ? "Never" : lastShownDateValue;
        hasShownToday = lastShownDateValue == todayDate;
        
        return hasShownToday;
    }
    
    /// <summary>
    /// Marks the panel as shown today (account-specific)
    /// </summary>
    private void MarkPanelAsShownToday()
    {
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
        string key = GetLastShownDateKey();
        PlayerPrefs.SetString(key, todayDate);
        PlayerPrefs.Save();
        Debug.Log($"[DailyRewardManager] Marked panel as shown on {todayDate} for user {currentUserId}");
    }
    
    /// <summary>
    /// Resets the daily show tracking (useful for testing) - account-specific
    /// </summary>
    [ContextMenu("Reset Daily Show Tracking")]
    public void ResetDailyShowTracking()
    {
        string key = GetLastShownDateKey();
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log($"[DailyRewardManager] Daily show tracking reset for user {currentUserId}");
    }
    
    /// <summary>
    /// Hides the daily reward panel
    /// </summary>
    public void HidePanel()
    {
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(false);
            Debug.Log("[DailyRewardManager] Daily reward panel hidden");
        }
    }
    
    /// <summary>
    /// Closes the daily reward panel and fires the OnDailyRewardPanelClosed event
    /// </summary>
    public void ClosePanel()
    {
        HidePanel();
        OnDailyRewardPanelClosed?.Invoke();
        Debug.Log("[DailyRewardManager] Daily reward panel closed - event fired");
    }
    
    /// <summary>
    /// Updates debug info fields in real-time (for editor debugging)
    /// </summary>
    private IEnumerator UpdateDebugInfo()
    {
        while (true)
        {
            // Update current time
            currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Update storage key
            UpdateStorageKeyDisplay();
            
            // Update panel assignment status
            isPanelAssigned = dailyRewardPanel != null;
            isPanelActive = dailyRewardPanel != null && dailyRewardPanel.activeSelf;
            
            // Update last shown date and has shown today status
            string key = GetLastShownDateKey();
            string lastShownDateValue = PlayerPrefs.GetString(key, "");
            lastShownDate = string.IsNullOrEmpty(lastShownDateValue) ? "Never" : lastShownDateValue;
            
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            hasShownToday = lastShownDateValue == todayDate;
            
            yield return new WaitForSeconds(1f); // Update every second
        }
    }
    
    #region Test Methods
    
    /// <summary>
    /// Test method: Show daily reward panel (for testing)
    /// </summary>
    [ContextMenu("Show Daily Reward Panel")]
    public void TestShowPanel()
    {
        Debug.Log("[DailyRewardManager] Test: Manually triggering ShowPanel()");
        ShowPanel();
    }
    
    /// <summary>
    /// Test method: Collect current day reward
    /// </summary>
    [ContextMenu("Collect Current Day Reward")]
    public void TestCollectReward()
    {
        CollectCurrentDayReward();
    }
    
    /// <summary>
    /// Test method: Advance to next day
    /// </summary>
    [ContextMenu("Advance To Next Day")]
    public void TestAdvanceDay()
    {
        AdvanceToNextDay();
    }
    
    /// <summary>
    /// Test method: Reset all rewards
    /// </summary>
    [ContextMenu("Reset Weekly Rewards")]
    public void TestResetRewards()
    {
        ResetWeeklyRewards();
    }
    
    #endregion
}

