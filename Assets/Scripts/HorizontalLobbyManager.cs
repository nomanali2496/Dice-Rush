using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

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
    
    [Header("UI Panels")]
    [SerializeField] private GameObject waitingPanel;
    [SerializeField] private TMPro.TextMeshProUGUI waitingStatusText;
    [SerializeField] private Button cancelMatchmakingButton; // Close button on the waiting panel
    [SerializeField] private Button disconnectSocketButton; // Back button to leave lobby and disconnect
    
    [Header("Lobby Game Objects Settings")]
    private int BACKGROUND_COUNT => LobbyModel.Configurations.Length; 
    
    // Lobby scroll management
    private LobbyGameObjectData[] _lobbyObjectsData; // Array of current lobby objects
    private int currentScrollIndex = 0; // Current scroll position
    private bool isScrollAnimating = false; // Prevents multiple scrolls at once
    
    // Horizontal Carousel - Dynamic Items
    private List<LobbyRowCellView> allLobbyItems = new List<LobbyRowCellView>();
    private Transform centerAnchor;
    
    [Header("Dot Navigation (Dynamic)")]
    [SerializeField] private GameObject dotPrefab; // Prefab for a single dot button
    [SerializeField] private Transform dotsContainer; // Layout group parent for dots
    [SerializeField] private Sprite selectedDotImage; // Image to show when dot is selected
    [SerializeField] private Sprite unselectedDotImage; // Image to show when dot is not selected
    
    private List<Button> dynamicDots = new List<Button>(); // List of instantiated dots


    
    // Swipe detection
    private Vector2 swipeStartPos;
    private bool isSwiping = false;
    
    // Multi-step scroll animation tracking
    private Coroutine multiScrollCoroutine = null;
    
    // Auto-select flag to prevent re-selecting on every enable
    private bool hasAutoSelectedOnce = false;
    
    // Tracks the lobby they are currently matchmaking for
    private int _pendingLobbyIndex = -1;

    // True while this panel is visible — used by SocketManager to decide whether
    // to reconnect on app resume.
    public static bool IsLobbyActive { get; private set; }
    
    private float _waitingSpriteRotationAngle = 0f;
    
    private void Start()
    {
        LobbyModel.OnConfigurationsUpdated += HandleLobbyDataUpdated;
        MatchmakingModel.OnMatchmakingFailed += HandleMatchError;
        MatchmakingModel.OnMatchmakingUpdated += HandleMatchmakingUpdated;
        
        LobbyModel.FetchConfigurations();
        
        InitializeLobbyRows();
        SetupScrollButtons();
        
        // Wire cancel matchmaking button
        if (cancelMatchmakingButton != null)
            cancelMatchmakingButton.onClick.AddListener(OnCancelMatchmaking);
            
        // Wire back/disconnect button — only called when navigating to the HOME screen.
        // Gameplay → Lobby transitions do NOT trigger this; the socket stays alive there.
        if (disconnectSocketButton != null)
            disconnectSocketButton.onClick.AddListener(OnNavigateToHome);
        
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
        IsLobbyActive = true;

        // Ensure the socket is alive whenever the lobby is visible.
        // This covers returning from gameplay (socket was kept alive) AND resuming
        // after a home-screen back (socket was disconnected) if the user re-enters.
        if (BackendAuthService.HasStoredJWT() && SocketManager.Instance != null)
            SocketManager.Instance.ConnectToServer();

        // Ensure background music is playing if settings allow (e.g. returning from game)
        if (SettingManager.instance != null)
            SettingManager.instance.ApplyMusicSetting();

        // Refresh lobby rows when GameObject becomes active
        if (_lobbyObjectsData != null && _lobbyObjectsData.Length > 0)
            UpdateVisibleRows();

        // Start retry routine to ensure buttons light up when data is ready (e.g. after returning from game)
        StartCoroutine(InitializeButtonsRoutine());

        // Subscribe to events
        MatchmakingModel.OnMatchFound += HandleMatchFound;
    }

    private void OnDisable()
    {
        IsLobbyActive = false;
        MatchmakingModel.OnMatchFound -= HandleMatchFound;
    }

    private void OnDestroy()
    {
        LobbyModel.OnConfigurationsUpdated -= HandleLobbyDataUpdated;
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
        
        // Rotate waiting status text icon/sprite
        UpdateWaitingStatusSpriteRotation();
    }
    
    private void UpdateWaitingStatusSpriteRotation()
    {
        if (waitingPanel == null || !waitingPanel.activeInHierarchy || waitingStatusText == null)
            return;

        // Ensure the text mesh is updated
        waitingStatusText.ForceMeshUpdate();
        TMP_TextInfo textInfo = waitingStatusText.textInfo;

        if (textInfo != null && textInfo.characterCount > 0)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[0];
            if (charInfo.isVisible)
            {
                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                if (textInfo.meshInfo != null && materialIndex < textInfo.meshInfo.Length)
                {
                    Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                    if (vertices != null && vertexIndex + 3 < vertices.Length)
                    {
                        // Calculate the center of the sprite quad
                        Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) * 0.5f;

                        // Rotate the 4 vertices of the quad
                        _waitingSpriteRotationAngle -= Time.deltaTime * 200f; // Rotate 200 degrees per second (clockwise)
                        Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, _waitingSpriteRotationAngle), Vector3.one);

                        for (int i = 0; i < 4; i++)
                        {
                            vertices[vertexIndex + i] = matrix.MultiplyPoint3x4(vertices[vertexIndex + i] - center);
                        }

                        // Push the updated vertices to the mesh
                        waitingStatusText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                    }
                }
            }
        }
    }
    

    
    #region Lobby Rows Management (Single Horizontal Position)
    
    /// <summary>
    /// Initializes the lobby items pool and anchors
    /// </summary>
    private void InitializeLobbyRows()
    {
        // 1. Setup Anchors - Use the PARENT of the row so we don't disable the new items
        if (centerRow != null) centerAnchor = centerRow.transform.parent;
        
        if (centerAnchor == null)
        {
            Debug.LogError("[HorizontalLobbyManager] Row reference is missing! Cannot setup carousel.");
            return;
        }

        // 2. Create data for all lobby items
        _lobbyObjectsData = new LobbyGameObjectData[BACKGROUND_COUNT];
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            LobbyConfig config = LobbyModel.GetConfig(i);
            string imageUrl = config != null ? config.imageUrl : "";
            string lobbyId = config != null ? config.lobbyId : "";
            _lobbyObjectsData[i] = new LobbyGameObjectData(i, i, lobbyId, imageUrl);
        }
        
        // 3. Instantiate Layout Items (Pool)
        LobbyRowCellView prefab = centerRow;
        
        // Prepare container
        // Clear existing items if re-initializing
        foreach(var item in allLobbyItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        allLobbyItems.Clear();
        
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            // Instantiate new item
            LobbyRowCellView newItem = Instantiate(prefab);
            newItem.name = $"LobbyItem_{i}";
            
            // Ensure the clone is active and script is enabled 
            // (in case the template was already disabled)
            newItem.gameObject.SetActive(true);
            newItem.enabled = true;
            
            // DEEP ENABLE: Ensure all visual children are also turned on
            foreach (Transform child in newItem.transform)
            {
                child.gameObject.SetActive(true);
            }
            
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
        InitializeDotNavigation();
        
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
            
            // Hide only the IMMEDIATE children of the original row (visuals)
            // but NOT the new lobby items we just instantiated
            foreach(Transform child in row.transform)
            {
                // Safety check: Don't disable items that are part of our new list
                bool isNewItem = false;
                foreach(var item in allLobbyItems) { if(item.transform == child) isNewItem = true; }
                
                if (!isNewItem) child.gameObject.SetActive(false);
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
    public void UpdateVisibleRows()
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
        
        // Use the official mode and ID from the backend database
        string mode = !string.IsNullOrEmpty(config.mode) ? config.mode : $"{config.numberOfPlayers}p"; 
        string lobbyId = config.lobbyId;

        Debug.Log($"[HorizontalLobbyManager] UI Button Clicked. Requesting match: ID {lobbyId}, Mode {mode}, Stake {entryFee}");
        
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);
        }
        if (waitingStatusText != null)
            waitingStatusText.text = "<size=200%><voffset=0.35em><sprite name=\"Dice 1 1\"></voffset></size><space=-0.5em>Match Starting Soon\nWaiting for players to join...";
        
        // Trigger the middle layer
        MatchmakingModel.FindMatch(mode, entryFee, lobbyId);
    }

    private void HandleMatchFound(StartGameData data)
    {
        Debug.Log("[HorizontalLobbyManager] Match Found event received from middle layer!");
        
        // Only refresh profile for NEW matches to sync balance deduction.
        // Skip it for recovered matches (resume) to keep the flow silent as requested.
        GameManager gm = GameManager.Instance;
        bool isAlreadyInGame = gm != null && gm.IsGameplayScreenActive;

        if (!isAlreadyInGame && BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            BackendAuthService.Instance.GetProfile(null);
        }

        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
        }
        
        // Even if _pendingLobbyIndex is -1 (meaning it's a recovered match), we still start the game
        StartGameFromNetwork(_pendingLobbyIndex, data, -1);
    }

    private void HandleMatchError(string errorMessage)
    {
        Debug.LogError("[HorizontalLobbyManager] Matchmaking error: " + errorMessage);
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
        }
        // Future: Show a popup to the user
    }

    private void HandleMatchmakingUpdated(int current, int target)
    {
        // Text is updated continuously by the animation, so we don't need to overwrite it here.
    }

    public void HideWaitingPanel()
    {
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Called by the close/cancel button on the waiting panel.
    /// Emits 'cancelMatchmaking' with the current lobbyId and hides the panel.
    /// </summary>
    public void OnCancelMatchmaking()
    {
        Debug.Log("[HorizontalLobbyManager] Cancel matchmaking button pressed.");
        
        // CancelSearch() internally emits 'cancelMatchmaking' with { lobbyId } via the socket
        MatchmakingModel.CancelSearch();

        if (waitingPanel != null)
        {
            waitingPanel.SetActive(false);
        }
        _pendingLobbyIndex = -1;
    }

    /// Called ONLY when the player navigates back to the Home/Main screen.
    /// Cancels any active search and safely closes the socket.
    /// Going from gameplay → lobby does NOT call this — socket stays alive there.
    private void OnNavigateToHome()
    {
        Debug.Log("<color=#FF6347>⚠️ [Lobby] Navigating home — cancelling search and disconnecting socket.</color>");

        if (MatchmakingModel.IsSearching)
        {
            MatchmakingModel.CancelSearch();
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(false);
            }
            _pendingLobbyIndex = -1;
        }

        if (SocketManager.Instance != null)
            SocketManager.Instance.Disconnect();
    }

    public void StartGameFromNetwork(int lobbyIndex, StartGameData matchData = null, int overrideCPUs = -1)
    {
        if (gameplayObject != null)
        {
            LobbyConfig config = null;
            if (lobbyIndex >= 0 && lobbyIndex < BACKGROUND_COUNT)
            {
                config = GetLobbyConfig(lobbyIndex);
            }
            else if (matchData != null)
            {
                // Reconstruct a pseudo-config for recovered matches
                config = new LobbyConfig(-1, 2, matchData.stake, 0, 0f, 0, 0f, 0, 0f, 0);
                config.lobbyId = "recovered_match";
                config.mode = matchData.roomId;
            }

            GameManager gameManager = FindObjectOfType<GameManager>();
            
            if (gameManager != null && config != null)
            {
                if (gameplayObject.activeSelf) gameplayObject.SetActive(false);
                gameManager.ConfigureGameFromLobby(config, overrideCPUs, matchData);
            }
            
            gameplayObject.SetActive(true);
        }
    }
    
    private void HandleLobbyDataUpdated()
    {
        Debug.Log($"[HorizontalLobbyManager] Lobby data updated. Re-initializing {BACKGROUND_COUNT} rows.");
        InitializeLobbyRows();
        InitializeDotNavigation(); // Re-create navigation dots for the new count
        RefreshAllPlayButtons();
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
    
    private void InitializeDotNavigation()
    {
        if (dotPrefab == null || dotsContainer == null) return;
        
        // 1. Clear existing dots
        foreach (var dot in dynamicDots)
        {
            if (dot != null) Destroy(dot.gameObject);
        }
        dynamicDots.Clear();
        
        // 2. Create new dots based on lobby count
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            GameObject dotObj = Instantiate(dotPrefab, dotsContainer);
            Button dotBtn = dotObj.GetComponent<Button>();
            
            if (dotBtn != null)
            {
                int index = i;
                dotBtn.onClick.RemoveAllListeners();
                dotBtn.onClick.AddListener(() => OnDotButtonClicked(index));
                dynamicDots.Add(dotBtn);
            }
        }
        
        // 3. Highlight the current dot
        UpdateDotButtons();
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
        if (dynamicDots == null || dynamicDots.Count == 0) return;
        
        int activeDotIndex = currentScrollIndex;
        
        for (int i = 0; i < dynamicDots.Count; i++)
        {
            if (dynamicDots[i] != null)
            {
                Image dotImage = dynamicDots[i].GetComponent<Image>();
                RectTransform dotRect = dynamicDots[i].GetComponent<RectTransform>();
                
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
                            dotRect.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack);
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
