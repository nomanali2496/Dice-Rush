using UnityEngine;

/// <summary>
/// Scales the GameObject's localScale and adjusts its anchored Y position 
/// dynamically based on the parent RectTransform's dimensions (Canvas space)
/// relative to a reference resolution of 1080 x 1920.
/// This leaves the RectTransform's width, height, anchors, and pivot untouched.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class UIAspectRatioEnforcer : MonoBehaviour
{
    [Tooltip("The reference screen width where scale is 1 (e.g. 1080).")]
    [SerializeField] private float referenceScreenWidth = 1080f;

    [Tooltip("The reference screen height where scale is 1 (e.g. 1920).")]
    [SerializeField] private float referenceScreenHeight = 1920f;

    [Tooltip("The design Y position (Pos Y) at the reference screen resolution (e.g. 150).")]
    [SerializeField] private float referencePosY = 150f;

    [Tooltip("If true, adjusts the anchored position's Y coordinate dynamically using referencePosY scaled. If false, anchoredPosition is left untouched.")]
    [SerializeField] private bool useReferencePosY = true;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        UpdateLayout();
    }

    private void Start()
    {
        UpdateLayout();
    }

    private void Update()
    {
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (parentRectTransform == null)
        {
            parentRectTransform = transform.parent as RectTransform;
        }

        if (parentRectTransform == null) return;
        if (referenceScreenWidth <= 0f || referenceScreenHeight <= 0f) return;

        // Get parent dimensions in Canvas/UI space (not raw pixel space)
        Rect parentRect = parentRectTransform.rect;
        float parentWidth = parentRect.width;
        float parentHeight = parentRect.height;

        if (parentWidth <= 0f || parentHeight <= 0f) return;

        // Calculate scale factors relative to parent dimensions in Canvas space
        float scaleX = parentWidth / referenceScreenWidth;
        float scaleY = parentHeight / referenceScreenHeight;

        // Use the minimum scale factor to ensure it fits the screen bounds
        float scaleFactor = Mathf.Min(scaleX, scaleY);

        // Apply scale factor to localScale
        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        // Scale the Y position proportionally if enabled
        if (useReferencePosY)
        {
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = 0f; // Kept at middle center X = 0
            pos.y = referencePosY * scaleFactor;
            rectTransform.anchoredPosition = pos;
        }
    }
}
