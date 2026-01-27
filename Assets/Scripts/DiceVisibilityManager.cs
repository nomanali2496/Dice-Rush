using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages dice visibility based on panel states
/// Hides dice when panels are shown over gameplay, shows them when on pure gameplay
/// </summary>
public class DiceVisibilityManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to DicesController")]
    [SerializeField] private DicesController dicesController;
    
    [Header("Gameplay Screen")]
    [Tooltip("Reference to the gameplay screen GameObject - dice visibility is only monitored when this screen is active")]
    [SerializeField] private GameObject gameplayScreen;
    [Tooltip("If true, monitors dice visibility even without a gameplay screen reference (may cause dice to hide at startup)")]
    [SerializeField] private bool monitorWithoutGameplayScreen = false;
    
    [Header("Panels to Monitor")]
    [Tooltip("List of panel GameObjects that should hide the dice when active")]
    [SerializeField] private List<GameObject> panelsToMonitor = new List<GameObject>();
    
    [Header("Auto-Detect Panels")]
    [Tooltip("Automatically detect panels by name patterns (e.g., panels containing 'Panel', 'Popup', 'Menu')")]
    [SerializeField] private bool autoDetectPanels = true;
    [SerializeField] private string[] panelNamePatterns = new string[] { "Panel", "Popup", "Menu" };
    
    private bool areDicesHidden = false;
    private bool hasGameplayStarted = false;
    
    private void Start()
    {
        if (dicesController == null)
        {
            dicesController = FindObjectOfType<DicesController>();
            if (dicesController == null)
            {
                Debug.LogWarning("[DiceVisibilityManager] DicesController not found! Please assign it in the inspector.");
            }
        }
        
        // Try to find gameplay screen if not assigned
        if (gameplayScreen == null)
        {
            GameObject foundGameplayScreen = GameObject.Find("GameplayScreen");
            if (foundGameplayScreen != null)
            {
                gameplayScreen = foundGameplayScreen;
                Debug.Log("[DiceVisibilityManager] Found GameplayScreen automatically.");
            }
            else
            {
                // Try alternate names
                foundGameplayScreen = GameObject.Find("Gameplay");
                if (foundGameplayScreen != null)
                {
                    gameplayScreen = foundGameplayScreen;
                    Debug.Log("[DiceVisibilityManager] Found Gameplay screen automatically.");
                }
                else if (!monitorWithoutGameplayScreen)
                {
                    Debug.LogWarning("[DiceVisibilityManager] Gameplay screen not assigned and not found! Dice visibility will only be monitored if 'Monitor Without Gameplay Screen' is enabled.");
                }
            }
        }
        
        if (autoDetectPanels)
        {
            AutoDetectPanels();
        }
    }
    
    private void Update()
    {
        if (dicesController == null) return;
        
        // Only monitor if gameplay screen is active (or if monitoring without gameplay screen is enabled)
        bool isGameplayActive = monitorWithoutGameplayScreen || (gameplayScreen != null && gameplayScreen.activeSelf);
        
        if (!isGameplayActive)
        {
            // Not on gameplay screen, reset flag
            hasGameplayStarted = false;
            areDicesHidden = false;
            return;
        }
        
        // When gameplay first becomes active, show dice
        if (!hasGameplayStarted)
        {
            hasGameplayStarted = true;
            dicesController.ShowDices();
            areDicesHidden = false;
            Debug.Log("[DiceVisibilityManager] Gameplay started - dice shown");
        }
        
        // Check if any monitored panel is active
        bool anyPanelActive = false;
        foreach (GameObject panel in panelsToMonitor)
        {
            if (panel != null && panel.activeSelf)
            {
                anyPanelActive = true;
                break;
            }
        }
        
        // Simple logic: Panel active = hide dice, Panel off = show dice
        if (anyPanelActive && !areDicesHidden)
        {
            // Panel is active, hide dice
            dicesController.HideDices();
            areDicesHidden = true;
        }
        else if (!anyPanelActive && areDicesHidden)
        {
            // No panels active, show dice
            dicesController.ShowDices();
            areDicesHidden = false;
        }
    }
    
    /// <summary>
    /// Automatically detects panels in the scene hierarchy
    /// </summary>
    private void AutoDetectPanels()
    {
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            // Check if object name matches any pattern
            foreach (string pattern in panelNamePatterns)
            {
                if (obj.name.Contains(pattern) && !panelsToMonitor.Contains(obj))
                {
                    // Additional check: make sure it's likely a UI panel (has Canvas or is child of Canvas)
                    if (obj.GetComponentInParent<Canvas>() != null)
                    {
                        panelsToMonitor.Add(obj);
                        Debug.Log($"[DiceVisibilityManager] Auto-detected panel: {obj.name}");
                        break;
                    }
                }
            }
        }
        
        Debug.Log($"[DiceVisibilityManager] Total panels monitored: {panelsToMonitor.Count}");
    }
    
    /// <summary>
    /// Manually add a panel to monitor
    /// </summary>
    public void AddPanelToMonitor(GameObject panel)
    {
        if (panel != null && !panelsToMonitor.Contains(panel))
        {
            panelsToMonitor.Add(panel);
            Debug.Log($"[DiceVisibilityManager] Added panel to monitor: {panel.name}");
        }
    }
    
    /// <summary>
    /// Manually remove a panel from monitoring
    /// </summary>
    public void RemovePanelFromMonitor(GameObject panel)
    {
        if (panelsToMonitor.Contains(panel))
        {
            panelsToMonitor.Remove(panel);
            Debug.Log($"[DiceVisibilityManager] Removed panel from monitor: {panel.name}");
        }
    }
    
    /// <summary>
    /// Force show dice (useful for debugging or special cases)
    /// </summary>
    public void ForceShowDices()
    {
        if (dicesController != null)
        {
            dicesController.ShowDices();
            areDicesHidden = false;
        }
    }
    
    /// <summary>
    /// Force hide dice (useful for debugging or special cases)
    /// </summary>
    public void ForceHideDices()
    {
        if (dicesController != null)
        {
            dicesController.HideDices();
            areDicesHidden = true;
        }
    }
}
