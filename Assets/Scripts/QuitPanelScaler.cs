using UnityEngine;

/// <summary>
/// Automatically scales quit panel UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles quit panel container and its buttons (all anchor-snapped)
/// </summary>
public class QuitPanelScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Quit Panel (Anchor-Snapped)")]
    [Tooltip("Quit panel container - will be scaled by offsets (reference: 1176 x 820)")]
    [SerializeField] private RectTransform quitPanel;
    
    [Header("Buttons (Anchor-Snapped)")]
    [Tooltip("Button 1 in quit panel - will be scaled by offsets (reference: 385 x 179)")]
    [SerializeField] private RectTransform button1;
    [Tooltip("Button 2 in quit panel - will be scaled by offsets (reference: 385 x 179)")]
    [SerializeField] private RectTransform button2;
    
    // Original offsets for anchor-snapped elements
    // Stores: (left, right, top, bottom)
    private Vector4 originalPanelOffsets;
    private Vector4 originalButton1Offsets;
    private Vector4 originalButton2Offsets;
    
    private float scaleFactor = 1f;
    
    private void Awake()
    {
        // Store original offsets for quit panel
        if (quitPanel != null)
        {
            StoreOriginalOffsets(quitPanel, ref originalPanelOffsets, "Quit Panel");
        }
        
        // Store original offsets for button 1
        if (button1 != null)
        {
            StoreOriginalOffsets(button1, ref originalButton1Offsets, "Button 1");
        }
        
        // Store original offsets for button 2
        if (button2 != null)
        {
            StoreOriginalOffsets(button2, ref originalButton2Offsets, "Button 2");
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
        
        // Scale quit panel
        ScaleQuitPanel();
        
        // Scale buttons
        ScaleButton1();
        ScaleButton2();
        
        Debug.Log($"[QuitPanelScaler] Applied scaling (Scale: {scaleFactor:F3}) for Screen: {Screen.width}x{Screen.height}.");
    }
    
    /// <summary>
    /// Scales the quit panel by adjusting its offsets
    /// </summary>
    private void ScaleQuitPanel()
    {
        if (quitPanel == null)
        {
            return;
        }
        
        // Scale the offsets
        float scaledLeft = originalPanelOffsets.x * scaleFactor;
        float scaledRight = originalPanelOffsets.y * scaleFactor;
        float scaledTop = originalPanelOffsets.z * scaleFactor;
        float scaledBottom = originalPanelOffsets.w * scaleFactor;
        
        // Apply scaled offsets
        quitPanel.offsetMin = new Vector2(scaledLeft, scaledBottom);
        quitPanel.offsetMax = new Vector2(-scaledRight, -scaledTop);
    }
    
    /// <summary>
    /// Scales button 1 by adjusting its offsets
    /// </summary>
    private void ScaleButton1()
    {
        if (button1 == null)
        {
            return;
        }
        
        // Scale the offsets
        float scaledLeft = originalButton1Offsets.x * scaleFactor;
        float scaledRight = originalButton1Offsets.y * scaleFactor;
        float scaledTop = originalButton1Offsets.z * scaleFactor;
        float scaledBottom = originalButton1Offsets.w * scaleFactor;
        
        // Apply scaled offsets
        button1.offsetMin = new Vector2(scaledLeft, scaledBottom);
        button1.offsetMax = new Vector2(-scaledRight, -scaledTop);
    }
    
    /// <summary>
    /// Scales button 2 by adjusting its offsets
    /// </summary>
    private void ScaleButton2()
    {
        if (button2 == null)
        {
            return;
        }
        
        // Scale the offsets
        float scaledLeft = originalButton2Offsets.x * scaleFactor;
        float scaledRight = originalButton2Offsets.y * scaleFactor;
        float scaledTop = originalButton2Offsets.z * scaleFactor;
        float scaledBottom = originalButton2Offsets.w * scaleFactor;
        
        // Apply scaled offsets
        button2.offsetMin = new Vector2(scaledLeft, scaledBottom);
        button2.offsetMax = new Vector2(-scaledRight, -scaledTop);
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
