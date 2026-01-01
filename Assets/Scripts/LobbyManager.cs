using UnityEngine;
using System.Collections;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Manages the lobby scene with infinite vertical background scrolling animation
/// Handles continuous background movement to create a seamless scrolling effect
/// Also manages lobby game objects using Enhanced Scroller
/// </summary>
public class LobbyManager : MonoBehaviour, IEnhancedScrollerDelegate
{
    [Header("Background Settings")]
    [SerializeField] private Transform[] backgroundPanels; // Array of background panels for seamless looping
    [SerializeField] private float scrollSpeed = 2f; // Speed of background scrolling (units per second)
    
    [Header("Background Dimensions")]
    [SerializeField] private float backgroundHeight = 10f; // Height of each background panel (for seamless looping)
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupBackgrounds = true; // Automatically find and setup backgrounds
    [SerializeField] private string backgroundTag = "Background"; // Tag to search for backgrounds
    
    [Header("Camera Settings (Optional)")]
    [SerializeField] private Camera targetCamera; // Camera to calculate viewport bounds (auto-finds Main Camera if null)
    [SerializeField] private float resetOffset = 2f; // Extra offset below viewport to trigger reset early
    
    [Header("Enhanced Scroller (Lobby Game Objects)")]
    [SerializeField] private EnhancedScroller scroller; // Enhanced Scroller for lobby game objects
    [SerializeField] private LobbyRowCellView cellViewPrefab; // Prefab for lobby row cells (contains 2 items)
    [SerializeField] private float cellSize = 200f; // Height of each row cell
    [SerializeField] private int cellsPerRow = 2; // Number of cells per row
    
    [Header("Gameplay Object")]
    [SerializeField] private GameObject gameplayObject; // Gameplay object to open when play button is clicked
    
    [Header("Lobby Game Objects Settings")]
    private const int BACKGROUND_COUNT = 12; // Total number of background sprites (0-12)
    
    [Header("Lobby Configurations")]
    [SerializeField] private LobbyConfig[] lobbyConfigurations = new LobbyConfig[12]; // 13 lobby configurations (can be set in inspector)
    
    private bool isScrolling = true;
    private float resetThresholdY;
    
    /// <summary>
    /// Internal representation of lobby game objects data
    /// </summary>
    private SmallList<LobbyGameObjectData> _lobbyObjectsData;
    
    private bool isScrollerInitialized = false;
    
    private void Start()
    {
        InitializeBackgrounds();
        InitializeLobbyConfigurations();
        InitializeLobbyObjects();
    }
    
    private void OnEnable()
    {
        // Reinitialize scroller when GameObject becomes active
        if (isScrollerInitialized && scroller != null)
        {
            RefreshLobbyObjects();
            // Refresh play buttons when lobby becomes active (in case currency changed)
            RefreshAllPlayButtons();
        }
    }
    
    private void Update()
    {
        if (isScrolling && backgroundPanels != null && backgroundPanels.Length > 0)
        {
            ScrollBackgrounds();
        }
    }
    
    /// <summary>
    /// Gets the actual height of a panel using its renderer bounds
    /// </summary>
    private float GetPanelHeight(Transform panel)
    {
        // Try SpriteRenderer first
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.y > 0)
        {
            return spriteRenderer.bounds.size.y;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.rect.height * rectTransform.lossyScale.y;
        }
        
