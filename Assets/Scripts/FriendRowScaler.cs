using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically scales friend row UI elements and Enhanced Scroller settings based on screen resolution
/// Reference resolution: 1440x3216
/// Handles cell size, spacing, and all elements within each row
/// All row elements are anchor-snapped and use offset scaling
/// </summary>
public class FriendRowScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Scroller Settings")]
    [SerializeField] private InviteFriendManager inviteFriendManager; // Reference to InviteFriendManager (auto-assigned if null)
    [SerializeField] private float referenceCellSize = 156.4f; // Reference cell size (height)
    [SerializeField] private float referenceSpacing = 21f; // Reference spacing between cells
    
    private float scaleFactorX = 1f;
    private float scaleFactorY = 1f;
    private float scaledCellSize;
    private float scaledSpacing;
    
    private void Awake()
    {
        // Auto-assign InviteFriendManager if not set
        if (inviteFriendManager == null)
        {
            inviteFriendManager = FindObjectOfType<InviteFriendManager>();
            if (inviteFriendManager != null)
            {
                Debug.Log("[FriendRowScaler] Auto-assigned InviteFriendManager from scene");
            }
            else
            {
                Debug.LogWarning("[FriendRowScaler] InviteFriendManager not found in scene!");
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
        
        Debug.Log($"[FriendRowScaler] ====== SCALE CALCULATION ======");
        Debug.Log($"[FriendRowScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[FriendRowScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[FriendRowScaler] Cell Size: {referenceCellSize:F2} -> {scaledCellSize:F2}");
        Debug.Log($"[FriendRowScaler] Spacing: {referenceSpacing:F2} -> {scaledSpacing:F2}");
        
        // Apply scaled cell size to InviteFriendManager
        ApplyScrollerSettings();
        
        Debug.Log($"[FriendRowScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Applies the scaled cell size and spacing to the InviteFriendManager
    /// </summary>
    private void ApplyScrollerSettings()
    {
        if (inviteFriendManager == null)
        {
            Debug.LogWarning("[FriendRowScaler] InviteFriendManager is not assigned!");
            return;
        }
        
        // Use the new public method to set cell size and reload scroller
        inviteFriendManager.SetCellSizeAndReload(scaledCellSize);
        Debug.Log($"[FriendRowScaler] Updated cell size to {scaledCellSize:F2} and reloaded scroller");
    }
    
    /// <summary>
    /// Call this from FriendRowUI to scale individual row elements
    /// This should be called when each row is created/initialized
    /// Note: All elements are now anchor-snapped, so we scale their offsets
    /// </summary>
    public void ScaleRowElements(RectTransform profileImg, RectTransform onlineOfflineImg, 
                                  TextMeshProUGUI nameTxt, TextMeshProUGUI idTxt, RectTransform lineImg)
    {
        // Use uniform scaleFactor for proportional scaling
        float uniformScale = Mathf.Min(scaleFactorX, scaleFactorY);
        
        // Scale profile image offsets
        if (profileImg != null)
        {
            ScaleElementOffsets(profileImg, uniformScale);
        }
        
        // Scale online/offline image offsets
        if (onlineOfflineImg != null)
        {
            ScaleElementOffsets(onlineOfflineImg, uniformScale);
        }
        
        // Scale name text container offsets (auto-sizing remains enabled)
        if (nameTxt != null)
        {
            RectTransform nameRect = nameTxt.GetComponent<RectTransform>();
            if (nameRect != null)
            {
                ScaleElementOffsets(nameRect, uniformScale);
            }
        }
        
        // Scale ID text container offsets (auto-sizing remains enabled)
        if (idTxt != null)
        {
            RectTransform idRect = idTxt.GetComponent<RectTransform>();
            if (idRect != null)
            {
                ScaleElementOffsets(idRect, uniformScale);
            }
        }
        
        // Scale line image offsets
        if (lineImg != null)
        {
            ScaleElementOffsets(lineImg, uniformScale);
        }
    }
    
    /// <summary>
    /// Scales an anchor-snapped element's offsets proportionally
    /// </summary>
    private void ScaleElementOffsets(RectTransform element, float scale)
    {
        if (element == null) return;
        
        // Get current offsets
        Vector2 offsetMin = element.offsetMin;
        Vector2 offsetMax = element.offsetMax;
        
        // Scale the offsets proportionally
        element.offsetMin = offsetMin * scale;
        element.offsetMax = offsetMax * scale;
    }
    
    /// <summary>
    /// Gets the scaled cell size (for use by Enhanced Scroller)
    /// </summary>
    public float GetScaledCellSize()
    {
        return scaledCellSize;
    }
    
    /// <summary>
    /// Gets the scaled spacing
    /// </summary>
    public float GetScaledSpacing()
    {
        return scaledSpacing;
    }
    
    /// <summary>
    /// Gets the current scale factors
    /// </summary>
    public Vector2 GetScaleFactors()
    {
        return new Vector2(scaleFactorX, scaleFactorY);
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
