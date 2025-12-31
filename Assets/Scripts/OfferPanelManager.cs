using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages sequential offer panel display on user login
/// Shows all seven offer panels one by one when main menu becomes active for the first time after login
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
    
    [Header("Close Button (Optional)")]
    // [SerializeField] private Button[] closeButtons; // Close buttons for each panel (optional)
    
    private bool hasShownOfferThisSession = false;
    private int currentActivePanelIndex = -1;
    private int nextPanelIndexToShow = 0; // Track which panel to show next (0-6)
    private bool hasLoggedIn = false;
    private bool isMonitoringMainMenu = false;
    private bool hasDailyRewardPanelClosed = false; // Start as false - will be set to true when panel closes
    private bool isWaitingForDailyRewardPanel = false;
    private bool isShowingPanelsSequence = false; // Track if we're currently showing panels in sequence
    private bool hasTriggeredDailyRewardPanel = false; // Track if we've already triggered the daily reward panel for this login
    private bool hasShownAfterMainMenuActivation = false; // Track if we've shown offer panels after main menu activation for this login
    private bool isWaitingForNextPanel = false; // Prevent duplicate triggers when waiting for next panel
    
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
        nextPanelIndexToShow = 0; // Reset to show panels from the beginning
        isShowingPanelsSequence = false; // Reset sequence flag
        hasTriggeredDailyRewardPanel = false; // Reset flag so daily reward panel can show again on next login
        hasShownAfterMainMenuActivation = false; // Reset flag so offer panels can show again on next login
        isWaitingForNextPanel = false; // Reset waiting flag
        Debug.Log("[OfferPanelManager] User logged in, waiting for main menu to activate...");
        
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
                // Check if we should show offer panel
                if (!showOnEveryLogin && hasShownOfferThisSession)
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
    /// Shows the next offer panel in sequence (0, 1, 2, 3, 4, 5, 6)
    /// </summary>
    private void ShowNextOfferPanelInSequence()
    {
        // Validate that we have offer panels
        if (offerPanels == null || offerPanels.Length == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No offer panels assigned!");
            return;
        }
        
        // Check if we've shown all panels
        if (nextPanelIndexToShow >= offerPanels.Length)
        {
            Debug.Log("[OfferPanelManager] All offer panels have been shown!");
            isShowingPanelsSequence = false;
            hasShownOfferThisSession = true;
            return;
        }
        
        // Find the next valid panel (skip null panels)
        int validPanelCount = 0;
        int currentIndex = nextPanelIndexToShow;
        int attempts = 0;
        
        // Count valid panels first
        foreach (GameObject panel in offerPanels)
        {
            if (panel != null)
                validPanelCount++;
        }
        
        if (validPanelCount == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No valid offer panels found!");
            return;
        }
        
        // Find next valid panel starting from nextPanelIndexToShow
        while (currentIndex < offerPanels.Length && offerPanels[currentIndex] == null && attempts < offerPanels.Length)
        {
            currentIndex++;
            attempts++;
        }
        
        // If we've reached the end, check if all panels have been shown
        if (currentIndex >= offerPanels.Length)
        {
            Debug.Log("[OfferPanelManager] All offer panels have been shown!");
            isShowingPanelsSequence = false;
            hasShownOfferThisSession = true;
            return;
        }
        
        // Hide all panels first
        HideAllPanels();
        
        // Show the next panel in sequence with animation
        if (offerPanels[currentIndex] != null)
        {
            currentActivePanelIndex = currentIndex;
            nextPanelIndexToShow = currentIndex + 1; // Update to next panel for next time
            isShowingPanelsSequence = true;
            hasShownOfferThisSession = true;
            
            // Start scale animation
            StartCoroutine(AnimatePanelScale(offerPanels[currentIndex]));
            
            Debug.Log($"[OfferPanelManager] Showing offer panel {currentIndex + 1} of {offerPanels.Length} (Panel {currentIndex + 1})");
        }
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
                int previousIndex = currentActivePanelIndex;
                currentActivePanelIndex = -1;
                
                // Move to next panel index
                nextPanelIndexToShow = previousIndex + 1;
                
                // Show next panel in sequence if there are more to show
                if (nextPanelIndexToShow < offerPanels.Length && isShowingPanelsSequence && !isWaitingForNextPanel)
                {
                    isWaitingForNextPanel = true;
                    StartCoroutine(ShowNextPanelAfterDelay());
                }
                else if (nextPanelIndexToShow >= offerPanels.Length)
                {
                    // All panels have been shown
                    isShowingPanelsSequence = false;
                    isWaitingForNextPanel = false;
                    Debug.Log("[OfferPanelManager] All offer panels have been shown and closed!");
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
                        Debug.Log($"[OfferPanelManager] Detected panel {currentActivePanelIndex + 1} was closed, showing next panel...");
                        
                        // Move to next panel index
                        nextPanelIndexToShow = currentActivePanelIndex + 1;
                        
                        // Show next panel if there are more to show
                        if (nextPanelIndexToShow < offerPanels.Length)
                        {
                            isWaitingForNextPanel = true;
                            StartCoroutine(ShowNextPanelAfterDelay());
                        }
                        else
                        {
                            // All panels have been shown
                            isShowingPanelsSequence = false;
                            currentActivePanelIndex = -1;
                            Debug.Log("[OfferPanelManager] All offer panels have been shown and closed!");
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
    /// Setup close buttons for each panel
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
        nextPanelIndexToShow = 0;
        isShowingPanelsSequence = false;
    }
}

