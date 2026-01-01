using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EnhancedUI.EnhancedScroller;
using DG.Tweening;
using System.Collections.Generic;
using DOTweenTween = DG.Tweening.Tween;

/// <summary>
/// Row cell view that contains 2 LobbyGameObjectUI items for grid layout
/// </summary>
public class LobbyRowCellView : EnhancedScrollerCellView
{
    [Header("Lobby Game Object Items")]
    [SerializeField] private LobbyGameObjectUI item1; // First item in the row
    [SerializeField] private LobbyGameObjectUI item2; // Second item in the row
    
    [Header("Play Buttons")]
    [SerializeField] private Button playButton1; // Play button for first item
    [SerializeField] private Button playButton2; // Play button for second item
    
    [Header("Play Button Hover Animation Settings")]
    [SerializeField] private float buttonHoverScale = 1.15f; // Scale factor when hovering over button (default 1.15 = 15% larger)
    [SerializeField] private float buttonAnimationDuration = 0.2f; // Duration of button scale animation in seconds
    [SerializeField] private Ease buttonScaleEase = Ease.OutBack; // Easing function for button scale animation
    
    private LobbyManager lobbyManager;
    private Dictionary<Button, DOTweenTween> buttonTweens = new Dictionary<Button, DOTweenTween>();
    private Dictionary<Button, Vector3> buttonOriginalScales = new Dictionary<Button, Vector3>();
    
    private void Awake()
    {
        // Find LobbyManager in the scene
        lobbyManager = FindObjectOfType<LobbyManager>();
        
        // Setup button listeners and hover animations
        if (playButton1 != null)
        {
            playButton1.onClick.AddListener(OnPlayButton1Clicked);
            SetupButtonHoverAnimation(playButton1);
        }
        
        if (playButton2 != null)
        {
            playButton2.onClick.AddListener(OnPlayButton2Clicked);
            SetupButtonHoverAnimation(playButton2);
        }
    }
    
    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks
        if (playButton1 != null)
        {
            playButton1.onClick.RemoveListener(OnPlayButton1Clicked);
            CleanupButtonHoverAnimation(playButton1);
        }
        
        if (playButton2 != null)
        {
            playButton2.onClick.RemoveListener(OnPlayButton2Clicked);
            CleanupButtonHoverAnimation(playButton2);
        }
    }
    
    /// <summary>
    /// Sets the data for this row (2 items)
    /// </summary>
    /// <param name="data1">Data for first item (can be null)</param>
    /// <param name="data2">Data for second item (can be null)</param>
    public void SetData(LobbyGameObjectData data1, LobbyGameObjectData data2)
    {
        // Set first item
        if (item1 != null)
        {
            if (data1 != null)
            {
                item1.SetData(data1);
                item1.gameObject.SetActive(true);
                
                // Show and update play button for first item
                if (playButton1 != null)
                {
                    playButton1.gameObject.SetActive(true);
                    UpdatePlayButtonInteractability(playButton1, data1.lobbyIndex);
                    ResetButtonScale(playButton1); // Reset scale when data is set (cell recycling)
                }
            }
            else
            {
                item1.gameObject.SetActive(false);
                
                // Hide play button if no data
                if (playButton1 != null)
                {
                    playButton1.gameObject.SetActive(false);
                }
            }
        }
        
        // Set second item
        if (item2 != null)
        {
            if (data2 != null)
            {
                item2.SetData(data2);
                item2.gameObject.SetActive(true);
                
                // Show and update play button for second item
                if (playButton2 != null)
                {
                    playButton2.gameObject.SetActive(true);
                    UpdatePlayButtonInteractability(playButton2, data2.lobbyIndex);
                    ResetButtonScale(playButton2); // Reset scale when data is set (cell recycling)
                }
            }
            else
            {
                item2.gameObject.SetActive(false);
                
                // Hide play button if no data
                if (playButton2 != null)
                {
                    playButton2.gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Updates the interactability of a play button based on whether player can afford entry fee
    /// </summary>
    private void UpdatePlayButtonInteractability(Button playButton, int lobbyIndex)
    {
        if (playButton == null || lobbyManager == null)
        {
            return;
        }
        
        // Check if player can afford entry fee
        bool canAfford = lobbyManager.CanPlayerAffordEntryFee(lobbyIndex);
        playButton.interactable = canAfford;
        
        if (!canAfford)
        {
            Debug.Log($"[LobbyRowCellView] Play button disabled for Lobby {lobbyIndex + 1} - player cannot afford entry fee");
        }
    }
    
    /// <summary>
    /// Refreshes the interactability of play buttons (call this when player currency changes)
    /// </summary>
    public void RefreshPlayButtonsInteractability()
    {
        if (item1 != null && playButton1 != null)
        {
            LobbyGameObjectData data1 = item1.GetCurrentData();
            if (data1 != null)
            {
                UpdatePlayButtonInteractability(playButton1, data1.lobbyIndex);
            }
        }
        
        if (item2 != null && playButton2 != null)
        {
            LobbyGameObjectData data2 = item2.GetCurrentData();
            if (data2 != null)
            {
                UpdatePlayButtonInteractability(playButton2, data2.lobbyIndex);
            }
        }
    }
    
    /// <summary>
    /// Called when play button 1 is clicked
    /// </summary>
    private void OnPlayButton1Clicked()
    {
        if (lobbyManager != null && item1 != null)
        {
            LobbyGameObjectData data = item1.GetCurrentData();
            if (data != null)
            {
                // Pass the lobby index to open the correct lobby configuration
                lobbyManager.OpenGameplayObject(data.lobbyIndex);
            }
        }
        else
        {
            Debug.LogWarning("[LobbyRowCellView] LobbyManager or item1 is null! Cannot open gameplay object.");
        }
    }
    
    /// <summary>
    /// Called when play button 2 is clicked
    /// </summary>
    private void OnPlayButton2Clicked()
    {
        if (lobbyManager != null && item2 != null)
        {
            LobbyGameObjectData data = item2.GetCurrentData();
            if (data != null)
            {
                // Pass the lobby index to open the correct lobby configuration
                lobbyManager.OpenGameplayObject(data.lobbyIndex);
            }
        }
        else
        {
            Debug.LogWarning("[LobbyRowCellView] LobbyManager or item2 is null! Cannot open gameplay object.");
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
            .SetEase(buttonScaleEase);
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
            .SetEase(buttonScaleEase);
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
    
    #endregion
}

