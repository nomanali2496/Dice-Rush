using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Manages sequential offer panel display on user login
/// Shows 3 random offer panels one by one when main menu becomes active for the first time after login
/// </summary>
public class OfferPanelManager : MonoBehaviour
{
    [Header("Offer Panels")]
    [SerializeField] private GameObject[] offerPanels = new GameObject[7];
    
    [Header("Main Menu Reference")]
    [SerializeField] private GameObject mainMenuScreen; // Main menu GameObject to monitor
    
    [Header("Settings")]
    [SerializeField] private float showDelay = 1f; // Delay before showing offer panel after main menu activates
    [SerializeField] private bool showOnEveryLogin = false; // If false, shows only once per session
    [SerializeField] private float delayBetweenPanels = 0.5f; // Delay between closing one panel and showing the next
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f; // Scale animation duration
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Scale animation curve
    
    [Header("Debug Info")]
    [SerializeField] private string timeUntilNextOffer = "N/A"; // Time remaining until offers can be shown again (for editor debugging)
    [SerializeField] private string currentTime = "N/A"; // Current system time
    [SerializeField] private string lastShownDate = "N/A"; // Last date when offers were shown
    [SerializeField] private string nextResetTime = "N/A"; // When the next reset will occur (midnight)
    
    [Header("Close Button (Optional)")]
    // [SerializeField] private Button[] closeButtons; // Close buttons for each panel (optional)
    
    private bool hasShownOfferThisSession = false;
    private int currentActivePanelIndex = -1;
    private int nextPanelIndexToShow = 0; // Track which panel to show next in the selected panels sequence
    private List<int> selectedPanelIndices = new List<int>(); // Stores 3 random panel indices to show
    private bool hasLoggedIn = false;
    private bool isMonitoringMainMenu = false;
    private bool hasDailyRewardPanelClosed = false; // Start as false - will be set to true when panel closes
    private bool isWaitingForDailyRewardPanel = false;
    private bool isShowingPanelsSequence = false; // Track if we're currently showing panels in sequence
    private bool hasTriggeredDailyRewardPanel = false; // Track if we've already triggered the daily reward panel for this login
    private bool hasShownAfterMainMenuActivation = false; // Track if we've shown offer panels after main menu activation for this login
    private bool isWaitingForNextPanel = false; // Prevent duplicate triggers when waiting for next panel
    
    private const string LAST_OFFER_SHOWN_TIMESTAMP_KEY_PREFIX = "OfferPanelManager_LastShownTimestamp_";
    
    private string currentUserId = ""; // Store current user's PlayFab ID for account-specific storage
    
    private AuthenticationManager authManager;
    private DailyRewardManager dailyRewardManager;
    
    private void Start()
    {
        // Hide all offer panels initially
        HideAllPanels();
        
        // Find AuthenticationManager in scene
        authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess += OnUserLoggedIn;
            Debug.Log("[OfferPanelManager] Successfully subscribed to AuthenticationManager events");
        }
        else
        {
            Debug.LogWarning("[OfferPanelManager] AuthenticationManager not found! Offer panels will not show on login.");
        }
        
        // Find DailyRewardManager in scene
        dailyRewardManager = FindObjectOfType<DailyRewardManager>();
        if (dailyRewardManager != null)
        {
            dailyRewardManager.OnDailyRewardPanelClosed += OnDailyRewardPanelClosed;
            Debug.Log("[OfferPanelManager] Successfully subscribed to DailyRewardManager events");
        }
        else
        {
            Debug.LogWarning("[OfferPanelManager] DailyRewardManager not found! Offer panels will show immediately after login.");
        }
        
        // Start monitoring main menu if assigned
        if (mainMenuScreen != null)
        {
            StartCoroutine(MonitorMainMenuActivation());
        }
        else
        {
            Debug.LogWarning("[OfferPanelManager] Main Menu Screen not assigned! Will try to find it automatically.");
            // Try to find MainMenuScreen by name
            GameObject foundMainMenu = GameObject.Find("MainMenuScreen");
            if (foundMainMenu != null)
            {
                mainMenuScreen = foundMainMenu;
                StartCoroutine(MonitorMainMenuActivation());
                Debug.Log("[OfferPanelManager] Found MainMenuScreen automatically.");
            }
        }
        
        // Start monitoring panel state to auto-advance when panels are closed
        StartCoroutine(MonitorPanelState());
        
