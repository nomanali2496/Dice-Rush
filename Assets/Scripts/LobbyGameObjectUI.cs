using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EnhancedUI.EnhancedScroller;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Data class to hold lobby game object information
/// </summary>
[System.Serializable]
public class LobbyGameObjectData
{
    public int backgroundIndex; // Legacy fallback index
    public int lobbyIndex; // Index of the lobby
    public string lobbyId; // Matches the database ID (e.g. "combo-dice")
    public string imageUrl; // URL/Path for dynamic remote images
    
    public LobbyGameObjectData(int backgroundIndex, int lobbyIndex, string lobbyId = "", string imageUrl = "")
    {
        this.backgroundIndex = backgroundIndex;
        this.lobbyIndex = lobbyIndex;
        this.lobbyId = lobbyId;
        this.imageUrl = imageUrl;
    }
    
    // Legacy constructor for backward compatibility
    public LobbyGameObjectData(int backgroundIndex)
    {
        this.backgroundIndex = backgroundIndex;
        this.lobbyIndex = backgroundIndex; // Use background index as lobby index by default
    }
}

/// <summary>
/// Manages the UI display for a single lobby game object
/// Works with Enhanced Scroller for efficient scrolling
/// Supports 13 different background sprites
/// </summary>
public class LobbyGameObjectUI : EnhancedScrollerCellView, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Background Image")]
    [SerializeField] private Image backgroundImage; // Main background image component
    
    [Header("Selection Visuals")]
    [SerializeField] private Image unselectedOverlayImage; // Image to show when item is NOT selected (e.g. dark overlay)
    
    [Header("Hover Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f; // Scale factor when hovering (default 1.1 = 10% larger)
    [SerializeField] private float animationDuration = 0.3f; // Duration of scale animation in seconds
    [SerializeField] private AnimationCurve scaleCurve; // Animation curve for smooth scaling (set in inspector, defaults to smooth ease)
    
    [Header("ID-Based Sprite Mapping")]
    [SerializeField] private List<LobbyVisualMapping> spriteMappings = new List<LobbyVisualMapping>();
    
    [System.Serializable]
    public class LobbyVisualMapping
    {
        public string lobbyId; // Matches the "id" from your API (e.g. "combo-dice")
        public Sprite sprite;
    }
    
    private LobbyGameObjectData currentData;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    
    private void Awake()
    {
        InitializeScale();
        
        // Initialize scale curve with a smooth easing if not set in inspector
        if (scaleCurve == null || scaleCurve.keys.Length == 0)
        {
            // Create a smooth ease-out curve (similar to OutQuad in DOTween)
            scaleCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        }
    }
    
    /// <summary>
    /// Initializes the RectTransform and stores the original scale
    /// </summary>
    private void InitializeScale()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        
        // Only initialize originalScale once (when it's still the default zero vector)
        // Vector3.zero means the field hasn't been initialized yet
        if (rectTransform != null && originalScale == Vector3.zero)
        {
            originalScale = rectTransform.localScale;
            // Ensure we have a valid scale (default to Vector3.one if somehow zero)
            if (originalScale == Vector3.zero)
            {
                originalScale = Vector3.one;
                rectTransform.localScale = Vector3.one; // Also set the actual scale
            }
        }
    }
    
    private void OnDestroy()
    {
        // Stop any active coroutines to prevent errors
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
    }
    
    /// <summary>
    /// Sets the data for this cell view (called by Enhanced Scroller)
    /// </summary>
    /// <param name="data">The lobby game object data</param>
    public void SetData(LobbyGameObjectData data)
    {
        currentData = data;
        SetupBackground(data.backgroundIndex);
        
        // Reset scale when data is set (important for cell recycling)
        InitializeScale(); // Ensure scale is initialized
        if (rectTransform != null)
        {
            // Stop any active coroutine
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
                scaleCoroutine = null;
            }
            // Reset to original scale immediately
            rectTransform.localScale = originalScale;
        }
    }
    
    /// <summary>
    /// Sets up the background sprite based on the lobbyId or fallback index
    /// </summary>
    public void SetupBackground(int fallbackIndex)
    {
        if (backgroundImage == null) return;
        
        // 1. Try to find sprite by ID (Recommended Mode)
        if (currentData != null && !string.IsNullOrEmpty(currentData.lobbyId))
        {
            Sprite mappedSprite = GetSpriteById(currentData.lobbyId);
            if (mappedSprite != null)
            {
                backgroundImage.sprite = mappedSprite;
                backgroundImage.enabled = true;
                return;
            }
        }

        // 2. Fallback to Dynamic URL if provided
        if (currentData != null && !string.IsNullOrEmpty(currentData.imageUrl))
        {
            LobbyModel.LoadLobbySprite(currentData.imageUrl, (downloadedSprite) => {
                if (downloadedSprite != null)
                {
                    backgroundImage.sprite = downloadedSprite;
                    backgroundImage.enabled = true;
                }
                else
                {
                    ApplyLocalSprite(fallbackIndex);
                }
            });
            return;
        }

        // 3. Last Fallback: Use numeric index
        ApplyLocalSprite(fallbackIndex);
    }

    private Sprite GetSpriteById(string id)
    {
        if (spriteMappings == null) return null;
        
        foreach (var mapping in spriteMappings)
        {
            if (mapping != null && mapping.lobbyId == id)
            {
                return mapping.sprite;
            }
        }
        return null;
    }

    private void ApplyLocalSprite(int index)
    {
        // Simple fallback if no mappings exist
        if (spriteMappings != null && index >= 0 && index < spriteMappings.Count)
        {
            backgroundImage.sprite = spriteMappings[index].sprite;
            backgroundImage.enabled = true;
        }
        else
        {
            // If even fallback fails, we can't show anything
            backgroundImage.enabled = (backgroundImage.sprite != null);
        }
    }
    
    /// <summary>
    /// Gets the current data for this object
    /// </summary>
    public LobbyGameObjectData GetCurrentData()
    {
        return currentData;
    }
    
    /// <summary>
    /// Gets the current background index
    /// </summary>
    public int GetCurrentBackgroundIndex()
    {
        return currentData != null ? currentData.backgroundIndex : -1;
    }
    
    /// <summary>
    /// Called when pointer enters the lobby object (hover start)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        InitializeScale(); // Ensure scale is initialized
        if (rectTransform == null) return;
        
        // Stop any existing animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        // Scale up using coroutine
        Vector3 targetScale = originalScale * hoverScale;
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale, animationDuration));
    }
    
    /// <summary>
    /// Called when pointer exits the lobby object (hover end)
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        InitializeScale(); // Ensure scale is initialized
        if (rectTransform == null) return;
        
        // Stop any existing animation
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        // Scale back to original using coroutine
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale, animationDuration));
    }
    
    /// <summary>
    /// Coroutine to animate scale smoothly using AnimationCurve
    /// </summary>
    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        if (rectTransform == null) yield break;
        
        Vector3 startScale = rectTransform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Apply animation curve for smooth easing
            float curveValue = scaleCurve.Evaluate(t);
            
            // Interpolate between start and target scale
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            
            yield return null;
        }
        
        // Ensure we end exactly at target scale
        rectTransform.localScale = targetScale;
        scaleCoroutine = null;
    }
    /// <summary>
    /// Sets the visual state based on whether this item is currently selected/focused
    /// </summary>
    public void SetSelectedState(bool isSelected)
    {
        if (unselectedOverlayImage != null)
        {
            // If selected, hide overlay. If not selected, show overlay.
            unselectedOverlayImage.gameObject.SetActive(!isSelected);
        }
    }
}

