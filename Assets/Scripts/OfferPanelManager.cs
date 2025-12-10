using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages random offer panel display on user login
/// Shows one of seven offer panels randomly when main menu becomes active for the first time after login
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
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f; // Scale animation duration
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Scale animation curve
    
    [Header("Close Button (Optional)")]
    // [SerializeField] private Button[] closeButtons; // Close buttons for each panel (optional)
    
    private bool hasShownOfferThisSession = false;
    private int currentActivePanelIndex = -1;
    private bool hasLoggedIn = false;
    private bool isMonitoringMainMenu = false;
    
    private AuthenticationManager authManager;
    
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
    }
    
    /// <summary>
    /// Called when user successfully logs in
    /// </summary>
    private void OnUserLoggedIn()
    {
        hasLoggedIn = true;
        Debug.Log("[OfferPanelManager] User logged in, waiting for main menu to activate...");
    }
    
    /// <summary>
    /// Monitors when main menu becomes active for the first time after login
    /// </summary>
    private IEnumerator MonitorMainMenuActivation()
    {
        isMonitoringMainMenu = true;
        bool hasShownAfterMainMenuActivation = false;
        
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
                
                // Show offer panel for the first time after main menu activates
                if (!hasShownAfterMainMenuActivation)
                {
                    hasShownAfterMainMenuActivation = true;
                    yield return new WaitForSeconds(showDelay);
                    ShowRandomOfferPanelWithAnimation();
                }
            }
            
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }
    
    /// <summary>
    /// Shows a random offer panel with smooth scale animation
    /// </summary>
    private void ShowRandomOfferPanelWithAnimation()
    {
        // Validate that we have offer panels
        if (offerPanels == null || offerPanels.Length == 0)
        {
            Debug.LogWarning("[OfferPanelManager] No offer panels assigned!");
            return;
        }
        
        // Count valid panels
        int validPanelCount = 0;
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
        
        // Hide all panels first
        HideAllPanels();
        
        // Select a random panel index
        int randomIndex = Random.Range(0, offerPanels.Length);
        
        // Find a valid panel (skip null panels)
        int attempts = 0;
        while (offerPanels[randomIndex] == null && attempts < offerPanels.Length)
        {
            randomIndex = (randomIndex + 1) % offerPanels.Length;
            attempts++;
        }
        
        // Show the randomly selected panel with animation
        if (offerPanels[randomIndex] != null)
        {
            currentActivePanelIndex = randomIndex;
            hasShownOfferThisSession = true;
            
            // Start scale animation
            StartCoroutine(AnimatePanelScale(offerPanels[randomIndex]));
            
            Debug.Log($"[OfferPanelManager] Showing offer panel {randomIndex + 1} of {validPanelCount} with animation");
        }
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
    /// Closes the currently active offer panel
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (currentActivePanelIndex >= 0 && currentActivePanelIndex < offerPanels.Length)
        {
            if (offerPanels[currentActivePanelIndex] != null)
            {
                offerPanels[currentActivePanelIndex].SetActive(false);
                currentActivePanelIndex = -1;
            }
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
    }
}

