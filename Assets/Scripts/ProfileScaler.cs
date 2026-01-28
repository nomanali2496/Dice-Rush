using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Automatically scales profile UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles parent containers with sizeDelta scaling and anchor-snapped children with offset scaling
/// </summary>
public class ProfileScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Lower Part (Not Anchored)")]
    [Tooltip("Lower part container - will be scaled by sizeDelta")]
    [SerializeField] private RectTransform lowerPart;
    [Tooltip("Background in lower part (same size as lower part)")]
    [SerializeField] private RectTransform lowerPartBackground;
    
    [Header("Upper Buttons (Not Anchored)")]
    [Tooltip("Upper buttons container - will be scaled by sizeDelta")]
    [SerializeField] private RectTransform upperButtons;
    [Tooltip("Child buttons inside upper buttons (anchor-snapped)")]
    [SerializeField] private List<RectTransform> upperButtonsChildren = new List<RectTransform>();
    
    [Header("Four Panels (Not Anchored)")]
    [Tooltip("Panel 1 container - will be scaled by sizeDelta")]
    [SerializeField] private RectTransform panel1;
    [Tooltip("Panel 2 container - will be scaled by sizeDelta")]
    [SerializeField] private RectTransform panel2;
    [Tooltip("Panel 3 container - will be scaled by sizeDelta")]
    [SerializeField] private RectTransform panel3;
    [Tooltip("Panel 4 container - will be scaled by sizeDelta")]
    [SerializeField] private RectTransform panel4;
    
    [Tooltip("Children inside panels (anchor-snapped)")]
    [SerializeField] private List<RectTransform> panel1Children = new List<RectTransform>();
    [SerializeField] private List<RectTransform> panel2Children = new List<RectTransform>();
    [SerializeField] private List<RectTransform> panel3Children = new List<RectTransform>();
    [SerializeField] private List<RectTransform> panel4Children = new List<RectTransform>();
    
    [Header("Text Elements (Auto-Size Enabled - Container Scales)")]
    [Header("Panel 1 Texts")]
    [Tooltip("Name text (font 34, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel1NameText;
    [Tooltip("Email text (font 34, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel1EmailText;
    [Tooltip("Name input field text (font 50, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel1NameInputText;
    [Tooltip("Email displayed text (font 35, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel1EmailDisplayText;
    
    [Header("Panel 2 Texts")]
    [Tooltip("No item text (font 37.6, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel2NoItemText;
    
    [Header("Panel 3 Texts")]
    [Tooltip("Watch video text (font 33.7, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel3WatchVideoText;
    
    [Header("Panel 4 Texts")]
    [Tooltip("Email text (font 47, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel4EmailText;
    [Tooltip("Email display text (font 55, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel4EmailDisplayText;
    [Tooltip("Tell us text (font 47, auto-size enabled)")]
    [SerializeField] private TextMeshProUGUI panel4TellUsText;
    
    [Header("Non-Auto-Size Texts (Font Size Scales)")]
    [Tooltip("Upper button texts (4 buttons, font size 32, auto-size DISABLED)")]
    [SerializeField] private List<TMP_Text> upperButtonTexts = new List<TMP_Text>();
    
    [Tooltip("Tell input field text (font 48.2, auto-size DISABLED)")]
    [SerializeField] private TMP_InputField panel4TellInputField;
    
    // Original sizes for parent containers (not anchored)
    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();
    
    // Original offsets for anchor-snapped children
    // Stores: (left, right, top, bottom)
    private Dictionary<RectTransform, Vector4> originalOffsets = new Dictionary<RectTransform, Vector4>();
    
    // Original font sizes for non-auto-size texts
    private Dictionary<TMP_Text, float> originalFontSizes = new Dictionary<TMP_Text, float>();
    
    private float scaleFactor = 1f;
    
    private void Awake()
    {
        // Store original sizes for parent containers
        StoreOriginalSize(lowerPart, "Lower Part");
        StoreOriginalSize(lowerPartBackground, "Lower Part Background");
        StoreOriginalSize(upperButtons, "Upper Buttons");
        StoreOriginalSize(panel1, "Panel 1");
        StoreOriginalSize(panel2, "Panel 2");
        StoreOriginalSize(panel3, "Panel 3");
        StoreOriginalSize(panel4, "Panel 4");
        
        // Store original offsets for anchor-snapped children
        foreach (var child in upperButtonsChildren)
        {
            StoreOriginalOffsets(child, "Upper Buttons Child");
        }
        
        foreach (var child in panel1Children)
        {
            StoreOriginalOffsets(child, "Panel 1 Child");
        }
        
        foreach (var child in panel2Children)
        {
            StoreOriginalOffsets(child, "Panel 2 Child");
        }
        
        foreach (var child in panel3Children)
        {
            StoreOriginalOffsets(child, "Panel 3 Child");
        }
        
        foreach (var child in panel4Children)
        {
            StoreOriginalOffsets(child, "Panel 4 Child");
        }
        
        // Store original font sizes for non-auto-size texts
        // Upper button texts
        foreach (var text in upperButtonTexts)
        {
            if (text != null)
            {
                originalFontSizes[text] = text.fontSize;
            }
        }
        
        // Panel 4 Tell Input Field
        if (panel4TellInputField != null)
        {
            if (panel4TellInputField.textComponent != null)
            {
                originalFontSizes[panel4TellInputField.textComponent] = panel4TellInputField.textComponent.fontSize;
            }
            if (panel4TellInputField.placeholder is TMP_Text placeholder)
            {
                originalFontSizes[placeholder] = placeholder.fontSize;
            }
        }
    }
    
    private void Start()
    {
        ApplyScaling();
    }
    
    /// <summary>
    /// Stores the original size of a non-anchored element
    /// </summary>
    private void StoreOriginalSize(RectTransform element, string elementName)
    {
        if (element != null)
        {
            originalSizes[element] = element.sizeDelta;
            Debug.Log($"[ProfileScaler] Stored original size for {elementName}: {element.sizeDelta}");
        }
    }
    
    /// <summary>
    /// Stores the original offsets of an anchor-snapped element
    /// </summary>
    private void StoreOriginalOffsets(RectTransform element, string elementName)
    {
        if (element != null)
        {
            float left = element.offsetMin.x;
            float bottom = element.offsetMin.y;
            float right = -element.offsetMax.x;
            float top = -element.offsetMax.y;
            
            originalOffsets[element] = new Vector4(left, right, top, bottom);
            Debug.Log($"[ProfileScaler] Stored original offsets for {elementName}: L={left:F2}, R={right:F2}, T={top:F2}, B={bottom:F2}");
        }
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
        
        Debug.Log($"[ProfileScaler] ====== PROFILE SCALING ======");
        Debug.Log($"[ProfileScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[ProfileScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[ProfileScaler] Uniform Scale Factor: {scaleFactor:F3}");
        
        // Scale parent containers (sizeDelta scaling)
        ScaleElementBySize(lowerPart, "Lower Part");
        ScaleElementBySize(lowerPartBackground, "Lower Part Background");
        ScaleElementBySize(upperButtons, "Upper Buttons");
        ScaleElementBySize(panel1, "Panel 1");
        ScaleElementBySize(panel2, "Panel 2");
        ScaleElementBySize(panel3, "Panel 3");
        ScaleElementBySize(panel4, "Panel 4");
        
        // Scale anchor-snapped children (offset scaling)
        foreach (var child in upperButtonsChildren)
        {
            ScaleElementByOffset(child, "Upper Buttons Child");
        }
        
        foreach (var child in panel1Children)
        {
            ScaleElementByOffset(child, "Panel 1 Child");
        }
        
        foreach (var child in panel2Children)
        {
            ScaleElementByOffset(child, "Panel 2 Child");
        }
        
        foreach (var child in panel3Children)
        {
            ScaleElementByOffset(child, "Panel 3 Child");
        }
        
        foreach (var child in panel4Children)
        {
            ScaleElementByOffset(child, "Panel 4 Child");
        }
        
        // Scale text font sizes for non-auto-size texts
        ScaleNonAutoSizeTexts();
        
        Debug.Log($"[ProfileScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Scales a non-anchored element by adjusting its sizeDelta
    /// </summary>
    private void ScaleElementBySize(RectTransform element, string elementName)
    {
        if (element == null || !originalSizes.ContainsKey(element))
        {
            return;
        }
        
        Vector2 originalSize = originalSizes[element];
        Vector2 scaledSize = new Vector2(
            originalSize.x * scaleFactor,
            originalSize.y * scaleFactor
        );
        
        element.sizeDelta = scaledSize;
        Debug.Log($"[ProfileScaler] Scaled {elementName} size: {originalSize} -> {scaledSize}");
    }
    
    /// <summary>
    /// Scales an anchor-snapped element by adjusting its offsets
    /// </summary>
    private void ScaleElementByOffset(RectTransform element, string elementName)
    {
        if (element == null || !originalOffsets.ContainsKey(element))
        {
            return;
        }
        
        Vector4 originalOffset = originalOffsets[element];
        
        // Scale the offsets
        float scaledLeft = originalOffset.x * scaleFactor;
        float scaledRight = originalOffset.y * scaleFactor;
        float scaledTop = originalOffset.z * scaleFactor;
        float scaledBottom = originalOffset.w * scaleFactor;
        
        // Apply scaled offsets
        element.offsetMin = new Vector2(scaledLeft, scaledBottom);
        element.offsetMax = new Vector2(-scaledRight, -scaledTop);
        
        Debug.Log($"[ProfileScaler] Scaled {elementName} offsets: L={scaledLeft:F2}, R={scaledRight:F2}, T={scaledTop:F2}, B={scaledBottom:F2}");
    }
    
    /// <summary>
    /// Scales font sizes for texts that don't have auto-size enabled
    /// </summary>
    private void ScaleNonAutoSizeTexts()
    {
        // Scale upper button texts
        foreach (var text in upperButtonTexts)
        {
            if (text != null && originalFontSizes.ContainsKey(text))
            {
                float originalSize = originalFontSizes[text];
                float scaledSize = originalSize * scaleFactor;
                text.fontSize = scaledSize;
                Debug.Log($"[ProfileScaler] Scaled upper button text font: {originalSize:F1} -> {scaledSize:F1}");
            }
        }
        
        // Scale Panel 4 Tell Input Field (text and placeholder)
        if (panel4TellInputField != null)
        {
            if (panel4TellInputField.textComponent != null && originalFontSizes.ContainsKey(panel4TellInputField.textComponent))
            {
                float originalSize = originalFontSizes[panel4TellInputField.textComponent];
                float scaledSize = originalSize * scaleFactor;
                panel4TellInputField.textComponent.fontSize = scaledSize;
                Debug.Log($"[ProfileScaler] Scaled Panel 4 Tell Input text font: {originalSize:F1} -> {scaledSize:F1}");
            }
            
            if (panel4TellInputField.placeholder is TMP_Text placeholder && originalFontSizes.ContainsKey(placeholder))
            {
                float originalSize = originalFontSizes[placeholder];
                float scaledSize = originalSize * scaleFactor;
                placeholder.fontSize = scaledSize;
                Debug.Log($"[ProfileScaler] Scaled Panel 4 Tell Input placeholder font: {originalSize:F1} -> {scaledSize:F1}");
            }
        }
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