        // Fallback to configured value
        return backgroundHeight;
    }
    
    /// <summary>
    /// Gets the actual bottom edge of a panel using its renderer bounds
    /// </summary>
    private float GetPanelBottomEdge(Transform panel)
    {
        // Try SpriteRenderer first
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.y > 0)
        {
            return spriteRenderer.bounds.min.y;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.y - (rectTransform.rect.height * 0.5f * rectTransform.lossyScale.y);
        }
        
        // Fallback to calculated value
        return panel.position.y - (backgroundHeight * 0.5f);
    }
    
    /// <summary>
    /// Gets the actual top edge of a panel using its renderer bounds
    /// </summary>
    private float GetPanelTopEdge(Transform panel)
    {
        // Try SpriteRenderer first
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.y > 0)
        {
            return spriteRenderer.bounds.max.y;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.y + (rectTransform.rect.height * 0.5f * rectTransform.lossyScale.y);
        }
        
        // Fallback to calculated value
        return panel.position.y + (backgroundHeight * 0.5f);
    }
    
    /// <summary>
    /// Initializes the background panels for infinite scrolling
    /// </summary>
    private void InitializeBackgrounds()
    {
        // Auto-setup if enabled
        if (autoSetupBackgrounds && (backgroundPanels == null || backgroundPanels.Length == 0))
        {
            GameObject[] backgroundObjects = GameObject.FindGameObjectsWithTag(backgroundTag);
            if (backgroundObjects.Length > 0)
            {
                backgroundPanels = new Transform[backgroundObjects.Length];
                for (int i = 0; i < backgroundObjects.Length; i++)
                {
                    backgroundPanels[i] = backgroundObjects[i].transform;
                }
            }
        }
        
        // Validate backgrounds
        if (backgroundPanels == null || backgroundPanels.Length == 0)
        {
            Debug.LogWarning("[LobbyManager] No background panels assigned! Please assign backgrounds in the inspector.");
            return;
        }
        
        // Remove null references
        System.Collections.Generic.List<Transform> validPanels = new System.Collections.Generic.List<Transform>();
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null)
            {
                validPanels.Add(panel);
            }
        }
        backgroundPanels = validPanels.ToArray();
        
        if (backgroundPanels.Length == 0)
        {
            Debug.LogError("[LobbyManager] No valid background panels found!");
            return;
        }
        
        // Get camera for viewport calculations
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Calculate reset threshold - reposition when bottom edge goes below this point
        // This ensures panels are repositioned before visible gaps appear
        if (targetCamera != null)
        {
            // Calculate bottom of camera viewport in world space
            float cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
            resetThresholdY = cameraBottom - resetOffset;
        }
        else
        {
            // Fallback: use a fixed threshold below zero
            // Use actual panel height if available
            float avgHeight = backgroundHeight;
            if (backgroundPanels.Length > 0 && backgroundPanels[0] != null)
            {
                avgHeight = GetPanelHeight(backgroundPanels[0]);
            }
            resetThresholdY = -avgHeight - resetOffset;
        }
        
        // Position backgrounds vertically for seamless looping
        PositionBackgroundsForLooping();
        
        Debug.Log($"[LobbyManager] Initialized {backgroundPanels.Length} background panel(s) for infinite scrolling");
    }
    
    /// <summary>
    /// Positions background panels vertically for seamless infinite looping
    /// </summary>
    private void PositionBackgroundsForLooping()
    {
        if (backgroundPanels == null || backgroundPanels.Length == 0) return;
        
        // Sort backgrounds by current Y position (top to bottom)
        System.Array.Sort(backgroundPanels, (a, b) => b.position.y.CompareTo(a.position.y));
        
        // Position them one after another using actual panel heights
        float currentTopEdge = 0f;
        for (int i = 0; i < backgroundPanels.Length; i++)
        {
            if (backgroundPanels[i] == null) continue;
            
            float panelHeight = GetPanelHeight(backgroundPanels[i]);
            float panelCenterY = currentTopEdge - (panelHeight * 0.5f);
            
            Vector3 pos = backgroundPanels[i].position;
            pos.y = panelCenterY;
            backgroundPanels[i].position = pos;
            
            // Next panel starts where this one ends
            currentTopEdge -= panelHeight;
        }
    }
    
    /// <summary>
    /// Scrolls all background panels downward and resets them when they go off-screen
    /// </summary>
    private void ScrollBackgrounds()
    {
        // Calculate camera bottom for off-screen detection
        float cameraBottom = resetThresholdY;
        if (targetCamera != null)
        {
            cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
        }
        
        for (int i = 0; i < backgroundPanels.Length; i++)
        {
            if (backgroundPanels[i] == null) continue;
            
            // Move background down
            Vector3 position = backgroundPanels[i].position;
            position.y -= scrollSpeed * Time.deltaTime;
            backgroundPanels[i].position = position;
            
            // Get actual top and bottom edges of this panel (from renderer bounds)
            float topEdge = GetPanelTopEdge(backgroundPanels[i]);
            float bottomEdge = GetPanelBottomEdge(backgroundPanels[i]);
            
            // Only reposition when the panel is FULLY off-screen (top edge is below camera bottom)
            // This ensures at least one panel is always visible
            if (topEdge <= cameraBottom)
            {
                // Find the highest background panel's top edge (using actual bounds)
                float highestTopEdge = float.MinValue;
                foreach (Transform panel in backgroundPanels)
                {
                    if (panel != null && panel != backgroundPanels[i])
                    {
                        float panelTopEdge = GetPanelTopEdge(panel);
                        if (panelTopEdge > highestTopEdge)
                        {
                            highestTopEdge = panelTopEdge;
                        }
                    }
                }
                
                // Get this panel's actual height
                float panelHeight = GetPanelHeight(backgroundPanels[i]);
                
                // Place this panel directly above the highest one (seamless connection)
                // Position so its bottom edge touches the highest panel's top edge
                float newCenterY = highestTopEdge + (panelHeight * 0.5f);
                
                position.y = newCenterY;
                backgroundPanels[i].position = position;
            }
        }
    }
    
    /// <summary>
    /// Starts the background scrolling animation
    /// </summary>
    public void StartScrolling()
    {
        isScrolling = true;
    }
    
    /// <summary>
    /// Stops the background scrolling animation
    /// </summary>
    public void StopScrolling()
    {
        isScrolling = false;
    }
    
    /// <summary>
    /// Sets the scroll speed
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = Mathf.Max(0f, speed);
    }
    
    /// <summary>
    /// Gets the current scroll speed
    /// </summary>
    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }
    
    /// <summary>
    /// Resets all background positions to initial state
    /// </summary>
    public void ResetBackgrounds()
    {
        PositionBackgroundsForLooping();
    }
    
    #region Enhanced Scroller (Lobby Game Objects)
    
    /// <summary>
    /// Initializes the lobby game objects scroller
    /// </summary>
    private void InitializeLobbyObjects()
    {
        if (scroller == null)
        {
            Debug.LogWarning("[LobbyManager] Enhanced Scroller not assigned for lobby objects. Skipping initialization.");
            return;
        }
        
        // Set this script as the scroller's delegate
        scroller.Delegate = this;
        
        // Load 13 background objects (use coroutine to wait for scroller initialization)
        StartCoroutine(LoadLobbyObjectsDataDelayed());
        isScrollerInitialized = true;
    }
    
    /// <summary>
    /// Refreshes the lobby objects when GameObject becomes active again
    /// </summary>
    private void RefreshLobbyObjects()
    {
        if (scroller != null)
        {
            scroller.Delegate = this;
            
            // Reload data if we have data (use coroutine to wait for scroller initialization)
            if (_lobbyObjectsData != null && _lobbyObjectsData.Count > 0)
            {
                StartCoroutine(ReloadLobbyObjectsDataDelayed());
            }
            else
            {
                // If no data, load 13 background objects
                StartCoroutine(LoadLobbyObjectsDataDelayed());
            }
        }
    }
    
    /// <summary>
    /// Coroutine to reload data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator ReloadLobbyObjectsDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            scroller.ReloadData();
        }
    }
    
    /// <summary>
    /// Coroutine to load data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator LoadLobbyObjectsDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        LoadLobbyObjectsData();
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
        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn())
        {
            return false; // Can't check if not logged in
        }
        
        LobbyConfig config = GetLobbyConfig(lobbyIndex);
        if (config == null)
        {
            return false; // No config found
        }
        
        // Check if player has enough cash
        if (config.entryFeeCash > 0f)
        {
            if (PlayFabManager.Instance.currentPlayerData.cash < config.entryFeeCash)
            {
                return false; // Can't afford cash entry fee
            }
        }
        
        // Check if player has enough chips
        if (config.entryFeeChips > 0)
        {
            if (PlayFabManager.Instance.currentPlayerData.coins < config.entryFeeChips)
            {
                return false; // Can't afford chips entry fee
            }
        }
        
        return true; // Player can afford entry fee
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
    /// Loads 13 lobby objects data - one for each background sprite (0-12)
    /// </summary>
    public void LoadLobbyObjectsData()
    {
        if (_lobbyObjectsData == null)
        {
            _lobbyObjectsData = new SmallList<LobbyGameObjectData>();
        }
        else
        {
            _lobbyObjectsData.Clear();
        }
        
        // Create exactly 13 objects, one for each background sprite (indices 0-12)
        for (int i = 0; i < BACKGROUND_COUNT; i++)
        {
            _lobbyObjectsData.Add(new LobbyGameObjectData(i, i));
        }
        
        // Reload the scroller with new data (only if scroller is active and initialized)
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        Debug.Log($"[LobbyManager] Loaded {_lobbyObjectsData.Count} lobby objects - one for each background sprite");
    }
    
    /// <summary>
    /// Adds a lobby game object entry
    /// </summary>
    public void AddLobbyObject(int backgroundIndex, int lobbyIndex = -1)
    {
        if (_lobbyObjectsData == null)
        {
            _lobbyObjectsData = new SmallList<LobbyGameObjectData>();
        }
        
        // Use lobbyIndex if provided, otherwise use backgroundIndex
        int actualLobbyIndex = lobbyIndex >= 0 ? lobbyIndex : backgroundIndex;
        _lobbyObjectsData.Add(new LobbyGameObjectData(backgroundIndex, actualLobbyIndex));
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
    }
    
    /// <summary>
    /// Clears all lobby objects data
    /// </summary>
    public void ClearLobbyObjects()
    {
        if (_lobbyObjectsData != null)
        {
            _lobbyObjectsData.Clear();
        }
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        Debug.Log("[LobbyManager] Lobby objects data cleared");
    }
    
    /// <summary>
    /// Refreshes all play buttons interactability based on player's current currency
    /// Call this when player's cash or chips change
    /// </summary>
    public void RefreshAllPlayButtons()
    {
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Reload data to refresh all cells
            scroller.ReloadData();
            Debug.Log("[LobbyManager] Refreshed all play buttons interactability");
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
                Debug.Log($"[LobbyManager] Configured game for Lobby {lobbyIndex + 1}: {config.numberOfPlayers} players, Entry: {config.entryFeeChips} chips / {config.entryFeeCash} cash");
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
    
    #endregion
    
    #region IEnhancedScrollerDelegate Implementation
    
    /// <summary>
    /// Returns the number of rows (cells) in the lobby objects list
    /// Each row contains 2 items
    /// </summary>
    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        if (_lobbyObjectsData == null || _lobbyObjectsData.Count == 0)
            return 0;
        
        // Calculate number of rows: ceil(data count / cells per row)
        return Mathf.CeilToInt((float)_lobbyObjectsData.Count / cellsPerRow);
    }
    
    /// <summary>
    /// Returns the size of each row cell (height for vertical scroller)
    /// </summary>
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellSize;
    }
    
    /// <summary>
    /// Gets the cell view (row) for a specific data index
    /// Each row contains 2 items
    /// </summary>
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Check if prefab is assigned
        if (cellViewPrefab == null)
        {
            Debug.LogError("[LobbyManager] Cell View Prefab is not assigned!");
            return null;
        }
        
        // Get a row cell from the scroller (recycled or new)
        LobbyRowCellView rowCellView = scroller.GetCellView(cellViewPrefab) as LobbyRowCellView;
        
        if (rowCellView == null)
        {
            Debug.LogError("[LobbyManager] Failed to get LobbyRowCellView cell view!");
            return null;
        }
        
        // Calculate the starting index for this row
        int startingIndex = dataIndex * cellsPerRow;
        
        // Get data for the two items in this row
        LobbyGameObjectData data1 = null;
        LobbyGameObjectData data2 = null;
        
        if (_lobbyObjectsData != null)
        {
            // First item
            if (startingIndex < _lobbyObjectsData.Count)
            {
                data1 = _lobbyObjectsData[startingIndex];
            }
            
            // Second item
            if (startingIndex + 1 < _lobbyObjectsData.Count)
            {
                data2 = _lobbyObjectsData[startingIndex + 1];
            }
        }
        
        // Set the name for debugging
        rowCellView.name = $"Lobby Row {dataIndex}";
        
        // Set the data for this row (2 items)
        rowCellView.SetData(data1, data2);
        
        return rowCellView;
    }
    
    #endregion
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor helper: Draws gizmos to visualize background boundaries
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (backgroundPanels == null) return;
        
        Gizmos.color = Color.yellow;
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null)
            {
                Vector3 center = panel.position;
                Vector3 size = new Vector3(20f, backgroundHeight, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
    #endif
}

