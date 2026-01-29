using UnityEngine;

/// <summary>
/// Automatically scales account panel UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles hybrid scaling: sizeDelta for parent containers, offset for anchor-snapped children
/// </summary>
public class AccountPanelScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Upper Part (Not Anchored)")]
    [Tooltip("Upper part container - will be scaled by sizeDelta (reference: 1022 x 408)")]
    [SerializeField] private RectTransform upperPart;
    
    [Header("Upper Part Children (Anchor-Snapped)")]
    [Tooltip("Title in upper part - will be scaled by offsets (reference: 790 x 408)")]
    [SerializeField] private RectTransform title;
    [Tooltip("Close button in upper part - will be scaled by offsets (reference: 251 x 266)")]
    [SerializeField] private RectTransform closeButton;
    
    [Header("Lower Part (Not Anchored)")]
    [Tooltip("Lower part container - will be scaled by sizeDelta (reference: 1301 x 1879)")]
    [SerializeField] private RectTransform lowerPart;
    
    [Header("Lower Part Children (Anchor-Snapped)")]
    [Tooltip("Upper button in lower part - will be scaled by offsets (reference: 761 x 137)")]
    [SerializeField] private RectTransform upperButton;
    [Tooltip("Panel 1 in lower part - will be scaled by offsets (reference: 1114 x 1307)")]
    [SerializeField] private RectTransform panel1;
    [Tooltip("Panel 2 in lower part - will be scaled by offsets (reference: 1114 x 1566)")]
    [SerializeField] private RectTransform panel2;
    
    // Original offsets for all anchor-snapped elements
    // Stores: (left, right, top, bottom)
    private Vector4 originalUpperPartOffsets;
    private Vector4 originalLowerPartOffsets;
    private Vector4 originalTitleOffsets;
    private Vector4 originalCloseButtonOffsets;
    private Vector4 originalUpperButtonOffsets;
    private Vector4 originalPanel1Offsets;
    private Vector4 originalPanel2Offsets;
    
    private float scaleFactor = 1f;
    
    private void Awake()
    {
        // Store original offsets for all anchor-snapped elements
        // Parent containers
        if (upperPart != null)
        {
            StoreOriginalOffsets(upperPart, ref originalUpperPartOffsets, "Upper Part");
        }
        
        if (lowerPart != null)
        {
            StoreOriginalOffsets(lowerPart, ref originalLowerPartOffsets, "Lower Part");
        }
        
        // Upper part children
        if (title != null)
        {
            StoreOriginalOffsets(title, ref originalTitleOffsets, "Title");
        }
        
        if (closeButton != null)
        {
            StoreOriginalOffsets(closeButton, ref originalCloseButtonOffsets, "Close Button");
        }
        
        // Lower part children
        if (upperButton != null)
        {
            StoreOriginalOffsets(upperButton, ref originalUpperButtonOffsets, "Upper Button");
        }
        
        if (panel1 != null)
        {
            StoreOriginalOffsets(panel1, ref originalPanel1Offsets, "Panel 1");
        }
        
        if (panel2 != null)
        {
            StoreOriginalOffsets(panel2, ref originalPanel2Offsets, "Panel 2");
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
        Debug.Log($"[AccountPanelScaler] Stored original offsets for {elementName}: L={left:F2}, R={right:F2}, T={top:F2}, B={bottom:F2}");
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
        
        Debug.Log($"[AccountPanelScaler] ====== ACCOUNT PANEL SCALING ======");
        Debug.Log($"[AccountPanelScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[AccountPanelScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[AccountPanelScaler] Uniform Scale Factor: {scaleFactor:F3}");
        
        // Scale parent containers and children (all use offset scaling)
        ScaleUpperPart();
        ScaleLowerPart();
        ScaleTitle();
        ScaleCloseButton();
        ScaleUpperButton();
        ScalePanel1();
        ScalePanel2();
        
        Debug.Log($"[AccountPanelScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Scales the upper part by adjusting its offsets
    /// </summary>
    private void ScaleUpperPart()
    {
        if (upperPart == null)
        {
            return;
        }
        
        float scaledLeft = originalUpperPartOffsets.x * scaleFactor;
        float scaledRight = originalUpperPartOffsets.y * scaleFactor;
        float scaledTop = originalUpperPartOffsets.z * scaleFactor;
        float scaledBottom = originalUpperPartOffsets.w * scaleFactor;
        
        upperPart.offsetMin = new Vector2(scaledLeft, scaledBottom);
        upperPart.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled upper part offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales the lower part by adjusting its offsets
    /// </summary>
    private void ScaleLowerPart()
    {
        if (lowerPart == null)
        {
            return;
        }
        
        float scaledLeft = originalLowerPartOffsets.x * scaleFactor;
        float scaledRight = originalLowerPartOffsets.y * scaleFactor;
        float scaledTop = originalLowerPartOffsets.z * scaleFactor;
        float scaledBottom = originalLowerPartOffsets.w * scaleFactor;
        
        lowerPart.offsetMin = new Vector2(scaledLeft, scaledBottom);
        lowerPart.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled lower part offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales the title by adjusting its offsets
    /// </summary>
    private void ScaleTitle()
    {
        if (title == null)
        {
            return;
        }
        
        float scaledLeft = originalTitleOffsets.x * scaleFactor;
        float scaledRight = originalTitleOffsets.y * scaleFactor;
        float scaledTop = originalTitleOffsets.z * scaleFactor;
        float scaledBottom = originalTitleOffsets.w * scaleFactor;
        
        title.offsetMin = new Vector2(scaledLeft, scaledBottom);
        title.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled title offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales the close button by adjusting its offsets
    /// </summary>
    private void ScaleCloseButton()
    {
        if (closeButton == null)
        {
            return;
        }
        
        float scaledLeft = originalCloseButtonOffsets.x * scaleFactor;
        float scaledRight = originalCloseButtonOffsets.y * scaleFactor;
        float scaledTop = originalCloseButtonOffsets.z * scaleFactor;
        float scaledBottom = originalCloseButtonOffsets.w * scaleFactor;
        
        closeButton.offsetMin = new Vector2(scaledLeft, scaledBottom);
        closeButton.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled close button offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales the upper button by adjusting its offsets
    /// </summary>
    private void ScaleUpperButton()
    {
        if (upperButton == null)
        {
            return;
        }
        
        float scaledLeft = originalUpperButtonOffsets.x * scaleFactor;
        float scaledRight = originalUpperButtonOffsets.y * scaleFactor;
        float scaledTop = originalUpperButtonOffsets.z * scaleFactor;
        float scaledBottom = originalUpperButtonOffsets.w * scaleFactor;
        
        upperButton.offsetMin = new Vector2(scaledLeft, scaledBottom);
        upperButton.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled upper button offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales panel 1 by adjusting its offsets
    /// </summary>
    private void ScalePanel1()
    {
        if (panel1 == null)
        {
            return;
        }
        
        float scaledLeft = originalPanel1Offsets.x * scaleFactor;
        float scaledRight = originalPanel1Offsets.y * scaleFactor;
        float scaledTop = originalPanel1Offsets.z * scaleFactor;
        float scaledBottom = originalPanel1Offsets.w * scaleFactor;
        
        panel1.offsetMin = new Vector2(scaledLeft, scaledBottom);
        panel1.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled panel 1 offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales panel 2 by adjusting its offsets
    /// </summary>
    private void ScalePanel2()
    {
        if (panel2 == null)
        {
            return;
        }
        
        float scaledLeft = originalPanel2Offsets.x * scaleFactor;
        float scaledRight = originalPanel2Offsets.y * scaleFactor;
        float scaledTop = originalPanel2Offsets.z * scaleFactor;
        float scaledBottom = originalPanel2Offsets.w * scaleFactor;
        
        panel2.offsetMin = new Vector2(scaledLeft, scaledBottom);
        panel2.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[AccountPanelScaler] Scaled panel 2 offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