        // Start updating time until next offer (for editor debugging)
        StartCoroutine(UpdateTimeUntilNextOffer());
        
        // Setup close buttons if provided
        // SetupCloseButtons();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess -= OnUserLoggedIn;
        }
        
        if (dailyRewardManager != null)
        {
            dailyRewardManager.OnDailyRewardPanelClosed -= OnDailyRewardPanelClosed;
        }
    }
    
    /// <summary>
    /// Called when user successfully logs in
    /// </summary>
    private void OnUserLoggedIn()
    {
        hasLoggedIn = true;
        
        // Get the current user's PlayFab ID for account-specific storage
        if (PlayFabManager.Instance != null)
        {
            currentUserId = PlayFabManager.Instance.GetPlayFabId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogWarning("[OfferPanelManager] PlayFabId is empty, using device-based storage (not account-specific)");
            }
            else
            {
                Debug.Log($"[OfferPanelManager] User logged in with PlayFabId: {currentUserId}");
            }
        }
        else
        {
            Debug.LogWarning("[OfferPanelManager] PlayFabManager not found, using device-based storage (not account-specific)");
            currentUserId = ""; // Fallback to device-based storage
        }
        
        // Check if offers have been shown today
        bool hasShownToday = HasShownOffersToday();
        
        // Only reset flags if offers haven't been shown today
        if (!hasShownToday)
        {
            // Generate 3 random panel indices
            GenerateRandomPanelIndices();
            nextPanelIndexToShow = 0; // Reset to show panels from the beginning
            isShowingPanelsSequence = false; // Reset sequence flag
            hasShownAfterMainMenuActivation = false; // Reset flag so offer panels can show again
            hasShownOfferThisSession = false; // Reset session flag
            Debug.Log("[OfferPanelManager] User logged in, offers haven't been shown today. Will show 3 random offers when main menu activates.");
        }
        else
        {
            // Offers already shown today, don't reset flags
            hasShownOfferThisSession = true; // Mark as shown to prevent showing again
            Debug.Log("[OfferPanelManager] User logged in, but offers have already been shown today. Skipping offer display.");
        }
        
        hasTriggeredDailyRewardPanel = false; // Reset flag so daily reward panel can show again on next login
        isWaitingForNextPanel = false; // Reset waiting flag
        
        // Always wait for daily reward panel to close (it will open when main menu activates)
        // Initialize as false so we wait for the daily reward panel to close
        hasDailyRewardPanelClosed = false;
        
        // Only set to true if daily reward manager doesn't exist (meaning no daily reward panel will show)
        if (dailyRewardManager == null)
        {
            hasDailyRewardPanelClosed = true;
            Debug.Log("[OfferPanelManager] DailyRewardManager not found, will show offer panel without waiting.");
        }
        else
        {
            Debug.Log("[OfferPanelManager] Will wait for daily reward panel to close before showing offer panel. Daily reward panel will open when main menu activates.");
        }
    }
    
    /// <summary>
    /// Called when user logs out - clears current user data and resets flags
    /// </summary>
    public void OnUserLoggedOut()
    {
        Debug.Log("[OfferPanelManager] User logged out, clearing user data and resetting flags");
        
        // Clear current user ID immediately
        currentUserId = "";
        
        // Reset all flags
        hasLoggedIn = false;
        hasShownOfferThisSession = false;
        nextPanelIndexToShow = 0;
        selectedPanelIndices.Clear();
        isShowingPanelsSequence = false;
        hasTriggeredDailyRewardPanel = false;
        hasShownAfterMainMenuActivation = false;
        isWaitingForNextPanel = false;
        hasDailyRewardPanelClosed = false;
        currentActivePanelIndex = -1;
        
        // Hide all panels
        HideAllPanels();
        
        Debug.Log("[OfferPanelManager] User data cleared - timer will reset for next user");
    }
    
    /// <summary>
    /// Called when daily reward panel closes
    /// </summary>
    private void OnDailyRewardPanelClosed()
    {
        hasDailyRewardPanelClosed = true;
        isWaitingForDailyRewardPanel = false;
        Debug.Log("[OfferPanelManager] Daily reward panel closed, can now show offer panel");
    }
    
    /// <summary>
    /// Monitors when main menu becomes active for the first time after login
    /// </summary>
    private IEnumerator MonitorMainMenuActivation()
    {
        isMonitoringMainMenu = true;
        
        while (true)
        {
            // Check if main menu is active and user has logged in
            if (mainMenuScreen != null && mainMenuScreen.activeSelf && hasLoggedIn)
            {
                // Check if we should show offer panel (must not have shown today AND not shown this session)
                bool hasShownToday = HasShownOffersToday();
                if (hasShownToday || hasShownOfferThisSession)
                {
                    yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
                    continue;
                }
                
                // Trigger daily reward panel to show when main menu first activates
                if (!hasTriggeredDailyRewardPanel && dailyRewardManager != null)
                {
                    hasTriggeredDailyRewardPanel = true;
                    dailyRewardManager.ShowPanel();
                    Debug.Log("[OfferPanelManager] Triggered daily reward panel to show");
                }
                
                // Wait for daily reward panel to close first
                // Only rely on the event - don't check panel state directly to avoid timing issues
                if (!hasDailyRewardPanelClosed)
                {
                    // If DailyRewardManager exists, we MUST wait for the OnDailyRewardPanelClosed event
                    // Don't check panel state here - just wait for the event to fire
                    yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
                    continue;
                }
                
                // Show first offer panel for the first time after main menu activates AND daily reward panel is closed
                if (!hasShownAfterMainMenuActivation && hasDailyRewardPanelClosed && !isShowingPanelsSequence)
                {
                    hasShownAfterMainMenuActivation = true;
                    yield return new WaitForSeconds(showDelay);
                    ShowNextOfferPanelInSequence();
                }
            }
            
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }
    
    /// <summary>
    /// Generates 3 random unique panel indices from available panels
    /// </summary>
    private void GenerateRandomPanelIndices()
    {
        selectedPanelIndices.Clear();
        
        // Validate that we have offer panels
        if (offerPanels == null || offerPanels.Length == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No offer panels assigned!");
            return;
        }
        
        // Get list of valid panel indices (non-null panels)
        List<int> validIndices = new List<int>();
        for (int i = 0; i < offerPanels.Length; i++)
        {
            if (offerPanels[i] != null)
            {
                validIndices.Add(i);
            }
        }
        
        if (validIndices.Count == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No valid offer panels found!");
            return;
        }
        
        // If we have 3 or fewer valid panels, use all of them
        int panelsToSelect = Mathf.Min(3, validIndices.Count);
        
        // Shuffle the valid indices and take the first 3
        for (int i = 0; i < validIndices.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, validIndices.Count);
            int temp = validIndices[i];
            validIndices[i] = validIndices[randomIndex];
            validIndices[randomIndex] = temp;
        }
        
        // Take the first 3 (or all if less than 3)
        selectedPanelIndices = validIndices.Take(panelsToSelect).ToList();
        
        Debug.Log($"[OfferPanelManager] Generated {selectedPanelIndices.Count} random panel indices: {string.Join(", ", selectedPanelIndices)}");
    }
    
    /// <summary>
    /// Shows the next offer panel from the 3 randomly selected panels
    /// </summary>
    private void ShowNextOfferPanelInSequence()
    {
        // Validate that we have offer panels
        if (offerPanels == null || offerPanels.Length == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No offer panels assigned!");
            return;
        }
        
        // Check if we have selected panel indices (should be generated on login)
        if (selectedPanelIndices == null || selectedPanelIndices.Count == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No panel indices selected! Generating now...");
            GenerateRandomPanelIndices();
            if (selectedPanelIndices == null || selectedPanelIndices.Count == 0)
            {
                Debug.LogWarning("[OfferPanelManager] Failed to generate panel indices!");
                return;
            }
        }
        
        // Check if we've shown all selected panels
        if (nextPanelIndexToShow >= selectedPanelIndices.Count)
        {
            Debug.Log("[OfferPanelManager] All 3 random offer panels have been shown!");
            isShowingPanelsSequence = false;
            hasShownOfferThisSession = true;
            return;
        }
        
        // Get the actual panel index from our selected list
        int selectedIndex = selectedPanelIndices[nextPanelIndexToShow];
        
        // Validate the panel exists and is not null
        if (selectedIndex < 0 || selectedIndex >= offerPanels.Length || offerPanels[selectedIndex] == null)
        {
            Debug.LogWarning($"[OfferPanelManager] Invalid panel index {selectedIndex}! Skipping to next panel.");
            nextPanelIndexToShow++;
            // Try to show next panel
            if (nextPanelIndexToShow < selectedPanelIndices.Count)
            {
                ShowNextOfferPanelInSequence();
            }
            return;
        }
        
        // Hide all panels first
        HideAllPanels();
        
        // Show the next panel in sequence with animation
        currentActivePanelIndex = selectedIndex;
        isShowingPanelsSequence = true;
        hasShownOfferThisSession = true;
        
        // Mark that offers have been shown today when showing the first panel
        if (nextPanelIndexToShow == 0)
        {
            MarkOffersAsShownToday();
            Debug.Log("[OfferPanelManager] Marked offers as shown for today.");
        }
        
        // Setup button listeners to auto-close panel when any button is clicked
        SetupPanelButtonListeners(offerPanels[selectedIndex]);
        
        // Start scale animation
        StartCoroutine(AnimatePanelScale(offerPanels[selectedIndex]));
        
        Debug.Log($"[OfferPanelManager] Showing offer panel {nextPanelIndexToShow + 1} of {selectedPanelIndices.Count} (Panel index {selectedIndex})");
        
        // Update to next panel for next time
        nextPanelIndexToShow++;
    }
    
    /// <summary>
    /// Shows a random offer panel with smooth scale animation (kept for backward compatibility)
    /// </summary>
    private void ShowRandomOfferPanelWithAnimation()
    {
        ShowNextOfferPanelInSequence();
    }
    
    /// <summary>
    /// Shows a random offer panel from the seven available panels (without animation, for manual calls)
    /// </summary>
    public void ShowRandomOfferPanel()
    {
        ShowRandomOfferPanelWithAnimation();
    }
    
    /// <summary>
    /// Animates panel scale from 0 to 1 with smooth animation
    /// </summary>
    private IEnumerator AnimatePanelScale(GameObject panel)
    {
        if (panel == null) yield break;
        
        // Get RectTransform for UI elements, or Transform for 3D
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        Transform transform = panel.transform;
        
        // Set initial scale to 0 and activate panel
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        
        if (rectTransform != null)
        {
            rectTransform.localScale = initialScale;
        }
        else
        {
            transform.localScale = initialScale;
        }
        
        panel.SetActive(true);
        
        // Animate scale
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Use animation curve for smooth easing
            float curveValue = scaleCurve.Evaluate(t);
            Vector3 currentScale = Vector3.Lerp(initialScale, targetScale, curveValue);
            
            if (rectTransform != null)
            {
                rectTransform.localScale = currentScale;
            }
            else
            {
                transform.localScale = currentScale;
            }
            
            yield return null;
        }
        
        // Ensure final scale
        if (rectTransform != null)
        {
            rectTransform.localScale = targetScale;
        }
        else
        {
            transform.localScale = targetScale;
        }
    }
    
    /// <summary>
    /// Hides all offer panels
    /// </summary>
    public void HideAllPanels()
    {
        if (offerPanels == null)
            return;
        
        foreach (GameObject panel in offerPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
        
        currentActivePanelIndex = -1;
    }
    
    /// <summary>
    /// Closes the currently active offer panel and shows the next one in sequence
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (currentActivePanelIndex >= 0 && currentActivePanelIndex < offerPanels.Length)
        {
            if (offerPanels[currentActivePanelIndex] != null)
            {
                offerPanels[currentActivePanelIndex].SetActive(false);
                currentActivePanelIndex = -1;
                
                // Show next panel in sequence if there are more to show
                if (nextPanelIndexToShow < selectedPanelIndices.Count && isShowingPanelsSequence && !isWaitingForNextPanel)
                {
                    isWaitingForNextPanel = true;
                    StartCoroutine(ShowNextPanelAfterDelay());
                }
                else if (nextPanelIndexToShow >= selectedPanelIndices.Count)
                {
                    // All 3 selected panels have been shown
                    isShowingPanelsSequence = false;
                    isWaitingForNextPanel = false;
                    Debug.Log("[OfferPanelManager] All 3 random offer panels have been shown and closed!");
                }
            }
        }
    }
    
    /// <summary>
    /// Shows the next panel after a short delay
    /// </summary>
    private IEnumerator ShowNextPanelAfterDelay()
    {
        yield return new WaitForSeconds(delayBetweenPanels);
        isWaitingForNextPanel = false;
        ShowNextOfferPanelInSequence();
    }
    
    /// <summary>
    /// Monitors the state of the current panel and automatically shows the next one when it closes
    /// This ensures panels advance even if close buttons don't call CloseCurrentPanel()
    /// </summary>
    private IEnumerator MonitorPanelState()
    {
        int lastActivePanelIndex = -1;
        
        while (true)
        {
            // Only monitor if we're in a sequence and not already waiting for next panel
            if (isShowingPanelsSequence && currentActivePanelIndex >= 0 && !isWaitingForNextPanel)
            {
                // Check if the current panel is still active
                if (currentActivePanelIndex < offerPanels.Length && offerPanels[currentActivePanelIndex] != null)
                {
                    bool isPanelActive = offerPanels[currentActivePanelIndex].activeSelf;
                    
                    // If panel was active before but is now inactive, it was closed
                    if (lastActivePanelIndex == currentActivePanelIndex && !isPanelActive)
                    {
                        Debug.Log($"[OfferPanelManager] Detected panel {currentActivePanelIndex} was closed, showing next panel...");
                        
                        // Show next panel if there are more to show
                        if (nextPanelIndexToShow < selectedPanelIndices.Count)
                        {
                            isWaitingForNextPanel = true;
                            StartCoroutine(ShowNextPanelAfterDelay());
                        }
                        else
                        {
                            // All 3 selected panels have been shown
                            isShowingPanelsSequence = false;
                            currentActivePanelIndex = -1;
                            Debug.Log("[OfferPanelManager] All 3 random offer panels have been shown and closed!");
                        }
                        
                        lastActivePanelIndex = -1;
                    }
                    else if (isPanelActive)
                    {
                        // Panel is still active, update tracking
                        lastActivePanelIndex = currentActivePanelIndex;
                    }
                }
            }
            else
            {
                lastActivePanelIndex = -1;
            }
            
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }
    
    /// <summary>
    /// Sets up button listeners for a panel to automatically close when any button is clicked
    /// This includes Buy buttons, Purchase buttons, or any other buttons in the panel
    /// Note: This adds the close listener without removing existing listeners to preserve button functionality
    /// </summary>
    private void SetupPanelButtonListeners(GameObject panel)
    {
        if (panel == null) return;
        
        // Find all buttons in the panel (including children)
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning($"[OfferPanelManager] No buttons found in panel {panel.name}");
            return;
        }
        
        // Add listener to each button to close the panel when clicked
        // We add it without removing existing listeners to preserve button functionality (like purchase handlers)
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                // Check if this listener is already added to avoid duplicates
                // We'll use a wrapper method to check
                button.onClick.AddListener(() => {
                    Debug.Log($"[OfferPanelManager] Button '{button.name}' clicked in panel, closing panel...");
                    CloseCurrentPanel();
                });
            }
        }
        
        Debug.Log($"[OfferPanelManager] Setup {buttons.Length} button listeners for panel {panel.name} - panels will auto-close on button click");
    }
    
    /// <summary>
    /// Setup close buttons for each panel (kept for backward compatibility)
    /// </summary>
    // private void SetupCloseButtons()
    // {
    //     if (closeButtons == null || closeButtons.Length == 0)
    //         return;
    //     
    //     foreach (Button closeButton in closeButtons)
    //     {
    //         if (closeButton != null)
    //         {
    //             closeButton.onClick.RemoveAllListeners();
    //             closeButton.onClick.AddListener(CloseCurrentPanel);
    //         }
    //     }
    // }
    
    /// <summary>
    /// Manually trigger showing a random offer panel (for testing)
    /// </summary>
    [ContextMenu("Show Random Offer Panel")]
    public void TestShowRandomPanel()
    {
        ShowRandomOfferPanel();
    }
    
    /// <summary>
    /// Reset session flag (useful for testing)
    /// </summary>
    [ContextMenu("Reset Session Flag")]
    public void ResetSessionFlag()
    {
        hasShownOfferThisSession = false;
        GenerateRandomPanelIndices(); // Regenerate random panels
        nextPanelIndexToShow = 0;
        isShowingPanelsSequence = false;
    }
    
    /// <summary>
    /// Gets the account-specific PlayerPrefs key for storing offer timestamp
    /// </summary>
    private string GetOfferTimestampKey()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            // Fallback to device-based storage if no user ID
            return LAST_OFFER_SHOWN_TIMESTAMP_KEY_PREFIX + "Device";
        }
        return LAST_OFFER_SHOWN_TIMESTAMP_KEY_PREFIX + currentUserId;
    }
    
    /// <summary>
    /// Checks if offers have been shown today (same calendar day)
    /// Resets automatically at midnight (start of new day)
    /// </summary>
    private bool HasShownOffersToday()
    {
        string key = GetOfferTimestampKey();
        
        if (!PlayerPrefs.HasKey(key))
        {
            return false; // Never shown before
        }
        
        string lastShownTimestampString = PlayerPrefs.GetString(key);
        
        if (DateTime.TryParse(lastShownTimestampString, out DateTime lastShownTime))
        {
            DateTime now = DateTime.Now;
            
            // Check if last shown date is the same as today's date
            return lastShownTime.Date == now.Date;
        }
        
        return false; // Invalid timestamp, treat as not shown
    }
    
    /// <summary>
    /// Marks that offers have been shown by storing the current timestamp
    /// </summary>
    private void MarkOffersAsShownToday()
    {
        DateTime now = DateTime.Now;
        string timestampString = now.ToString("yyyy-MM-dd HH:mm:ss");
        string key = GetOfferTimestampKey();
        PlayerPrefs.SetString(key, timestampString);
        PlayerPrefs.Save();
        Debug.Log($"[OfferPanelManager] Saved offer shown timestamp for user {currentUserId}: {timestampString}");
    }
    
    /// <summary>
    /// Reset daily flag (useful for testing)
    /// </summary>
    [ContextMenu("Reset Daily Flag")]
    public void ResetDailyFlag()
    {
        string key = GetOfferTimestampKey();
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log($"[OfferPanelManager] Reset daily offer flag for user {currentUserId} - offers will show again now.");
    }
    
    /// <summary>
    /// Calculates the time remaining until midnight (start of next day)
    /// Offers reset automatically at midnight, not after 24 hours
    /// </summary>
    private TimeSpan GetTimeUntilNextOffer()
    {
        string key = GetOfferTimestampKey();
        
        if (!PlayerPrefs.HasKey(key))
        {
            return TimeSpan.Zero; // Never shown, available now
        }
        
        string lastShownTimestampString = PlayerPrefs.GetString(key);
        
        if (DateTime.TryParse(lastShownTimestampString, out DateTime lastShownTime))
        {
            DateTime now = DateTime.Now;
            
            // Calculate next midnight (start of next day)
            DateTime nextMidnight = now.Date.AddDays(1);
            TimeSpan timeRemaining = nextMidnight - now;
            
            // Return zero if time has already passed (shouldn't happen, but safety check)
            return timeRemaining.TotalSeconds > 0 ? timeRemaining : TimeSpan.Zero;
        }
        
        return TimeSpan.Zero; // Invalid timestamp, available now
    }
    
    /// <summary>
    /// Updates the time until next offer display field (for editor debugging)
    /// </summary>
    private IEnumerator UpdateTimeUntilNextOffer()
    {
        while (true)
        {
            DateTime now = DateTime.Now;
            currentTime = now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Only show meaningful info after user has logged in
            if (!hasLoggedIn)
            {
                timeUntilNextOffer = "Waiting for login...";
                lastShownDate = "Waiting for login...";
                nextResetTime = "Waiting for login...";
            }
            else
            {
                string key = GetOfferTimestampKey();
                if (PlayerPrefs.HasKey(key))
                {
                    string timestampString = PlayerPrefs.GetString(key);
                    if (DateTime.TryParse(timestampString, out DateTime lastShownTime))
                    {
                        lastShownDate = lastShownTime.ToString("yyyy-MM-dd HH:mm:ss");
                        // Calculate next midnight (start of next day)
                        DateTime nextAvailableTime = now.Date.AddDays(1);
                        nextResetTime = nextAvailableTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        lastShownDate = "Invalid";
                        nextResetTime = "N/A";
                    }
                }
                else
                {
                    lastShownDate = "Never";
                    nextResetTime = "N/A";
                }
                
                if (HasShownOffersToday())
                {
                    TimeSpan timeRemaining = GetTimeUntilNextOffer();
                    if (timeRemaining.TotalSeconds > 0)
                    {
                        int totalHours = (int)timeRemaining.TotalHours;
                        int minutes = timeRemaining.Minutes;
                        int seconds = timeRemaining.Seconds;
                        timeUntilNextOffer = $"{totalHours:D2}:{minutes:D2}:{seconds:D2}";
                    }
                    else
                    {
                        timeUntilNextOffer = "Available now";
                    }
                }
                else
                {
                    timeUntilNextOffer = "Available now";
                }
            }
            
            yield return new WaitForSeconds(1f); // Update every second
        }
    }
}

