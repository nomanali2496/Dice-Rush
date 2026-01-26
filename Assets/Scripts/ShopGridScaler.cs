using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically scales Grid Layout Group cell size and spacing based on screen resolution
/// Designed for static shop elements that already exist in the scene
/// Reference resolution: 1440x3216
/// </summary>
public class ShopGridScaler : MonoBehaviour
{
    [Header("Reference Values (1440x3216 resolution)")]
    [SerializeField] private Vector2 referenceCellSize = new Vector2(480f, 590f); // Reference cell size at 1440x3216
    [SerializeField] private Vector2 referenceSpacing = new Vector2(-4f, 116.7f); // Reference spacing at 1440x3216
    [SerializeField] private Vector2 referenceParentSize = new Vector2(1440f, 1311f); // Reference parent size at 1440x3216
    
    [Header("Padding (in pixels)")]
    [SerializeField] private float paddingLeft = 0f; // Padding on the left side
    [SerializeField] private float paddingRight = 0f; // Padding on the right side
    [SerializeField] private float paddingTop = 0f; // Padding on the top side
    [SerializeField] private float paddingBottom = 0f; // Padding on the bottom side
    
    [Header("References")]
    [SerializeField] private GridLayoutGroup gridLayoutGroup; // Grid Layout Group component (auto-assigned if null)
    
    private RectTransform rectTransform;
    private float scaleFactor = 1f;
    
    private void Start()
    {
        // Auto-assign Grid Layout Group if not set
        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
        }
        
        if (gridLayoutGroup == null)
        {
            Debug.LogError("[ShopGridScaler] No Grid Layout Group found on this GameObject!");
            return;
        }
        
        // Get RectTransform
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[ShopGridScaler] No RectTransform found on this GameObject!");
            return;
        }
        
        // Apply scaled values
        ApplyScaling();
    }
    
    /// <summary>
    /// Calculates scale factor and applies it to Grid Layout Group
    /// </summary>
    private void ApplyScaling()
    {
        // Get current parent size
        float currentWidth = rectTransform.rect.width;
        float currentHeight = rectTransform.rect.height;
        
        // Subtract padding from available space
        float availableWidth = currentWidth - (paddingLeft + paddingRight);
        float availableHeight = currentHeight - (paddingTop + paddingBottom);
        
        // Calculate scale factors based on width and height
        float scaleX = availableWidth / referenceParentSize.x;
        float scaleY = availableHeight / referenceParentSize.y;
        
        // Use the smaller scale factor to maintain aspect ratio
        scaleFactor = Mathf.Min(scaleX, scaleY);
        
        // Calculate scaled cell size
        Vector2 scaledCellSize = new Vector2(
            referenceCellSize.x * scaleFactor,
            referenceCellSize.y * scaleFactor
        );
        
        // Calculate scaled spacing
        Vector2 scaledSpacing = new Vector2(
            referenceSpacing.x * scaleFactor,
            referenceSpacing.y * scaleFactor
        );
        
        // Apply to Grid Layout Group
        gridLayoutGroup.cellSize = scaledCellSize;
        gridLayoutGroup.spacing = scaledSpacing;
        
        // Apply padding to Grid Layout Group
        gridLayoutGroup.padding = new RectOffset(
            Mathf.RoundToInt(paddingLeft),
            Mathf.RoundToInt(paddingRight),
            Mathf.RoundToInt(paddingTop),
            Mathf.RoundToInt(paddingBottom)
        );
        
        // Log results
        Debug.Log($"[ShopGridScaler] ====== SCALE CALCULATION ====== Screen: {Screen.width}x{Screen.height} | Parent Size: {currentWidth:F2}x{currentHeight:F2} | Available: {availableWidth:F2}x{availableHeight:F2}");
        Debug.Log($"[ShopGridScaler] Scale Factor: {scaleFactor:F3} | Cell Size: {scaledCellSize.x:F2}x{scaledCellSize.y:F2} | Spacing: {scaledSpacing.x:F2}x{scaledSpacing.y:F2}");
        Debug.Log($"[ShopGridScaler] Padding: L{paddingLeft:F0} R{paddingRight:F0} T{paddingTop:F0} B{paddingBottom:F0}");
    }
    
    /// <summary>
    /// Call this to manually recalculate and apply scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        if (gridLayoutGroup != null && rectTransform != null)
        {
            ApplyScaling();
        }
    }
    
    /// <summary>
    /// Gets the current scale factor
    /// </summary>
    public float GetScaleFactor()
    {
        return scaleFactor;
    }
}
