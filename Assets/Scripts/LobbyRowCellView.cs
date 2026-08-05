using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EnhancedUI.EnhancedScroller;
using DG.Tweening;
using System.Collections.Generic;
using DOTweenTween = DG.Tweening.Tween;

/// <summary>
/// Row cell view that contains 1 LobbyGameObjectUI item
/// Also detects swipes and forwards to LobbyManager
/// </summary>
public class LobbyRowCellView : EnhancedScrollerCellView, IPointerDownHandler, IPointerUpHandler
{
    [Header("Lobby Game Object Item")]
    [SerializeField] private LobbyGameObjectUI item; // Lobby item in the row
    
    [Header("Play Button")]
    [SerializeField] private Button playButton; // Play button for the item
    
    [Header("Play Button Hover Animation Settings")]
    [SerializeField] private float buttonHoverScale = 1.15f; // Scale factor when hovering over button (default 1.15 = 15% larger)
    [SerializeField] private float buttonAnimationDuration = 0.2f; // Duration of button scale animation in seconds
    [SerializeField] private Ease buttonScaleEase = Ease.OutBack; // Easing function for button scale animation
    
    private HorizontalLobbyManager horizontalLobbyManager;
    private Dictionary<Button, DOTweenTween> buttonTweens = new Dictionary<Button, DOTweenTween>();
    private Dictionary<Button, Vector3> buttonOriginalScales = new Dictionary<Button, Vector3>();
    
    // Swipe detection
    [Header("Swipe Detection")]
    [SerializeField] private bool enableSwipeOnCard = true; // Enable swipe detection on this card
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger
    [SerializeField] private bool useVelocityScrolling = true; // Use velocity-based scrolling
    [SerializeField] private float pixelsPerItem = 100f; // Pixels per item for velocity scrolling
    [SerializeField] private int maxScrollItems = 1; // Max items to scroll
    
    private Vector2 swipeStartPos;
    private bool isSwiping = false;
    
