using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Manages the lobby scene with 3 fixed lobby rows
/// Handles carousel scrolling with swipe and button controls
/// </summary>
public class LobbyManager : MonoBehaviour
{
    
    [Header("Lobby Rows (3 Fixed Positions)")]
    [SerializeField] private LobbyRowCellView upperRow; // Upper lobby row
    [SerializeField] private LobbyRowCellView middleRow; // Middle lobby row (main focus)
    [SerializeField] private LobbyRowCellView lowerRow; // Lower lobby row
    
    [Header("Scroll Controls")]
    [SerializeField] private Button scrollUpButton; // Button to scroll up (show previous items)
    [SerializeField] private Button scrollDownButton; // Button to scroll down (show next items)
    [SerializeField] private bool enableSwipeDetection = false; // Enable/disable swipe scrolling (set to false if using new Input System)
    
    [Header("Scroll Animation Settings")]
    [SerializeField] private float scrollAnimationDuration = 0.3f; // Duration of scroll animation
    [SerializeField] private Ease scrollEase = Ease.OutCubic; // Easing for scroll animation
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger scroll
    
    [Header("Carousel Scale Settings")]
    [SerializeField] private float middleRowScale = 1.0f; // Scale for middle (focused) row
    [SerializeField] private float sideRowScale = 0.85f; // Scale for upper and lower (side) rows
    [SerializeField] private bool enableCarouselScaling = true; // Enable/disable carousel scaling effect
    
    [Header("Gameplay Object")]
    [SerializeField] private GameObject gameplayObject; // Gameplay object to open when play button is clicked
    
    [Header("Lobby Game Objects Settings")]
    private const int BACKGROUND_COUNT = 12; // Total number of background sprites (0-11)
    
    [Header("Lobby Configurations")]
    [SerializeField] private LobbyConfig[] lobbyConfigurations = new LobbyConfig[12]; // 12 lobby configurations (can be set in inspector)
    
    // Lobby scroll management
    private LobbyGameObjectData[] _lobbyObjectsData; // Array of all 12 lobby objects
    private int currentScrollIndex = 0; // Current scroll position
    private bool isScrollAnimating = false; // Prevents multiple scrolls at once
    
    // Vertical Carousel - Dynamic Items
    private List<LobbyRowCellView> allLobbyItems = new List<LobbyRowCellView>();
    private Transform upperAnchor;
    private Transform middleAnchor;
    private Transform lowerAnchor;
    
    [Header("Carousel Visual Settings")]
    [SerializeField] private float sideOpacity = 0.5f; // Opacity for upper/lower rows
    
    [Header("Dot Navigation")]
    [SerializeField] private Button[] dotButtons; // Dot buttons for navigation
    [SerializeField] private Sprite selectedDotImage; // Image to show when dot is selected
    [SerializeField] private Sprite unselectedDotImage; // Image to show when dot is not selected


    
    // Swipe detection
    private Vector2 swipeStartPos;
    private bool isSwiping = false;
    
    // Multi-step scroll animation tracking
    private Coroutine multiScrollCoroutine = null;
    
    private void Start()
    {
        InitializeLobbyConfigurations();
        InitializeLobbyRows();
        SetupScrollButtons();
        
        // Removed InitializeButtonsRoutine from here, it is now called in OnEnable to handle re-enabling
    }
    
    private IEnumerator InitializeButtonsRoutine()
    {
        // Retry for 5 seconds
        float elapsed = 0f;
        while (elapsed < 5f)
        {
            RefreshAllPlayButtons();
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            
            // If logged in, we can slow down or stop if finding success? 
            // Better to just keep checking for a few seconds to catch the data load event.
        }
    }
    
    private void OnEnable()
    {
        // Ensure background music is playing if settings allow (e.g. returning from game)
        if (SettingManager.instance != null)
        {
            SettingManager.instance.ApplyMusicSetting();
        }

        // Refresh lobby rows when GameObject becomes active
        if (_lobbyObjectsData != null && _lobbyObjectsData.Length > 0)
        {
            UpdateVisibleRows();
        }

        // Start retry routine to ensure buttons light up when data is ready (e.g. after returning from game)
        StartCoroutine(InitializeButtonsRoutine());
    }
    
    private void Update()
    {
        // Swipe detection
        HandleSwipeInput();
    }
    

    
    #region Lobby Rows Management (3 Fixed Positions)
    
