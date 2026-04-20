using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Automatically scales Horizontal Lobby UI elements based on screen size
/// Reference resolution horizontally: 3216x1440
/// Handles hybrid scaling: sizeDelta for non-anchored elements, offset scaling for anchored elements
/// </summary>
public class HorizontalLobbyScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(3216f, 1440f);

    [Header("Standalone Elements (Anchored)")]
    [Tooltip("Back Button")]
    [SerializeField] private RectTransform backButton;
    [Tooltip("Title Image")]
    [SerializeField] private RectTransform titleImage;

    [Header("Dot Buttons (Anchored)")]
    [Tooltip("Dot Buttons Parent")]
    [SerializeField] private RectTransform dotButtonsParent;
    [Tooltip("List of 12 Dot Buttons")]
    [SerializeField] private List<RectTransform> dotButtons = new List<RectTransform>();

    [Header("Overall Container (Anchored)")]
    [Tooltip("Overall Game Object")]
    [SerializeField] private RectTransform overallContainer;

    [Header("Center Part")]
    [Tooltip("Center Part Panel - Anchored")]
    [SerializeField] private RectTransform centerPart;
    [Tooltip("Center Lobby Row - NOT Anchored")]
    [SerializeField] private RectTransform centerLobbyRow;

    // Storage for original offsets (for anchored elements)
    private Dictionary<RectTransform, Vector4> originalOffsets = new Dictionary<RectTransform, Vector4>();
    
    // Storage for original sizes (for non-anchored elements)
    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();

    private float scaleFactor = 1f;

    private void Awake()
    {
        // Store offsets for anchored elements
        StoreOriginalOffsets(backButton, "Back Button");
        StoreOriginalOffsets(titleImage, "Title Image");
        StoreOriginalOffsets(dotButtonsParent, "Dot Buttons Parent");
        StoreOriginalOffsets(overallContainer, "Overall Container");
        StoreOriginalOffsets(centerPart, "Center Part");

        foreach (var btn in dotButtons)
        {
            StoreOriginalOffsets(btn, "Dot Button");
        }

        // Store sizes for non-anchored elements
        StoreOriginalSize(centerLobbyRow, "Center Lobby Row");
    }

    private void Start()
    {
        ApplyScaling();
    }

    private void StoreOriginalOffsets(RectTransform element, string name)
    {
        if (element != null)
        {
            float left = element.offsetMin.x;
            float bottom = element.offsetMin.y;
            float right = -element.offsetMax.x;
            float top = -element.offsetMax.y;
            originalOffsets[element] = new Vector4(left, right, top, bottom);
        }
    }

    private void StoreOriginalSize(RectTransform element, string name)
    {
        if (element != null)
        {
            originalSizes[element] = element.sizeDelta;
        }
    }

    private void ApplyScaling()
    {
        // Calculate scale factors
        float scaleFactorX = Screen.width / referenceScreenSize.x;
        float scaleFactorY = Screen.height / referenceScreenSize.y;
        
        // Smart scaling logic tailored for horizontal orientation
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

        Debug.Log($"[HorizontalLobbyScaler] Screen: {Screen.width}x{Screen.height}, Scale Factor: {scaleFactor:F3}");

        // Apply Offset Scaling (Anchored)
        ScaleByOffset(backButton);
        ScaleByOffset(titleImage);
        ScaleByOffset(dotButtonsParent);
        ScaleByOffset(overallContainer);
        ScaleByOffset(centerPart);

        foreach (var btn in dotButtons)
        {
            ScaleByOffset(btn);
        }

        // Apply SizeDelta Scaling (Not Anchored)
        ScaleBySizeDelta(centerLobbyRow);
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

    private void ScaleBySizeDelta(RectTransform element)
    {
        if (element == null || !originalSizes.ContainsKey(element)) return;

        Vector2 originalSize = originalSizes[element];
        element.sizeDelta = originalSize * scaleFactor;
    }
}
