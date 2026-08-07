using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the gameplay screen composed against a fixed design resolution.
/// The root still fills the device, while direct gameplay children are remapped
/// into a centered 1080 x 1920 reference area so spacing and proportions match.
/// </summary>
public class GamePlayReferenceScaler : MonoBehaviour
{
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1080f, 1920f);
    [SerializeField] private bool keepFullScreenChildrenUnscaled = true;
    [SerializeField] private bool coverBackgroundChildren = true;

    private readonly List<ChildLayout> childLayouts = new List<ChildLayout>();
    private RectTransform root;
    private RectTransform canvasRoot;
    private Vector2 lastRootSize;
    public float CurrentScale { get; private set; } = 1f;

    private void Awake()
    {
        root = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRoot = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        // Auto-detect and sync reference resolution with parent CanvasScaler if present
        CanvasScaler canvasScaler = GetComponentInParent<CanvasScaler>();
        if (canvasScaler != null && canvasScaler.referenceResolution.x > 0 && canvasScaler.referenceResolution.y > 0)
        {
            Vector2 refRes = canvasScaler.referenceResolution;
            // Ensure portrait orientation (width < height) if screen is portrait
            if (refRes.x > refRes.y)
            {
                refRes = new Vector2(refRes.y, refRes.x);
            }
            referenceScreenSize = refRes;
        }

        CaptureOriginalLayouts();
    }

    private void OnEnable()
    {
        ApplyScaling();
    }

    private void LateUpdate()
    {
        if (root == null)
        {
            return;
        }

        Vector2 rootSize = root.rect.size;
        if (!Approximately(rootSize, lastRootSize))
        {
            ApplyScaling();
        }
    }

    private void CaptureOriginalLayouts()
    {
        childLayouts.Clear();

        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform child = root.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            childLayouts.Add(new ChildLayout(child));
        }
    }

    private void ApplyScaling()
    {
        if (root == null || referenceScreenSize.x <= 0f || referenceScreenSize.y <= 0f)
        {
            return;
        }

        Vector2 rootSize = root.rect.size;
        if (rootSize.x <= 0f || rootSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(rootSize.x / referenceScreenSize.x, rootSize.y / referenceScreenSize.y);
        CurrentScale = scale;
        Vector2 contentSize = referenceScreenSize * scale;
        Vector2 contentOffset = (rootSize - contentSize) * 0.5f;

        foreach (ChildLayout layout in childLayouts)
        {
            if (layout.Transform == null)
            {
                continue;
            }

            if (coverBackgroundChildren && layout.IsBackground)
            {
                CoverScreen(layout.Transform);
                continue;
            }

            if (keepFullScreenChildrenUnscaled && layout.IsFullScreen)
            {
                layout.Restore();
                continue;
            }

            RectTransform child = layout.Transform;
            child.anchorMin = ToScaledAnchor(layout.AnchorMin, contentOffset, contentSize, rootSize);
            child.anchorMax = ToScaledAnchor(layout.AnchorMax, contentOffset, contentSize, rootSize);
            child.anchoredPosition = layout.AnchoredPosition * scale;
            child.sizeDelta = layout.SizeDelta * scale;
            child.pivot = layout.Pivot;
        }

        lastRootSize = rootSize;
    }

    private void CoverScreen(RectTransform child)
    {
        RectTransform coverRoot = canvasRoot != null ? canvasRoot : root;
        Vector2 coverSize = coverRoot.rect.size;
        if (coverSize.x <= 0f || coverSize.y <= 0f)
        {
            return;
        }

        Vector3 coverCenterWorld = coverRoot.TransformPoint(coverRoot.rect.center);
        Vector3 coverCenterLocal = root.InverseTransformPoint(coverCenterWorld);
        CoverArea(child, coverSize, coverCenterLocal);
    }

    private static void CoverArea(RectTransform child, Vector2 coverSize, Vector2 coverCenter)
    {
        child.anchorMin = new Vector2(0.5f, 0.5f);
        child.anchorMax = new Vector2(0.5f, 0.5f);
        child.pivot = new Vector2(0.5f, 0.5f);
        child.anchoredPosition = coverCenter;

        Image image = child.GetComponent<Image>();
        Sprite sprite = image != null ? image.sprite : null;
        if (sprite == null || sprite.rect.height <= 0f)
        {
            child.sizeDelta = coverSize;
            return;
        }

        float spriteAspect = sprite.rect.width / sprite.rect.height;
        float rootAspect = coverSize.x / coverSize.y;

        child.sizeDelta = rootAspect > spriteAspect
            ? new Vector2(coverSize.x, coverSize.x / spriteAspect)
            : new Vector2(coverSize.y * spriteAspect, coverSize.y);
    }

    private static Vector2 ToScaledAnchor(Vector2 referenceAnchor, Vector2 contentOffset, Vector2 contentSize, Vector2 rootSize)
    {
        return new Vector2(
            (contentOffset.x + referenceAnchor.x * contentSize.x) / rootSize.x,
            (contentOffset.y + referenceAnchor.y * contentSize.y) / rootSize.y
        );
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) < 0.5f && Mathf.Abs(a.y - b.y) < 0.5f;
    }

    private sealed class ChildLayout
    {
        public readonly RectTransform Transform;
        public readonly Vector2 AnchorMin;
        public readonly Vector2 AnchorMax;
        public readonly Vector2 AnchoredPosition;
        public readonly Vector2 SizeDelta;
        public readonly Vector2 Pivot;
        public readonly bool IsFullScreen;
        public readonly bool IsBackground;

        public ChildLayout(RectTransform transform)
        {
            Transform = transform;
            AnchorMin = transform.anchorMin;
            AnchorMax = transform.anchorMax;
            AnchoredPosition = transform.anchoredPosition;
            SizeDelta = transform.sizeDelta;
            Pivot = transform.pivot;
            IsFullScreen = Approximately(AnchorMin, Vector2.zero)
                           && Approximately(AnchorMax, Vector2.one)
                           && Approximately(SizeDelta, Vector2.zero)
                           && Approximately(AnchoredPosition, Vector2.zero);
            IsBackground = transform.name == "Background";
        }

        public void Restore()
        {
            Transform.anchorMin = AnchorMin;
            Transform.anchorMax = AnchorMax;
            Transform.anchoredPosition = AnchoredPosition;
            Transform.sizeDelta = SizeDelta;
            Transform.pivot = Pivot;
        }
    }
}
