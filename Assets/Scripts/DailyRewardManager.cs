using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Animation - Source Positions (Daily Reward Display) - One for Each Day (1-7)")]
    [SerializeField] private RectTransform[] sourceRewardPositions = new RectTransform[7]; // Starting positions for animation for each day (0-6 for days 1-7)
    
    [Header("Animation - Target Position (Top Display)")]
    [SerializeField] private RectTransform targetCashPosition; // Target position for cash (top display)
    [SerializeField] private RectTransform targetChipsPosition; // Target position for chips (top display)
    
    [Header("Animation - Sprites")]
    [SerializeField] private Sprite cashSprite; // Sprite for cash animation
    [SerializeField] private Sprite chipsSprite; // Sprite for chips animation
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f; // Duration of animation in seconds
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Animation curve for movement
    [SerializeField] private int baseParticles = 3; // Base number of particles (minimum)
    [SerializeField] private float particlesPerCash = 0.5f; // Number of particles per unit of cash (e.g., 0.5 = 1 particle per $2)
    [SerializeField] private float particlesPerChip = 0.1f; // Number of particles per chip (e.g., 0.1 = 1 particle per 10 chips)
    [SerializeField] private int maxParticles = 20; // Maximum number of particles to spawn
    [SerializeField] private float delayBetweenParticles = 0.1f; // Delay between spawning each particle (one by one effect)
    
    [Header("Animation - Prefab")]
    [SerializeField] private GameObject animatedSpritePrefab; // Prefab for animated cash/chips sprites (should have Image component)
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip cashSound; // Sound to play for each cash particle
    [SerializeField] private AudioClip chipsSound; // Sound to play for each chip particle
    
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
    
    // Animation tracking
    private List<GameObject> activeAnimations = new List<GameObject>(); // Track active animation objects
    private PlayerProfileUI playerProfileUI;
    
    // Values for count-up animation
    private float initialCash = 0f;
    private int initialChips = 0;
    private float targetCash = 0f;
    private int targetChips = 0;
    private float currentAnimatedCash = 0f;
    private int currentAnimatedChips = 0;
    
    // Events for reward collection and panel management (optional - can be used by other scripts)
    public System.Action<int, float, float> OnRewardCollected; // day, chips, cash
    public System.Action OnDailyRewardPanelClosed; // Fired when the daily reward panel is closed
    
    private void Start()
    {
        InitializeDailyRewards();
        SetupCloseButton();
        
        // Find PlayerProfileUI for animation target positions
        playerProfileUI = FindObjectOfType<PlayerProfileUI>();
        
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
        
        // Clean up animations
        CleanupAnimations();
    }
    
    private void OnDisable()
    {
        // Clean up any active animations when disabled
        CleanupAnimations();
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
        
        // Get initial values before adding rewards
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            initialCash = PlayFabManager.Instance.currentPlayerData.cash;
            initialChips = PlayFabManager.Instance.currentPlayerData.coins;
            
            // Calculate target values (initial + reward)
            targetCash = initialCash + cashAmount;
            targetChips = initialChips + Mathf.RoundToInt(chipsAmount);
            
            // Set current animated values to initial
            currentAnimatedCash = initialCash;
            currentAnimatedChips = initialChips;
            
            // Update UI to show initial values (before animation)
            if (playerProfileUI != null)
            {
                playerProfileUI.UpdateAllCurrencyDisplays(initialChips, initialCash);
            }
        }
        
        // Play animation instead of immediately adding rewards
        StartCoroutine(PlayRewardAnimationAndAddRewards(chipsAmount, cashAmount, collectedDay));
    }
    
    /// <summary>
    /// Plays reward animation and then adds rewards to player's account
    /// </summary>
    private IEnumerator PlayRewardAnimationAndAddRewards(float chipsAmount, float cashAmount, int collectedDay)
    {
        // Play animation for chips if present
        if (chipsAmount > 0f)
        {
            PlayRewardAnimation(chipsAmount, false, collectedDay); // false = chips
        }
        
        // Play animation for cash if present
        if (cashAmount > 0f)
        {
            PlayRewardAnimation(cashAmount, true, collectedDay); // true = cash
        }
        
        // Wait for animation to complete before adding rewards
        yield return new WaitForSeconds(animationDuration + (delayBetweenParticles * maxParticles) + 0.2f);
        
        // Add rewards to player's account
        AddRewards(chipsAmount, cashAmount);
        
        // Ensure final values are exact (in case of rounding errors)
        if (playerProfileUI != null)
        {
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
            {
                PlayerData data = PlayFabManager.Instance.currentPlayerData;
                playerProfileUI.UpdateAllCurrencyDisplays(data.coins, data.cash);
            }
        }
        
        // Trigger event
        OnRewardCollected?.Invoke(collectedDay, chipsAmount, cashAmount);
        
        // Advance to next day after collecting
        AdvanceToNextDay();
        
        Debug.Log($"[DailyRewardManager] Collected Day {collectedDay} reward: {chipsAmount} Chips, {cashAmount} Cash. Advanced to day {currentDay}");
        
        // Auto-close panel after delay
        yield return new WaitForSeconds(autoCloseDelay);
        ClosePanel();
        Debug.Log("[DailyRewardManager] Panel auto-closed after collecting reward");
    }
    
    /// <summary>
    /// Plays the animation of cash or chips moving from daily reward position to top display
    /// </summary>
    private void PlayRewardAnimation(float amount, bool isCash, int day)
    {
        RectTransform sourcePosition = GetSourceRewardPosition(day);
        if (sourcePosition == null)
        {
            Debug.LogWarning($"[DailyRewardManager] Source reward position not assigned for day {day}! Cannot play animation.");
            return;
        }
        
        RectTransform targetPosition = isCash ? targetCashPosition : targetChipsPosition;
        if (targetPosition == null)
        {
            Debug.LogWarning($"[DailyRewardManager] Target position not assigned for {(isCash ? "cash" : "chips")}! Cannot play animation.");
            return;
        }
        
        Sprite rewardSprite = isCash ? cashSprite : chipsSprite;
        if (rewardSprite == null)
        {
            Debug.LogWarning($"[DailyRewardManager] Reward sprite not assigned for {(isCash ? "cash" : "chips")}! Cannot play animation.");
            return;
        }
        
        Debug.Log($"[DailyRewardManager] Starting animation - Reward: {amount} {(isCash ? "cash" : "chips")}");
        StartCoroutine(AnimateParticles(sourcePosition, targetPosition, rewardSprite, amount, isCash));
    }
    
    /// <summary>
    /// Gets RectTransform for a specific day
    /// </summary>
    private RectTransform GetSourceRewardPosition(int day)
    {
        // Convert day (1-7) to array index (0-6)
        int dayIndex = day - 1;
        
        if (dayIndex < 0 || dayIndex >= 7)
        {
            Debug.LogWarning($"[DailyRewardManager] Invalid day {day}! Must be 1-7.");
            return null;
        }
        
        if (sourceRewardPositions != null && dayIndex < sourceRewardPositions.Length && sourceRewardPositions[dayIndex] != null)
        {
            return sourceRewardPositions[dayIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// Animates particles moving from source to target position
    /// Particles spawn one by one from the exact source transform position
    /// Number of particles is based on the reward amount
    /// Also animates the count-up of cash/chips display
    /// </summary>
    private IEnumerator AnimateParticles(RectTransform source, RectTransform target, Sprite sprite, float amount, bool isCash)
    {
        Debug.Log($"[DailyRewardManager] AnimateParticles - Type: {(isCash ? "Cash" : "Chips")}, Amount: {amount}, Source: {source.position}, Target: {target.position}");
        
        // Calculate particle count based on reward amount
        int particleCount;
        if (isCash)
        {
            // For cash: base particles + (amount * particlesPerCash)
            particleCount = Mathf.RoundToInt(baseParticles + (amount * particlesPerCash));
        }
        else
        {
            // For chips: base particles + (amount * particlesPerChip)
            particleCount = Mathf.RoundToInt(baseParticles + (amount * particlesPerChip));
        }
        
        // Clamp to max particles
        particleCount = Mathf.Clamp(particleCount, baseParticles, maxParticles);
        
        Debug.Log($"[DailyRewardManager] Calculated {particleCount} particles for {(isCash ? "cash" : "chips")} reward of {amount}");
        
        // Calculate increment per particle
        float incrementPerParticle = amount / particleCount;
        
        // Get the exact source position (world position for UI)
        Vector3 sourceWorldPos = source.position;
        
        // Spawn particles one by one with delay
        for (int i = 0; i < particleCount; i++)
        {
            // Create particle at exact source position
            GameObject particle = CreateAnimatedSprite(sourceWorldPos, sprite);
            if (particle != null)
            {
                activeAnimations.Add(particle);
                
                // Set particle to exact source position (no random spread)
                RectTransform particleRect = particle.GetComponent<RectTransform>();
                if (particleRect != null)
                {
                    particleRect.position = sourceWorldPos;
                }
                else
                {
                    particle.transform.position = sourceWorldPos;
                }
                
                Debug.Log($"[DailyRewardManager] Created particle {i + 1}/{particleCount} at exact source position {sourceWorldPos}");
                
                // Play sound for this particle
                PlayParticleSound(isCash);
                
                // Animate this particle to target and update count-up
                StartCoroutine(AnimateSingleParticleWithCountUp(particle.transform, target.position, animationDuration, incrementPerParticle, isCash));
            }
            else
            {
                Debug.LogWarning($"[DailyRewardManager] Failed to create particle {i + 1}");
            }
            
            // Wait before spawning next particle (one by one effect)
            if (i < particleCount - 1) // Don't wait after the last particle
            {
                yield return new WaitForSeconds(delayBetweenParticles);
            }
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Creates an animated sprite GameObject
    /// </summary>
    private GameObject CreateAnimatedSprite(Vector3 position, Sprite sprite)
    {
        GameObject spriteObj;
        
        // Find the Canvas to parent the sprite to (for proper UI rendering)
        // First try to find Canvas from dailyRewardPanel, then fall back to script's GameObject
        Canvas canvas = null;
        Transform parentTransform = transform; // Default fallback
        
        if (dailyRewardPanel != null)
        {
            // Try to find Canvas from daily reward panel
            canvas = dailyRewardPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                parentTransform = canvas.transform;
            }
            else
            {
                // If no Canvas found, use the daily reward panel itself as parent
                parentTransform = dailyRewardPanel.transform;
            }
        }
        else
        {
            // Fallback: try to find Canvas from script's GameObject
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                parentTransform = canvas.transform;
            }
            else
            {
                // Last resort: use script's GameObject
                parentTransform = transform;
            }
        }
        
        if (animatedSpritePrefab != null)
        {
            spriteObj = Instantiate(animatedSpritePrefab, parentTransform);
        }
        else
        {
            // Create a simple GameObject with Image component if no prefab
            spriteObj = new GameObject("AnimatedSprite");
            spriteObj.transform.SetParent(parentTransform);
            
            // Add RectTransform for UI element
            RectTransform rectTransform = spriteObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100f, 100f); // Default size
            
            Image img = spriteObj.AddComponent<Image>();
            img.sprite = sprite;
        }
        
        // Set position using RectTransform for UI elements
        RectTransform rect = spriteObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position = position;
        }
        else
        {
            spriteObj.transform.position = position;
        }
        
        // Set sprite if Image component exists
        Image image = spriteObj.GetComponent<Image>();
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
        }
        
        return spriteObj;
    }
    
    /// <summary>
    /// Animates a single particle from source to target and updates count-up display
    /// </summary>
    private IEnumerator AnimateSingleParticleWithCountUp(Transform particle, Vector3 targetPosition, float duration, float increment, bool isCash)
    {
        RectTransform rectTransform = particle.GetComponent<RectTransform>();
        Vector3 startPosition;
        
        // Get start position based on whether it's a UI element or world object
        if (rectTransform != null)
        {
            startPosition = rectTransform.position;
        }
        else
        {
            startPosition = particle.position;
        }
        
        float elapsed = 0f;
        bool countUpApplied = false;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = movementCurve.Evaluate(t);
            
            // Interpolate position
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            if (rectTransform != null)
            {
                rectTransform.position = currentPosition;
            }
            else
            {
                particle.position = currentPosition;
            }
            
            // Apply count-up when particle reaches 80% of the way there
            if (!countUpApplied && t >= 0.8f)
            {
                if (isCash)
                {
                    currentAnimatedCash += increment;
                    currentAnimatedCash = Mathf.Min(currentAnimatedCash, targetCash); // Don't exceed target
                }
                else
                {
                    currentAnimatedChips += Mathf.RoundToInt(increment);
                    currentAnimatedChips = Mathf.Min(currentAnimatedChips, targetChips); // Don't exceed target
                }
                
                // Update UI display
                if (playerProfileUI != null)
                {
                    playerProfileUI.UpdateAllCurrencyDisplays(currentAnimatedChips, currentAnimatedCash);
                }
                
                countUpApplied = true;
            }
            
            // Keep size the same throughout animation (no scaling)
            particle.localScale = Vector3.one;
            
            yield return null;
        }
        
        // Ensure final position
        if (rectTransform != null)
        {
            rectTransform.position = targetPosition;
        }
        else
        {
            particle.position = targetPosition;
        }
        
        // Destroy particle after a short delay
        yield return new WaitForSeconds(0.1f);
        
        if (particle != null && particle.gameObject != null)
        {
            activeAnimations.Remove(particle.gameObject);
            Destroy(particle.gameObject);
        }
    }
    
    /// <summary>
    /// Plays the appropriate sound for a particle (cash or chips)
    /// </summary>
    private void PlayParticleSound(bool isCash)
    {
        AudioClip soundToPlay = isCash ? cashSound : chipsSound;
        
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, Camera.main.transform.position);
        }
        else
        {
            Debug.LogWarning($"[DailyRewardManager] {(isCash ? "Cash" : "Chips")} sound not assigned!");
        }
    }
    
    /// <summary>
    /// Cleans up all active animations
    /// </summary>
    private void CleanupAnimations()
    {
        foreach (GameObject anim in activeAnimations)
        {
            if (anim != null)
            {
                Destroy(anim);
            }
        }
        activeAnimations.Clear();
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

