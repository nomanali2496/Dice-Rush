using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically scales Grid Layout Group cell size and spacing based on screen resolution.
/// Attach this script directly to AvatarManager and AvatarFrameManager.
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class GridLayoutScaler : MonoBehaviour
{
    [Header("Reference Resolution")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f); // Update this to match your Canvas Scaler reference resolution!

    private GridLayoutGroup gridLayoutGroup;
    private Vector2 originalCellSize;
    private Vector2 originalSpacing;
    private RectOffset originalPadding;

    private void Awake()
    {
        gridLayoutGroup = GetComponent<GridLayoutGroup>();

        if (gridLayoutGroup != null)
        {
            originalCellSize = gridLayoutGroup.cellSize;
            originalSpacing = gridLayoutGroup.spacing;
            
            // Store original padding
            originalPadding = new RectOffset(
                gridLayoutGroup.padding.left,
                gridLayoutGroup.padding.right,
                gridLayoutGroup.padding.top,
                gridLayoutGroup.padding.bottom
            );
        }
    }

    private void Start()
    {
        ApplyScaling();
    }

    private void ApplyScaling()
    {
        if (gridLayoutGroup == null) return;

        // Calculate scale factor based on screen width vs reference width
        float scaleFactor = (float)Screen.width / referenceResolution.x;

        // Scale cell size
        gridLayoutGroup.cellSize = originalCellSize * scaleFactor;

        // Scale spacing
        gridLayoutGroup.spacing = originalSpacing * scaleFactor;

        // Scale padding
        gridLayoutGroup.padding = new RectOffset(
            Mathf.RoundToInt(originalPadding.left * scaleFactor),
            Mathf.RoundToInt(originalPadding.right * scaleFactor),
            Mathf.RoundToInt(originalPadding.top * scaleFactor),
            Mathf.RoundToInt(originalPadding.bottom * scaleFactor)
        );
    }
}
