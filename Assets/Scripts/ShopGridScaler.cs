using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Automatically scales Grid Layout Group cell size and spacing based on screen resolution
/// Designed for static shop elements that already exist in the scene
/// Reference resolution: 1440x3216
/// </summary>
public class ShopGridScaler : MonoBehaviour
{
    [Header("Padding (in pixels)")]
    [SerializeField] private float paddingLeft = 0f; // Padding on the left side
    [SerializeField] private float paddingRight = 0f; // Padding on the right side
    [SerializeField] private float paddingTop = 0f; // Padding on the top side
    [SerializeField] private float paddingBottom = 0f; // Padding on the bottom side
    
    [Header("References")]
    [SerializeField] private GridLayoutGroup gridLayoutGroup; // Grid Layout Group component (auto-assigned if null)
    [SerializeField] private RectTransform cashBar; // Reference to Cash bar
    [SerializeField] private RectTransform coinsBar; // Reference to Coins bar
    [SerializeField] private RectTransform upperTitle; // Reference to Upper Title (e.g. "GET COINS")
    [SerializeField] private RectTransform lowerTitle; // Reference to Lower Title
    
    // Reference resolution
    private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    // Storage for original values (captured automatically in Awake)
    private Vector2 originalCellSize;
    private Vector2 originalSpacing;
    
    // Storage for original offsets (for anchored elements)
    private Dictionary<RectTransform, Vector4> originalOffsets = new Dictionary<RectTransform, Vector4>();
    
    private RectTransform rectTransform;
    private float scaleFactor = 1f;

    private void Awake()
    {
        // Auto-assign Grid Layout Group if not set
        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
        }

        // Get RectTransform
        rectTransform = GetComponent<RectTransform>();

        // Store original grid layout values before any scaling occurs
        if (gridLayoutGroup != null)
        {
            originalCellSize = gridLayoutGroup.cellSize;
            originalSpacing = gridLayoutGroup.spacing;
        }

        // Store original offsets for all anchored elements before any scaling happens
        StoreOriginalOffsets(cashBar);
        StoreOriginalOffsets(coinsBar);
        StoreOriginalOffsets(upperTitle);
        StoreOriginalOffsets(lowerTitle);
    }
    
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
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
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
        // Calculate scale factors based on screen vs reference
        float scaleFactorX = Screen.width / referenceScreenSize.x;
        float scaleFactorY = Screen.height / referenceScreenSize.y;
        
        // Use smart scaling logic
        float maxScale = Mathf.Max(scaleFactorX, scaleFactorY);
        float scaledWidth = referenceScreenSize.x * maxScale;
        float scaledHeight = referenceScreenSize.y * maxScale;
        
        if (scaledWidth > Screen.width || scaledHeight > Screen.height)
        {
            scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
        }
        else
        {
            scaleFactor = maxScale;
        }
        
        // Calculate scaled cell size
        Vector2 scaledCellSize = new Vector2(
            originalCellSize.x * scaleFactor,
            originalCellSize.y * scaleFactor
        );
        
        // Calculate scaled spacing
        Vector2 scaledSpacing = new Vector2(
            originalSpacing.x * scaleFactor,
            originalSpacing.y * scaleFactor
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

        // Scale other UI elements using Offset Scaling (Marginal scaling)
        ScaleByOffset(cashBar);
        ScaleByOffset(coinsBar);
        ScaleByOffset(upperTitle);
        ScaleByOffset(lowerTitle);
        
        // Log results
        Debug.Log($"[ShopGridScaler] ====== SCALE CALCULATION ====== Screen: {Screen.width}x{Screen.height} | Scale Factor: {scaleFactor:F3}");
        Debug.Log($"[ShopGridScaler] Cell Size: {scaledCellSize.x:F2}x{scaledCellSize.y:F2} | Spacing: {scaledSpacing.x:F2}x{scaledSpacing.y:F2}");
        Debug.Log($"[ShopGridScaler] Padding: L{paddingLeft:F0} R{paddingRight:F0} T{paddingTop:F0} B{paddingBottom:F0}");
    }

    private void StoreOriginalOffsets(RectTransform element)
    {
        if (element != null && !originalOffsets.ContainsKey(element))
        {
            float left = element.offsetMin.x;
            float bottom = element.offsetMin.y;
            float right = -element.offsetMax.x;
            float top = -element.offsetMax.y;
            originalOffsets[element] = new Vector4(left, right, top, bottom);
        }
    }

    private void ScaleByOffset(RectTransform element)
    {
        if (element == null || !originalOffsets.ContainsKey(element)) return;

        Vector4 offsets = originalOffsets[element];
        float scaledLeft = offsets.x * scaleFactor;
        float scaledRight = offsets.y * scaleFactor;
        float scaledTop = offsets.z * scaleFactor;
        float scaledBottom = offsets.w * scaleFactor;

        element.offsetMin = new Vector2(scaledLeft, scaledBottom);
        element.offsetMax = new Vector2(-scaledRight, -scaledTop);
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
