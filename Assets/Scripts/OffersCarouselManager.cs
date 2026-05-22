using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages a horizontal carousel display of 7 offers
/// Shows 3 offers at a time: left (low opacity), center (selected, larger), right (low opacity)
/// Uses explicit position references instead of layout groups
/// </summary>
public class OffersCarouselManager : MonoBehaviour
{
    [Header("Offer Items")]
    [SerializeField] private GameObject[] offerItems = new GameObject[7]; // 7 offer prefabs/GameObjects
    
    [Header("Position References")]
    [SerializeField] private Transform leftPosition; // Transform for left offer position
    [SerializeField] private Transform centerPosition; // Transform for center/selected offer position
    [SerializeField] private Transform rightPosition; // Transform for right offer position
    
    [Header("Dot Buttons (7 buttons for navigation)")]
    [SerializeField] private Button[] dotButtons = new Button[7]; // 7 dot buttons at bottom
    
    [Header("Dot Button Images")]
    [SerializeField] private Sprite selectedDotImage; // Image to show when dot is selected
    [SerializeField] private Sprite unselectedDotImage; // Image to show when dot is not selected (optional, uses default if null)
    
    [Header("Visual Settings")]
    [SerializeField] private float selectedScale = 1.2f; // Scale multiplier for center/selected offer
    [SerializeField] private float sideOpacity = 0.5f; // Opacity for left/right offers (0-1)
    [SerializeField] private float transitionDuration = 0.3f; // Animation duration
    
    [Header("Navigation")]
    [SerializeField] private Button leftNavButton; // Optional: Button to scroll left
    [SerializeField] private Button rightNavButton; // Optional: Button to scroll right
    
    private int currentSelectedIndex = 0; // Currently selected offer (center)
    private Dictionary<int, RectTransform> offerTransforms = new Dictionary<int, RectTransform>();
    private Dictionary<int, CanvasGroup> offerCanvasGroups = new Dictionary<int, CanvasGroup>();
    private Coroutine transitionCoroutine;
    
    // Multi-step scroll animation tracking
    private Coroutine multiScrollCoroutine = null;
    
    // Track which offers are currently in left, center, right positions
    private int leftOfferIndex = -1;
    private int centerOfferIndex = 0;
    private int rightOfferIndex = 1;
    private bool isFirstInitialization = true;
    
    private void Start()
    {
        InitializeOffers();
        SetupNavigationButtons();
        SetupDotButtons();
        UpdateOfferPositions();
    }
    
    /// <summary>
    /// Initializes all offer items and sets up their visual components
    /// </summary>
    private void InitializeOffers()
    {
        if (centerPosition == null)
        {
            Debug.LogError("[OffersCarouselManager] Center position not assigned!");
            return;
        }
        
        if (leftPosition == null || rightPosition == null)
        {
            Debug.LogWarning("[OffersCarouselManager] Left or Right position not assigned!");
        }
        
        // Clear existing dictionaries
        offerTransforms.Clear();
        offerCanvasGroups.Clear();
        
        // Setup each offer item
        for (int i = 0; i < offerItems.Length; i++)
        {
            if (offerItems[i] == null)
            {
                Debug.LogWarning($"[OffersCarouselManager] Offer item {i} is null!");
                continue;
            }
            
            // Ensure offer is active
            offerItems[i].SetActive(true);
            
            // Get or add RectTransform
            RectTransform rectTransform = offerItems[i].GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = offerItems[i].AddComponent<RectTransform>();
            }
            
            // Get or add CanvasGroup for opacity control
            CanvasGroup canvasGroup = offerItems[i].GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = offerItems[i].AddComponent<CanvasGroup>();
            }
            
