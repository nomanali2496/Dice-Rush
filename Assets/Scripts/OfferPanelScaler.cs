using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Automatically scales offer panel UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles offer panels, close buttons, and price buttons
/// </summary>
public class OfferPanelScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Offer Panels (Not Anchored)")]
    [Tooltip("All offer panel containers - will be scaled by sizeDelta (reference: 1296.1 x 1573.1)")]
    [SerializeField] private List<RectTransform> offerPanels = new List<RectTransform>();
    
    [Header("Close Buttons (Not Anchored)")]
    [Tooltip("Close buttons in offer panels - will be scaled by sizeDelta (reference: 258.4 x 258.6)")]
    [SerializeField] private List<RectTransform> closeButtons = new List<RectTransform>();
    
    [Header("Price Buttons (Not Anchored)")]
    [Tooltip("Price buttons in offer panels - will be scaled by sizeDelta (reference: 498.4 x 179.3)")]
    [SerializeField] private List<RectTransform> priceButtons = new List<RectTransform>();
    
    // Original sizes for non-anchored elements
    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();
    
    private float scaleFactor = 1f;
    
    private void Awake()
    {
        // Store original sizes for offer panels
        foreach (var panel in offerPanels)
        {
            if (panel != null)
            {
                originalSizes[panel] = panel.sizeDelta;
                Debug.Log($"[OfferPanelScaler] Stored original size for offer panel: {panel.sizeDelta}");
            }
        }
        
        // Store original sizes for close buttons
        foreach (var button in closeButtons)
        {
            if (button != null)
            {
                originalSizes[button] = button.sizeDelta;
                Debug.Log($"[OfferPanelScaler] Stored original size for close button: {button.sizeDelta}");
            }
        }
        
        // Store original sizes for price buttons
        foreach (var button in priceButtons)
        {
            if (button != null)
            {
                originalSizes[button] = button.sizeDelta;
                Debug.Log($"[OfferPanelScaler] Stored original size for price button: {button.sizeDelta}");
            }
        }
    }
    
    private void Start()
    {
        ApplyScaling();
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
        
        Debug.Log($"[OfferPanelScaler] ====== OFFER PANEL SCALING ======");
        Debug.Log($"[OfferPanelScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[OfferPanelScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[OfferPanelScaler] Uniform Scale Factor: {scaleFactor:F3}");
        
        // Scale all offer panels
        foreach (var panel in offerPanels)
        {
            ScaleElement(panel, "Offer Panel");
        }
        
        // Scale all close buttons
        foreach (var button in closeButtons)
        {
            ScaleElement(button, "Close Button");
        }
        
        // Scale all price buttons
        foreach (var button in priceButtons)
        {
            ScaleElement(button, "Price Button");
        }
        
        Debug.Log($"[OfferPanelScaler] Scaling applied successfully!");
    }
    
    /// <summary>
    /// Scales a non-anchored element by adjusting its sizeDelta
    /// </summary>
    private void ScaleElement(RectTransform element, string elementType)
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
        Debug.Log($"[OfferPanelScaler] Scaled {elementType}: {originalSize} -> {scaledSize}");
    }
    
    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