    private void Awake()
    {
        // Find HorizontalLobbyManager in the scene
        horizontalLobbyManager = FindFirstObjectByType<HorizontalLobbyManager>();
        
        // Setup button listener and hover animation
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            SetupButtonHoverAnimation(playButton);
        }
    }
    
    private void OnDestroy()
    {
        // Remove listener to prevent memory leaks
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
            CleanupButtonHoverAnimation(playButton);
        }
        CleanupTweens();
    }
    
    /// <summary>
    /// Sets the data for this row (1 item)
    /// </summary>
    /// <param name="data">Data for the item (can be null)</param>
    public void SetData(LobbyGameObjectData data)
    {
        // Set item
        if (item != null)
        {
            if (data != null)
            {
                item.SetData(data);
                item.gameObject.SetActive(true);
                
                // Show and update play button
                if (playButton != null)
                {
                    playButton.gameObject.SetActive(true);
                    UpdatePlayButtonInteractability(playButton, data.lobbyIndex);
                    ResetButtonScale(playButton); // Reset scale when data is set (cell recycling)
                }
            }
            else
            {
                item.gameObject.SetActive(false);
                
                // Hide play button if no data
                if (playButton != null)
                {
                    playButton.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Updates the interactability of a play button based on whether player can afford entry fee
    /// </summary>
    private void UpdatePlayButtonInteractability(Button playButton, int lobbyIndex)
    {
        if (playButton == null || horizontalLobbyManager == null)
        {
            return;
        }
        
        // Check if player can afford entry fee
        bool canAfford = horizontalLobbyManager.CanPlayerAffordEntryFee(lobbyIndex);

        playButton.interactable = canAfford;
        
        if (!canAfford)
        {
            Debug.Log($"[LobbyRowCellView] Play button disabled for Lobby {lobbyIndex + 1} - player cannot afford entry fee");
        }
    }
    
    /// <summary>
    /// Refreshes the interactability of play button (call this when player currency changes)
    /// </summary>
    public void RefreshPlayButtonsInteractability()
    {
        if (item != null && playButton != null)
        {
            LobbyGameObjectData data = item.GetCurrentData();
            if (data != null)
            {
                UpdatePlayButtonInteractability(playButton, data.lobbyIndex);
            }
        }
    }
    
    /// <summary>
    /// Called when play button is clicked
    /// </summary>
    private void OnPlayButtonClicked()
    {
        Debug.Log("play button clicked");
        if (item != null)
        {
            LobbyGameObjectData data = item.GetCurrentData();
            if (data != null)
            {
                if (horizontalLobbyManager != null)
                {
                    horizontalLobbyManager.OpenGameplayObject(data.lobbyIndex);
                }
                else
                {
                    Debug.LogWarning("[LobbyRowCellView] No HorizontalLobbyManager found! Cannot open gameplay object.");
                }
            }
        }
    }
    
    #region Button Hover Animation
    
    /// <summary>
    /// Sets up hover animation for a button using EventTrigger
    /// </summary>
    private void SetupButtonHoverAnimation(Button button)
    {
        if (button == null) return;
        
        // Get or add EventTrigger component
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Store original scale
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null && !buttonOriginalScales.ContainsKey(button))
        {
            buttonOriginalScales[button] = rectTransform.localScale;
        }
        
        // Remove existing entries if any
        eventTrigger.triggers.Clear();
        
        // Add PointerEnter event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnButtonPointerEnter(button); });
        eventTrigger.triggers.Add(pointerEnter);
        
        // Add PointerExit event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnButtonPointerExit(button); });
        eventTrigger.triggers.Add(pointerExit);
    }
    
    /// <summary>
    /// Cleans up hover animation for a button
    /// </summary>
    private void CleanupButtonHoverAnimation(Button button)
    {
        if (button == null) return;
        
        // Kill any active tween
        if (buttonTweens.ContainsKey(button) && buttonTweens[button] != null && buttonTweens[button].IsActive())
        {
            buttonTweens[button].Kill();
            buttonTweens.Remove(button);
        }
        
        // Remove from dictionaries
        buttonOriginalScales.Remove(button);
    }
    
    /// <summary>
    /// Called when pointer enters a button (hover start)
    /// </summary>
    private void OnButtonPointerEnter(Button button)
    {
        if (button == null || !button.interactable) return;
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Get original scale
        Vector3 originalScale = Vector3.one;
        if (buttonOriginalScales.ContainsKey(button))
        {
            originalScale = buttonOriginalScales[button];
        }
        else
        {
            originalScale = rectTransform.localScale;
            buttonOriginalScales[button] = originalScale;
        }
        
        // Kill any existing tween for this button
        if (buttonTweens.ContainsKey(button) && buttonTweens[button] != null && buttonTweens[button].IsActive())
        {
            buttonTweens[button].Kill();
        }
        
        // Scale up using DOTween
        Vector3 targetScale = originalScale * buttonHoverScale;
        buttonTweens[button] = rectTransform.DOScale(targetScale, buttonAnimationDuration)
            .SetEase(buttonScaleEase)
            .SetLink(button.gameObject);
    }
    
    /// <summary>
    /// Called when pointer exits a button (hover end)
    /// </summary>
    private void OnButtonPointerExit(Button button)
    {
        if (button == null) return;
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Get original scale
        Vector3 originalScale = Vector3.one;
        if (buttonOriginalScales.ContainsKey(button))
        {
            originalScale = buttonOriginalScales[button];
        }
        else
        {
            originalScale = rectTransform.localScale;
            buttonOriginalScales[button] = originalScale;
        }
        
        // Kill any existing tween for this button
        if (buttonTweens.ContainsKey(button) && buttonTweens[button] != null && buttonTweens[button].IsActive())
        {
            buttonTweens[button].Kill();
        }
        
        // Scale back to original using DOTween
        buttonTweens[button] = rectTransform.DOScale(originalScale, buttonAnimationDuration)
            .SetEase(buttonScaleEase)
            .SetLink(button.gameObject);
    }
    
    /// <summary>
    /// Resets button scale to original (used when cell is recycled)
    /// </summary>
    private void ResetButtonScale(Button button)
    {
        if (button == null) return;
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Kill any active tween
        if (buttonTweens.ContainsKey(button) && buttonTweens[button] != null && buttonTweens[button].IsActive())
        {
            buttonTweens[button].Kill();
            buttonTweens.Remove(button);
        }
        
        // Reset to original scale
        if (buttonOriginalScales.ContainsKey(button))
        {
            rectTransform.localScale = buttonOriginalScales[button];
        }
        else
        {
            // Store current scale as original if not stored yet
            buttonOriginalScales[button] = rectTransform.localScale;
        }
    }

    private void OnDisable()
    {
        CleanupTweens();
    }

    private void CleanupTweens()
    {
        transform.DOKill();
        if (buttonTweens != null)
        {
            foreach (var kvp in buttonTweens)
            {
                if (kvp.Value != null && kvp.Value.IsActive())
                {
                    kvp.Value.Kill();
                }
            }
            buttonTweens.Clear();
        }
    }
    
    #endregion
    
    /// <summary>
    /// Update the selected visual state
    /// </summary>
    public void SetSelectedState(bool isSelected)
    {
        if (item != null)
        {
            item.SetSelectedState(isSelected);
        }
    }

    #region Swipe Detection (Forward to HorizontalLobbyManager)
    
    /// <summary>
    /// Called when pointer/touch begins on this lobby card
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableSwipeOnCard) return;
        
        swipeStartPos = eventData.position;
        isSwiping = true;
    }
    
    /// <summary>
    /// Called when pointer/touch is released from this lobby card
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enableSwipeOnCard || !isSwiping) return;
        
        Vector2 swipeEndPos = eventData.position;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        
        if (horizontalLobbyManager != null)
        {
            if (Mathf.Abs(swipeDelta.x) > swipeThreshold && Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                if (useVelocityScrolling)
                {
                    float swipeDistance = Mathf.Abs(swipeDelta.x);
                    int itemsToScroll = Mathf.Clamp(Mathf.RoundToInt(swipeDistance / pixelsPerItem), 1, maxScrollItems);
                    
                    if (swipeDelta.x < 0) horizontalLobbyManager.ScrollMultiple(itemsToScroll);
                    else horizontalLobbyManager.ScrollMultiple(-itemsToScroll);
                }
                else
                {
                    if (swipeDelta.x < 0) horizontalLobbyManager.ScrollRight();
                    else horizontalLobbyManager.ScrollLeft();
                }
            }
        }
        
        isSwiping = false;
    }
    
    #endregion
}

