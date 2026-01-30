using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Automatically scales Offer Carousel UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles hybrid scaling: sizeDelta for panels, offset scaling for anchored elements
/// </summary>
public class OfferCarouselScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);

    [Header("Left/Right Parts (Anchored)")]
    [Tooltip("Left Part (reference: 942 x 1146)")]
    [SerializeField] private RectTransform leftPart;
    [Tooltip("Right Part (reference: 942 x 1146)")]
    [SerializeField] private RectTransform rightPart;

    [Header("Left/Right Buttons (Anchored)")]
    [Tooltip("Left Button (reference: 191.3505 x 1147.254)")]
    [SerializeField] private RectTransform leftButton;
    [Tooltip("Right Button (reference: 191.3505 x 1147.254)")]
    [SerializeField] private RectTransform rightButton;

    [Header("Middle Part (Anchored)")]
    [Tooltip("Middle Part (reference: 1138 x 1378)")]
    [SerializeField] private RectTransform middlePart;

    [Header("Back Button (Anchored)")]
    [Tooltip("Back Button (reference: 103 x 103)")]
    [SerializeField] private RectTransform backButton;

    [Header("Dot Buttons (Anchored)")]
    [Tooltip("Dot Buttons Parent (reference: 680 x 100)")]
    [SerializeField] private RectTransform dotButtonsParent;
    [Tooltip("List of 7 Dot Buttons (reference: 85 x 85 each)")]
    [SerializeField] private List<RectTransform> dotButtons = new List<RectTransform>();

    [Header("Offer Panels (Not Anchored)")]
    [Tooltip("List of 7 Offer Panels (reference: 963.7 x 1235 each)")]
    [SerializeField] private List<RectTransform> offerPanels = new List<RectTransform>();

    // Storage for original offsets (for anchored elements)
    private Dictionary<RectTransform, Vector4> originalOffsets = new Dictionary<RectTransform, Vector4>();
    
    // Storage for original sizes (for non-anchored elements)
    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();

    private float scaleFactor = 1f;

    private void Awake()
    {
        // Store offsets for anchored elements
        StoreOriginalOffsets(leftPart, "Left Part");
        StoreOriginalOffsets(rightPart, "Right Part");
        StoreOriginalOffsets(leftButton, "Left Button");
        StoreOriginalOffsets(rightButton, "Right Button");
        StoreOriginalOffsets(middlePart, "Middle Part");
        StoreOriginalOffsets(backButton, "Back Button");
        StoreOriginalOffsets(dotButtonsParent, "Dot Buttons Parent");

        // Store offsets for all dot buttons
        foreach (var btn in dotButtons)
        {
            StoreOriginalOffsets(btn, "Dot Button");
        }

        // Store sizes for non-anchored offer panels
        foreach (var panel in offerPanels)
        {
            StoreOriginalSize(panel, "Offer Panel");
        }
    }

    private void Start()
    {
        ApplyScaling();
    }

    /// <summary>
    /// Stores the original offsets of an anchored element
    /// </summary>
    private void StoreOriginalOffsets(RectTransform element, string elementName)
    {
        if (element == null)
        {
            Debug.LogWarning($"[OfferCarouselScaler] {elementName} is not assigned!");
            return;
        }

        float left = element.offsetMin.x;
        float bottom = element.offsetMin.y;
        float right = -element.offsetMax.x;
        float top = -element.offsetMax.y;

        originalOffsets[element] = new Vector4(left, right, top, bottom);
        Debug.Log($"[OfferCarouselScaler] Stored offsets for {elementName}: L={left:F2}, R={right:F2}, T={top:F2}, B={bottom:F2}");
    }

    /// <summary>
    /// Stores the original size (sizeDelta) of a non-anchored element
    /// </summary>
    private void StoreOriginalSize(RectTransform element, string elementName)
    {
        if (element == null)
        {
            Debug.LogWarning($"[OfferCarouselScaler] {elementName} is not assigned!");
            return;
        }

        originalSizes[element] = element.sizeDelta;
        Debug.Log($"[OfferCarouselScaler] Stored size for {elementName}: {element.sizeDelta.x:F2} x {element.sizeDelta.y:F2}");
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

        Debug.Log($"[OfferCarouselScaler] ====== OFFER CAROUSEL SCALING ======");
        Debug.Log($"[OfferCarouselScaler] Screen: {Screen.width}x{Screen.height} | Reference: {referenceScreenSize.x}x{referenceScreenSize.y}");
        Debug.Log($"[OfferCarouselScaler] Scale Factors: X={scaleFactorX:F3}, Y={scaleFactorY:F3}");
        Debug.Log($"[OfferCarouselScaler] Uniform Scale Factor: {scaleFactor:F3}");

        // Scale anchored elements (offset scaling)
        ScaleByOffset(leftPart);
        ScaleByOffset(rightPart);
        ScaleByOffset(leftButton);
        ScaleByOffset(rightButton);
        ScaleByOffset(middlePart);
        ScaleByOffset(backButton);
        ScaleByOffset(dotButtonsParent);

        foreach (var btn in dotButtons)
        {
            ScaleByOffset(btn);
        }

        // Scale non-anchored elements (sizeDelta scaling)
        foreach (var panel in offerPanels)
        {
            ScaleBySizeDelta(panel);
        }

        Debug.Log($"[OfferCarouselScaler] Scaling applied successfully!");
    }

    /// <summary>
    /// Scales an anchored element by adjusting its offsets
    /// </summary>
    private void ScaleByOffset(RectTransform element)
    {
        if (element == null || !originalOffsets.ContainsKey(element))
        {
            return;
        }

        Vector4 offsets = originalOffsets[element];
        float scaledLeft = offsets.x * scaleFactor;
        float scaledRight = offsets.y * scaleFactor;
        float scaledTop = offsets.z * scaleFactor;
        float scaledBottom = offsets.w * scaleFactor;

        element.offsetMin = new Vector2(scaledLeft, scaledBottom);
        element.offsetMax = new Vector2(-scaledRight, -scaledTop);
    }

    /// <summary>
    /// Scales a non-anchored element by adjusting its sizeDelta
    /// </summary>
    private void ScaleBySizeDelta(RectTransform element)
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
    }

    /// <summary>
    /// Recalculates and reapplies scaling (useful if resolution changes at runtime)
    /// </summary>
    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
