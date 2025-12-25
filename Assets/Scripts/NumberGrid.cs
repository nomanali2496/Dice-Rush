using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Player type enum for tracking selections separately
/// </summary>
public enum PlayerType
{
    Player,
    CPU
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
    [SerializeField] private float spacingX = 100f; // Horizontal spacing between boxes
    [SerializeField] private float spacingY = 100f; // Vertical spacing between boxes
    [SerializeField] private Vector2 startPosition = Vector2.zero; // Starting position for grid
    
    [Header("Component Names (in Prefab)")]
    [SerializeField] private string numberTextName = "NumberText"; // Name of the Text/TextMeshProUGUI component
    [SerializeField] private string powerupImageName = "PowerupImage"; // Name of the Image component
    [SerializeField] private string selectButtonName = "SelectButton"; // Name of the Button component
    [SerializeField] private string highlightImageName = "HighlightImage"; // Name of the highlight Image component
    
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
        public bool isSelected; // Whether this box is selected (by any player)
        public PlayerType selectedByPlayer = PlayerType.Player; // Which player selected this box
        public int selectedByCPUIndex = -1; // Which CPU index selected this box (-1 if player or not selected)
        public int selectedInRollSum = -1; // Dice sum when this box was selected (-1 if not selected)
        public int selectedInRollId = -1; // Roll ID when this box was selected (-1 if not selected)
    }
    
    private List<BoxData> boxDataList = new List<BoxData>(); // List to store all box data
    private List<GameObject> instantiatedBoxes = new List<GameObject>(); // List to store all instantiated boxes (for cleanup)
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
    
    // Events
    public System.Action<int, GameObject> OnBoxSelected; // Called when a box is selected (number, boxObject)
    public System.Action OnAllBoxesSelected; // Called when all selectable boxes are selected
    
    private void Start()
    {
        GenerateGrid();
        // Initially disable all buttons until dice are rolled
        UpdateBoxInteractability();
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
    /// Generates the 5x5 grid with random positioning
    /// </summary>
    public void GenerateGrid()
    {
        // Clear existing boxes if any
        ClearGrid();
        
        // Reset dice sums for both players
        currentDiceSumPlayer = -1;
        currentDiceSumCPU = -1;
        
        // Create list of all grid items (22 numbers + 3 powerups)
        List<GridItem> gridItems = new List<GridItem>();
        
        // Add numbers (2-12, each appearing twice)
        for (int i = MIN_NUMBER; i <= MAX_NUMBER; i++)
        {
            gridItems.Add(new GridItem { isPowerup = false, number = i });
            gridItems.Add(new GridItem { isPowerup = false, number = i });
        }
        
        // Add powerups with random sprite indices
        System.Random rng = new System.Random();
        for (int i = 0; i < POWERUP_COUNT; i++)
        {
            int powerupIndex = rng.Next(0, powerupSprites.Length);
            gridItems.Add(new GridItem { isPowerup = true, powerupIndex = powerupIndex });
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
        
        // Calculate world position
        int row = gridPosition / gridWidth;
        int col = gridPosition % gridWidth;
        Vector3 position = new Vector3(
            startPosition.x + col * spacingX,
            startPosition.y - row * spacingY,
            0f
        );
        
        // Instantiate the box
        GameObject box = Instantiate(numberBoxPrefab, transform);
        box.transform.localPosition = position;
        instantiatedBoxes.Add(box);
        
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
        if (numberText != null)
        {
            numberText.text = number.ToString();
            numberText.gameObject.SetActive(true);
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
        
        // Calculate world position
        int row = gridPosition / gridWidth;
        int col = gridPosition % gridWidth;
        Vector3 position = new Vector3(
            startPosition.x + col * spacingX,
            startPosition.y - row * spacingY,
            0f
        );
        
        // Instantiate the box
        GameObject box = Instantiate(numberBoxPrefab, transform);
        box.transform.localPosition = position;
        instantiatedBoxes.Add(box);
        
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
            selectedInRollId = -1
        };
        
        // Find and disable number text
        TextMeshProUGUI numberText = FindComponentInChildren<TextMeshProUGUI>(box, numberTextName);
        if (numberText != null)
        {
            numberText.gameObject.SetActive(false);
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
        
        // Find and enable powerup image with sprite
        Image powerupImage = FindComponentInChildren<Image>(box, powerupImageName);
        if (powerupImage != null)
        {
            powerupImage.sprite = powerupSprites[powerupSpriteIndex];
            powerupImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[NumberGrid] Could not find powerup image component '{powerupImageName}' in prefab!");
        }
        
        // Find select button (powerups don't have selectable buttons)
        Button selectButton = FindComponentInChildren<Button>(box, selectButtonName);
        if (selectButton != null)
        {
            selectButton.interactable = false; // Powerups are never selectable
        }
        
        // Powerups should never have highlight images shown
        Image highlightImage = FindComponentInChildren<Image>(box, highlightImageName);
        if (highlightImage != null)
        {
            // Don't assign highlight to powerup boxData, but make sure it's hidden
            highlightImage.gameObject.SetActive(false);
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
                bool shouldBeInteractable = !boxData.isSelected && 
                                           currentDiceSum > 0 && 
                                           boxData.number == currentDiceSum &&
                                           !IsAnyBoxWithNumberSelectedInCurrentRoll(boxData.number, PlayerType.Player);
                
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
        
        if (currentDiceSumPlayer <= 0 || boxData.number != currentDiceSumPlayer)
        {
            Debug.LogWarning($"[NumberGrid] Cannot select box with number {boxData.number}. Current player dice sum is {currentDiceSumPlayer}!");
            return;
        }
        
        // Check if another box with the same number is already selected by PLAYER in THIS roll
        // Prevent selecting a second box with the same number in the same roll (only for this player)
        bool anotherBoxSelectedInThisRoll = IsAnyBoxWithNumberSelectedInCurrentRoll(boxData.number, PlayerType.Player, -1);
        if (anotherBoxSelectedInThisRoll)
        {
            Debug.LogWarning($"[NumberGrid] Another box with number {boxData.number} is already selected by Player in this roll (sum: {currentDiceSumPlayer}). Cannot select another one until next roll.");
            return;
        }
        
        // Mark as selected (this locks the selection)
        boxData.isSelected = true;
        boxData.selectedByPlayer = PlayerType.Player; // Player selection
        boxData.selectedByCPUIndex = -1; // Not a CPU selection
        boxData.selectedInRollSum = currentDiceSumPlayer; // Track which roll this was selected in
        boxData.selectedInRollId = currentRollIdPlayer; // Track which roll ID this was selected in
        
        // Disable this button (permanently locked)
        if (boxData.selectButton != null)
        {
            boxData.selectButton.interactable = false;
        }
        
        // Show highlight (only for number boxes, not powerups)
        if (boxData.highlightImage != null && !boxData.isPowerup)
        {
            boxData.highlightImage.gameObject.SetActive(true);
            boxData.highlightImage.color = playerOutlineColor; // Use player's outline color
        }
        
        // Trigger event
        OnBoxSelected?.Invoke(boxData.number, boxData.boxObject);
        
        Debug.Log($"[NumberGrid] Box with number {boxData.number} selected and locked!");
        
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
            boxData.selectedInRollSum = -1;
            boxData.selectedInRollId = -1;
            
            if (boxData.highlightImage != null)
            {
                boxData.highlightImage.gameObject.SetActive(false);
                boxData.highlightImage.color = Color.white; // Reset to default color
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
        
        // Show highlight with appropriate color matching outline color
        if (boxData.highlightImage != null && !boxData.isPowerup)
        {
            boxData.highlightImage.gameObject.SetActive(true);
            
            // Set color based on player type's outline color
            if (playerType == PlayerType.Player)
            {
                boxData.highlightImage.color = playerOutlineColor;
            }
            else // CPU
            {
                // Get CPU color based on CPU index
                Color cpuColor = GetCPUOutlineColor(boxData.selectedByCPUIndex);
                boxData.highlightImage.color = cpuColor;
            }
        }
        
        // Trigger event
        OnBoxSelected?.Invoke(boxData.number, boxData.boxObject);
        
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
    /// Gets the count of selected boxes for each player/CPU
    /// Returns: Dictionary with PlayerType and CPU index as key, count as value
    /// Key format: "Player" for player, "CPU_0" for CPU index 0, "CPU_1" for CPU index 1, etc.
    /// </summary>
    public Dictionary<string, int> GetSelectionCounts()
    {
        Dictionary<string, int> selectionCounts = new Dictionary<string, int>();
        
        foreach (BoxData boxData in boxDataList)
        {
            if (!boxData.isPowerup && boxData.isSelected)
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
    /// Regenerates the grid with new random positions
    /// </summary>
    [ContextMenu("Regenerate Grid")]
    public void RegenerateGrid()
    {
        GenerateGrid();
    }
    
    private void OnDestroy()
    {
        ClearGrid();
    }
}

