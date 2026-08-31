using UnityEngine;

public class UIPanelScaler : MonoBehaviour
{
    [Header("Reference Settings")]
    [Tooltip("The screen resolution your UI was designed for (Matches your Canvas Scaler)")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("UI Elements to Scale")]
    public RectTransform[] elementsToScale;

    [Header("Scaling Settings")]
    [Tooltip("If true, it will only scale down on smaller screens, not scale up on larger ones.")]
    public bool onlyScaleDown = true;
    
    [Range(0f, 1f)]
    [Tooltip("0 = Match Width, 1 = Match Height. Usually 1 is better for portrait games.")]
    public float matchWidthOrHeight = 1f;

    private void Start()
    {
        AdjustScale();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Update in editor for testing in different resolutions
        if (!Application.isPlaying)
        {
            AdjustScale();
        }
    }
#endif

    private void AdjustScale()
    {
        if (elementsToScale == null || elementsToScale.Length == 0) return;

        float currentScreenWidth = Screen.width;
        float currentScreenHeight = Screen.height;

        // Calculate ratios
        float widthRatio = currentScreenWidth / referenceResolution.x;
        float heightRatio = currentScreenHeight / referenceResolution.y;

        // Calculate the final scale factor based on matchWidthOrHeight
        float scaleFactor = Mathf.Lerp(widthRatio, heightRatio, matchWidthOrHeight);

        if (onlyScaleDown && scaleFactor > 1f)
        {
            scaleFactor = 1f;
        }

        // Apply scale to all elements
        foreach (RectTransform rect in elementsToScale)
        {
            if (rect != null)
            {
                rect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
            }
        }
    }
}
