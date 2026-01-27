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
    
    [Header("Dice Scaling")]
    [Tooltip("Reference to the dice parent container (the container that holds both dice)")]
    [SerializeField] private RectTransform diceParentContainer;
    [Tooltip("Reference screen resolution for dice scaling")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    [Tooltip("Dice scale at reference resolution (3D model scale)")]
    [SerializeField] private float referenceDiceScale = 9000f;
    [Tooltip("Dice spacing at reference resolution (distance between dice centers)")]
    [SerializeField] private float referenceDiceSpacing = 200f;
    [Tooltip("Enable automatic dice scaling based on screen resolution")]
    [SerializeField] private bool enableDiceScaling = true;
    
    [Header("Panels to Monitor")]
    [Tooltip("List of panel GameObjects that should hide the dice when active")]
    [SerializeField] private List<GameObject> panelsToMonitor = new List<GameObject>();
    
    [Header("Auto-Detect Panels")]
    [Tooltip("Automatically detect panels by name patterns (e.g., panels containing 'Panel', 'Popup', 'Menu')")]
    [SerializeField] private bool autoDetectPanels = true;
    [SerializeField] private string[] panelNamePatterns = new string[] { "Panel", "Popup", "Menu" };
    
    private bool areDicesHidden = false;
    private bool hasGameplayStarted = false;
    private bool diceScalingApplied = false;
    
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
        
        // Apply dice scaling on start
        if (enableDiceScaling && dicesController != null)
        {
            ApplyDiceScaling();
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
        
        // When gameplay first becomes active, show dice and ensure scaling is applied
        if (!hasGameplayStarted)
        {
            hasGameplayStarted = true;
            dicesController.ShowDices();
            areDicesHidden = false;
            Debug.Log("[DiceVisibilityManager] Gameplay started - dice shown");
            
            // Apply/reapply dice scaling if enabled and not yet applied
            if (enableDiceScaling && !diceScalingApplied)
            {
                ApplyDiceScaling();
            }
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
    /// Applies scaling to dice based on screen resolution
    /// Scales relative to parent container to ensure dice never overflow
    /// </summary>
    private void ApplyDiceScaling()
    {
        if (dicesController == null)
        {
            Debug.LogWarning("[DiceVisibilityManager] Cannot apply dice scaling - DicesController is null!");
            return;
        }
        
        // Calculate scale factors
        float scaleFactorX = Screen.width / referenceScreenSize.x;
        float scaleFactorY = Screen.height / referenceScreenSize.y;
        
        // Use minimum scale factor to ensure dice fit on screen (proportional scaling)
        float uniformScaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // If dice parent container is assigned, further constrain scaling to fit within container
        if (diceParentContainer != null)
        {
            // Get the parent container's size
            float containerWidth = diceParentContainer.rect.width;
            float containerHeight = diceParentContainer.rect.height;
            
            // Calculate reference container size (at reference resolution)
            // Assuming parent scales the same way
            float refContainerWidth = containerWidth / uniformScaleFactor;
            float refContainerHeight = containerHeight / uniformScaleFactor;
            
            // Calculate max scale that fits within container
            // This ensures dice never overflow the container
            float maxScaleBasedOnContainer = Mathf.Min(
                containerWidth / refContainerWidth,
                containerHeight / refContainerHeight
            );
            
            // Use the most restrictive scale factor
            uniformScaleFactor = Mathf.Min(uniformScaleFactor, maxScaleBasedOnContainer);
            
            Debug.Log($"[DiceVisibilityManager] Parent Container: {containerWidth:F2}x{containerHeight:F2}");
            Debug.Log($"[DiceVisibilityManager] Constrained by container to scale: {uniformScaleFactor:F3}");
        }
        
        // Calculate scaled dice size
        float scaledDiceSize = referenceDiceScale * uniformScaleFactor;
        
        Debug.Log($"[DiceVisibilityManager] ====== DICE SCALING ======");
        Debug.Log($"[DiceVisibilityManager] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[DiceVisibilityManager] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[DiceVisibilityManager] Uniform Scale Factor: {uniformScaleFactor:F3}");
        Debug.Log($"[DiceVisibilityManager] Dice Scale: {referenceDiceScale:F0} -> {scaledDiceSize:F0}");
        
        // Access dice through reflection since they're private fields
        var dice1Field = typeof(DicesController).GetField("dice1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dice2Field = typeof(DicesController).GetField("dice2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (dice1Field != null && dice2Field != null)
        {
            GameObject dice1 = dice1Field.GetValue(dicesController) as GameObject;
            GameObject dice2 = dice2Field.GetValue(dicesController) as GameObject;
            
            int scaledCount = 0;
            
            if (dice1 != null)
            {
                dice1.transform.localScale = Vector3.one * scaledDiceSize;
                scaledCount++;
            }
            
            if (dice2 != null)
            {
                dice2.transform.localScale = Vector3.one * scaledDiceSize;
                scaledCount++;
            }
            
            if (scaledCount > 0)
            {
                diceScalingApplied = true;
                Debug.Log($"[DiceVisibilityManager] Dice scaling applied successfully to {scaledCount} dice!");
                
                // Center the dice container and 3D models
                if (diceParentContainer != null)
                {
                    CenterDiceContainer();
                }
                
                // Center the 3D dice models on X axis with scaled spacing
                float scaledSpacing = referenceDiceSpacing * uniformScaleFactor;
                CenterDice3DModels(dice1, dice2, scaledSpacing);
            }
            else
            {
                Debug.LogWarning("[DiceVisibilityManager] No dice found to scale!");
            }
        }
        else
        {
            Debug.LogError("[DiceVisibilityManager] Could not access dice1 and dice2 fields via reflection!");
        }
    }
    
    /// <summary>
    /// Centers the dice parent container in the middle of the screen
    /// </summary>
    private void CenterDiceContainer()
    {
        if (diceParentContainer == null) return;
        
        // Ensure anchors are set to center
        // This makes the container position relative to the center of the parent
        diceParentContainer.anchorMin = new Vector2(0.5f, 0.5f);
        diceParentContainer.anchorMax = new Vector2(0.5f, 0.5f);
        
        // Set pivot to center (makes the container rotate/scale from center)
        diceParentContainer.pivot = new Vector2(0.5f, 0.5f);
        
        // Position at center (0, 0 is center when anchors are at 0.5, 0.5)
        diceParentContainer.anchoredPosition = Vector2.zero;
        
        Debug.Log($"[DiceVisibilityManager] Dice container UI centered - Anchors: (0.5, 0.5), Position: (0, 0)");
    }
    
    /// <summary>
    /// Centers the 3D dice models on the X axis
    /// Positions them horizontally centered with scaled spacing between them
    /// </summary>
    private void CenterDice3DModels(GameObject dice1, GameObject dice2, float spacing)
    {
        if (dice1 == null && dice2 == null) return;
        
        float centerX = 0f; // Center position (0 is usually center)
        
        if (dice1 != null)
        {
            Vector3 pos1 = dice1.transform.localPosition;
            pos1.x = centerX - (spacing / 2f); // Position left of center
            dice1.transform.localPosition = pos1;
            Debug.Log($"[DiceVisibilityManager] Dice 1 centered at X: {pos1.x:F2} (spacing: {spacing:F2})");
        }
        
        if (dice2 != null)
        {
            Vector3 pos2 = dice2.transform.localPosition;
            pos2.x = centerX + (spacing / 2f); // Position right of center
            dice2.transform.localPosition = pos2;
            Debug.Log($"[DiceVisibilityManager] Dice 2 centered at X: {pos2.x:F2} (spacing: {spacing:F2})");
        }
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
