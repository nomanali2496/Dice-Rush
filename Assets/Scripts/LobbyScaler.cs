using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Automatically scales Lobby UI elements based on screen size
/// Reference resolution: 1440x3216
/// Handles hybrid scaling: sizeDelta for non-anchored elements, offset scaling for anchored elements
/// </summary>
public class LobbyScaler : MonoBehaviour
{
    [Header("Reference Screen Size")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);

    [Header("Standalone Elements (Anchored)")]
    [Tooltip("Back Button (reference: 102 x 103)")]
    [SerializeField] private RectTransform backButton;
    [Tooltip("Title Image (reference: 1452 x 342)")]
    [SerializeField] private RectTransform titleImage;

    [Header("Dot Buttons (Anchored)")]
    [Tooltip("Dot Buttons Parent (reference: 87 x 1167)")]
    [SerializeField] private RectTransform dotButtonsParent;
    [Tooltip("List of 12 Dot Buttons (reference: 45 x 45 each)")]
    [SerializeField] private List<RectTransform> dotButtons = new List<RectTransform>();

    [Header("Overall Container (Anchored)")]
    [Tooltip("Overall Game Object (reference: 1264 x 2039)")]
    [SerializeField] private RectTransform overallContainer;

    [Header("Upper Part")]
    [Tooltip("Upper Part Button - Anchored (reference: 994 x 1030)")]
    [SerializeField] private RectTransform upperPartButton;
    [Tooltip("Upper Lobby Row - NOT Anchored (reference: 994 x 1030)")]
    [SerializeField] private RectTransform upperLobbyRow;

    [Header("Lower Part")]
    [Tooltip("Lower Part Button - Anchored (reference: 994 x 1030)")]
    [SerializeField] private RectTransform lowerPartButton;
    [Tooltip("Lower Lobby Row - NOT Anchored (reference: 994 x 1030)")]
    [SerializeField] private RectTransform lowerLobbyRow;

    [Header("Middle Part")]
    [Tooltip("Middle Part Panel - Anchored (reference: 1230 x 1384)")]
    [SerializeField] private RectTransform middlePart;
    [Tooltip("Middle Lobby Row - NOT Anchored (reference: 1230 x 1384)")]
    [SerializeField] private RectTransform middleLobbyRow;

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
        StoreOriginalOffsets(upperPartButton, "Upper Part Button");
        StoreOriginalOffsets(lowerPartButton, "Lower Part Button");
        StoreOriginalOffsets(middlePart, "Middle Part");

        foreach (var btn in dotButtons)
        {
            StoreOriginalOffsets(btn, "Dot Button");
        }

        // Store sizes for non-anchored elements
        StoreOriginalSize(upperLobbyRow, "Upper Lobby Row");
        StoreOriginalSize(lowerLobbyRow, "Lower Lobby Row");
        StoreOriginalSize(middleLobbyRow, "Middle Lobby Row");
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
        
        // Smart scaling logic
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

        Debug.Log($"[LobbyScaler] Screen: {Screen.width}x{Screen.height}, Scale Factor: {scaleFactor:F3}");

        // Apply Offset Scaling (Anchored)
        ScaleByOffset(backButton);
        ScaleByOffset(titleImage);
        ScaleByOffset(dotButtonsParent);
        ScaleByOffset(overallContainer);
        ScaleByOffset(upperPartButton);
        ScaleByOffset(lowerPartButton);
        ScaleByOffset(middlePart);

        foreach (var btn in dotButtons)
        {
            ScaleByOffset(btn);
        }

        // Apply SizeDelta Scaling (Not Anchored)
        ScaleBySizeDelta(upperLobbyRow);
        ScaleBySizeDelta(lowerLobbyRow);
        ScaleBySizeDelta(middleLobbyRow);
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
