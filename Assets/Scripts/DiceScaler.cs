using UnityEngine;

/// <summary>
/// Manages automatic dice scaling based on screen resolution and parent container bounds.
/// Purely handles scaling and horizontal centering of the 3D dice models.
/// </summary>
public class DiceScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to DicesController")]
    [SerializeField] private DicesController dicesController;
    
    [Header("Dice Scaling Settings")]
    [Tooltip("Reference to the dice parent container (the RectTransform holding the dice)")]
    [SerializeField] private RectTransform diceParentContainer;
    [Tooltip("Reference screen resolution for dice scaling")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    [Tooltip("Dice scale at reference resolution (3D model scale)")]
    [SerializeField] private float referenceDiceScale = 9000f;
    [Tooltip("Dice spacing at reference resolution (distance between dice centers)")]
    [SerializeField] private float referenceDiceSpacing = 200f;
    
    private void Start()
    {
        if (dicesController == null)
        {
            dicesController = FindObjectOfType<DicesController>();
            if (dicesController == null)
            {
                Debug.LogWarning("[DiceScaler] DicesController not found! Please assign it in the inspector.");
                return;
            }
        }
        
        ApplyDiceScaling();
    }
    
    /// <summary>
    /// Applies scaling to dice based on screen resolution
    /// Scales relative to parent container to ensure dice never overflow
    /// </summary>
    public void ApplyDiceScaling()
    {
        if (dicesController == null)
        {
            Debug.LogWarning("[DiceScaler] Cannot apply dice scaling - DicesController is null!");
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
            float refContainerWidth = containerWidth / uniformScaleFactor;
            float refContainerHeight = containerHeight / uniformScaleFactor;
            
            // Calculate max scale that fits within container
            float maxScaleBasedOnContainer = Mathf.Min(
                containerWidth / refContainerWidth,
                containerHeight / refContainerHeight
            );
            
            // Use the most restrictive scale factor
            uniformScaleFactor = Mathf.Min(uniformScaleFactor, maxScaleBasedOnContainer);
            
            Debug.Log($"[DiceScaler] Parent Container size: {containerWidth:F2}x{containerHeight:F2}");
            Debug.Log($"[DiceScaler] Constrained by container to scale factor: {uniformScaleFactor:F3}");
        }
        
        // Calculate scaled dice size
        float scaledDiceSize = referenceDiceScale * uniformScaleFactor;
        
        Debug.Log($"[DiceScaler] ====== DICE SCALING ======");
        Debug.Log($"[DiceScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[DiceScaler] Uniform Scale Factor: {uniformScaleFactor:F3}");
        Debug.Log($"[DiceScaler] Dice Scale: {referenceDiceScale:F0} -> {scaledDiceSize:F0}");
        
        // Access dice through reflection since they're private fields in DicesController
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
                Debug.Log($"[DiceScaler] Dice scaling applied successfully to {scaledCount} dice!");
                
                // Center the dice container UI
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
                Debug.LogWarning("[DiceScaler] No dice game objects found to scale!");
            }
        }
        else
        {
            Debug.LogError("[DiceScaler] Could not access dice1 and dice2 fields via reflection!");
        }
    }
    
    /// <summary>
    /// Centers the dice parent container RectTransform in the middle of the screen
    /// </summary>
    private void CenterDiceContainer()
    {
        if (diceParentContainer == null) return;
        
        // Ensure anchors are set to center
        diceParentContainer.anchorMin = new Vector2(0.5f, 0.5f);
        diceParentContainer.anchorMax = new Vector2(0.5f, 0.5f);
        
        // Set pivot to center
        diceParentContainer.pivot = new Vector2(0.5f, 0.5f);
        
        // Position at center (0, 0)
        diceParentContainer.anchoredPosition = Vector2.zero;
        
        Debug.Log($"[DiceScaler] Dice container UI centered at (0, 0)");
    }
    
    /// <summary>
    /// Centers the 3D dice models on the X axis relative to parent center
    /// </summary>
    private void CenterDice3DModels(GameObject dice1, GameObject dice2, float spacing)
    {
        if (dice1 == null && dice2 == null) return;
        
        float centerX = 0f;
        
        if (dice1 != null)
        {
            Vector3 pos1 = dice1.transform.localPosition;
            pos1.x = centerX - (spacing / 2f); // Offset left
            dice1.transform.localPosition = pos1;
        }
        
        if (dice2 != null)
        {
            Vector3 pos2 = dice2.transform.localPosition;
            pos2.x = centerX + (spacing / 2f); // Offset right
            dice2.transform.localPosition = pos2;
        }
    }
}
