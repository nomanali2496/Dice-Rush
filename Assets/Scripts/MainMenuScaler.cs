using UnityEngine;

/// <summary>
/// Automatically scales main menu UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles dice image (sizeDelta) and cash/chips bars (offset scaling)
/// </summary>
public class MainMenuScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Dice Image (Not Anchored)")]
    [Tooltip("Dice image - will be scaled by sizeDelta (reference: 400x400)")]
    [SerializeField] private RectTransform diceImage;
    
    [Header("Cash and Chips Bars (Anchor-Snapped)")]
    [Tooltip("Cash bar - will be scaled by offsets (anchor-snapped)")]
    [SerializeField] private RectTransform cashBar;
    [Tooltip("Chips bar - will be scaled by offsets (anchor-snapped)")]
    [SerializeField] private RectTransform chipsBar;
    
    // Original sizes for non-anchored elements
    private Vector2 originalDiceSize;
    
    // Original offsets for anchor-snapped elements
    // Stores: (left, right, top, bottom)
    private Vector4 originalCashBarOffsets;
    private Vector4 originalChipsBarOffsets;
    
    private float scaleFactor = 1f;
    
    private void Awake()
    {
        // Store original size for dice image
        if (diceImage != null)
        {
            originalDiceSize = diceImage.sizeDelta;
            Debug.Log($"[MainMenuScaler] Stored original dice size: {originalDiceSize}");
        }
        
        // Store original offsets for cash bar
        if (cashBar != null)
        {
            StoreOriginalOffsets(cashBar, ref originalCashBarOffsets, "Cash Bar");
        }
        
        // Store original offsets for chips bar
        if (chipsBar != null)
        {
            StoreOriginalOffsets(chipsBar, ref originalChipsBarOffsets, "Chips Bar");
        }
    }
    
    private void Start()
    {
        ApplyScaling();
    }
    
    /// <summary>
    /// Stores the original offsets of an anchor-snapped element
    /// </summary>
    private void StoreOriginalOffsets(RectTransform element, ref Vector4 offsets, string elementName)
    {
        float left = element.offsetMin.x;
        float bottom = element.offsetMin.y;
        float right = -element.offsetMax.x;
        float top = -element.offsetMax.y;
        
        offsets = new Vector4(left, right, top, bottom);
        Debug.Log($"[MainMenuScaler] Stored original offsets for {elementName}: L={left:F2}, R={right:F2}, T={top:F2}, B={bottom:F2}");
    }
    
    /// <summary>
    /// Calculates scale factors and applies scaling to all elements
    /// </summary>
    private void ApplyScaling()
    {
        // Calculate scale factors based on screen size vs reference size
        float scaleFactorX = Screen.width / referenceScreenSize.x;
        float scaleFactorY = Screen.height / referenceScreenSize.y;
        
        // Smart scaling: Try Mathf.Max for better space utilization
        // But check if content would overflow - if so, use Mathf.Min
        float maxScale = Mathf.Max(scaleFactorX, scaleFactorY);
        float scaledWidth = referenceScreenSize.x * maxScale;
        float scaledHeight = referenceScreenSize.y * maxScale;
        
        // Check if using maxScale would cause overflow
        if (scaledWidth > Screen.width || scaledHeight > Screen.height)
        {
            // Content would overflow, use safe scaling
            scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
        }
        else
        {
            // Content fits, use max scaling for better space usage
            scaleFactor = maxScale;
        }
        
        Debug.Log($"[MainMenuScaler] ====== MAIN MENU SCALING ======");
        Debug.Log($"[MainMenuScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[MainMenuScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[MainMenuScaler] Uniform Scale Factor: {scaleFactor:F3}");
        
        // Scale dice image (sizeDelta scaling)
        ScaleDiceImage();
        
        // Scale cash and chips bars (offset scaling)
        ScaleCashBar();
        ScaleChipsBar();
        
        Debug.Log($"[MainMenuScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Scales the dice image by adjusting its sizeDelta
    /// </summary>
    private void ScaleDiceImage()
    {
        if (diceImage == null)
        {
            return;
        }
        
        Vector2 scaledSize = new Vector2(
            originalDiceSize.x * scaleFactor,
            originalDiceSize.y * scaleFactor
        );
        
        diceImage.sizeDelta = scaledSize;
        Debug.Log($"[MainMenuScaler] Scaled dice image: {originalDiceSize} -> {scaledSize}");
    }
    
    /// <summary>
    /// Scales the cash bar by adjusting its offsets
    /// </summary>
    private void ScaleCashBar()
    {
        if (cashBar == null)
        {
            return;
        }
        
        // Scale the offsets
        float scaledLeft = originalCashBarOffsets.x * scaleFactor;
        float scaledRight = originalCashBarOffsets.y * scaleFactor;
        float scaledTop = originalCashBarOffsets.z * scaleFactor;
        float scaledBottom = originalCashBarOffsets.w * scaleFactor;
        
        // Apply scaled offsets
        cashBar.offsetMin = new Vector2(scaledLeft, scaledBottom);
        cashBar.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[MainMenuScaler] Scaled cash bar offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales the chips bar by adjusting its offsets
    /// </summary>
    private void ScaleChipsBar()
    {
        if (chipsBar == null)
        {
            return;
        }
        
        // Scale the offsets
        float scaledLeft = originalChipsBarOffsets.x * scaleFactor;
        float scaledRight = originalChipsBarOffsets.y * scaleFactor;
        float scaledTop = originalChipsBarOffsets.z * scaleFactor;
        float scaledBottom = originalChipsBarOffsets.w * scaleFactor;
        
        // Apply scaled offsets
        chipsBar.offsetMin = new Vector2(scaledLeft, scaledBottom);
        chipsBar.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[MainMenuScaler] Scaled chips bar offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
