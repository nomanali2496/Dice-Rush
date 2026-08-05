using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Manages Championship UI button interactions with scale animations.
/// When one button is clicked, it scales up while the other scales down.
/// </summary>
public class ChampionshipUI : MonoBehaviour
{
    [Header("Buttons")]
    [Tooltip("The first button, selected by default.")]
    [SerializeField] private Button button1;
    
    [Tooltip("The second button.")]
    [SerializeField] private Button button2;

    [Header("Scaling Settings (Reference 1440x3216)")]
    [SerializeField] private Vector2 referenceScreenSize = new Vector2(1440f, 3216f);
    
    [Header("Animation Settings")]
    [Tooltip("Scale multiplier when a button is selected.")]
    [SerializeField] private float selectedScale = 1.15f;
    
    [Tooltip("Scale multiplier when a button is unselected.")]
    [SerializeField] private float unselectedScale = 0.85f;
    
    [Tooltip("Duration of the scale animation in seconds.")]
    [SerializeField] private float animationDuration = 0.3f;
    
    [Tooltip("Easing function for the scale animation.")]
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    // Original offsets for anchor-snapped buttons: (left, right, top, bottom)
    private Vector4 originalButton1Offsets;
    private Vector4 originalButton2Offsets;
    private float globalScaleFactor = 1f;

    private void Awake()
    {
        // Store original offsets for anchor-snapped scaling
        if (button1 != null) StoreOriginalOffsets(button1.GetComponent<RectTransform>(), ref originalButton1Offsets);
        if (button2 != null) StoreOriginalOffsets(button2.GetComponent<RectTransform>(), ref originalButton2Offsets);
        
        CalculateScaleFactor();
    }

    private void Start()
    {
        if (button1 == null || button2 == null)
        {
            Debug.LogError("[ChampionshipUI] Please assign both Button 1 and Button 2 in the inspector.");
            return;
        }

        // Apply initial physical scaling (offsets)
        ApplyPhysicalScaling();

        // Add click listeners to buttons
        button1.onClick.AddListener(() => SelectButton(1));
        button2.onClick.AddListener(() => SelectButton(2));

        // Set default selection (Button 1 selected)
        ApplySelectionState(button1, button2, false);
    }

    private void StoreOriginalOffsets(RectTransform rect, ref Vector4 offsets)
    {
        if (rect == null) return;
        // offsets = (left, right, top, bottom)
        offsets = new Vector4(rect.offsetMin.x, -rect.offsetMax.x, -rect.offsetMax.y, rect.offsetMin.y);
    }

    private void CalculateScaleFactor()
    {
        float scaleX = Screen.width / referenceScreenSize.x;
        float scaleY = Screen.height / referenceScreenSize.y;
        
        // Use uniform scaling (Min ensures it fits, though usually we match width for tall UIs)
        globalScaleFactor = Mathf.Min(scaleX, scaleY);
        
        // Ensure at least 1.0 if we don't want it smaller than reference on huge screens
        globalScaleFactor = Mathf.Max(globalScaleFactor, 1.0f);
    }

    private void ApplyPhysicalScaling()
    {
        if (button1 != null) ScaleButtonOffsets(button1.GetComponent<RectTransform>(), originalButton1Offsets);
        if (button2 != null) ScaleButtonOffsets(button2.GetComponent<RectTransform>(), originalButton2Offsets);
    }

    private void ScaleButtonOffsets(RectTransform rect, Vector4 original)
    {
        if (rect == null) return;
        
        rect.offsetMin = new Vector2(original.x * globalScaleFactor, original.w * globalScaleFactor);
        rect.offsetMax = new Vector2(-original.y * globalScaleFactor, -original.z * globalScaleFactor);
    }

    /// <summary>
    /// Handles button click events to update the UI state.
    /// </summary>
    /// <param name="index">1 for Button 1, 2 for Button 2.</param>
    public void SelectButton(int index)
    {
        if (index == 1)
        {
            ApplySelectionState(button1, button2, true);
        }
        else
        {
            ApplySelectionState(button2, button1, true);
        }
    }

    /// <summary>
    /// Applies scaling to both buttons based on which one is selected.
    /// </summary>
    /// <param name="selected">The button that was clicked.</param>
    /// <param name="unselected">The other button.</param>
    /// <param name="animate">Whether to animate the transition.</param>
    private void ApplySelectionState(Button selected, Button unselected, bool animate)
    {
        // Kill any existing tweens to avoid conflicts
        selected.transform.DOKill();
        unselected.transform.DOKill();

        if (animate)
        {
            // Animate to new scales
            selected.transform.DOScale(Vector3.one * selectedScale, animationDuration).SetEase(scaleEase).SetLink(selected.gameObject);
            unselected.transform.DOScale(Vector3.one * unselectedScale, animationDuration).SetEase(scaleEase).SetLink(unselected.gameObject);
        }
        else
        {
            // Instant scale set for initialization
            selected.transform.localScale = Vector3.one * selectedScale;
            unselected.transform.localScale = Vector3.one * unselectedScale;
        }
    }

    private void OnDestroy()
    {
        // Cleanup listeners
        if (button1 != null) button1.onClick.RemoveAllListeners();
        if (button2 != null) button2.onClick.RemoveAllListeners();
    }
}