            offerTransforms[i] = rectTransform;
            offerCanvasGroups[i] = canvasGroup;
        }
        
        // Calculate initial positions
        CalculateVisibleOffers();
    }
    
    /// <summary>
    /// Calculates which offers should be visible in left, center, right positions
    /// </summary>
    private void CalculateVisibleOffers()
    {
        centerOfferIndex = currentSelectedIndex;
        leftOfferIndex = currentSelectedIndex - 1;
        rightOfferIndex = currentSelectedIndex + 1;
        
        // Handle edge cases
        if (leftOfferIndex < 0)
        {
            leftOfferIndex = -1; // No left offer
        }
        
        if (rightOfferIndex >= offerItems.Length)
        {
            rightOfferIndex = -1; // No right offer
        }
    }
    
    /// <summary>
    /// Updates the positions and visual states of all offers
    /// </summary>
    private void UpdateOfferPositions()
    {
        CalculateVisibleOffers();
        
        // Position and update all offers
        for (int i = 0; i < offerItems.Length; i++)
        {
            if (offerItems[i] == null || !offerTransforms.ContainsKey(i)) continue;
            
            RectTransform rectTransform = offerTransforms[i];
            CanvasGroup canvasGroup = offerCanvasGroups[i];
            
            // Determine position and visual state
            Transform targetPosition = null;
            float targetScale = 1f;
            float targetOpacity = 0f;
            bool shouldBeVisible = false;
            
            if (i == centerOfferIndex)
            {
                // Center offer - selected
                targetPosition = centerPosition;
                targetScale = selectedScale;
                targetOpacity = 1f;
                shouldBeVisible = true;
            }
            else if (i == leftOfferIndex)
            {
                // Left offer
                targetPosition = leftPosition;
                targetScale = 1f;
                targetOpacity = sideOpacity;
                shouldBeVisible = true;
            }
            else if (i == rightOfferIndex)
            {
                // Right offer
                targetPosition = rightPosition;
                targetScale = 1f;
                targetOpacity = sideOpacity;
                shouldBeVisible = true;
            }
            else
            {
                // Hidden offer - keep it somewhere off-screen or inactive
                shouldBeVisible = false;
            }
            
            // Update position and visual state
            if (shouldBeVisible && targetPosition != null)
            {
                offerItems[i].SetActive(true);
                
                if (isFirstInitialization)
                {
                    // Set position instantly on first initialization
                    SetOfferPositionInstantly(i, targetPosition, targetScale, targetOpacity);
                }
                else
                {
                    // Animate position changes
                    StartCoroutine(AnimateOfferToPosition(i, targetPosition, targetScale, targetOpacity));
                }
            }
            else
            {
                // Hide offers that shouldn't be visible
                offerItems[i].SetActive(false);
            }
        }
        
        // Update dot button states
        UpdateDotButtons();
        
        // Update navigation button states (enable/disable based on position)
        UpdateNavigationButtonStates();
        
        // Mark that first initialization is complete
        if (isFirstInitialization)
        {
            isFirstInitialization = false;
        }
    }
    
    /// <summary>
    /// Updates navigation button interactability based on current position
    /// </summary>
    private void UpdateNavigationButtonStates()
    {
        // Disable left button if at first offer
        if (leftNavButton != null)
        {
            leftNavButton.interactable = (currentSelectedIndex > 0);
        }
        
        // Disable right button if at last offer
        if (rightNavButton != null)
        {
            rightNavButton.interactable = (currentSelectedIndex < offerItems.Length - 1);
        }
    }
    
    /// <summary>
    /// Instantly sets an offer to a target position, scale, and opacity (no animation)
    /// </summary>
    private void SetOfferPositionInstantly(int index, Transform targetPosition, float targetScale, float targetOpacity)
    {
        if (!offerTransforms.ContainsKey(index) || targetPosition == null) return;
        
        RectTransform rectTransform = offerTransforms[index];
        CanvasGroup canvasGroup = offerCanvasGroups[index];
        
        // Change parent to target position transform
        rectTransform.SetParent(targetPosition, false);
        
        // Set local position to center (zero)
        rectTransform.localPosition = Vector3.zero;
        
        // Set scale
        rectTransform.localScale = Vector3.one * targetScale;
        
        // Set opacity
        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetOpacity;
        }
    }
    
    /// <summary>
    /// Animates an offer to a target position, scale, and opacity
    /// </summary>
    private IEnumerator AnimateOfferToPosition(int index, Transform targetPosition, float targetScale, float targetOpacity)
    {
        if (!offerTransforms.ContainsKey(index) || targetPosition == null) yield break;
        
        RectTransform rectTransform = offerTransforms[index];
        CanvasGroup canvasGroup = offerCanvasGroups[index];
        
        // Store initial values before changing parent
        Vector3 startWorldPosition = rectTransform.position;
        Vector3 startLocalScale = rectTransform.localScale;
        float startOpacity = canvasGroup != null ? canvasGroup.alpha : 1f;
        
        // Change parent to target position transform (world position stays the same)
        rectTransform.SetParent(targetPosition, true);
        
        // Now get the current local position after parent change
        Vector3 startLocalPosition = rectTransform.localPosition;
        Vector3 endLocalPosition = Vector3.zero; // Position at the center of target transform
        Vector3 endLocalScale = Vector3.one * targetScale;
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            
            // Animate local position (relative to parent) to center it on the transform
            rectTransform.localPosition = Vector3.Lerp(startLocalPosition, endLocalPosition, t);
            
            // Animate scale
            rectTransform.localScale = Vector3.Lerp(startLocalScale, endLocalScale, t);
            
            // Animate opacity
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startOpacity, targetOpacity, t);
            }
            
            yield return null;
        }
        
        // Ensure final values
        rectTransform.localPosition = endLocalPosition;
        rectTransform.localScale = endLocalScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetOpacity;
        }
    }
    
    /// <summary>
    /// Scrolls to the next offer (right)
    /// </summary>
    public void ScrollRight()
    {
        Debug.Log($"[OffersCarouselManager] ScrollRight called. Current index: {currentSelectedIndex}, Total offers: {offerItems.Length}");
        
        if (currentSelectedIndex < offerItems.Length - 1)
        {
            int nextIndex = currentSelectedIndex + 1;
            Debug.Log($"[OffersCarouselManager] Scrolling to index: {nextIndex}");
            TransitionToOffer(nextIndex);
        }
        else
        {
            Debug.Log("[OffersCarouselManager] Already at the last offer, cannot scroll right");
        }
    }
    
    /// <summary>
    /// Scrolls to the previous offer (left)
    /// </summary>
    public void ScrollLeft()
    {
        Debug.Log($"[OffersCarouselManager] ScrollLeft called. Current index: {currentSelectedIndex}");
        
        if (currentSelectedIndex > 0)
        {
            int previousIndex = currentSelectedIndex - 1;
            Debug.Log($"[OffersCarouselManager] Scrolling to index: {previousIndex}");
            TransitionToOffer(previousIndex);
        }
        else
        {
            Debug.Log("[OffersCarouselManager] Already at the first offer, cannot scroll left");
        }
    }
    
    /// <summary>
    /// Scrolls by multiple positions with sequential animations (for swipe velocity-based scrolling)
    /// </summary>
    /// <param name="steps">Number of steps to scroll (positive = right, negative = left)</param>
    public void ScrollMultiple(int steps)
    {
        if (steps == 0) return;
        
        // Clamp steps to reasonable range
        int maxSteps = offerItems.Length - 1;
        steps = Mathf.Clamp(steps, -maxSteps, maxSteps);
        
        // Stop any existing scroll animation
        if (multiScrollCoroutine != null)
        {
            StopCoroutine(multiScrollCoroutine);
        }
        
        // Start sequential scroll animation
        multiScrollCoroutine = StartCoroutine(AnimateMultipleSteps(steps));
    }
    
    /// <summary>
    /// Coroutine that animates through each scroll step sequentially for smooth visual flow
    /// </summary>
    private IEnumerator AnimateMultipleSteps(int totalSteps)
    {
        int direction = totalSteps > 0 ? 1 : -1; // 1 = right, -1 = left
        int stepsRemaining = Mathf.Abs(totalSteps);
        
        // Calculate delay based on transition duration
        // Make it slightly faster than normal scroll for quick succession
        float stepDelay = transitionDuration * 1.5f;
        
        for (int i = 0; i < stepsRemaining; i++)
        {
            // Check boundaries before scrolling
            bool canScroll = false;
            
            if (direction > 0)
            {
                // Scrolling right
                if (currentSelectedIndex < offerItems.Length - 1)
                {
                    currentSelectedIndex++;
                    canScroll = true;
                }
            }
            else
            {
                // Scrolling left
                if (currentSelectedIndex > 0)
                {
                    currentSelectedIndex--;
                    canScroll = true;
                }
            }
            
            if (canScroll)
            {
                // Update positions with animation
                UpdateOfferPositions();
                
                // Play tap sound for each scroll step
                if (SoundManager.instance != null)
                {
                    SoundManager.instance.PlayTap();
                }
                
                // Wait for animation to complete before next step
                // Only wait if there are more steps to go
                if (i < stepsRemaining - 1)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }
            else
            {
                // Hit boundary, stop scrolling
                Debug.Log($"[OffersCarouselManager] Hit boundary at index {currentSelectedIndex}, stopping multi-scroll");
                break;
            }
        }
        
        multiScrollCoroutine = null;
    }
    
    /// <summary>
    /// Transitions to a specific offer index (called when dot button is clicked)
    /// </summary>
    public void TransitionToOffer(int index)
    {
        if (index < 0 || index >= offerItems.Length)
        {
            Debug.LogWarning($"[OffersCarouselManager] Invalid offer index: {index}");
            return;
        }
        
        if (index == currentSelectedIndex)
        {
            Debug.Log($"[OffersCarouselManager] Already at index {index}, skipping transition");
            return; // Already selected
        }
        
        Debug.Log($"[OffersCarouselManager] Transitioning to offer index: {index}");
        currentSelectedIndex = index;
        
        // Stop any existing transition
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        
        // Update positions
        UpdateOfferPositions();
    }
    
    /// <summary>
    /// Called when a dot button is clicked
    /// </summary>
    private void OnDotButtonClicked(int index)
    {
        TransitionToOffer(index);
    }
    
    /// <summary>
    /// Sets up dot button listeners
    /// </summary>
    private void SetupDotButtons()
    {
        for (int i = 0; i < dotButtons.Length; i++)
        {
            if (dotButtons[i] != null)
            {
                int index = i; // Capture index for closure
                dotButtons[i].onClick.RemoveAllListeners();
                dotButtons[i].onClick.AddListener(() => OnDotButtonClicked(index));
            }
        }
        
        UpdateDotButtons();
    }
    
    /// <summary>
    /// Updates dot button visual states (shows selected image on selected dot)
    /// </summary>
    private void UpdateDotButtons()
    {
        for (int i = 0; i < dotButtons.Length; i++)
        {
            if (dotButtons[i] != null)
            {
                Image dotImage = dotButtons[i].GetComponent<Image>();
                if (dotImage != null)
                {
                    if (i == currentSelectedIndex)
                    {
                        // Show selected image
                        if (selectedDotImage != null)
                        {
                            dotImage.sprite = selectedDotImage;
                        }
                        dotImage.color = Color.white; // Full opacity for selected
                    }
                    else
                    {
                        // Show unselected image
                        if (unselectedDotImage != null)
                        {
                            dotImage.sprite = unselectedDotImage;
                        }
                        // Keep current sprite if unselectedDotImage is null
                        dotImage.color = new Color(1f, 1f, 1f, 0.5f); // Lower opacity for unselected
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Sets up navigation button listeners
    /// </summary>
    private void SetupNavigationButtons()
    {
        if (leftNavButton != null)
        {
            leftNavButton.onClick.RemoveAllListeners();
            leftNavButton.onClick.AddListener(ScrollLeft);
            leftNavButton.interactable = true; // Ensure button is interactable
            Debug.Log($"[OffersCarouselManager] Left navigation button setup complete. Interactable: {leftNavButton.interactable}, Active: {leftNavButton.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[OffersCarouselManager] Left navigation button is not assigned!");
        }
        
        if (rightNavButton != null)
        {
            rightNavButton.onClick.RemoveAllListeners();
            rightNavButton.onClick.AddListener(ScrollRight);
            rightNavButton.interactable = true; // Ensure button is interactable
            Debug.Log($"[OffersCarouselManager] Right navigation button setup complete. Interactable: {rightNavButton.interactable}, Active: {rightNavButton.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[OffersCarouselManager] Right navigation button is not assigned!");
        }
    }
    
    /// <summary>
    /// Gets the currently selected offer index
    /// </summary>
    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }
    
    /// <summary>
    /// Gets the currently selected offer GameObject
    /// </summary>
    public GameObject GetCurrentSelectedOffer()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < offerItems.Length)
        {
            return offerItems[currentSelectedIndex];
        }
        return null;
    }
    
    private void OnDestroy()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
    }
    
    /// <summary>
    /// Gets all offer carousel items.
    /// </summary>
    public GameObject[] GetOfferItems()
    {
        return offerItems;
    }

    /// <summary>
    /// Editor helper: Refresh visual states (for testing in editor)
    /// </summary>
    [ContextMenu("Refresh Visual States")]
    private void RefreshVisualStates()
    {
        if (Application.isPlaying)
        {
            UpdateOfferPositions();
        }
    }
}
