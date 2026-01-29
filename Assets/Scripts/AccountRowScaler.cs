using UnityEngine;

/// <summary>
/// Automatically scales account row UI elements and Enhanced Scroller settings based on screen resolution
/// Reference resolution: 1440x3216
/// Handles cell size, spacing, and all elements within each row
/// All row elements are anchor-snapped and use offset scaling
/// </summary>
public class AccountRowScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Scroller Settings")]
    [SerializeField] private AccountManager accountManager; // Reference to AccountManager (auto-assigned if null)
    [SerializeField] private float referenceCellSize = 100f; // Reference cell size (height)
    [SerializeField] private float referenceSpacing = 21f; // Reference spacing between cells
    
    private float scaleFactorX = 1f;
    private float scaleFactorY = 1f;
    private float scaledCellSize;
    private float scaledSpacing;
    
    // Public properties to access scaled values
    public float ScaledCellSize => scaledCellSize;
    public float ScaledSpacing => scaledSpacing;
    public float ScaleFactorX => scaleFactorX;
    public float ScaleFactorY => scaleFactorY;
    
    private void Awake()
    {
        // Auto-assign AccountManager if not set
        if (accountManager == null)
        {
            accountManager = FindObjectOfType<AccountManager>();
            if (accountManager != null)
            {
                Debug.Log("[AccountRowScaler] Auto-assigned AccountManager from scene");
            }
            else
            {
                Debug.LogWarning("[AccountRowScaler] AccountManager not found in scene!");
            }
        }
    }
    
    private void Start()
    {
        ApplyScaling();
    }
    
    /// <summary>
    /// Calculates scale factors and applies scaling
    /// </summary>
    private void ApplyScaling()
    {
        // Calculate scale factors
        scaleFactorX = Screen.width / referenceScreenSize.x;
        scaleFactorY = Screen.height / referenceScreenSize.y;
        
        // Calculate scaled cell size and spacing
        scaledCellSize = referenceCellSize * scaleFactorY; // Use Y scale for vertical scrolling
        scaledSpacing = referenceSpacing * scaleFactorY;
        
        Debug.Log($"[AccountRowScaler] ====== SCALE CALCULATION ======");
        Debug.Log($"[AccountRowScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[AccountRowScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[AccountRowScaler] Cell Size: {referenceCellSize:F2} -> {scaledCellSize:F2}");
        Debug.Log($"[AccountRowScaler] Spacing: {referenceSpacing:F2} -> {scaledSpacing:F2}");
        
        // Apply scaled cell size to AccountManager
        ApplyScrollerSettings();
        
        Debug.Log($"[AccountRowScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Applies the scaled cell size to the AccountManager
    /// </summary>
    private void ApplyScrollerSettings()
    {
        if (accountManager == null)
        {
            Debug.LogWarning("[AccountRowScaler] AccountManager is not assigned!");
            return;
        }
        
        // Use the public method to set cell size and reload scroller
        accountManager.SetCellSizeAndReload(scaledCellSize);
        Debug.Log($"[AccountRowScaler] Updated cell size to {scaledCellSize:F2} and reloaded scroller");
    }
    
    /// <summary>
    /// Scales row elements by their offsets (for anchor-snapped elements)
    /// Call this method from AccountRowUI to scale individual row elements
    /// </summary>
    public void ScaleRowElements(RectTransform profileImage, RectTransform statusImage, 
                                 RectTransform textContainer, RectTransform lineImage)
    {
        // Use uniform scale factor (use min for proportional scaling)
        float uniformScale = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // Scale each element by offsets
        if (profileImage != null)
        {
            ScaleElementOffsets(profileImage, uniformScale);
        }
        
        if (statusImage != null)
        {
            ScaleElementOffsets(statusImage, uniformScale);
        }
        
        if (textContainer != null)
        {
            ScaleElementOffsets(textContainer, uniformScale);
        }
        
        if (lineImage != null)
        {
            ScaleElementOffsets(lineImage, uniformScale);
        }
    }
    
    /// <summary>
    /// Scales an anchor-snapped element by adjusting its offsets
    /// </summary>
    private void ScaleElementOffsets(RectTransform element, float scale)
    {
        // Get current offsets
        Vector2 offsetMin = element.offsetMin;
        Vector2 offsetMax = element.offsetMax;
        
        // Scale offsets
        element.offsetMin = offsetMin * scale;
        element.offsetMax = offsetMax * scale;
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
