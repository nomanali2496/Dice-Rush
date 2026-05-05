using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Manages the lobby scene with a single horizontal position
/// Handles carousel scrolling with swipe and button controls
/// </summary>
public class HorizontalLobbyManager : MonoBehaviour
{
    
    [Header("Lobby Row (Single Horizontal Position)")]
    [SerializeField] private LobbyRowCellView centerRow; // Center lobby row (main focus)
    
    [Header("Scroll Controls")]
    [SerializeField] private Button scrollLeftButton; // Button to scroll left (show previous items)
    [SerializeField] private Button scrollRightButton; // Button to scroll right (show next items)
    [SerializeField] private bool enableSwipeDetection = false; // Enable/disable swipe scrolling
    
    [Header("Scroll Animation Settings")]
    [SerializeField] private float scrollAnimationDuration = 0.3f; // Duration of scroll animation
    [SerializeField] private Ease scrollEase = Ease.OutCubic; // Easing for scroll animation
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger scroll
    [SerializeField] private float slideDistance = 1500f; // Horizontal distance to slide off-screen
    
    [Header("Carousel Scale Settings")]
    [SerializeField] private float centerRowScale = 1.0f; // Scale for center row
    
    [Header("Gameplay Object")]
    [SerializeField] private GameObject gameplayObject; // Gameplay object to open when play button is clicked
    
    [Header("Matchmaking UI")]
    [SerializeField] private GameObject waitingPanel; // Panel to show while finding match
    [SerializeField] private TMPro.TextMeshProUGUI waitingStatusText; // Text to update with player count
    
    [Header("Lobby Game Objects Settings")]
    private const int BACKGROUND_COUNT = 12; // Total number of background sprites (0-11)
    
    // Lobby scroll management
    private LobbyGameObjectData[] _lobbyObjectsData; // Array of all 12 lobby objects
    private int currentScrollIndex = 0; // Current scroll position
    private bool isScrollAnimating = false; // Prevents multiple scrolls at once
    
    // Horizontal Carousel - Dynamic Items
    private List<LobbyRowCellView> allLobbyItems = new List<LobbyRowCellView>();
    private Transform centerAnchor;
    
    [Header("Dot Navigation")]
    [SerializeField] private Button[] dotButtons; // Dot buttons for navigation
    [SerializeField] private Sprite selectedDotImage; // Image to show when dot is selected
    [SerializeField] private Sprite unselectedDotImage; // Image to show when dot is not selected


    
    // Swipe detection
    private Vector2 swipeStartPos;
    private bool isSwiping = false;
    
    // Multi-step scroll animation tracking
    private Coroutine multiScrollCoroutine = null;
    
    // Auto-select flag to prevent re-selecting on every enable
    private bool hasAutoSelectedOnce = false;
    
    // Tracks the lobby they are currently matchmaking for
    private int _pendingLobbyIndex = -1;
    
    private void Start()
    {
        LobbyModel.OnConfigurationsUpdated += RefreshAllPlayButtons;
        MatchmakingModel.OnMatchFound += HandleMatchFound;
        MatchmakingModel.OnMatchmakingFailed += HandleMatchError;
        MatchmakingModel.OnMatchmakingUpdated += HandleMatchmakingUpdated;
        
        LobbyModel.FetchConfigurations();
        
        InitializeLobbyRows();
        SetupScrollButtons();
        
        // Subscribe to PlayerModel events to update when backend data loads
        PlayerModel.OnDataUpdated += OnPlayerDataUpdated;
    }
    
