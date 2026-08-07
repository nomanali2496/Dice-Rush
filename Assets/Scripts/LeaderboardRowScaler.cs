using UnityEngine;

/// <summary>
/// Automatically scales leaderboard row UI elements and Enhanced Scroller settings based on screen resolution
/// Reference resolution: 1440x3216
/// Handles cell size, spacing, and all elements within each row
/// All row elements are anchor-snapped and use offset scaling
/// </summary>
public class LeaderboardRowScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Scroller Settings")]
    [SerializeField] private LeaderboardManager leaderboardManager; // Reference to LeaderboardManager (auto-assigned if null)
    [SerializeField] private float referenceCellSize = 155f; // Reference cell size (height)
    [SerializeField] private float referenceSpacing = 10f; // Reference spacing between cells
    
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
        // Auto-assign LeaderboardManager if not set
        if (leaderboardManager == null)
        {
            leaderboardManager = FindFirstObjectByType<LeaderboardManager>();
            if (leaderboardManager != null)
            {
                Debug.Log("[LeaderboardRowScaler] Auto-assigned LeaderboardManager from scene");
            }
            else
            {
                Debug.LogWarning("[LeaderboardRowScaler] LeaderboardManager not found in scene!");
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
        
        Debug.Log($"[LeaderboardRowScaler] ====== SCALE CALCULATION ======");
        Debug.Log($"[LeaderboardRowScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[LeaderboardRowScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[LeaderboardRowScaler] Cell Size: {referenceCellSize:F2} -> {scaledCellSize:F2}");
        Debug.Log($"[LeaderboardRowScaler] Spacing: {referenceSpacing:F2} -> {scaledSpacing:F2}");
        
        // Apply scaled cell size to LeaderboardManager
        ApplyScrollerSettings();
        
        Debug.Log($"[LeaderboardRowScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Applies the scaled cell size to the LeaderboardManager
    /// </summary>
    private void ApplyScrollerSettings()
    {
        if (leaderboardManager == null)
        {
            Debug.LogWarning("[LeaderboardRowScaler] LeaderboardManager is not assigned!");
            return;
        }
        
        // Use the public method to set cell size and reload scroller
        leaderboardManager.SetCellSizeAndReload(scaledCellSize);
        Debug.Log($"[LeaderboardRowScaler] Updated cell size to {scaledCellSize:F2} and reloaded scroller");
    }
    
    /// <summary>
    /// Scales row elements by their offsets (for anchor-snapped elements)
    /// Call this method from LeaderboardRowUI to scale individual row elements
    /// </summary>
    public void ScaleRowElements(RectTransform rankText, RectTransform profileImage, 
                                 RectTransform nameText, RectTransform scoreText)
    {
        // Use uniform scale factor (use min for proportional scaling)
        float uniformScale = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // Scale each element by offsets
        if (rankText != null)
        {
            ScaleElementOffsets(rankText, uniformScale);
        }
        
        if (profileImage != null)
        {
            ScaleElementOffsets(profileImage, uniformScale);
        }
        
        if (nameText != null)
        {
            ScaleElementOffsets(nameText, uniformScale);
        }
        
        if (scoreText != null)
        {
            ScaleElementOffsets(scoreText, uniformScale);
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
