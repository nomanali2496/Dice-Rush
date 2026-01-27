using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Automatically scales Invite Friends panel UI elements based on screen resolution
/// Reference resolution: 1440x3216
/// Supports both anchor-snapped elements (offset scaling) and center-anchored elements (sizeDelta scaling)
/// </summary>
public class InviteFriendsScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Upper Part Elements")]
    [SerializeField] private RectTransform upperPart; // 1022 x 408 (not anchored, uses sizeDelta)
    [SerializeField] private RectTransform title; // 790 x 408
    [SerializeField] private RectTransform closeButton; // 251 x 266
    
    [Header("Lower Part Elements")]
    [SerializeField] private RectTransform lowerPart; // 1334 x 1936 (not anchored, uses sizeDelta)
    [SerializeField] private RectTransform upperButton; // 760 x 194
    [SerializeField] private RectTransform panel1; // 1154 x 1566
    [SerializeField] private RectTransform panel2; // 1154 x 1566
    [SerializeField] private RectTransform scrollerContainer; // 1038 x 1301 (non-anchored, uses sizeDelta)
    [SerializeField] private RectTransform inviteFriendButton; // 458 x 167
    
    // Store original offset values for anchor-snapped elements
    private Dictionary<RectTransform, Vector4> originalOffsets = new Dictionary<RectTransform, Vector4>(); // left, right, top, bottom
    
    // Store original sizes for sizeDelta elements
    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();
    
    private float scaleFactor = 1f; // Single uniform scale factor for proportional scaling
    
    private void Awake()
    {
        // Store original offsets for anchor-snapped elements (keeps them positioned correctly)
        StoreOriginalOffsets(upperPart, "Upper Part");
        StoreOriginalOffsets(lowerPart, "Lower Part");
        
        StoreOriginalOffsets(title, "Title");
        StoreOriginalOffsets(closeButton, "Close Button");
        StoreOriginalOffsets(upperButton, "Upper Button");
        StoreOriginalOffsets(panel1, "Panel 1");
        StoreOriginalOffsets(panel2, "Panel 2");
        StoreOriginalOffsets(inviteFriendButton, "Invite Friend Button");
        
        // Store original size for scroller (will be scaled relative to panel2)
        StoreOriginalSize(scrollerContainer, "Scroller Container");
    }
    
    private void Start()
    {
        ApplyScaling();
    }
    
    /// <summary>
    /// Stores the original anchor offsets from the scene for an element
    /// </summary>
    private void StoreOriginalOffsets(RectTransform element, string elementName)
    {
        if (element == null)
        {
            Debug.LogWarning($"[InviteFriendsScaler] {elementName} is not assigned!");
            return;
        }
        
        // Store the current offsets (left, right, top, bottom)
        Vector4 offsets = new Vector4(
            element.offsetMin.x,  // left
            -element.offsetMax.x, // right (negative because offsetMax is measured from right edge)
            -element.offsetMax.y, // top (negative because offsetMax is measured from top edge)
            element.offsetMin.y   // bottom
        );
        
        originalOffsets[element] = offsets;
        Debug.Log($"[InviteFriendsScaler] Stored original offsets for {elementName}: Left={offsets.x:F2}, Right={offsets.y:F2}, Top={offsets.z:F2}, Bottom={offsets.w:F2}");
    }
    
    /// <summary>
    /// Stores the original size (sizeDelta) from the scene for an element
    /// </summary>
    private void StoreOriginalSize(RectTransform element, string elementName)
    {
        if (element == null)
        {
            Debug.LogWarning($"[InviteFriendsScaler] {elementName} is not assigned!");
            return;
        }
        
        // Store the current sizeDelta
        originalSizes[element] = element.sizeDelta;
        Debug.Log($"[InviteFriendsScaler] Stored original size for {elementName}: {element.sizeDelta.x:F2}x{element.sizeDelta.y:F2}");
    }
    
    /// <summary>
    /// Calculates scale factors and applies scaling to all UI elements
    /// </summary>
    private void ApplyScaling()
    {
        // Calculate scale factors based on screen size
        float scaleFactorX = Screen.width / referenceScreenSize.x;
        float scaleFactorY = Screen.height / referenceScreenSize.y;
        
        // Start with MAXIMUM scale factor for better space utilization
        float maxScale = Mathf.Max(scaleFactorX, scaleFactorY);
        
        // Calculate what the scaled content size would be
        float scaledWidth = referenceScreenSize.x * maxScale;
        float scaledHeight = referenceScreenSize.y * maxScale;
        
        // Check if scaled content fits within screen bounds
        // If it overflows, clamp the scale factor to fit
        if (scaledWidth > Screen.width || scaledHeight > Screen.height)
        {
            // Content would overflow, use min scale factor to fit within bounds
            scaleFactor = Mathf.Min(scaleFactorX, scaleFactorY);
            Debug.Log($"[InviteFriendsScaler] Content would overflow, using constrained scale factor");
        }
        else
        {
            // Content fits, use max scale for better space usage
            scaleFactor = maxScale;
            Debug.Log($"[InviteFriendsScaler] Content fits, using maximum scale factor");
        }
        
        Debug.Log($"[InviteFriendsScaler] ====== SCALE CALCULATION ======");
        Debug.Log($"[InviteFriendsScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[InviteFriendsScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[InviteFriendsScaler] Final Scale Factor: {scaleFactor:F3} (maximizing space while staying in bounds)");
        
        // Apply offset scaling to anchor-snapped elements (keeps them positioned correctly)
        ScaleElementByOffset(upperPart, "Upper Part");
        ScaleElementByOffset(lowerPart, "Lower Part");
        ScaleElementByOffset(title, "Title");
        ScaleElementByOffset(closeButton, "Close Button");
        ScaleElementByOffset(upperButton, "Upper Button");
        ScaleElementByOffset(panel1, "Panel 1");
        ScaleElementByOffset(panel2, "Panel 2");
        ScaleElementByOffset(inviteFriendButton, "Invite Friend Button");
        
        // Scale scroller AFTER panel2 is scaled, based on panel2's actual size
        ScaleScrollerRelativeToPanel2();
        
        Debug.Log($"[InviteFriendsScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Scales the scroller container relative to Panel 2's actual size
    /// Maintains the same ratio as in the reference resolution
    /// </summary>
    private void ScaleScrollerRelativeToPanel2()
    {
        if (scrollerContainer == null)
        {
            Debug.LogWarning("[InviteFriendsScaler] Scroller Container is not assigned!");
            return;
        }
        
        if (panel2 == null)
        {
            Debug.LogWarning("[InviteFriendsScaler] Panel 2 is not assigned! Cannot scale scroller relative to panel.");
            return;
        }
        
        // Reference sizes
        const float refPanel2Width = 1154f;
        const float refPanel2Height = 1566f;
        const float refScrollerWidth = 1038f;
        const float refScrollerHeight = 1301f;
        
        // Calculate reference ratios
        float widthRatio = refScrollerWidth / refPanel2Width;   // 0.8995
        float heightRatio = refScrollerHeight / refPanel2Height; // 0.8308
        
        // Get Panel 2's actual size after scaling
        float panel2Width = panel2.rect.width;
        float panel2Height = panel2.rect.height;
        
        // Calculate scroller size based on panel 2's actual size and the reference ratio
        Vector2 scrollerSize = new Vector2(
            panel2Width * widthRatio,
            panel2Height * heightRatio
        );
        
        // Apply to scroller
        scrollerContainer.sizeDelta = scrollerSize;
        
        Debug.Log($"[InviteFriendsScaler] Scroller Container: Scaled relative to Panel 2");
        Debug.Log($"[InviteFriendsScaler]   Panel 2 Size: {panel2Width:F2}x{panel2Height:F2}");
        Debug.Log($"[InviteFriendsScaler]   Ratios: W={widthRatio:F4}, H={heightRatio:F4}");
        Debug.Log($"[InviteFriendsScaler]   Scroller Size: {scrollerSize.x:F2}x{scrollerSize.y:F2}");
    }
    
    /// <summary>
    /// Scales an anchor-snapped element by adjusting its anchor offsets
    /// </summary>
    private void ScaleElementByOffset(RectTransform element, string elementName)
    {
        if (element == null || !originalOffsets.ContainsKey(element))
        {
            Debug.LogWarning($"[InviteFriendsScaler] {elementName} is not assigned or has no stored offsets!");
            return;
        }
        
        // Get the original offsets (left, right, top, bottom)
        Vector4 originalOffset = originalOffsets[element];
        
        // Calculate scaled offsets using uniform scale factor
        float scaledLeft = originalOffset.x * scaleFactor;
        float scaledRight = originalOffset.y * scaleFactor;
        float scaledTop = originalOffset.z * scaleFactor;
        float scaledBottom = originalOffset.w * scaleFactor;
        
        // Apply to RectTransform
        element.offsetMin = new Vector2(scaledLeft, scaledBottom);
        element.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[InviteFriendsScaler] {elementName}: Offsets scaled - L:{originalOffset.x:F0}->{scaledLeft:F2}, R:{originalOffset.y:F0}->{scaledRight:F2}, T:{originalOffset.z:F0}->{scaledTop:F2}, B:{originalOffset.w:F0}->{scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales a non-anchored element by adjusting its sizeDelta
    /// </summary>
    private void ScaleElementBySize(RectTransform element, string elementName)
    {
        if (element == null || !originalSizes.ContainsKey(element))
        {
            Debug.LogWarning($"[InviteFriendsScaler] {elementName} is not assigned or has no stored size!");
            return;
        }
        
        // Get the original size
        Vector2 originalSize = originalSizes[element];
        
        // Calculate scaled size using uniform scale factor
        Vector2 scaledSize = new Vector2(
            originalSize.x * scaleFactor,
            originalSize.y * scaleFactor
        );
        
        // Apply to RectTransform
        element.sizeDelta = scaledSize;
        
        Debug.Log($"[InviteFriendsScaler] {elementName}: Size scaled - {originalSize.x:F0}x{originalSize.y:F0} -> {scaledSize.x:F2}x{scaledSize.y:F2}");
    }
    
    /// <summary>
    /// Call this to manually recalculate and apply scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
    
    /// <summary>
    /// Gets the current scale factor
    /// </summary>
    public float GetScaleFactor()
    {
        return scaleFactor;
    }
}
