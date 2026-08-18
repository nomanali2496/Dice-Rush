using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Automatically scales the "Choose Your Mode" panel, grid layout group, and title/button elements based on screen resolution.
/// Reference resolution: 1440x3216
/// </summary>
public class ChooseYourModeScaler : MonoBehaviour
{
    [Header("Grid Layout Reference")]
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Anchored UI Elements")]
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private RectTransform backButtonRect;

    private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    private Vector2 originalCellSize;
    private Vector2 originalSpacing;
    private Dictionary<RectTransform, Vector4> originalOffsets = new Dictionary<RectTransform, Vector4>();
    private float scaleFactor = 1f;

    private void Awake()
    {
        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = GetComponentInChildren<GridLayoutGroup>();
        }

        if (gridLayoutGroup != null)
        {
            originalCellSize = gridLayoutGroup.cellSize;
            originalSpacing = gridLayoutGroup.spacing;
        }

        StoreOriginalOffsets(titleRect);
        StoreOriginalOffsets(backButtonRect);
    }

    private void Start()
    {
        ApplyScaling();
    }

    private void ApplyScaling()
    {
        float scaleFactorX = Screen.width / referenceScreenSize.x;
        float scaleFactorY = Screen.height / referenceScreenSize.y;

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

        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.cellSize = originalCellSize * scaleFactor;
            gridLayoutGroup.spacing = originalSpacing * scaleFactor;
        }

        ScaleByOffset(titleRect);
        ScaleByOffset(backButtonRect);

        Debug.Log($"[ChooseYourModeScaler] Applied scaling. Scale Factor: {scaleFactor:F3}");
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
        element.offsetMin = new Vector2(offsets.x * scaleFactor, offsets.w * scaleFactor);
        element.offsetMax = new Vector2(-offsets.y * scaleFactor, -offsets.z * scaleFactor);
    }

    public void RecalculateScaling()
    {
        ApplyScaling();
    }
}
