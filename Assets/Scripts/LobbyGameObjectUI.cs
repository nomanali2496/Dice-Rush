using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EnhancedUI.EnhancedScroller;
using System.Collections;

/// <summary>
/// Data class to hold lobby game object information
/// </summary>
[System.Serializable]
public class LobbyGameObjectData
{
    public int backgroundIndex; // Index of background sprite to use (0-12 for 13 backgrounds)
    public int lobbyIndex; // Index of the lobby (0-12 for 13 lobbies)
    
    public LobbyGameObjectData(int backgroundIndex, int lobbyIndex)
    {
        this.backgroundIndex = backgroundIndex;
        this.lobbyIndex = lobbyIndex;
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
    
    [Header("Hover Animation Settings")]
    [SerializeField] private float hoverScale = 1.1f; // Scale factor when hovering (default 1.1 = 10% larger)
    [SerializeField] private float animationDuration = 0.3f; // Duration of scale animation in seconds
    [SerializeField] private AnimationCurve scaleCurve; // Animation curve for smooth scaling (set in inspector, defaults to smooth ease)
    
    [Header("Background Sprites (13 Sprites)")]
    [SerializeField] private Sprite backgroundSprite0; // Background sprite 0
    [SerializeField] private Sprite backgroundSprite1; // Background sprite 1
    [SerializeField] private Sprite backgroundSprite2; // Background sprite 2
    [SerializeField] private Sprite backgroundSprite3; // Background sprite 3
    [SerializeField] private Sprite backgroundSprite4; // Background sprite 4
    [SerializeField] private Sprite backgroundSprite5; // Background sprite 5
    [SerializeField] private Sprite backgroundSprite6; // Background sprite 6
    [SerializeField] private Sprite backgroundSprite7; // Background sprite 7
    [SerializeField] private Sprite backgroundSprite8; // Background sprite 8
    [SerializeField] private Sprite backgroundSprite9; // Background sprite 9
    [SerializeField] private Sprite backgroundSprite10; // Background sprite 10
    [SerializeField] private Sprite backgroundSprite11; // Background sprite 11
    [SerializeField] private Sprite backgroundSprite12; // Background sprite 12
    
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
    /// Sets up the background sprite based on the index
    /// This method can still be called directly if not using Enhanced Scroller
    /// </summary>
    /// <param name="backgroundIndex">Index of background sprite to use (0-12)</param>
    public void SetupBackground(int backgroundIndex)
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("[LobbyGameObjectUI] Background Image is not assigned!");
            return;
        }
        
        // Clamp index to valid range (0-12)
        backgroundIndex = Mathf.Clamp(backgroundIndex, 0, 12);
        
        // Get the sprite based on index
        Sprite targetSprite = GetBackgroundSprite(backgroundIndex);
        
        if (targetSprite != null)
        {
            backgroundImage.sprite = targetSprite;
            backgroundImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[LobbyGameObjectUI] Background sprite at index {backgroundIndex} is not assigned!");
            backgroundImage.enabled = false;
        }
    }
    
    /// <summary>
    /// Gets the background sprite based on index
    /// </summary>
    /// <param name="index">Index of the background sprite (0-12)</param>
    /// <returns>The sprite at the given index, or null if not assigned</returns>
    private Sprite GetBackgroundSprite(int index)
    {
        switch (index)
        {
            case 0: return backgroundSprite0;
            case 1: return backgroundSprite1;
            case 2: return backgroundSprite2;
            case 3: return backgroundSprite3;
            case 4: return backgroundSprite4;
            case 5: return backgroundSprite5;
            case 6: return backgroundSprite6;
            case 7: return backgroundSprite7;
            case 8: return backgroundSprite8;
            case 9: return backgroundSprite9;
            case 10: return backgroundSprite10;
            case 11: return backgroundSprite11;
            case 12: return backgroundSprite12;
            default: return null;
        }
    }
    
    /// <summary>
    /// Updates only the background without changing other properties
    /// </summary>
    public void UpdateBackground(int backgroundIndex)
    {
        SetupBackground(backgroundIndex);
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.backgroundIndex = backgroundIndex;
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
    /// Sets all 13 background sprites at once (useful for initialization)
    /// </summary>
    public void SetBackgroundSprites(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[LobbyGameObjectUI] Sprites array is null or empty!");
            return;
        }
        
        // Set sprites up to index 12 (13 sprites total)
        int maxIndex = Mathf.Min(sprites.Length, 13);
        
        for (int i = 0; i < maxIndex; i++)
        {
            SetBackgroundSpriteAtIndex(i, sprites[i]);
        }
    }
    
    /// <summary>
    /// Sets a specific background sprite at a given index
    /// </summary>
    private void SetBackgroundSpriteAtIndex(int index, Sprite sprite)
    {
        switch (index)
        {
            case 0: backgroundSprite0 = sprite; break;
            case 1: backgroundSprite1 = sprite; break;
            case 2: backgroundSprite2 = sprite; break;
            case 3: backgroundSprite3 = sprite; break;
            case 4: backgroundSprite4 = sprite; break;
            case 5: backgroundSprite5 = sprite; break;
            case 6: backgroundSprite6 = sprite; break;
            case 7: backgroundSprite7 = sprite; break;
            case 8: backgroundSprite8 = sprite; break;
            case 9: backgroundSprite9 = sprite; break;
            case 10: backgroundSprite10 = sprite; break;
            case 11: backgroundSprite11 = sprite; break;
            case 12: backgroundSprite12 = sprite; break;
        }
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
}