    /// <summary>
    /// Initializes the lobby items pool and anchors
    /// </summary>
    private void InitializeLobbyRows()
    {
        // 1. Setup Anchors from existing assignments
        if (upperRow != null) upperAnchor = upperRow.transform;
        if (middleRow != null) middleAnchor = middleRow.transform;
        if (lowerRow != null) lowerAnchor = lowerRow.transform;
        
        if (upperAnchor == null || middleAnchor == null || lowerAnchor == null)
        {
            Debug.LogError("[LobbyManager] Row references are missing! Cannot setup carousel.");
            return;
        }

        // 2. Create data for all lobby items
        _lobbyObjectsData = new LobbyGameObjectData[BACKGROUND_COUNT];
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            _lobbyObjectsData[i] = new LobbyGameObjectData(i, i);
        }
        
        // 3. Instantiate Layout Items (Pool)
        // Use middleRow as the template prefab since it's the main focused one
        LobbyRowCellView prefab = middleRow;
        
        // Prepare container
        // Clear existing list
        allLobbyItems.Clear();
        
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            // Instantiate new item
            LobbyRowCellView newItem = Instantiate(prefab);
            newItem.name = $"LobbyItem_{i}";
            
            // Setup Data
            newItem.SetData(_lobbyObjectsData[i]);
            
            // Ensure it has CanvasGroup for fading
            if (newItem.GetComponent<CanvasGroup>() == null)
            {
                newItem.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Add to list
            allLobbyItems.Add(newItem);
        }
        
        // 4. Disable Original Rows (Turn them into invisible anchors)
        DisableOriginalRow(upperRow);
        DisableOriginalRow(middleRow);
        DisableOriginalRow(lowerRow);
        
        // Start at first position
        currentScrollIndex = 0;
        
        // Pre-position all items immediately to avoid "flying in from zero"
        // 1. Determine active indices
        int centerIndex = 0;
        int upperIndex = BACKGROUND_COUNT - 1; 
        int lowerIndex = 1;
        
        for (int i = 0; i < allLobbyItems.Count; i++)
        {
            var item = allLobbyItems[i];
            
            // If active, snap to its visible anchor
            if (i == centerIndex) SnapItemToPosition(item, middleAnchor, middleRowScale, 1f);
            else if (i == upperIndex) SnapItemToPosition(item, upperAnchor, sideRowScale, sideOpacity);
            else if (i == lowerIndex) SnapItemToPosition(item, lowerAnchor, sideRowScale, sideOpacity);
            
            // If inactive, snap to the closer anchor ("stacking" them)
            // Items > 1 are "below", Items < 0 (wrapped) are "above"
            // For the initial list 0,1,2..., items 2-11 are "below" so we stack them at lowerAnchor
            else 
            {
               item.gameObject.SetActive(false);
               SnapItemToPosition(item, lowerAnchor, sideRowScale, 0f);
            }
        }
        
        // Initial Position Update (Instant)
        UpdateLobbyPositions(true, 0);
        
        // Setup Dots
        SetupDotButtons();
        UpdateDotButtons();
        
