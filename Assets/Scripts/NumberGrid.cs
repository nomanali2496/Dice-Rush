using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// Player type enum for tracking selections separately
/// </summary>
public enum PlayerType
{
    Player,
    CPU,
    Network
}

/// <summary>
/// Manages a 5x5 grid of number boxes with random positioning
/// Numbers 2-12 appear twice, and 3 boxes contain powerups
/// Supports separate tracking for Player and CPU selections
/// </summary>
public class NumberGrid : MonoBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private GameObject numberBoxPrefab; // The prefab to instantiate
    
    [Header("Powerup Sprites")]
    [SerializeField] private Sprite[] powerupSprites = new Sprite[3]; // 3 powerup sprites
    
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5; // Grid width (5 columns)
    [SerializeField] private int gridHeight = 5; // Grid height (5 rows)
    [SerializeField] private float spacingX = 18.27f; // Horizontal spacing between boxes (constant)
    [SerializeField] private float spacingY = 0f; // Vertical spacing between boxes (constant)
    
    [Header("Padding (in pixels)")]
    [SerializeField] private float paddingLeft = 20f; // Padding on the left side
    [SerializeField] private float paddingRight = 20f; // Padding on the right side
    [SerializeField] private float paddingTop = 20f; // Padding on the top side
    [SerializeField] private float paddingBottom = 20f; // Padding on the bottom side

    [Header("Brix Scaling")]
    [SerializeField] private RectTransform brixParent;      // The purple frame containing the grid
    [SerializeField] private float referenceWidthValue = 1440f; // Scale is 1 at this width
    
    [SerializeField] private Vector2 startPosition = Vector2.zero; // Starting position for grid (calculated automatically)
    
    [Header("Reference Values (1440x3216 resolution)")]
    [SerializeField] private float referenceCellSizeX = 218.8f; // Reference cell width
    [SerializeField] private float referenceCellSizeY = 216.5f; // Reference cell height
    [SerializeField] private float referenceFontSize = 60f; // Reference font size for number text (at reference resolution)
    
    [Header("Slider Parent Reference (1440x3216 resolution)")]
    [SerializeField] private RectTransform sliderParent; // Reference to the slider's parent RectTransform
    [SerializeField] private float referenceSliderParentWidth = 446f; // Reference slider parent width (at reference resolution)
    [SerializeField] private float referenceSliderParentHeight = 110f; // Reference slider parent height (at reference resolution)
    [SerializeField] private float referenceHandleWidth = 144.69f; // Reference slider handle width (at reference resolution)
    [SerializeField] private float referenceHandleHeight = 144.69f; // Reference slider handle height (at reference resolution)
    
    // Calculated values (set during grid generation)
    private float currentCellSizeX; // Current scaled cell width
    private float currentCellSizeY; // Current scaled cell height
    private float scaleFactor = 1f; // Scale factor based on NumberGrid size
    
    [Header("Component Names (in Prefab)")]
    [SerializeField] private string numberTextName = "NumberText"; // Name of the Text/TextMeshProUGUI component
    [SerializeField] private string numberTextNameShadow = "NumberText1"; // Name of the Text/TextMeshProUGUI component
    [SerializeField] private string powerupImageName = "PowerupImage"; // Name of the Image component
    [SerializeField] private string selectButtonName = "SelectButton"; // Name of the Button component
    [SerializeField] private string highlightImageName = "HighlightImage"; // Name of the highlight Image component
    [SerializeField] private string boxBackgroundImageName = "MainBackground"; // Name of the background Image component
    
    [Header("Box Background Sprites")]
    [SerializeField] private Sprite normalBoxBgSprite; // Sprite for normal number boxes
    [SerializeField] private Sprite powerupBoxBgSprite; // Sprite for powerup boxes
    
    [Header("Powerup Timer UI")]
    [SerializeField] private Slider powerupTimerSlider; // Slider to show timer progress
    [SerializeField] private TextMeshProUGUI powerupTimerText; // Text to show timer countdown
    
    [Header("Powerup Timer Settings")]
    [SerializeField] private float powerupVisibleDuration = 5f; // How long powerup stays visible (seconds)
    [SerializeField] private float minDelayBetweenPowerups = 3f; // Minimum delay before next powerup appears
    [SerializeField] private float maxDelayBetweenPowerups = 10f; // Maximum delay before next powerup appears
    
    /// <summary>
    /// Box data structure to store box information
    /// </summary>
    private class BoxData
    {
        public GameObject boxObject;
        public int number; // Number value (for number boxes)
        public bool isPowerup; // Whether this is a powerup box
        public Button selectButton; // Button component
        public Image highlightImage; // Highlight image component
        public Image powerupImage; // Powerup image component (for powerup boxes)
        public int powerupSpriteIndex = -1; // Powerup sprite index (for powerup boxes)
        public bool isSelected; // Whether this box is selected (by any player)
        public PlayerType selectedByPlayer = PlayerType.Player; // Which player selected this box
        public int selectedByCPUIndex = -1; // Which CPU index selected this box (-1 if player or not selected)
        public int selectedInRollSum = -1; // Dice sum when this box was selected (-1 if not selected)
        public int selectedInRollId = -1; // Roll ID when this box was selected (-1 if not selected)
        public bool isPowerupVisible = false; // Whether this powerup is currently visible
        public Tweener powerupAnimation; // DOTween animation for powerup (when active)
    }
    
    private List<BoxData> boxDataList = new List<BoxData>(); // List to store all box data
    private List<GameObject> instantiatedBoxes = new List<GameObject>(); // List to store all instantiated boxes (for cleanup)
    private bool gridGenerated = false; // Flag to prevent double generation
    private string ownerUserId; // The Socket.io User ID that owns this grid
    private int tapSequence = 0; // Sequence number for tap validation
    private Coroutine serverPowerupUIUpdateCoroutine;
    
    // --- BOX DATA & PREFABS ---
    private const int MIN_NUMBER = 2;
    private const int MAX_NUMBER = 12;
    private const int POWERUP_COUNT = 3;
    
    // Separate tracking for Player and CPU
    private int currentDiceSumPlayer = -1; // Current dice sum for Player (-1 means no sum set yet)
    private int currentDiceSumCPU = -1; // Current dice sum for CPU (-1 means no sum set yet) - DEPRECATED: Use cpuDiceSums instead
    private int currentRollIdPlayer = 0; // Roll ID for Player
    private int currentRollIdCPU = 0; // Roll ID for CPU - DEPRECATED: Use cpuRollIds instead
    
    // Per-CPU tracking (for multiple CPUs)
    private Dictionary<int, int> cpuDiceSums = new Dictionary<int, int>(); // CPU dice sums by CPU index
    private Dictionary<int, int> cpuRollIds = new Dictionary<int, int>(); // CPU roll IDs by CPU index
    
    // Outline colors for players
    private Color playerOutlineColor = Color.white; // Player's outline color (default white)
    private Dictionary<int, Color> cpuOutlineColors = new Dictionary<int, Color>(); // CPU outline colors by index
    
    // Powerup timer system
    private Coroutine powerupTimerCoroutine; // Coroutine for powerup timer
    private BoxData currentlyVisiblePowerup = null; // Currently visible powerup box
    private BoxData lastTappedBoxData = null; // Track last tapped box for feedback
    private System.Random powerupRandom = new System.Random(); // Random for powerup selection
    
    // Events
    public System.Action<NumberGrid, int, GameObject> OnBoxSelected; // Called when a box is selected (grid, number, boxObject)
    public System.Action<NumberGrid, int, GameObject> OnWrongBoxSelected; // Called when a wrong box is clicked
    public System.Action OnAllBoxesSelected; // Called when all selectable boxes are selected
    
    private void Start()
    {
        AdjustBrixScale(); // Scale parent to fit screen width (as per your instruction)

        // Ensure slider is hidden by default
        if (powerupTimerSlider != null)
        {
            powerupTimerSlider.gameObject.SetActive(false);
        }
        if (powerupTimerText != null)
        {
            powerupTimerText.gameObject.SetActive(false);
        }
        
        // Only generate a random grid if one hasn't been provided by the server yet
        if (!gridGenerated)
        {
            GenerateGrid();
        }
        
        // Subscribe to server events
        MatchmakingModel.OnNumberLockedEvent += HandleServerNumberLocked;
        MatchmakingModel.OnTapResultEvent += HandleServerTapResult;
        MatchmakingModel.OnIconActivatedEvent += HandleServerIconActivated;
        MatchmakingModel.OnIconExpiredEvent += HandleServerIconExpired;
        
        // Initially disable all buttons until dice are rolled
        UpdateBoxInteractability();
    }

    /// <summary>
    /// Adjusts the scale factor based on aspect ratio difference (Ref: 1440/3216 = 0.447).
    /// Optimized for iPhones by using Screen.safeArea to account for notches and bars.
    /// </summary>
    private void AdjustBrixScale()
    {
        if (brixParent == null) return;
        
        float referenceAspect = 1440f / 3216f;
        
        // Using safeArea detects that notch phones are "shorter" and "wider", allowing for more scale
        Rect safeArea = Screen.safeArea;
        float currentAspect = safeArea.width / safeArea.height;
        
        float scale = currentAspect / referenceAspect;
        
        // Ensure we at least maintain the reference size
        scale = Mathf.Max(scale, 1.0f);
        
        brixParent.localScale = new Vector3(scale, scale, 1f);
        Debug.Log($"[NumberGrid] Optimized Brix Scale: {scale:F3} (Safe Area: {safeArea.width}x{safeArea.height})");
    }

    /// <summary>
    /// Grid item data structure
    /// </summary>
    private struct GridItem
    {
        public bool isPowerup;
        public int number; // Used if isPowerup is false
        public int powerupIndex; // Used if isPowerup is true
    }
    
    /// <summary>
    /// Sets the size and positions a box instance based on calculated cell size and grid position
    /// </summary>
    private void SetSizeAndPositionBox(GameObject box, int row, int col)
    {
        if (box == null) return;
        
        // Set the box size directly (no scaling, just change width/height)
        RectTransform boxRect = box.GetComponent<RectTransform>();
        if (boxRect != null)
        {
            // Set the size directly to match calculated cell size
            boxRect.sizeDelta = new Vector2(currentCellSizeX, currentCellSizeY);
        }
        
        // Calculate position based on cell size
        // Position is center-to-center: startPosition + (col * (cellWidth + spacing))
        Vector3 position = new Vector3(
            startPosition.x + col * (currentCellSizeX + spacingX),
            startPosition.y - row * (currentCellSizeY + spacingY),
            0f
        );
        
        // Set position
        box.transform.localPosition = position;
    }
    
    /// <summary>
    /// Calculates the cell sizes based on NumberGrid RectTransform size
    /// Ensures the 5x5 grid fits within the NumberGrid bounds
    /// </summary>
    private void CalculateGridScale()
    {
        // Get the NumberGrid RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("[NumberGrid] No RectTransform found! Using default cell sizes.");
            currentCellSizeX = referenceCellSizeX;
            currentCellSizeY = referenceCellSizeY;
            startPosition = Vector2.zero;
            return;
        }
        
        // Get the current size of the NumberGrid (available space)
        // Subtract individual padding from each side
        float availableWidth = rectTransform.rect.width - (paddingLeft + paddingRight);
        float availableHeight = rectTransform.rect.height - (paddingTop + paddingBottom);
        
        Debug.Log($"[NumberGrid] ====== SCALE CALCULATION ====== Screen: {Screen.width}x{Screen.height} | NumberGrid Size: {availableWidth:F2}x{availableHeight:F2} | RefCell: {referenceCellSizeX:F2}x{referenceCellSizeY:F2} | Spacing: {spacingX:F2},{spacingY:F2} | Padding: L{paddingLeft:F0} R{paddingRight:F0} T{paddingTop:F0} B{paddingBottom:F0}");
        
        // Calculate total grid size at reference scale
        // Grid width = 5 cells + 4 spacings (spacing is gap between cells)
        float referenceGridWidth = (gridWidth * referenceCellSizeX) + ((gridWidth - 1) * spacingX);
        // Grid height = 5 cells + 4 spacings
        float referenceGridHeight = (gridHeight * referenceCellSizeY) + ((gridHeight - 1) * spacingY);
        
        // Calculate scale factors for width and height
        float scaleX = availableWidth / referenceGridWidth;
        float scaleY = availableHeight / referenceGridHeight;
        
        // Use the smaller scale factor to ensure grid fits in both dimensions
        scaleFactor = Mathf.Min(scaleX, scaleY);
        
        // Calculate scaled cell sizes
        currentCellSizeX = referenceCellSizeX * scaleFactor;
        currentCellSizeY = referenceCellSizeY * scaleFactor;
        
        // Calculate actual grid size with scaled cells (spacing stays constant)
        float actualGridWidth = (gridWidth * currentCellSizeX) + ((gridWidth - 1) * spacingX);
        float actualGridHeight = (gridHeight * currentCellSizeY) + ((gridHeight - 1) * spacingY);
        
        // Calculate start position to center the grid within NumberGrid bounds
        // Position is for the CENTER of the first cell (top-left)
        // In UI coordinates: (0,0) is center, positive X is right, positive Y is up
        // Account for individual padding (offset horizontally by paddingLeft-paddingRight, vertically by paddingTop-paddingBottom)
        float horizontalPaddingOffset = (paddingLeft - paddingRight) / 2f;
        float verticalPaddingOffset = (paddingTop - paddingBottom) / 2f;
        
        startPosition = new Vector2(
            -actualGridWidth / 2f + currentCellSizeX / 2f + horizontalPaddingOffset,  // Center horizontally, then offset to first cell center, then adjust for padding difference
            actualGridHeight / 2f - currentCellSizeY / 2f - verticalPaddingOffset     // Center vertically (top), then offset to first cell center, then adjust for padding difference
        );
        
        Debug.Log($"[NumberGrid] Grid scale calculated - Available: {availableWidth:F2}x{availableHeight:F2}, Scale Factor: {scaleFactor:F3}, Cell Size: {currentCellSizeX:F2}x{currentCellSizeY:F2}, Grid Size: {actualGridWidth:F2}x{actualGridHeight:F2}, Start Position: {startPosition}");
    }
    
    public void SetOwner(string userId)
    {
        this.ownerUserId = userId;
    }
    
    public void GenerateGridFromMatrix(int[] matrix)
    {
        gridGenerated = true; // Set flag so Start() doesn't overwrite this
        tapSequence = 0; // Reset sequence for new match
        ClearGrid();
        CalculateGridScale();
        currentDiceSumPlayer = -1;
        currentDiceSumCPU = -1;
        cpuDiceSums.Clear();
        cpuRollIds.Clear();

        if (matrix == null || matrix.Length != 25)
        {
            Debug.LogError("[NumberGrid] Invalid matrix length! Falling back to random generation.");
            GenerateGrid();
            return;
        }

        // Create a shuffled list of available powerup sprite indices to ensure uniqueness
        List<int> availablePowerupIndices = new List<int>();
        for (int p = 0; p < powerupSprites.Length; p++) availablePowerupIndices.Add(p);
        ShuffleList(availablePowerupIndices);
        int powerupAssignmentCount = 0;

        for (int i = 0; i < matrix.Length; i++)
        {
            int number = matrix[i];
            if (number <= 0)
            {
                // Use the next unique powerup sprite from our shuffled list
                int spriteIdx = 0;
                if (powerupAssignmentCount < availablePowerupIndices.Count)
                {
                    spriteIdx = availablePowerupIndices[powerupAssignmentCount];
                    powerupAssignmentCount++;
                }
                else
                {
                    // Fallback if there are more than 3 powerups (shouldn't happen)
                    spriteIdx = UnityEngine.Random.Range(0, powerupSprites.Length);
                }
                
                CreatePowerupBox(spriteIdx, i);
            }
            else
            {
                CreateNumberBox(number, i);
            }
        }
        
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedTimerStartAfterGrid());
        }
        Debug.Log("[NumberGrid] Grid generated successfully from backend matrix!");
    }

    /// <summary>
    /// Generates the 5x5 grid with random positioning
    /// </summary>
    public void GenerateGrid()
    {
        gridGenerated = true;
        // Clear existing boxes if any
        ClearGrid();
        
        // Calculate scale factor and cell sizes based on NumberGrid RectTransform size
        CalculateGridScale();
        
        // Reset dice sums for both players
        currentDiceSumPlayer = -1;
        currentDiceSumCPU = -1;
        
        // Reset CPU dice sums and roll IDs dictionaries
        cpuDiceSums.Clear();
        cpuRollIds.Clear();
        
        // Create list of all grid items (22 numbers + 3 powerups)
        List<GridItem> gridItems = new List<GridItem>();
        
        // Add numbers (2-12, each appearing twice)
        for (int i = MIN_NUMBER; i <= MAX_NUMBER; i++)
        {
            gridItems.Add(new GridItem { isPowerup = false, number = i });
            gridItems.Add(new GridItem { isPowerup = false, number = i });
        }
        
        // Add powerups with unique sprite indices (no duplicates)
        System.Random rng = new System.Random();
        
        // Create a list of all available sprite indices
        List<int> availableSpriteIndices = new List<int>();
        for (int i = 0; i < powerupSprites.Length; i++)
        {
            availableSpriteIndices.Add(i);
        }
        
        // Shuffle the indices to randomize
        int n = availableSpriteIndices.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int value = availableSpriteIndices[k];
            availableSpriteIndices[k] = availableSpriteIndices[n];
            availableSpriteIndices[n] = value;
        }
        
        // Take the first POWERUP_COUNT unique indices
        for (int i = 0; i < POWERUP_COUNT && i < availableSpriteIndices.Count; i++)
        {
            gridItems.Add(new GridItem { isPowerup = true, powerupIndex = availableSpriteIndices[i] });
        }
        
        // Shuffle all items together for complete randomness
        ShuffleList(gridItems);
        
        // Create list of positions (0-24 for 5x5 grid)
        List<int> positions = new List<int>();
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            positions.Add(i);
        }
        
        // Shuffle positions for randomness
        ShuffleList(positions);
        
        // Place all items in shuffled positions
        for (int i = 0; i < gridItems.Count && i < positions.Count; i++)
        {
            int posIndex = positions[i];
            if (gridItems[i].isPowerup)
            {
                CreatePowerupBox(gridItems[i].powerupIndex, posIndex);
            }
            else
            {
                CreateNumberBox(gridItems[i].number, posIndex);
            }
        }
        
        // Restart powerup timer after grid is generated (wait one frame to ensure all boxes are created)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedTimerStartAfterGrid());
        }
    }
    
    /// <summary>
    /// Delays timer start by one frame after grid generation
    /// NOTE: Timer is NOT auto-started anymore - must be started by GameManager after countdown
    /// </summary>
    private IEnumerator DelayedTimerStartAfterGrid()
    {
        yield return null; // Wait one frame to ensure all boxes are fully created
        // Powerup timer is now started by GameManager after "3, 2, 1" countdown
        Debug.Log("[NumberGrid] Grid generated - waiting for GameManager to start powerup timer after countdown");
    }
    
    /// <summary>
    /// Public method to start powerup timer from GameManager (called after countdown)
    /// </summary>
    public void StartPowerupTimerFromGameManager()
    {
        StartPowerupTimer();
        Debug.Log("[NumberGrid] Powerup timer started by GameManager after countdown!");
    }
    
    /// <summary>
    /// Creates a number box at the specified grid position
    /// </summary>
    private void CreateNumberBox(int number, int gridPosition)
    {
        if (numberBoxPrefab == null)
        {
            Debug.LogError("[NumberGrid] Number box prefab is not assigned!");
            return;
        }
        
        // Calculate grid position (row and column)
        int row = gridPosition / gridWidth;
        int col = gridPosition % gridWidth;
        
        // Instantiate the box
        GameObject box = Instantiate(numberBoxPrefab, transform);
        
        // Scale and position the box
        SetSizeAndPositionBox(box, row, col);
        
        instantiatedBoxes.Add(box);
        
        // Set background sprite
        Image bgImage = FindComponentByName<Image>(box, boxBackgroundImageName);
        if (bgImage == null) bgImage = box.GetComponent<Image>(); // Fallback to root
        if (bgImage != null && normalBoxBgSprite != null)
        {
            bgImage.sprite = normalBoxBgSprite;
        }
        
        // Create box data
        BoxData boxData = new BoxData
        {
            boxObject = box,
            number = number,
            isPowerup = false,
            isSelected = false,
            selectedByPlayer = PlayerType.Player,
            selectedByCPUIndex = -1,
            selectedInRollSum = -1,
            selectedInRollId = -1
        };
        
        // Find and set number text
        TextMeshProUGUI numberText = FindComponentInChildren<TextMeshProUGUI>(box, numberTextName);
        TextMeshProUGUI numberTextShadow = FindComponentInChildren<TextMeshProUGUI>(box, numberTextNameShadow);
        if (numberText != null)
        {
            numberText.text = number.ToString();
            numberTextShadow.text = number.ToString();
            
            // Scale font size proportionally with cell size
            float scaledFontSize = referenceFontSize * scaleFactor;
            numberText.fontSize = scaledFontSize;
            numberTextShadow.fontSize = scaledFontSize;
            
            // Disable auto-sizing to use our calculated font size
            numberText.enableAutoSizing = false;
            numberTextShadow.enableAutoSizing = false;
            
            numberText.gameObject.SetActive(true);
            numberTextShadow.gameObject.SetActive(true);
        }
        else
        {
            // Try Text component as fallback
            Text textComponent = FindComponentInChildren<Text>(box, numberTextName);
            
            if (textComponent != null)
            {
                textComponent.text = number.ToString();
                textComponent.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[NumberGrid] Could not find number text component '{numberTextName}' in prefab!");
            }
        }
        
        // Find and disable powerup image
        Image powerupImage = FindComponentInChildren<Image>(box, powerupImageName);
        if (powerupImage != null)
        {
            powerupImage.gameObject.SetActive(false);
        }
        
        // Find select button
        Button selectButton = FindComponentInChildren<Button>(box, selectButtonName);
        if (selectButton != null)
        {
            boxData.selectButton = selectButton;
            selectButton.interactable = false; // Initially disabled
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => OnBoxButtonClicked(boxData));
        }
        else
        {
            Debug.LogWarning($"[NumberGrid] Could not find select button component '{selectButtonName}' in prefab!");
        }
        
        // Find highlight image
        Image highlightImage = FindComponentInChildren<Image>(box, highlightImageName);
        if (highlightImage != null)
        {
            boxData.highlightImage = highlightImage;
            highlightImage.gameObject.SetActive(false); // Initially hidden
        }
        else
        {
            Debug.LogWarning($"[NumberGrid] Could not find highlight image component '{highlightImageName}' in prefab!");
        }
        
        boxDataList.Add(boxData);
    }
    
    /// <summary>
    /// Creates a powerup box at the specified grid position
    /// </summary>
    private void CreatePowerupBox(int powerupSpriteIndex, int gridPosition)
    {
        if (numberBoxPrefab == null)
        {
            Debug.LogError("[NumberGrid] Number box prefab is not assigned!");
            return;
        }
        
        if (powerupSpriteIndex < 0 || powerupSpriteIndex >= powerupSprites.Length || powerupSprites[powerupSpriteIndex] == null)
        {
            Debug.LogWarning($"[NumberGrid] Invalid powerup sprite index: {powerupSpriteIndex}");
            return;
        }
        
        // Calculate grid position (row and column)
        int row = gridPosition / gridWidth;
        int col = gridPosition % gridWidth;
        
        // Instantiate the box
        GameObject box = Instantiate(numberBoxPrefab, transform);
        
        // Set size and position the box
        SetSizeAndPositionBox(box, row, col);
        
        instantiatedBoxes.Add(box);
        
        // Set background sprite
        Image bgImage = FindComponentByName<Image>(box, boxBackgroundImageName);
        if (bgImage == null) bgImage = box.GetComponent<Image>(); // Fallback to root
        if (bgImage != null && powerupBoxBgSprite != null)
        {
            bgImage.sprite = powerupBoxBgSprite;
        }
        
        // Create box data for powerup (powerups are not selectable)
        BoxData boxData = new BoxData
        {
            boxObject = box,
            number = -1, // Powerups don't have numbers
            isPowerup = true,
            isSelected = false,
            selectedByPlayer = PlayerType.Player,
            selectedByCPUIndex = -1,
            selectedInRollSum = -1,
            selectedInRollId = -1,
            powerupSpriteIndex = powerupSpriteIndex // Store the sprite index
        };
        
        // Find and disable number text
        TextMeshProUGUI numberText = FindComponentInChildren<TextMeshProUGUI>(box, numberTextName);
        TextMeshProUGUI numberTextShadow = FindComponentInChildren<TextMeshProUGUI>(box ,  numberTextNameShadow);
        if (numberText != null)
        {
            numberText.gameObject.SetActive(false);
            numberTextShadow.gameObject.SetActive(false);
        }
        else
        {
            // Try Text component as fallback
            Text textComponent = FindComponentInChildren<Text>(box, numberTextName);
            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(false);
            }
        }
        
        // Find and store powerup image with sprite (start visible but inactive)
        Image powerupImage = FindComponentInChildren<Image>(box, powerupImageName);
        if (powerupImage != null)
        {
            powerupImage.sprite = powerupSprites[powerupSpriteIndex];
            powerupImage.gameObject.SetActive(true); // Start visible
            boxData.powerupImage = powerupImage; // Store reference
        }
        else
        {
            Debug.LogWarning($"[NumberGrid] Could not find powerup image component '{powerupImageName}' in prefab!");
        }
        
        // Find select button (powerups are selectable only when visible)
        Button selectButton = FindComponentInChildren<Button>(box, selectButtonName);
        if (selectButton != null)
        {
            boxData.selectButton = selectButton;
            selectButton.interactable = false; // Initially disabled (only enabled when visible)
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => OnPowerupButtonClicked(boxData));
        }
        
        // Find and store highlight image (for showing when powerup is selected)
        Image highlightImage = FindComponentInChildren<Image>(box, highlightImageName);
        if (highlightImage != null)
        {
            boxData.highlightImage = highlightImage; // Store reference for when powerup is selected
            highlightImage.gameObject.SetActive(false); // Initially hidden
        }
        else
        {
            Debug.LogWarning($"[NumberGrid] Could not find highlight image component '{highlightImageName}' in prefab!");
        }
        
        boxDataList.Add(boxData);
    }
    
    /// <summary>
    /// Finds a component in children by name
    /// </summary>
    private T FindComponentInChildren<T>(GameObject parent, string name) where T : Component
    {
        if (parent == null) return null;
        
        // Check if the parent itself matches
        if (parent.name == name)
        {
            T component = parent.GetComponent<T>();
            if (component != null) return component;
        }
        
        // Search in children
        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
            {
                T component = child.GetComponent<T>();
                if (component != null) return component;
            }
            
            // Recursively search in grandchildren
            T found = FindComponentInChildren<T>(child.gameObject, name);
            if (found != null) return found;
        }
        
        return null;
    }

    /// <summary>
    /// Finds a component in children by name (non-recursive first, then root)
    /// </summary>
    private T FindComponentByName<T>(GameObject parent, string name) where T : Component
    {
        if (parent == null) return null;
        
        // Check children first
        Transform child = parent.transform.Find(name);
        if (child != null)
        {
            return child.GetComponent<T>();
        }
        
        // If name matches parent
        if (parent.name == name)
        {
            return parent.GetComponent<T>();
        }
        
        return null;
    }
    
    /// <summary>
    /// Shuffles a list using Fisher-Yates algorithm
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    
    /// <summary>
    /// Clears all instantiated boxes
    /// </summary>
    public void ClearGrid()
    {
        // Stop powerup timer
        StopPowerupTimer();
        
        foreach (GameObject box in instantiatedBoxes)
        {
            if (box != null)
            {
                DestroyImmediate(box);
            }
        }
        instantiatedBoxes.Clear();
        boxDataList.Clear();
        currentDiceSumPlayer = -1;
        currentDiceSumCPU = -1;
        currentlyVisiblePowerup = null;
        
        // Clear CPU dice sums and roll IDs dictionaries
        cpuDiceSums.Clear();
        cpuRollIds.Clear();
    }
    
    /// <summary>
    /// Sets the dice sum for a specific player and enables/disables boxes accordingly
    /// </summary>
    public void SetDiceSum(int sum, PlayerType playerType = PlayerType.Player, int cpuIndex = 0)
    {
        // Every time SetDiceSum is called, it's a new roll (dice were rolled again)
        // Increment roll ID to distinguish between rolls, even if the sum is the same
        if (playerType == PlayerType.Player)
        {
            currentRollIdPlayer++;
            currentDiceSumPlayer = sum;
            tapSequence = 0; // Reset sequence for anti-cheat on every new roll
        }
        else // CPU
        {
            // Track per CPU index
            if (!cpuRollIds.ContainsKey(cpuIndex))
            {
                cpuRollIds[cpuIndex] = 0;
            }
            cpuRollIds[cpuIndex]++;
            
            if (!cpuDiceSums.ContainsKey(cpuIndex))
            {
                cpuDiceSums[cpuIndex] = -1;
            }
            cpuDiceSums[cpuIndex] = sum;
            
            // Also update legacy variable for backward compatibility
            currentRollIdCPU = cpuRollIds[cpuIndex];
            currentDiceSumCPU = sum;
        }
        
        // Update interactability for the specific player
        UpdateBoxInteractability(playerType);
        
        int availableCount = GetAvailableBoxCount(sum, playerType);
        int rollId = (playerType == PlayerType.Player) ? currentRollIdPlayer : (cpuRollIds.ContainsKey(cpuIndex) ? cpuRollIds[cpuIndex] : currentRollIdCPU);
        Debug.Log($"[NumberGrid] {playerType} (CPU index: {cpuIndex}) dice sum set to {sum}. {availableCount} box(es) with number {sum} are now selectable. Roll ID: {rollId}");
    }
    
    /// <summary>
    /// Clears the current dice sum for a specific player (disables all buttons for that player)
    /// </summary>
    public void ClearDiceSum(PlayerType playerType = PlayerType.Player)
    {
        if (playerType == PlayerType.Player)
        {
            currentDiceSumPlayer = -1;
        }
        else // CPU
        {
            currentDiceSumCPU = -1;
        }
        UpdateBoxInteractability(playerType);
    }
    
    /// <summary>
    /// Updates button interactability based on current dice sum for a specific player
    /// </summary>
    private void UpdateBoxInteractability(PlayerType playerType = PlayerType.Player)
    {
        int currentDiceSum = (playerType == PlayerType.Player) ? currentDiceSumPlayer : currentDiceSumCPU;
        
        foreach (BoxData boxData in boxDataList)
        {
            if (boxData.isPowerup || boxData.selectButton == null)
            {
                continue; // Skip powerups and boxes without buttons
            }
            
            // For Player: Enable button only if box number matches player's dice sum and box is not selected
            // For CPU: Buttons are not interactable (CPU selects programmatically)
            if (playerType == PlayerType.Player)
            {
                // Allow all unselected boxes to be interactable so we can detect "Wrong" clicks for -50 penalty
                bool shouldBeInteractable = !boxData.isSelected && currentDiceSum > 0;
                boxData.selectButton.interactable = shouldBeInteractable;
            }
            // CPU doesn't use buttons, so we don't need to update button interactability for CPU
        }
    }
    
    /// <summary>
    /// Called when a box button is clicked
    /// </summary>
    private void OnBoxButtonClicked(BoxData boxData)
    {
        if (boxData.isSelected)
        {
            // Box is already selected and locked - do nothing
            return;
        }
        
        if (boxData.isPowerup)
        {
            Debug.LogWarning("[NumberGrid] Cannot select powerup boxes!");
            return;
        }
        
        // REMOVED local checks for dice sum and duplicate taps.
        // We now send EVERY tap to the server so it can validate and penalize (-50) if wrong.
        
        // Store this as the last tapped box for visual feedback from server
        lastTappedBoxData = boxData;

        // EMIT TAP TO SERVER instead of locking locally
        GameManager gm = FindObjectOfType<GameManager>();
        string roomId = gm != null ? gm.GetCurrentRoomId() : null;
        
        if (!string.IsNullOrEmpty(roomId))
        {
            // Find index of this boxData in our list
            int index = boxDataList.IndexOf(boxData);
            if (index != -1)
            {
                MatchmakingModel.EmitTapNumber(roomId, index, boxData.number, tapSequence);
                tapSequence++; // Increment AFTER sending so the first tap is 0
            }
        }
        else
        {
            // Fallback for offline/Photon testing (original logic)
            PerformLocalBoxLock(boxData, PlayerType.Player);
        }
    }

    /// <summary>
    /// Processes a number lock confirmation from the server
    /// </summary>
    private void HandleServerNumberLocked(NumberLockedData data)
    {
        // Check if this lock belongs to this grid's owner
        if (data.userId != ownerUserId) return;
        
        if (data.index >= 0 && data.index < boxDataList.Count)
        {
            BoxData boxData = boxDataList[data.index];
            if (!boxData.isSelected)
            {
                if (boxData.isPowerup)
                {
                    // Powerup lock handled specifically to stop timers and pulsar effects
                    LockPowerup(boxData, PlayerType.Player, -1);
                    
                    // Trigger events (same as OnPowerupButtonClicked)
                    OnBoxSelected?.Invoke(this, -1, boxData.boxObject);
                    CheckIfAllBoxesSelected();
                }
                else
                {
                    PerformLocalBoxLock(boxData, PlayerType.Player);
                }

                // Stop server-driven powerup UI update if this was the active powerup
                if (currentlyVisiblePowerup == boxData && serverPowerupUIUpdateCoroutine != null)
                {
                    StopCoroutine(serverPowerupUIUpdateCoroutine);
                    serverPowerupUIUpdateCoroutine = null;
                }
            }
        }
    }

    private void HandleServerTapResult(TapResultData data)
    {
        if (!data.correct)
        {
            // Trigger the "Wrong" visual effect on the specific box that was tapped
            GameObject targetObj = (lastTappedBoxData != null) ? lastTappedBoxData.boxObject : null;
            int num = (lastTappedBoxData != null) ? lastTappedBoxData.number : -1;
            
            OnWrongBoxSelected?.Invoke(this, num, targetObj); 
            Debug.Log("<color=#FF0000>❌ Server says: WRONG TAP!</color>");
        }
        else
        {
            Debug.Log("<color=#00FF00>✅ Server says: CORRECT TAP!</color>");
        }
    }

    /// <summary>
    /// Physically locks a box on the grid
    /// </summary>
    private void PerformLocalBoxLock(BoxData boxData, PlayerType playerType)
    {
        // Mark as selected (this locks the selection)
        boxData.isSelected = true;
        boxData.selectedByPlayer = playerType;
        boxData.selectedByCPUIndex = -1;
        boxData.selectedInRollSum = (playerType == PlayerType.Player) ? currentDiceSumPlayer : currentDiceSumCPU;
        boxData.selectedInRollId = (playerType == PlayerType.Player) ? currentRollIdPlayer : currentRollIdCPU;
        
        // Disable this button (permanently locked)
        if (boxData.selectButton != null)
        {
            boxData.selectButton.interactable = false;
        }
        
        // Show highlight
        if (boxData.highlightImage != null)
        {
            boxData.highlightImage.gameObject.SetActive(true);
        }
        
        // Trigger event
        OnBoxSelected?.Invoke(this, boxData.number, boxData.boxObject);
        
        Debug.Log($"[NumberGrid] Box with index {boxDataList.IndexOf(boxData)} (number {boxData.number}) locked by server!");
        
        // Check if all boxes are selected
        CheckIfAllBoxesSelected();
    }
    
    /// <summary>
    /// Checks if any box with the given number is already selected by a specific player in the CURRENT roll
    /// This prevents selecting a second box with the same number in the same roll (per player)
    /// </summary>
    private bool IsAnyBoxWithNumberSelectedInCurrentRoll(int number, PlayerType playerType, int cpuIndex = 0)
    {
        int currentRollId;
        if (playerType == PlayerType.Player)
        {
            currentRollId = currentRollIdPlayer;
        }
        else // CPU
        {
            currentRollId = cpuRollIds.ContainsKey(cpuIndex) ? cpuRollIds[cpuIndex] : currentRollIdCPU;
        }
        
        foreach (BoxData boxData in boxDataList)
        {
            // Check if box is selected by this player AND was selected in the current roll (same roll ID)
            // For CPUs, also check if it's the same CPU index
            if (!boxData.isPowerup && 
                boxData.number == number && 
                boxData.isSelected && 
                boxData.selectedByPlayer == playerType &&
                boxData.selectedInRollId == currentRollId)
            {
                // For CPUs, also check if it's the same CPU index
                if (playerType == PlayerType.CPU)
                {
                    if (boxData.selectedByCPUIndex == cpuIndex)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Resets all selections (useful for new round)
    /// </summary>
    public void ResetSelections()
    {
        foreach (BoxData boxData in boxDataList)
        {
            boxData.isSelected = false;
            boxData.selectedByPlayer = PlayerType.Player;
            boxData.selectedByCPUIndex = -1;
            boxData.selectedInRollSum = -1;
            boxData.selectedInRollId = -1;
            
            if (boxData.highlightImage != null)
            {
                boxData.highlightImage.gameObject.SetActive(false);
            }
            
            // Reset powerup visibility state (but don't show/hide - timer will handle that)
            if (boxData.isPowerup)
            {
                boxData.isPowerupVisible = false;
                // Button will be enabled/disabled by timer system
            }
        }
        
        currentDiceSumPlayer = -1;
        currentDiceSumCPU = -1;
        currentRollIdPlayer = 0;
        currentRollIdCPU = 0;
        UpdateBoxInteractability(PlayerType.Player);
    }
    
    /// <summary>
    /// Gets the number of available (unselected) boxes for a given sum and player
    /// </summary>
    public int GetAvailableBoxCount(int sum, PlayerType playerType = PlayerType.Player, int cpuIndex = 0)
    {
        int count = 0;
        int currentRollId;
        if (playerType == PlayerType.Player)
        {
            currentRollId = currentRollIdPlayer;
        }
        else
        {
            currentRollId = cpuRollIds.ContainsKey(cpuIndex) ? cpuRollIds[cpuIndex] : currentRollIdCPU;
        }
        
        foreach (BoxData boxData in boxDataList)
        {
            if (!boxData.isPowerup && !boxData.isSelected && boxData.number == sum)
            {
                // Check if another box with the same number is already selected by this player in current roll
                bool anotherSelected = IsAnyBoxWithNumberSelectedInCurrentRoll(sum, playerType, cpuIndex);
                if (!anotherSelected)
                {
                    count++;
                }
            }
        }
        return count;
    }
    
    /// <summary>
    /// Gets a list of available (unselected) box GameObjects for a given sum and player
    /// </summary>
    public List<GameObject> GetAvailableBoxes(int sum, PlayerType playerType = PlayerType.Player, int cpuIndex = 0)
    {
        List<GameObject> availableBoxes = new List<GameObject>();
        
        foreach (BoxData boxData in boxDataList)
        {
            if (!boxData.isPowerup && !boxData.isSelected && boxData.number == sum)
            {
                // Check if another box with the same number is already selected by this player in current roll
                bool anotherSelected = IsAnyBoxWithNumberSelectedInCurrentRoll(sum, playerType, cpuIndex);
                if (!anotherSelected)
                {
                    availableBoxes.Add(boxData.boxObject);
                }
            }
        }
        return availableBoxes;
    }
    
    /// <summary>
    /// Selects a box programmatically (for CPU or other automated selection)
    /// </summary>
    public bool SelectBox(GameObject boxObject, bool isCPU = false)
    {
        // Convert bool to PlayerType for backward compatibility
        PlayerType playerType = isCPU ? PlayerType.CPU : PlayerType.Player;
        return SelectBox(boxObject, playerType);
    }
    
    /// <summary>
    /// Selects a box programmatically (for CPU or other automated selection) - with PlayerType and CPU index
    /// </summary>
    public bool SelectBox(GameObject boxObject, PlayerType playerType, int cpuIndex = 0)
    {
        // Find the box data
        BoxData boxData = boxDataList.Find(b => b.boxObject == boxObject);
        if (boxData == null)
        {
            Debug.LogWarning($"[NumberGrid] Box object not found in boxDataList!");
            return false;
        }
        
        if (boxData.isSelected)
        {
            Debug.LogWarning($"[NumberGrid] Box with number {boxData.number} is already selected!");
            return false;
        }
        
        if (boxData.isPowerup)
        {
            Debug.LogWarning("[NumberGrid] Cannot select powerup boxes!");
            return false;
        }
        
        // Get the current dice sum for this player
        int currentDiceSum;
        if (playerType == PlayerType.Player)
        {
            currentDiceSum = currentDiceSumPlayer;
        }
        else // CPU
        {
            currentDiceSum = cpuDiceSums.ContainsKey(cpuIndex) ? cpuDiceSums[cpuIndex] : currentDiceSumCPU;
        }
        
        if (currentDiceSum <= 0 || boxData.number != currentDiceSum)
        {
            Debug.LogWarning($"[NumberGrid] Cannot select box with number {boxData.number}. Current {playerType} (CPU index: {cpuIndex}) dice sum is {currentDiceSum}!");
            return false;
        }
        
        // Check if another box with the same number is already selected by THIS player/CPU in THIS roll
        bool anotherBoxSelectedInThisRoll = IsAnyBoxWithNumberSelectedInCurrentRoll(boxData.number, playerType, cpuIndex);
        if (anotherBoxSelectedInThisRoll)
        {
            Debug.LogWarning($"[NumberGrid] Another box with number {boxData.number} is already selected by {playerType} (CPU index: {cpuIndex}) in this roll. Cannot select another one.");
            return false;
        }
        
        // Get the current roll ID for this player
        int currentRollId;
        if (playerType == PlayerType.Player)
        {
            currentRollId = currentRollIdPlayer;
        }
        else // CPU
        {
            currentRollId = cpuRollIds.ContainsKey(cpuIndex) ? cpuRollIds[cpuIndex] : currentRollIdCPU;
        }
        
        // Mark as selected
        boxData.isSelected = true;
        boxData.selectedByPlayer = playerType;
        boxData.selectedByCPUIndex = (playerType == PlayerType.CPU) ? cpuIndex : -1; // Store the CPU index
        boxData.selectedInRollSum = currentDiceSum;
        boxData.selectedInRollId = currentRollId;
        
        // Disable button (if it's a player selection)
        if (boxData.selectButton != null && playerType == PlayerType.Player)
        {
            boxData.selectButton.interactable = false;
        }
        
        // Show highlight (only for number boxes, not powerups)
        if (boxData.highlightImage != null && !boxData.isPowerup)
        {
            boxData.highlightImage.gameObject.SetActive(true);
        }
        
        // Trigger event
        OnBoxSelected?.Invoke(this, boxData.number, boxData.boxObject);
        
        Debug.Log($"[NumberGrid] Box with number {boxData.number} selected by {playerType}!");
        
        // Check if all boxes are selected
        CheckIfAllBoxesSelected();
        
        return true;
    }
    
    /// <summary>
    /// Checks if all selectable boxes (non-powerup) are selected
    /// </summary>
    private void CheckIfAllBoxesSelected()
    {
        int totalSelectableBoxes = 0;
        int selectedBoxes = 0;
        
        foreach (BoxData boxData in boxDataList)
        {
            if (!boxData.isPowerup) // Only count non-powerup boxes
            {
                totalSelectableBoxes++;
                if (boxData.isSelected)
                {
                    selectedBoxes++;
                }
            }
        }
        
        // If all selectable boxes are selected, trigger event
        if (selectedBoxes >= totalSelectableBoxes && totalSelectableBoxes > 0)
        {
            Debug.Log($"[NumberGrid] All {totalSelectableBoxes} selectable boxes are selected!");
            OnAllBoxesSelected?.Invoke();
        }
    }
    
    /// <summary>
    /// Gets the count of selected boxes for each player/CPU (includes powerups)
    /// Returns: Dictionary with PlayerType and CPU index as key, count as value
    /// Key format: "Player" for player, "CPU_0" for CPU index 0, "CPU_1" for CPU index 1, etc.
    /// </summary>
    public Dictionary<string, int> GetSelectionCounts()
    {
        Dictionary<string, int> selectionCounts = new Dictionary<string, int>();
        
        foreach (BoxData boxData in boxDataList)
        {
            if (boxData.isSelected) // Include both number boxes and powerups
            {
                string key;
                if (boxData.selectedByPlayer == PlayerType.Player)
                {
                    key = "Player";
                }
                else // CPU
                {
                    key = $"CPU_{boxData.selectedByCPUIndex}";
                }
                
                if (selectionCounts.ContainsKey(key))
                {
                    selectionCounts[key]++;
                }
                else
                {
                    selectionCounts[key] = 1;
                }
            }
        }
        
        return selectionCounts;
    }

    /// <summary>
    /// Gets the total points based on the new scoring system:
    /// - Number box: +100
    /// - Power-up icon: +200
    /// - All boxes complete: +1200
    /// </summary>
    public int GetTotalPoints(string playerKey)
    {
        int points = 0;
        int selectedCount = 0;
        int totalSelectable = 0;

        foreach (BoxData boxData in boxDataList)
        {
            if (!boxData.isPowerup) totalSelectable++;

            if (boxData.isSelected)
            {
                string key = (boxData.selectedByPlayer == PlayerType.Player) ? "Player" : $"CPU_{boxData.selectedByCPUIndex}";
                if (key == playerKey)
                {
                    if (boxData.isPowerup) points += 200;
                    else points += 100;
                    
                    if (!boxData.isPowerup) selectedCount++;
                }
            }
        }

        // Completion bonus
        if (selectedCount >= totalSelectable && totalSelectable > 0)
        {
            points += 1200;
        }

        return points;
    }
    
    /// <summary>
    /// Gets the winner information
    /// Returns: Tuple with (PlayerType, cpuIndex, selectionCount)
    /// cpuIndex is -1 for player, 0+ for CPUs
    /// </summary>
    public (PlayerType playerType, int cpuIndex, int count) GetWinner()
    {
        Dictionary<string, int> selectionCounts = GetSelectionCounts();
        
        string winnerKey = null;
        int maxCount = -1;
        
        foreach (var kvp in selectionCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                winnerKey = kvp.Key;
            }
        }
        
        if (winnerKey == null)
        {
            return (PlayerType.Player, -1, 0);
        }
        
        if (winnerKey == "Player")
        {
            return (PlayerType.Player, -1, maxCount);
        }
        else if (winnerKey.StartsWith("CPU_"))
        {
            string indexStr = winnerKey.Substring(4); // Remove "CPU_" prefix
            if (int.TryParse(indexStr, out int cpuIndex))
            {
                return (PlayerType.CPU, cpuIndex, maxCount);
            }
        }
        
        return (PlayerType.Player, -1, 0);
    }
    
    /// <summary>
    /// Gets the current dice sum for a specific player
    /// </summary>
    public int GetCurrentDiceSum(PlayerType playerType = PlayerType.Player)
    {
        return (playerType == PlayerType.Player) ? currentDiceSumPlayer : currentDiceSumCPU;
    }
    
    /// <summary>
    /// Sets the player's outline color
    /// </summary>
    public void SetPlayerOutlineColor(Color color)
    {
        playerOutlineColor = color;
        Debug.Log($"[NumberGrid] Player outline color set to: {color}");
    }
    
    /// <summary>
    /// Sets a CPU's outline color by index
    /// </summary>
    public void SetCPUOutlineColor(int cpuIndex, Color color)
    {
        cpuOutlineColors[cpuIndex] = color;
        Debug.Log($"[NumberGrid] CPU {cpuIndex} outline color set to: {color}");
    }
    
    /// <summary>
    /// Gets a CPU's outline color by index
    /// </summary>
    public Color GetCPUOutlineColor(int cpuIndex)
    {
        if (cpuOutlineColors.ContainsKey(cpuIndex))
        {
            return cpuOutlineColors[cpuIndex];
        }
        return Color.red; // Default fallback
    }
    
    /// <summary>
    /// Gets the currently visible powerup GameObject (null if no powerup is visible)
    /// </summary>
    public GameObject GetCurrentlyVisiblePowerup()
    {
        if (currentlyVisiblePowerup != null && currentlyVisiblePowerup.isPowerupVisible && !currentlyVisiblePowerup.isSelected)
        {
            return currentlyVisiblePowerup.boxObject;
        }
        return null;
    }
    
    /// <summary>
    /// Regenerates the grid with new random positions
    /// </summary>
    [ContextMenu("Regenerate Grid")]
    public void RegenerateGrid()
    {
        GenerateGrid();
        // Timer is automatically started by GenerateGrid()
    }
    
    /// <summary>
    /// Starts the powerup timer system
    /// </summary>
    private void StartPowerupTimer()
    {
        // Only start if GameObject is active and enabled
        if (!gameObject.activeInHierarchy || !enabled)
        {
            Debug.LogWarning("[NumberGrid] Cannot start powerup timer - GameObject is not active!");
            return;
        }
        
        StopPowerupTimer(); // Stop any existing timer

        // If we are in a server-driven match, do NOT start the local timer loop.
        // Powerups will be activated by 'iconActivated' events from the server.
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && !string.IsNullOrEmpty(gm.GetCurrentRoomId()))
        {
            Debug.Log("[NumberGrid] Server-driven match detected. Local powerup timer loop disabled.");
            return;
        }
        
        // Only start if we have powerup boxes
        int powerupCount = boxDataList.Count(b => b.isPowerup);
        if (powerupCount == 0)
        {
            Debug.LogWarning("[NumberGrid] Cannot start powerup timer - no powerup boxes found!");
            return;
        }
        
        // Start the coroutine
        powerupTimerCoroutine = StartCoroutine(PowerupTimerLoop());
        Debug.Log($"[NumberGrid] Powerup timer started! ({powerupCount} powerup boxes available)");
    }
    
    /// <summary>
    /// Public method to restart the powerup timer (useful for debugging or external calls)
    /// </summary>
    public void RestartPowerupTimer()
    {
        StartPowerupTimer();
    }
    
    /// <summary>
    /// Stops the powerup timer system
    /// </summary>
    private void StopPowerupTimer()
    {
        if (powerupTimerCoroutine != null)
        {
            StopCoroutine(powerupTimerCoroutine);
            powerupTimerCoroutine = null;
        }
        
        // Hide currently visible powerup
        if (currentlyVisiblePowerup != null)
        {
            HidePowerup(currentlyVisiblePowerup);
        }
        
        // Hide and reset UI
        if (powerupTimerSlider != null)
        {
            powerupTimerSlider.value = 0f;
            powerupTimerSlider.gameObject.SetActive(false);
        }
        if (powerupTimerText != null)
        {
            powerupTimerText.text = "";
            powerupTimerText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Main loop for powerup timer system
    /// </summary>
    private IEnumerator PowerupTimerLoop()
    {
        while (true)
        {
            // Wait for random delay before showing next powerup
            float delay = Random.Range(minDelayBetweenPowerups, maxDelayBetweenPowerups);
            yield return new WaitForSeconds(delay);
            
            // Get list of all unselected powerup boxes
            List<BoxData> powerupBoxes = boxDataList.Where(b => b.isPowerup && !b.isSelected).ToList();
            
            if (powerupBoxes.Count > 0)
            {
                // Randomly select a powerup box
                int randomIndex = powerupRandom.Next(0, powerupBoxes.Count);
                BoxData selectedPowerup = powerupBoxes[randomIndex];
                
                // Show the powerup
                ShowPowerup(selectedPowerup);
                
                // Wait for visibility duration while updating timer
                // Check if powerup was selected during countdown
                float elapsed = 0f;
                while (elapsed < powerupVisibleDuration)
                {
                    // Check if powerup was selected (locked) during countdown
                    if (selectedPowerup.isSelected)
                    {
                        // Powerup was selected, stop timer for this powerup and continue loop
                        // Timer UI is already hidden by LockPowerup method
                        currentlyVisiblePowerup = null;
                        break;
                    }
                    
                    elapsed += Time.deltaTime;
                    float remaining = powerupVisibleDuration - elapsed;
                    
                    // Update slider (0 to 1, where 1 is full time elapsed - moves left to right)
                    if (powerupTimerSlider != null && powerupTimerSlider.gameObject.activeSelf)
                    {
                        powerupTimerSlider.value = elapsed / powerupVisibleDuration;
                    }
                    
                    // Update text (show remaining seconds)
                    if (powerupTimerText != null && powerupTimerText.gameObject.activeSelf)
                    {
                        powerupTimerText.text = Mathf.CeilToInt(remaining).ToString();
                    }
                    
                    yield return null;
                }
                
                // Only hide the powerup if it wasn't selected during countdown
                if (!selectedPowerup.isSelected)
                {
                    HidePowerup(selectedPowerup);
                }
            }
        }
    }
    
    /// <summary>
    /// Shows a powerup box (makes it selectable and starts animation)
    /// </summary>
    private void ShowPowerup(BoxData powerupBox)
    {
        if (powerupBox == null || !powerupBox.isPowerup) return;
        
        currentlyVisiblePowerup = powerupBox;
        powerupBox.isPowerupVisible = true;
        
        // Powerup image is already visible, just start animating it
        if (powerupBox.powerupImage != null)
        {
            // Kill any existing animation
            if (powerupBox.powerupAnimation != null)
            {
                powerupBox.powerupAnimation.Kill();
            }
            
            // Reset scale before starting animation
            powerupBox.powerupImage.transform.localScale = Vector3.one;
            
            // Create pulsing scale animation with DOTween
            // Scale up and down continuously in a loop
            powerupBox.powerupAnimation = powerupBox.powerupImage.transform
                .DOScale(1.2f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        // Enable button (make it selectable)
        if (powerupBox.selectButton != null)
        {
            powerupBox.selectButton.interactable = true;
        }
        
        // Show timer UI (slider and text)
        if (powerupTimerSlider != null)
        {
            powerupTimerSlider.gameObject.SetActive(true);
            powerupTimerSlider.value = 0f; // Start at empty (left side)
            
            // Set slider handle sprite to match the powerup sprite
            if (powerupBox.powerupSpriteIndex >= 0 && powerupBox.powerupSpriteIndex < powerupSprites.Length && powerupSprites[powerupBox.powerupSpriteIndex] != null)
            {
                // Get the handle RectTransform
                RectTransform handleRect = powerupTimerSlider.handleRect;
                if (handleRect != null)
                {
                    // Get the Image component from the handle
                    Image handleImage = handleRect.GetComponent<Image>();
                    if (handleImage != null)
                    {
                        handleImage.sprite = powerupSprites[powerupBox.powerupSpriteIndex];
                    }
                    else
                    {
                        Debug.LogWarning("[NumberGrid] Slider handle does not have an Image component!");
                    }
                    
                    // Scale the handle size based on the slider parent's scale factor
                    if (sliderParent != null)
                    {
                        // Calculate the parent's current scale by comparing to reference size
                        float parentScaleX = sliderParent.rect.width / referenceSliderParentWidth;
                        float parentScaleY = sliderParent.rect.height / referenceSliderParentHeight;
                        
                        // Use the smaller scale to maintain aspect ratio
                        float parentScale = Mathf.Min(parentScaleX, parentScaleY);
                        
                        // Apply scale to handle
                        float scaledHandleWidth = referenceHandleWidth * parentScale;
                        float scaledHandleHeight = referenceHandleHeight * parentScale;
                        handleRect.sizeDelta = new Vector2(scaledHandleWidth, scaledHandleHeight);
                        
                        Debug.Log($"[NumberGrid] Slider handle scaled - Parent Size: {sliderParent.rect.width:F2}x{sliderParent.rect.height:F2}, Parent Scale: {parentScale:F3}, Handle Size: {scaledHandleWidth:F2}x{scaledHandleHeight:F2}");
                    }
                    else
                    {
                        Debug.LogWarning("[NumberGrid] Slider parent RectTransform is not assigned! Cannot scale handle properly.");
                    }
                }
                else
                {
                    Debug.LogWarning("[NumberGrid] Slider handleRect is not assigned!");
                }
            }
        }
        if (powerupTimerText != null)
        {
            powerupTimerText.gameObject.SetActive(true);
            powerupTimerText.text = Mathf.CeilToInt(powerupVisibleDuration).ToString();
        }
        
        Debug.Log($"[NumberGrid] Powerup animation started for {powerupVisibleDuration} seconds!");
    }
    
    /// <summary>
    /// Hides a powerup box (stops animation and makes it unselectable, but keeps sprite visible)
    /// </summary>
    private void HidePowerup(BoxData powerupBox)
    {
        if (powerupBox == null || !powerupBox.isPowerup) return;
        
        if (currentlyVisiblePowerup == powerupBox)
        {
            currentlyVisiblePowerup = null;
        }
        
        powerupBox.isPowerupVisible = false;
        
        // Stop animation and reset scale (keep image visible)
        if (powerupBox.powerupImage != null)
        {
            // Kill the animation
            if (powerupBox.powerupAnimation != null)
            {
                powerupBox.powerupAnimation.Kill();
                powerupBox.powerupAnimation = null;
            }
            
            // Reset scale to normal
            powerupBox.powerupImage.transform.localScale = Vector3.one;
            
            // Keep image visible (don't disable it)
            // powerupBox.powerupImage.gameObject.SetActive(true); // Already visible
        }
        
        // Disable button (make it unselectable)
        if (powerupBox.selectButton != null)
        {
            powerupBox.selectButton.interactable = false;
        }
        
        // Hide timer UI (slider and text)
        if (powerupTimerSlider != null)
        {
            powerupTimerSlider.value = 0f;
            powerupTimerSlider.gameObject.SetActive(false);
        }
        if (powerupTimerText != null)
        {
            powerupTimerText.text = "";
            powerupTimerText.gameObject.SetActive(false);
        }
        
        Debug.Log($"[NumberGrid] Powerup animation stopped and disabled!");
    }
    
    /// <summary>
    /// Called when a powerup button is clicked
    /// </summary>
    private void OnPowerupButtonClicked(BoxData boxData)
    {
        if (boxData.isSelected)
        {
            // Box is already selected and locked - do nothing
            return;
        }
        
        if (!boxData.isPowerup)
        {
            Debug.LogWarning("[NumberGrid] This is not a powerup box!");
            return;
        }
        
        if (!boxData.isPowerupVisible)
        {
            Debug.LogWarning("[NumberGrid] Powerup is not currently visible!");
            return;
        }
        
        // Store this as the last tapped box for visual feedback from server
        lastTappedBoxData = boxData;

        // EMIT TAP TO SERVER instead of locking locally
        // This ensures the server validates the tap and broadcasts the lock to all players
        GameManager gm = FindObjectOfType<GameManager>();
        string roomId = gm != null ? gm.GetCurrentRoomId() : null;
        
        if (!string.IsNullOrEmpty(roomId))
        {
            // Find index of this boxData in our list
            int index = boxDataList.IndexOf(boxData);
            if (index != -1)
            {
                // Send 0 for powerups as per backend contract
                Debug.Log($"[NumberGrid] Emitting powerup tap: index={index}, sequence={tapSequence}");
                MatchmakingModel.EmitTapNumber(roomId, index, 0, tapSequence);
                tapSequence++; // Increment AFTER sending so the first tap is 0
            }
        }
        else
        {
            // Fallback for offline/Photon testing (original logic)
            Debug.Log("[NumberGrid] No active match, locking powerup locally.");
            LockPowerup(boxData, PlayerType.Player, -1);
            
            // Trigger event (use -1 or 0 as number for powerups)
            OnBoxSelected?.Invoke(this, -1, boxData.boxObject);
            
            // Check if all boxes are selected
            CheckIfAllBoxesSelected();
        }
    }
    
    /// <summary>
    /// Selects a powerup programmatically (for CPU or other automated selection)
    /// </summary>
    public bool SelectPowerup(GameObject boxObject, PlayerType playerType, int cpuIndex = 0)
    {
        // Find the box data
        BoxData boxData = boxDataList.Find(b => b.boxObject == boxObject);
        if (boxData == null)
        {
            Debug.LogWarning($"[NumberGrid] Box object not found in boxDataList!");
            return false;
        }
        
        if (boxData.isSelected)
        {
            Debug.LogWarning($"[NumberGrid] Powerup is already selected!");
            return false;
        }
        
        if (!boxData.isPowerup)
        {
            Debug.LogWarning("[NumberGrid] This is not a powerup box!");
            return false;
        }
        
        if (!boxData.isPowerupVisible)
        {
            Debug.LogWarning("[NumberGrid] Powerup is not currently visible!");
            return false;
        }
        
        // Lock the powerup (keep it visible but locked)
        LockPowerup(boxData, playerType, cpuIndex);
        
        // Trigger event
        OnBoxSelected?.Invoke(this, -1, boxData.boxObject);
        
        Debug.Log($"[NumberGrid] Powerup selected by {playerType}!");
        
        // Check if all boxes are selected
        CheckIfAllBoxesSelected();
        
        return true;
    }
    
    /// <summary>
    /// Locks a powerup (keeps it visible but makes it unselectable, stops animation)
    /// </summary>
    private void LockPowerup(BoxData boxData, PlayerType playerType, int cpuIndex)
    {
        if (boxData == null || !boxData.isPowerup) return;
        
        // Mark as selected
        boxData.isSelected = true;
        boxData.selectedByPlayer = playerType;
        boxData.selectedByCPUIndex = (playerType == PlayerType.CPU) ? cpuIndex : -1;
        boxData.selectedInRollSum = -1; // Powerups don't have dice sum
        boxData.selectedInRollId = -1; // Powerups don't have roll ID
        
        // Stop animation and reset scale
        if (boxData.powerupImage != null)
        {
            // Kill the animation
            if (boxData.powerupAnimation != null)
            {
                boxData.powerupAnimation.Kill();
                boxData.powerupAnimation = null;
            }
            
            // Reset scale to normal
            boxData.powerupImage.transform.localScale = Vector3.one;
        }
        
        // Keep powerup visible (don't hide it)
        // Powerup image stays visible
        
        // Disable button (permanently locked)
        if (boxData.selectButton != null)
        {
            boxData.selectButton.interactable = false;
        }
        
        // Show highlight
        if (boxData.highlightImage != null)
        {
            boxData.highlightImage.gameObject.SetActive(true);
        }
        
        // Hide timer UI (slider and text) since powerup is now locked
        if (currentlyVisiblePowerup == boxData)
        {
            currentlyVisiblePowerup = null;
        }
        boxData.isPowerupVisible = false; // No longer in timer cycle
        
        // Hide timer UI
        if (powerupTimerSlider != null)
        {
            powerupTimerSlider.value = 0f;
            powerupTimerSlider.gameObject.SetActive(false);
        }
        if (powerupTimerText != null)
        {
            powerupTimerText.text = "";
            powerupTimerText.gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // If grid is already generated but timer is not running, restart it
        // Wait one frame to ensure everything is initialized
        if (boxDataList.Count > 0)
        {
            StartCoroutine(DelayedTimerStart());
        }
    }
    
    /// <summary>
    /// Delays timer start by one frame to ensure everything is initialized
    /// </summary>
    private IEnumerator DelayedTimerStart()
    {
        yield return null; // Wait one frame
        if (powerupTimerCoroutine == null && boxDataList.Count > 0)
        {
            StartPowerupTimer();
        }
    }
    
    private void OnDisable()
    {
        // Stop timer when GameObject is disabled
        StopPowerupTimer();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from server events
        MatchmakingModel.OnNumberLockedEvent -= HandleServerNumberLocked;
        MatchmakingModel.OnTapResultEvent -= HandleServerTapResult;
        MatchmakingModel.OnIconActivatedEvent -= HandleServerIconActivated;
        MatchmakingModel.OnIconExpiredEvent -= HandleServerIconExpired;

        StopPowerupTimer();
        ClearGrid();
    }

    #region Server Powerup Handling

    /// <summary>
    /// Called when the server activates a powerup (icon)
    /// </summary>
    private void HandleServerIconActivated(IconActivatedData data)
    {
        if (data.index >= 0 && data.index < boxDataList.Count)
        {
            BoxData boxData = boxDataList[data.index];
            if (boxData.isPowerup)
            {
                Debug.Log($"[NumberGrid] Server activated powerup at index {data.index} for {data.duration}ms");
                ShowPowerup(boxData);
                
                // Update UI based on server duration
                if (serverPowerupUIUpdateCoroutine != null) StopCoroutine(serverPowerupUIUpdateCoroutine);
                serverPowerupUIUpdateCoroutine = StartCoroutine(ServerPowerupUIUpdate(boxData, data.duration / 1000f));
            }
        }
    }

    /// <summary>
    /// Called when the server signals that a powerup has expired
    /// </summary>
    private void HandleServerIconExpired(IconExpiredData data)
    {
        if (data.index >= 0 && data.index < boxDataList.Count)
        {
            BoxData boxData = boxDataList[data.index];
            if (currentlyVisiblePowerup == boxData)
            {
                Debug.Log($"[NumberGrid] Server powerup at index {data.index} expired.");
                
                if (serverPowerupUIUpdateCoroutine != null)
                {
                    StopCoroutine(serverPowerupUIUpdateCoroutine);
                    serverPowerupUIUpdateCoroutine = null;
                }
                
                if (!boxData.isSelected)
                {
                    HidePowerup(boxData);
                }
            }
        }
    }

    /// <summary>
    /// Coroutine that updates the powerup timer UI for server-driven powerups
    /// </summary>
    private IEnumerator ServerPowerupUIUpdate(BoxData boxData, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // If the powerup is locked (by this player or another), stop updating
            if (boxData.isSelected) yield break;
            
            elapsed += Time.deltaTime;
            
            // Update slider (0 to 1)
            if (powerupTimerSlider != null)
            {
                powerupTimerSlider.value = elapsed / duration;
            }
            
            // Update text (seconds remaining)
            if (powerupTimerText != null)
            {
                powerupTimerText.text = Mathf.CeilToInt(duration - elapsed).ToString();
            }
            
            yield return null;
        }
        
        serverPowerupUIUpdateCoroutine = null;
    }

    #endregion
}

