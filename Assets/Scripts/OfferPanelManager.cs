using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages random offer panel display on user login
/// Shows one of seven offer panels randomly when user successfully logs in
/// </summary>
public class OfferPanelManager : MonoBehaviour
{
    [Header("Offer Panels")]
    [SerializeField] private GameObject[] offerPanels = new GameObject[7];
    
    [Header("Settings")]
    [SerializeField] private float showDelay = 1f; // Delay before showing offer panel after login
    [SerializeField] private bool showOnEveryLogin = false; // If false, shows only once per session
    
    [Header("Close Button (Optional)")]
    // [SerializeField] private Button[] closeButtons; // Close buttons for each panel (optional)
    
    private bool hasShownOfferThisSession = false;
    private int currentActivePanelIndex = -1;
    
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
        // Check if we should show offer panel
        if (!showOnEveryLogin && hasShownOfferThisSession)
        {
            return;
        }
        
        // Show random offer panel after delay
        StartCoroutine(ShowRandomOfferPanelDelayed());
    }
    
    /// <summary>
    /// Shows a random offer panel after a delay
    /// </summary>
    private IEnumerator ShowRandomOfferPanelDelayed()
    {
        yield return new WaitForSeconds(showDelay);
        
        ShowRandomOfferPanel();
    }
    
    /// <summary>
    /// Shows a random offer panel from the seven available panels
    /// </summary>
    public void ShowRandomOfferPanel()
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
        
        // Show the randomly selected panel
        if (offerPanels[randomIndex] != null)
        {
            offerPanels[randomIndex].SetActive(true);
            currentActivePanelIndex = randomIndex;
            hasShownOfferThisSession = true;
            
            Debug.Log($"[OfferPanelManager] Showing offer panel {randomIndex + 1} of {validPanelCount}");
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

