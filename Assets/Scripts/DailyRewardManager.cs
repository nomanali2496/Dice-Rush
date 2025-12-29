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
    
    [Header("Collect Button")]
    [SerializeField] private Button collectButton; // Button to collect the current day's reward
    
    [Header("Reward Amounts")]
    [SerializeField] private float[] chipsRewards = new float[7]; // Chips amount for each day
    [SerializeField] private float[] cashRewards = new float[7]; // Cash amount for each day
    
    [Header("Settings")]
    [SerializeField] private int currentDay = 1; // Current day (1-7)
    [SerializeField] private bool resetOnNewWeek = true; // Reset rewards when new week starts
    
    [Header("Daily Reward Panel")]
    [SerializeField] private GameObject dailyRewardPanel; // The daily reward panel GameObject
    
    [Header("Main Menu Reference")]
    [SerializeField] private GameObject mainMenuScreen; // Main menu GameObject to monitor
    
    [Header("Panel Settings")]
    [SerializeField] private float showDelay = 0.5f; // Delay before showing daily reward panel after main menu activates
    [SerializeField] private bool showOnEveryMainMenuActivation = false; // If false, shows only once per session
    
    [Header("UI Manager Reference")]
    [SerializeField] private PlayerProfileUI playerProfileUI; // Reference to PlayerProfileUI for updating currency displays
    
    /// <summary>
    /// Internal representation of daily rewards data
    /// </summary>
    private DailyRewardData[] dailyRewards = new DailyRewardData[7];
    
    // Events for reward collection (optional - can be used by other scripts)
    public System.Action<int, float, float> OnRewardCollected; // day, chips, cash
    
    // Event for when daily reward panel is closed
    public System.Action OnDailyRewardPanelClosed;
    
    private bool wasPanelActive = false; // Track previous panel state
    private bool hasShownDailyRewardThisSession = false; // Track if we've shown the panel this session
    private bool isMonitoringMainMenu = false; // Track if we're monitoring the main menu
    
    private void Start()
    {
        InitializeDailyRewards();
        UpdateUI();
        
        // Initialize panel state tracking
        if (dailyRewardPanel != null)
        {
            wasPanelActive = dailyRewardPanel.activeSelf;
            // Hide panel initially
            dailyRewardPanel.SetActive(false);
        }
        
        // Start monitoring main menu if assigned
        if (mainMenuScreen != null)
        {
            StartCoroutine(MonitorMainMenuActivation());
        }
        else
        {
            Debug.LogWarning("[DailyRewardManager] Main Menu Screen not assigned! Will try to find it automatically.");
            // Try to find MainMenuScreen by name
            GameObject foundMainMenu = GameObject.Find("MainMenuScreen");
            if (foundMainMenu != null)
            {
                mainMenuScreen = foundMainMenu;
                StartCoroutine(MonitorMainMenuActivation());
                Debug.Log("[DailyRewardManager] Found MainMenuScreen automatically.");
            }
            else
            {
                Debug.LogWarning("[DailyRewardManager] MainMenuScreen not found! Daily reward panel will not open automatically.");
            }
        }
    }
    
    /// <summary>
    /// Monitors when main menu becomes active and shows daily reward panel
    /// </summary>
    private IEnumerator MonitorMainMenuActivation()
    {
        isMonitoringMainMenu = true;
        bool hasShownAfterMainMenuActivation = false;
        
        while (true)
        {
            // Check if main menu is active
            if (mainMenuScreen != null && mainMenuScreen.activeSelf)
            {
                // Check if we should show daily reward panel
                if (!showOnEveryMainMenuActivation && hasShownDailyRewardThisSession)
                {
                    yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
                    continue;
                }
                
                // Show daily reward panel for the first time after main menu activates
                if (!hasShownAfterMainMenuActivation)
                {
                    hasShownAfterMainMenuActivation = true;
                    yield return new WaitForSeconds(showDelay);
                    ShowDailyRewardPanel();
                }
            }
            
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }
    
    private void Update()
    {
        // Monitor daily reward panel state to detect when it closes
        if (dailyRewardPanel != null)
        {
            bool isPanelActive = dailyRewardPanel.activeSelf;
            
            // If panel was active and now is inactive, it just closed
            if (wasPanelActive && !isPanelActive)
            {
                OnDailyRewardPanelClosed?.Invoke();
                Debug.Log("[DailyRewardManager] Daily reward panel closed");
            }
            
            wasPanelActive = isPanelActive;
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
        
        // Add rewards (you can connect this to your game's currency system)
        AddRewards(chipsAmount, cashAmount);
        
        // Trigger event
        OnRewardCollected?.Invoke(currentDay, chipsAmount, cashAmount);
        
        // Update UI
        UpdateUI();
        
        Debug.Log($"[DailyRewardManager] Collected Day {currentDay} reward: {chipsAmount} Chips, {cashAmount} Cash");
    }
    
    /// <summary>
    /// Adds rewards to player's account using PlayFabManager and updates UI
    /// </summary>
    private void AddRewards(float chips, float cash)
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogError("[DailyRewardManager] PlayFabManager.Instance is null! Cannot add rewards.");
            return;
        }

        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[DailyRewardManager] Player is not logged in! Cannot add rewards.");
            return;
        }

        // Add chips (coins) to player - convert float to int
        if (chips > 0)
        {
            int chipsInt = Mathf.RoundToInt(chips);
            PlayFabManager.Instance.AddCoins(chipsInt);
            Debug.Log($"[DailyRewardManager] Added {chipsInt} chips (coins) to player");
        }

        // Add cash to player
        if (cash > 0)
        {
            PlayFabManager.Instance.AddCash(cash);
            Debug.Log($"[DailyRewardManager] Added {cash} cash to player");
        }

        // Update all UI displays using PlayerProfileUI
        UpdateAllCurrencyDisplays();

        Debug.Log($"[DailyRewardManager] Rewards added successfully - Chips: {chips}, Cash: {cash}");
    }

    /// <summary>
    /// Updates all currency displays (main menu, gameplay, winner screen) using PlayerProfileUI
    /// </summary>
    private void UpdateAllCurrencyDisplays()
    {
        if (PlayFabManager.Instance == null || PlayFabManager.Instance.currentPlayerData == null)
        {
            Debug.LogWarning("[DailyRewardManager] Cannot update UI - PlayFabManager or player data is null");
            return;
        }

        PlayerData data = PlayFabManager.Instance.currentPlayerData;
        int chips = data.coins; // Chips are stored as coins
        float cash = data.cash; // Cash is stored as float

        // Update all UI displays using PlayerProfileUI
        if (playerProfileUI != null)
        {
            playerProfileUI.UpdateAllCurrencyDisplays(chips, cash);
        }
        else
        {
            Debug.LogWarning("[DailyRewardManager] PlayerProfileUI reference is null! Cannot update UI displays. Assign it in Inspector.");
        }
    }
    
    /// <summary>
    /// Sets the current day (1-7)
    /// </summary>
    public void SetCurrentDay(int day)
    {
        currentDay = Mathf.Clamp(day, 1, 7);
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
    /// Advances to the next day
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
    /// Gets the daily reward panel GameObject
    /// </summary>
    public GameObject GetDailyRewardPanel()
    {
        return dailyRewardPanel;
    }
    
    /// <summary>
    /// Checks if the daily reward panel is currently active
    /// </summary>
    public bool IsDailyRewardPanelActive()
    {
        return dailyRewardPanel != null && dailyRewardPanel.activeSelf;
    }
    
    /// <summary>
    /// Shows/opens the daily reward panel
    /// </summary>
    public void ShowDailyRewardPanel()
    {
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(true);
            wasPanelActive = true;
            hasShownDailyRewardThisSession = true;
            Debug.Log("[DailyRewardManager] Daily reward panel opened");
        }
        else
        {
            Debug.LogWarning("[DailyRewardManager] Daily reward panel GameObject is not assigned!");
        }
    }
    
    /// <summary>
    /// Hides/closes the daily reward panel
    /// </summary>
    public void HideDailyRewardPanel()
    {
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(false);
            Debug.Log("[DailyRewardManager] Daily reward panel closed");
        }
    }
    
    #region Test Methods
    
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