    private IEnumerator InitializeButtonsRoutine()
    {
        // Retry for 5 seconds
        float elapsed = 0f;
        
        while (elapsed < 5f)
        {
            RefreshAllPlayButtons();
            
            // Auto-select lobby based on balance (only on first load)
            if (!hasAutoSelectedOnce)
            {
                // Check if player data is actually populated
                bool dataLooksLoaded = (PlayerModel.CashBalance > 0 || PlayerModel.Coins > 0);
                
                if (dataLooksLoaded)
                {
                    Debug.Log($"[HorizontalLobbyManager] Player data loaded after {elapsed}s - Cash: ${PlayerModel.CashBalance}, Chips: {PlayerModel.Coins}");
                    AutoSelectLobbyBasedOnBalance();
                    hasAutoSelectedOnce = true; // Mark as done, don't auto-select again
                }
                else if (elapsed < 4.5f)
                {
                    Debug.Log($"[HorizontalLobbyManager] Waiting for player data to load... (elapsed: {elapsed}s, Cash: ${PlayerModel.CashBalance}, Chips: {PlayerModel.Coins})");
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        
        // Final attempt after 5 seconds even if data shows zeros
        if (!hasAutoSelectedOnce)
        {
            Debug.LogWarning($"[HorizontalLobbyManager] Final auto-select attempt after 5 seconds with Cash: ${PlayerModel.CashBalance}, Chips: {PlayerModel.Coins}");
            AutoSelectLobbyBasedOnBalance();
            hasAutoSelectedOnce = true;
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

    private void OnDestroy()
    {
        LobbyModel.OnConfigurationsUpdated -= RefreshAllPlayButtons;
        MatchmakingModel.OnMatchFound -= HandleMatchFound;
        MatchmakingModel.OnMatchmakingFailed -= HandleMatchError;
        MatchmakingModel.OnMatchmakingUpdated -= HandleMatchmakingUpdated;
        
        // Unsubscribe from events
        PlayerModel.OnDataUpdated -= OnPlayerDataUpdated;
    }
    
    /// <summary>
    /// Event handler for when PlayerModel data is updated from backend
    /// Ensures lobby buttons refresh immediately even if internet was slow
    /// </summary>
    private void OnPlayerDataUpdated()
    {
        Debug.Log($"[HorizontalLobbyManager] Player data updated event received. Refreshing all play buttons...");
        RefreshAllPlayButtons();
        
        // If we haven't auto-selected yet, do it now that data is definitely ready
        if (!hasAutoSelectedOnce)
        {
             AutoSelectLobbyBasedOnBalance();
             hasAutoSelectedOnce = true;
        }
    }
    
    private void Update()
    {
        // Swipe detection
        HandleSwipeInput();
    }
    

    
    #region Lobby Rows Management (Single Horizontal Position)
    
    /// <summary>
    /// Initializes the lobby items pool and anchors
    /// </summary>
    private void InitializeLobbyRows()
    {
        // 1. Setup Anchors from existing assignments
        if (centerRow != null) centerAnchor = centerRow.transform;
        
        if (centerAnchor == null)
        {
            Debug.LogError("[HorizontalLobbyManager] Row reference is missing! Cannot setup carousel.");
            return;
        }

        // 2. Create data for all lobby items
        _lobbyObjectsData = new LobbyGameObjectData[BACKGROUND_COUNT];
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            _lobbyObjectsData[i] = new LobbyGameObjectData(i, i);
        }
        
        // 3. Instantiate Layout Items (Pool)
        LobbyRowCellView prefab = centerRow;
        
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
            
            // By default hide off screen
            newItem.gameObject.SetActive(false);
            SnapItemToPosition(newItem, centerAnchor, new Vector3(slideDistance, 0, 0), centerRowScale, 0f);
        }
        
        // 4. Disable Original Rows (Turn them into invisible anchors)
        DisableOriginalRow(centerRow);
        
        // Start at first position
        currentScrollIndex = 0;
        
        // Initial Position Update (Instant)
        UpdateLobbyPositions(true, 0);
        
        // Setup Dots
        SetupDotButtons();
        UpdateDotButtons();
        
        Debug.Log($"[HorizontalLobbyManager] Initialized {BACKGROUND_COUNT} dynamic lobby items for horizontal carousel");
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
        if (scrollLeftButton != null)
        {
            scrollLeftButton.onClick.AddListener(ScrollLeft);
        }
        
        if (scrollRightButton != null)
        {
            scrollRightButton.onClick.AddListener(ScrollRight);
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
        
        for (int i = 0; i < allLobbyItems.Count; i++)
        {
             LobbyRowCellView item = allLobbyItems[i];
             if (item == null) continue;
             
             bool isSelected = (i == centerIndex);

             // If this is the new center item
             if (isSelected)
             {
                 item.gameObject.SetActive(true);
                 item.RefreshPlayButtonsInteractability();
                 item.SetSelectedState(true);

                 if (instant)
                 {
                     SnapItemToPosition(item, centerAnchor, Vector3.zero, centerRowScale, 1f);
                 }
                 else
                 {
                     if (scrollDirection != 0 && !isScrollAnimating)
                     {
                         // New item comes from the side based on scroll direction
                         // Scroll Right (direction > 0) means items come from right (+slideDistance)
                         // Scroll Left (direction < 0) means items come from left (-slideDistance)
                         float startX = scrollDirection > 0 ? slideDistance : -slideDistance;
                         SnapItemToPosition(item, centerAnchor, new Vector3(startX, 0, 0), centerRowScale, 0f);
                     }
                     StartCoroutine(AnimateItemToPosition(item, centerAnchor, Vector3.zero, centerRowScale, 1f));
                 }
             }
             else
             {
                 item.SetSelectedState(false);
                 
                 // Check if it was previously in the center to animate out
                 bool wasPreviousCenter = false;
                 if (!instant && scrollDirection != 0)
                 {
                     int previousCenterIndex = GetWrappedIndex(currentScrollIndex - scrollDirection);
                     if (i == previousCenterIndex)
                     {
                         wasPreviousCenter = true;
                     }
                 }

                 if (wasPreviousCenter)
                 {
                     // Animate out to the opposite side
                     float targetX = scrollDirection > 0 ? -slideDistance : slideDistance;
                     StartCoroutine(AnimateItemOutOfView(item, centerAnchor, new Vector3(targetX, 0, 0), centerRowScale, 0f));
                 }
                 else
                 {
                     // Hide immediately if not animating
                     item.gameObject.SetActive(false);
                 }
             }
        }
        
        // Update Dots
        UpdateDotButtons();
    }
    
    private void SnapItemToPosition(LobbyRowCellView item, Transform anchor, Vector3 localPos, float scale, float opacity)
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
        }
        
        item.transform.localPosition = localPos;
        
        // Apply both the carousel scale AND the screen resolution scale
        item.transform.localScale = Vector3.one * scale * screenRatio;
        
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = opacity;
    }

    private float GetScreenScaleFactor()
    {
        // Reference resolution from LobbyScaler (assumed to be landscape or logic reused)
        float refWidth = 1440f;
        float refHeight = 3216f;
        
        float scaleX = Screen.width / refWidth;
        float scaleY = Screen.height / refHeight;
        
        return Mathf.Min(scaleX, scaleY);
    }

    private IEnumerator AnimateItemToPosition(LobbyRowCellView item, Transform anchor, Vector3 targetPos, float scale, float opacity)
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

        // Animate Local Move
        item.transform.DOLocalMove(targetPos, scrollAnimationDuration).SetEase(scrollEase);
        
        // Animate Scale (Carousel Scale * Screen Ratio)
        item.transform.DOScale(scale * screenRatio, scrollAnimationDuration).SetEase(scrollEase);
        
        // Animate Opacity
        if (cg != null)
        {
            cg.DOFade(opacity, scrollAnimationDuration);
        }
        
        yield return new WaitForSeconds(scrollAnimationDuration);
    }

    private IEnumerator AnimateItemOutOfView(LobbyRowCellView item, Transform anchor, Vector3 targetPos, float scale, float opacity)
    {
        item.transform.DOKill();
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg != null) cg.DOKill();

        item.transform.SetParent(anchor, true);
        
        float screenRatio = GetScreenScaleFactor();

        item.transform.DOLocalMove(targetPos, scrollAnimationDuration).SetEase(scrollEase);
        item.transform.DOScale(scale * screenRatio, scrollAnimationDuration).SetEase(scrollEase);
        
        if (cg != null)
        {
            cg.DOFade(opacity, scrollAnimationDuration);
        }
        
        yield return new WaitForSeconds(scrollAnimationDuration);
        item.gameObject.SetActive(false);
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
    
    // Auto Select Logic remains identical as LobbyManager.cs
    private int FindFirstAffordableCashLobby(float playerCash)
    {
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            LobbyConfig config = GetLobbyConfig(i);
            if (config != null && config.entryFeeCash > 0f && playerCash >= config.entryFeeCash)
                return i;
        }
        return -1;
    }
    