        Debug.Log($"[LobbyManager] Initialized {BACKGROUND_COUNT} dynamic lobby items for vertical carousel");
    }
    
    private void DisableOriginalRow(LobbyRowCellView row)
    {
        if (row != null)
        {
            // Disable visuals but keep GameObject active for Transform anchor
            if (row.GetComponent<Image>() != null) row.GetComponent<Image>().enabled = false;
            if (row.GetComponent<CanvasGroup>() != null) row.GetComponent<CanvasGroup>().alpha = 0f;
            row.enabled = false; // Disable the script
            
            // Hide children if any (clean anchor)
            foreach(Transform child in row.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Sets up scroll button click listeners
    /// </summary>
    private void SetupScrollButtons()
    {
        if (scrollUpButton != null)
        {
            scrollUpButton.onClick.AddListener(ScrollUp);
        }
        
        if (scrollDownButton != null)
        {
            scrollDownButton.onClick.AddListener(ScrollDown);
        }
    }
    
    /// <summary>
    /// Updates positions of all lobby items based on current scroll index
    /// </summary>
    /// <param name="instant">If true, snaps to position without animation</param>
    private void UpdateVisibleRows()
    {
        // Wrapper for compatibility with existing calls
        UpdateLobbyPositions(false, 0);
    }
    
    private void UpdateLobbyPositions(bool instant = false, int scrollDirection = 0)
    {
        if (allLobbyItems == null || allLobbyItems.Count == 0) return;
        
        int centerIndex = GetWrappedIndex(currentScrollIndex);
        int upperIndex = GetWrappedIndex(currentScrollIndex - 1);
        int lowerIndex = GetWrappedIndex(currentScrollIndex + 1);
        
        for (int i = 0; i < allLobbyItems.Count; i++)
        {
             LobbyRowCellView item = allLobbyItems[i];
             if (item == null) continue;
             
             Transform targetAnchor = null;
             float targetScale = sideRowScale;
             float targetOpacity = 0f;
             bool isVisible = false;
             bool isEntering = false;
             
             // Determine state
             if (i == centerIndex)
             {
                 targetAnchor = middleAnchor;
                 targetScale = middleRowScale;
                 targetOpacity = 1f;
                 isVisible = true;
             }
             else if (i == upperIndex)
             {
                 targetAnchor = upperAnchor;
                 targetScale = sideRowScale;
                 targetOpacity = sideOpacity;
                 isVisible = true;
                 
                 // If scrolling UP (direction -1), this slot is being filled by a new item coming from above
                 if (scrollDirection < 0 && !item.gameObject.activeSelf) isEntering = true;
             }
             else if (i == lowerIndex)
             {
                 targetAnchor = lowerAnchor;
                 targetScale = sideRowScale;
                 targetOpacity = sideOpacity;
                 isVisible = true;
                 
                 // If scrolling DOWN (direction 1), this slot is being filled by a new item coming from below
                 if (scrollDirection > 0 && !item.gameObject.activeSelf) isEntering = true;
             }
             
             if (isVisible && targetAnchor != null)
             {
                 // Smart Snap for new entrants:
                 if (isEntering && !instant)
                 {
                     SnapItemToPosition(item, targetAnchor, sideRowScale * 0.5f, 0f);
                 }
                 
                 item.gameObject.SetActive(true);
                 
                 // Refresh interactability state immediately when item becomes visible/updating
                 item.RefreshPlayButtonsInteractability();
                 
                 // Update visual selection state (Overlay)
                 // Only center item is "Selected", others are "Unselected"
                 bool isSelected = (i == centerIndex);
                 item.SetSelectedState(isSelected);

                 if (instant)
                 {
                     SnapItemToPosition(item, targetAnchor, targetScale, targetOpacity);
                 }
                 else
                 {
                     StartCoroutine(AnimateItemToPosition(item, targetAnchor, targetScale, targetOpacity));
                 }
             }
             else
             {
                 // When hiding an item...
                 item.gameObject.SetActive(false);
             }
        }
        
        // Update Dots
        UpdateDotButtons();
    }
    
    private void SnapItemToPosition(LobbyRowCellView item, Transform anchor, float scale, float opacity)
    {
        item.transform.SetParent(anchor, false);
        
        // Calculate screen scale factor to ensure children resize correctly
        float screenRatio = GetScreenScaleFactor();
        
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Center the item - do NOT stretch, let localScale handle the sizing
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            
            // Note: We keep the original SizeDelta of the prefab here
        }
        
        item.transform.localPosition = Vector3.zero;
        
        // Apply both the carousel scale AND the screen resolution scale
        item.transform.localScale = Vector3.one * scale * screenRatio;
        
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = opacity;
    }

    private float GetScreenScaleFactor()
    {
        // Reference resolution from LobbyScaler
        float refWidth = 1440f;
        float refHeight = 3216f;
        
        float scaleX = Screen.width / refWidth;
        float scaleY = Screen.height / refHeight;
        
        // Use the same logic as LobbyScaler to ensure consistency
        // If screen is smaller than reference, we shrink. 
        // We typically want to fit the width for portrait UI.
        return Mathf.Min(scaleX, scaleY);
    }

    private IEnumerator AnimateItemToPosition(LobbyRowCellView item, Transform anchor, float scale, float opacity)
    {
        // Kill previous animations
        item.transform.DOKill();
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg != null) cg.DOKill();

        // Reparent first to ensure correct draw order/context
        // true = keep world position, but we want smooth local move
        item.transform.SetParent(anchor, true);
        
        // Calculate screen scale to ensure children resize correctly
        float screenRatio = GetScreenScaleFactor();

        // Animate Local Move to Zero (Anchor Center)
        item.transform.DOLocalMove(Vector3.zero, scrollAnimationDuration).SetEase(scrollEase);
        
        // Animate Scale (Carousel Scale * Screen Ratio)
        item.transform.DOScale(scale * screenRatio, scrollAnimationDuration).SetEase(scrollEase);
        
        // Animate Opacity
        if (cg != null)
        {
            cg.DOFade(opacity, scrollAnimationDuration);
        }
        
        yield return null;
    }

    /// <summary>
    /// Gets a wrapped index (loops around the array)
    /// </summary>
    private int GetWrappedIndex(int index)
    {
        int wrappedIndex = index % BACKGROUND_COUNT;
        if (wrappedIndex < 0)
        {
            wrappedIndex += BACKGROUND_COUNT;
        }
        return wrappedIndex;
    }
    
    // Obsolete - functionality moved to UpdateLobbyPositions
    private void ApplyCarouselScales(bool animate) { }
    
    /// <summary>
    /// Scrolls up (shows previous items)
    /// </summary>
    public void ScrollUp()
    {
        // Scroll Up means we want to see "Index - 1"
        // In the carousel visualization:
        // Current: I
        // Up: I-1
        // So we decrement index
        
        // Debounce if needed, or allow rapid scrolling?
        // OffersCarouselManager allows overwriting transition.
        
        currentScrollIndex--;
        if (currentScrollIndex < 0) currentScrollIndex = BACKGROUND_COUNT - 1;
        
        UpdateLobbyPositions(false, -1);
    }
    
    /// <summary>
    /// Scrolls down (shows next items)
    /// </summary>
    public void ScrollDown()
    {
        currentScrollIndex++;
        if (currentScrollIndex >= BACKGROUND_COUNT) currentScrollIndex = 0;
        
        UpdateLobbyPositions(false, 1);
    }
    
    /// <summary>
    /// Scrolls by multiple positions with sequential animations (for swipe velocity-based scrolling)
    /// </summary>
    /// <param name="steps">Number of steps to scroll (positive = down, negative = up)</param>
    public void ScrollMultiple(int steps)
    {
        if (steps == 0) return;
        
        // Clamp steps to reasonable range to prevent excessive scrolling
        steps = Mathf.Clamp(steps, -BACKGROUND_COUNT, BACKGROUND_COUNT);
        
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
        int direction = totalSteps > 0 ? 1 : -1;
        int stepsRemaining = Mathf.Abs(totalSteps);
        
        // Calculate delay based on animation duration
        // Make it slightly faster than normal scroll for quick succession
        float stepDelay = scrollAnimationDuration * 0.7f;
        
        for (int i = 0; i < stepsRemaining; i++)
        {
            // Perform one scroll step
            if (direction > 0)
            {
                currentScrollIndex++;
                if (currentScrollIndex >= BACKGROUND_COUNT) currentScrollIndex = 0;
            }
            else
            {
                currentScrollIndex--;
                if (currentScrollIndex < 0) currentScrollIndex = BACKGROUND_COUNT - 1;
            }
            
            // Update positions with animation
            UpdateLobbyPositions(false, direction);
            
            // Play tap sound for each scroll step
            if (SettingManager.instance != null)
            {
                SettingManager.instance.PlayTapSound();
            }
            
            // Wait for animation to complete before next step
            // Only wait if there are more steps to go
            if (i < stepsRemaining - 1)
            {
                yield return new WaitForSeconds(stepDelay);
            }
        }
        
        multiScrollCoroutine = null;
    }
    
    // Obsolete
    private IEnumerator AnimateScroll(int direction)
    {
        yield break;
    }
    
    /// <summary>
    /// Handles swipe input detection using Touch API (works with both old and new Input System)
    /// </summary>
    private void HandleSwipeInput()
    {
        // Skip if swipe detection is disabled
        if (!enableSwipeDetection)
        {
            return;
        }
        
        // Use Touch API for mobile/touch devices (works with both input systems)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                swipeStartPos = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                Vector2 swipeEndPos = touch.position;
                Vector2 swipeDelta = swipeEndPos - swipeStartPos;
                
                // Check if swipe is long enough
                if (Mathf.Abs(swipeDelta.y) > swipeThreshold)
                {
                    // Swipe up = scroll down (show next items)
                    if (swipeDelta.y < 0)
                    {
                        ScrollDown();
                    }
                    // Swipe down = scroll up (show previous items)
                    else
                    {
                        ScrollUp();
                    }
                }
                
                isSwiping = false;
            }
        }
    }
    
    /// <summary>
    /// Initializes lobby configurations with default values if not set in inspector
    /// </summary>
    private void InitializeLobbyConfigurations()
    {
        if (lobbyConfigurations == null || lobbyConfigurations.Length != BACKGROUND_COUNT)
        {
            lobbyConfigurations = new LobbyConfig[BACKGROUND_COUNT];
        }
        
        // Initialize any null configurations with default values
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            if (lobbyConfigurations[i] == null)
            {
                // Default configuration: 2 players (1 player + 1 CPU), entry fee 10 chips, prizes 20/10/5 chips
                lobbyConfigurations[i] = new LobbyConfig(
                    index: i,
                    players: 2,
                    entryCash: 0f,
                    entryChips: 10,
                    firstCash: 0f,
                    firstChips: 20,
                    secondCash: 0f,
                    secondChips: 10,
                    thirdCash: 0f,
                    thirdChips: 5
                );
                Debug.Log($"[LobbyManager] Initialized default configuration for Lobby {i + 1}");
            }
        }
        
        Debug.Log($"[LobbyManager] Initialized {BACKGROUND_COUNT} lobby configurations");
    }
    
    /// <summary>
    /// Gets the lobby configuration for a specific lobby index
    /// </summary>
    public LobbyConfig GetLobbyConfig(int lobbyIndex)
    {
        if (lobbyConfigurations != null && lobbyIndex >= 0 && lobbyIndex < lobbyConfigurations.Length)
        {
            return lobbyConfigurations[lobbyIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Checks if the player can afford the entry fee for a specific lobby
    /// </summary>
    public bool CanPlayerAffordEntryFee(int lobbyIndex)
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("CanPlayerAffordEntryFee: PlayFabManager is null");
            return false;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("CanPlayerAffordEntryFee: Not logged in");
            return false;
        }
        
        LobbyConfig config = GetLobbyConfig(lobbyIndex);
        if (config == null)
        {
            Debug.LogError($"CanPlayerAffordEntryFee: No config for index {lobbyIndex}");
            return false; 
        }
        
        // Log values for debugging
        var data = PlayFabManager.Instance.currentPlayerData;
        // Debug.Log($"Checking Affordability for Lobby {lobbyIndex}: Cash({data.cash}/{config.entryFeeCash}), Chips({data.coins}/{config.entryFeeChips})");
        
        // Check if player has enough cash
        if (config.entryFeeCash > 0f)
        {
            if (data.cash < config.entryFeeCash)
            {
                // Debug.LogWarning($"Can't afford Cash: Has {data.cash}, Needs {config.entryFeeCash}");
                return false; 
            }
        }
        
        // Check if player has enough chips
        if (config.entryFeeChips > 0)
        {
            if (data.coins < config.entryFeeChips)
            {
                // Debug.LogWarning($"Can't afford Chips: Has {data.coins}, Needs {config.entryFeeChips}");
                return false; 
            }
        }
        
        return true; 
    }
    
    /// <summary>
    /// Sets the lobby configuration for a specific lobby index
    /// </summary>
    public void SetLobbyConfig(int lobbyIndex, LobbyConfig config)
    {
        if (lobbyConfigurations != null && lobbyIndex >= 0 && lobbyIndex < lobbyConfigurations.Length)
        {
            lobbyConfigurations[lobbyIndex] = config;
            Debug.Log($"[LobbyManager] Updated configuration for Lobby {lobbyIndex + 1}");
        }
    }
    
    /// <summary>
    /// Opens the gameplay object with the specified lobby configuration
    /// </summary>
    /// <param name="lobbyIndex">Index of the lobby (0-12)</param>
    public void OpenGameplayObject(int lobbyIndex = -1)
    {
        if (gameplayObject != null)
        {
            // Get the lobby configuration
            LobbyConfig config = null;
            if (lobbyIndex >= 0 && lobbyIndex < BACKGROUND_COUNT)
            {
                config = GetLobbyConfig(lobbyIndex);
            }
            
            // Configure GameManager with lobby settings before opening gameplay
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && config != null)
            {
                // Ensure gameplay screen is inactive first to ensure proper reset
                if (gameplayObject.activeSelf)
                {
                    gameplayObject.SetActive(false);
                }
                
                // Configure with new lobby settings
                gameManager.ConfigureGameFromLobby(config);
                Debug.Log($"[LobbyManager] Configured game for Lobby {lobbyIndex }: {config.numberOfPlayers} players, Entry: {config.entryFeeChips} chips / {config.entryFeeCash} cash");
            }
            else if (gameManager == null)
            {
                Debug.LogWarning("[LobbyManager] GameManager not found! Game will start with default settings.");
            }
            else if (config == null)
            {
                Debug.LogWarning($"[LobbyManager] Lobby configuration not found for index {lobbyIndex}! Game will start with default settings.");
            }
            
            // Activate gameplay screen
            gameplayObject.SetActive(true);
            Debug.Log($"[LobbyManager] Gameplay object opened for Lobby {lobbyIndex + 1}");
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Gameplay object is not assigned! Please assign it in the inspector.");
        }
    }
    
    /// <summary>
    /// Refreshes all play buttons interactability based on player's current currency
    /// Call this when player's cash or chips change
    /// </summary>
    public void RefreshAllPlayButtons()
    {
        if (allLobbyItems != null)
        {
            foreach(var item in allLobbyItems)
            {
                if (item != null && item.gameObject.activeSelf)
                {
                    item.RefreshPlayButtonsInteractability();
                }
            }
        }
        
        Debug.Log("[LobbyManager] Refreshed all visible play buttons interactability");
    }
    
    #endregion
    

    /// <summary>
    /// Sets up dot button listeners
    /// </summary>
    private void SetupDotButtons()
    {
        if (dotButtons == null) return;
        
        for (int i = 0; i < dotButtons.Length; i++)
        {
            if (dotButtons[i] != null)
            {
                int index = i; // Move to local scope for closure
                dotButtons[i].onClick.RemoveAllListeners();
                dotButtons[i].onClick.AddListener(() => OnDotButtonClicked(index));
            }
        }
    }

    /// <summary>
    /// Called when a dot button is clicked
    /// </summary>
    private void OnDotButtonClicked(int targetIndex)
    {
        // Play click sound
        if (SettingManager.instance != null)
        {
            SettingManager.instance.PlayTapSound();
        }
        
        if (targetIndex == currentScrollIndex) return;
        
        // Determine direction for smooth scroll visual
        // If clicking a dot "below" current, scroll down. "Above" scroll up.
        int direction = (targetIndex > currentScrollIndex) ? 1 : -1;
        
        // Handle wrap-around cases for shorter path? 
        // For simplicity, just jump to it. OR scroll nicely if close.
        
        // Update index
        currentScrollIndex = targetIndex;
        // Clamp
        if (currentScrollIndex < 0) currentScrollIndex = 0;
        if (currentScrollIndex >= BACKGROUND_COUNT) currentScrollIndex = BACKGROUND_COUNT - 1;
        
        UpdateLobbyPositions(false, direction);
    }

    /// <summary>
    /// Updates dot button visual states with scale animation
    /// </summary>
    private void UpdateDotButtons()
    {
        if (dotButtons == null) return;
        
        int activeDotIndex = currentScrollIndex;
        
        for (int i = 0; i < dotButtons.Length; i++)
        {
            if (dotButtons[i] != null)
            {
                Image dotImage = dotButtons[i].GetComponent<Image>();
                RectTransform dotRect = dotButtons[i].GetComponent<RectTransform>();
                
                if (dotImage != null)
                {
                    bool isSelected = (i == activeDotIndex);
                     
                    if (isSelected)
                    {
                        if (selectedDotImage != null) dotImage.sprite = selectedDotImage;
                        dotImage.color = Color.white;
                        
                        // Animate Scale Up
                        if (dotRect != null)
                        {
                            dotRect.DOKill();
                            dotRect.DOScale(1.4f, 0.2f).SetEase(Ease.OutBack);
                        }
                    }
                    else
                    {
                        if (unselectedDotImage != null) dotImage.sprite = unselectedDotImage;
                        dotImage.color = (unselectedDotImage == null) ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
                        
                        // Animate Scale Down
                        if (dotRect != null)
                        {
                            dotRect.DOKill();
                            dotRect.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad);
                        }
                    }
                }
            }
        }
    }
}