    private int FindFirstAffordableChipsOnlyLobby(int playerChips)
    {
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            LobbyConfig config = GetLobbyConfig(i);
            if (config != null && config.entryFeeCash == 0f && config.entryFeeChips > 0 && playerChips >= config.entryFeeChips)
                return i;
        }
        return -1;
    }
    
    private void AutoSelectLobbyBasedOnBalance()
    {
        int selectedLobbyIndex = FindFirstAffordableCashLobby(PlayerModel.CashBalance);
        if (selectedLobbyIndex < 0) selectedLobbyIndex = FindFirstAffordableChipsOnlyLobby(PlayerModel.Coins);
        
        if (selectedLobbyIndex >= 0 && selectedLobbyIndex != currentScrollIndex)
        {
            currentScrollIndex = selectedLobbyIndex;
            UpdateLobbyPositions(true, 0); 
            UpdateDotButtons();
        }
    }
    
    /// <summary>
    /// Scrolls Left (shows previous items)
    /// </summary>
    public void ScrollLeft()
    {
        currentScrollIndex--;
        if (currentScrollIndex < 0) currentScrollIndex = BACKGROUND_COUNT - 1;
        
        UpdateLobbyPositions(false, -1);
    }
    
    /// <summary>
    /// Scrolls Right (shows next items)
    /// </summary>
    public void ScrollRight()
    {
        currentScrollIndex++;
        if (currentScrollIndex >= BACKGROUND_COUNT) currentScrollIndex = 0;
        
        UpdateLobbyPositions(false, 1);
    }
    
    /// <summary>
    /// Scrolls by multiple positions with sequential animations (for swipe velocity-based scrolling)
    /// </summary>
    /// <param name="steps">Number of steps to scroll (positive = right, negative = left)</param>
    public void ScrollMultiple(int steps)
    {
        if (steps == 0) return;
        
        steps = Mathf.Clamp(steps, -BACKGROUND_COUNT, BACKGROUND_COUNT);
        
        if (multiScrollCoroutine != null)
        {
            StopCoroutine(multiScrollCoroutine);
        }
        
        multiScrollCoroutine = StartCoroutine(AnimateMultipleSteps(steps));
    }
    
    private IEnumerator AnimateMultipleSteps(int totalSteps)
    {
        int direction = totalSteps > 0 ? 1 : -1;
        int stepsRemaining = Mathf.Abs(totalSteps);
        
        float stepDelay = scrollAnimationDuration * 0.7f;
        
        for (int i = 0; i < stepsRemaining; i++)
        {
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
            
            UpdateLobbyPositions(false, direction);
            
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlayTap();
            }
            
            if (i < stepsRemaining - 1)
            {
                yield return new WaitForSeconds(stepDelay);
            }
        }
        
        multiScrollCoroutine = null;
    }
    
    /// <summary>
    /// Handles swipe input detection using horizontal swipes
    /// </summary>
    private void HandleSwipeInput()
    {
        if (!enableSwipeDetection) return;
        
        // Handle Touch Input
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
                ProcessSwipeCompletion(touch.position);
            }
        }
        // Handle Mouse Input (for Unity Editor testing)
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                swipeStartPos = Input.mousePosition;
                isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && isSwiping)
            {
                ProcessSwipeCompletion(Input.mousePosition);
            }
        }
    }

    private void ProcessSwipeCompletion(Vector2 swipeEndPos)
    {
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        
        // Check if horizontal swipe is long enough
        if (Mathf.Abs(swipeDelta.x) > swipeThreshold && Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            // Swipe Left = scroll right (show next items)
            if (swipeDelta.x < 0)
            {
                ScrollRight();
            }
            // Swipe Right = scroll left (show previous items)
            else
            {
                ScrollLeft();
            }
        }
        
        isSwiping = false;
    }
    
    public LobbyConfig GetLobbyConfig(int lobbyIndex)
    {
        return LobbyModel.GetConfig(lobbyIndex);
    }
    
    public bool CanPlayerAffordEntryFee(int lobbyIndex)
    {
        LobbyConfig config = GetLobbyConfig(lobbyIndex);
        if (config == null) return false; 
        
        if (config.entryFeeCash > 0f && PlayerModel.CashBalance < config.entryFeeCash) return false; 
        if (config.entryFeeChips > 0 && PlayerModel.Coins < config.entryFeeChips) return false; 
        
        return true; 
    }
    
    public void SetLobbyConfig(int lobbyIndex, LobbyConfig config)
    {
        // Now handled via LobbyModel if needed.
        // Direct modification is disabled to ensure SSOT (Single Source of Truth).
        Debug.LogWarning("[HorizontalLobbyManager] SetLobbyConfig called. Use LobbyModel to update configurations.");
    }
    
    public void OpenGameplayObject(int lobbyIndex = -1)
    {
        LobbyConfig config = GetLobbyConfig(lobbyIndex);
        if (config == null) return;

        _pendingLobbyIndex = lobbyIndex;
        
        // Calculate the fee (prioritize cash, fallback to chips)
        int entryFee = config.entryFeeCash > 0 ? (int)config.entryFeeCash : config.entryFeeChips;
        
        // Mode is based on player count, e.g. "2p" or "5p"
        string mode = $"{config.numberOfPlayers}p"; 

        Debug.Log($"[HorizontalLobbyManager] UI Button Clicked. Requesting match via Middle Layer: Mode {mode}, Stake {entryFee}");
        
        if (waitingPanel != null) waitingPanel.SetActive(true);
        if (waitingStatusText != null) waitingStatusText.text = "Waiting for players...";
        
        // Trigger the middle layer
        MatchmakingModel.FindMatch(mode, entryFee);
    }

    private void HandleMatchFound(StartGameData data)
    {
        Debug.Log("[HorizontalLobbyManager] Match Found event received from middle layer!");
        if (waitingPanel != null) waitingPanel.SetActive(false);
        
        if (_pendingLobbyIndex >= 0)
        {
            StartGameFromNetwork(_pendingLobbyIndex, data, -1);
        }
    }

    private void HandleMatchError(string errorMessage)
    {
        Debug.LogError("[HorizontalLobbyManager] Matchmaking error: " + errorMessage);
        if (waitingPanel != null) waitingPanel.SetActive(false);
        // Future: Show a popup to the user
    }

    private void HandleMatchmakingUpdated(int current, int target)
    {
        if (waitingStatusText != null)
        {
            if (target > 0)
            {
                waitingStatusText.text = $"Players: {current}/{target}";
            }
            else
            {
                waitingStatusText.text = $"Waiting for players...";
            }
        }
    }

    public void StartGameFromNetwork(int lobbyIndex, StartGameData matchData = null, int overrideCPUs = -1)
    {
        if (gameplayObject != null)
        {
            LobbyConfig config = (lobbyIndex >= 0 && lobbyIndex < BACKGROUND_COUNT) ? GetLobbyConfig(lobbyIndex) : null;
            GameManager gameManager = FindObjectOfType<GameManager>();
            
            if (gameManager != null && config != null)
            {
                if (gameplayObject.activeSelf) gameplayObject.SetActive(false);
                gameManager.ConfigureGameFromLobby(config, overrideCPUs, matchData);
            }
            
            gameplayObject.SetActive(true);
        }
    }
    
    public void RefreshAllPlayButtons()
    {
        if (allLobbyItems != null)
        {
            foreach(var item in allLobbyItems)
            {
                if (item != null) item.RefreshPlayButtonsInteractability();
            }
        }
    }
    #endregion
    
    private void SetupDotButtons()
    {
        if (dotButtons == null) return;
        
        for (int i = 0; i < dotButtons.Length; i++)
        {
            if (dotButtons[i] != null)
            {
                int index = i; 
                dotButtons[i].onClick.RemoveAllListeners();
                dotButtons[i].onClick.AddListener(() => OnDotButtonClicked(index));
            }
        }
    }

    private void OnDotButtonClicked(int targetIndex)
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();
        
        if (targetIndex == currentScrollIndex) return;
        
        int direction = (targetIndex > currentScrollIndex) ? 1 : -1;
        currentScrollIndex = Mathf.Clamp(targetIndex, 0, BACKGROUND_COUNT - 1);
        
        UpdateLobbyPositions(false, direction);
    }

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
