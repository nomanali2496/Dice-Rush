using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages a 5x5 grid of number boxes with random positioning
/// Numbers 2-12 appear twice, and 3 boxes contain powerups
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
        public bool isSelected; // Whether this box is selected
        public int selectedInRollSum = -1; // Dice sum when this box was selected (-1 if not selected)
        public int selectedInRollId = -1; // Roll ID when this box was selected (-1 if not selected)
    }
    
    private List<BoxData> boxDataList = new List<BoxData>(); // List to store all box data
    private List<GameObject> instantiatedBoxes = new List<GameObject>(); // List to store all instantiated boxes (for cleanup)
    private const int MIN_NUMBER = 2;
    private const int MAX_NUMBER = 12;
    private const int POWERUP_COUNT = 3;
    private int currentDiceSum = -1; // Current dice sum (-1 means no sum set yet)
    private int lastRollDiceSum = -1; // Dice sum from the last roll (to detect new rolls)
    private int currentRollId = 0; // Roll ID that increments each time dice are rolled (to distinguish same sum in different rolls)
    
    // Events
    public System.Action<int, GameObject> OnBoxSelected; // Called when a box is selected (number, boxObject)
    
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
        
        // Reset dice sum
        currentDiceSum = -1;
        
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
        currentDiceSum = -1;
    }
    
    /// <summary>
    /// Sets the dice sum and enables/disables boxes accordingly
    /// </summary>
    public void SetDiceSum(int sum)
    {
        // Every time SetDiceSum is called, it's a new roll (dice were rolled again)
        // Increment roll ID to distinguish between rolls, even if the sum is the same
        currentRollId++;
        
        lastRollDiceSum = currentDiceSum; // Store previous sum
        currentDiceSum = sum;
        
        // Only update interactability - don't reset selections
        // Selections persist across rolls, but new boxes become available
        UpdateBoxInteractability();
        
        int availableCount = GetAvailableBoxCount(sum);
        Debug.Log($"[NumberGrid] Dice sum set to {sum}. {availableCount} box(es) with number {sum} are now selectable. Roll ID: {currentRollId}");
    }
    
    /// <summary>
    /// Clears the current dice sum (disables all buttons)
    /// </summary>
    public void ClearDiceSum()
    {
        currentDiceSum = -1;
        UpdateBoxInteractability();
    }
    
    /// <summary>
    /// Updates button interactability based on current dice sum
    /// </summary>
    private void UpdateBoxInteractability()
    {
        foreach (BoxData boxData in boxDataList)
        {
            if (boxData.isPowerup || boxData.selectButton == null)
            {
                continue; // Skip powerups and boxes without buttons
            }
            
            // Enable button only if:
            // 1. Box number matches dice sum
            // 2. Box is not already selected
            // 3. No other box with the same number is currently blocking it (only disable if another is selected AND we're trying to select a different one)
            bool shouldBeInteractable = !boxData.isSelected && 
                                       currentDiceSum > 0 && 
                                       boxData.number == currentDiceSum;
            
            boxData.selectButton.interactable = shouldBeInteractable;
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
        
        if (currentDiceSum <= 0 || boxData.number != currentDiceSum)
        {
            Debug.LogWarning($"[NumberGrid] Cannot select box with number {boxData.number}. Current dice sum is {currentDiceSum}!");
            return;
        }
        
        // Check if another box with the same number is already selected in THIS roll
        // Prevent selecting a second box with the same number in the same roll
        bool anotherBoxSelectedInThisRoll = IsAnyBoxWithNumberSelectedInCurrentRoll(boxData.number);
        if (anotherBoxSelectedInThisRoll)
        {
            Debug.LogWarning($"[NumberGrid] Another box with number {boxData.number} is already selected in this roll (sum: {currentDiceSum}). Cannot select another one until next roll.");
            return;
        }
        
        // Mark as selected (this locks the selection)
        boxData.isSelected = true;
        boxData.selectedInRollSum = currentDiceSum; // Track which roll this was selected in
        boxData.selectedInRollId = currentRollId; // Track which roll ID this was selected in
        
        // Disable this button (permanently locked)
        if (boxData.selectButton != null)
        {
            boxData.selectButton.interactable = false;
        }
        
        // Show highlight (only for number boxes, not powerups)
        if (boxData.highlightImage != null && !boxData.isPowerup)
        {
            boxData.highlightImage.gameObject.SetActive(true);
        }
        
        // Trigger event
        OnBoxSelected?.Invoke(boxData.number, boxData.boxObject);
        
        Debug.Log($"[NumberGrid] Box with number {boxData.number} selected and locked!");
    }
    
    /// <summary>
    /// Checks if any box with the given number is already selected in the CURRENT roll
    /// This prevents selecting a second box with the same number in the same roll
    /// </summary>
    private bool IsAnyBoxWithNumberSelectedInCurrentRoll(int number)
    {
        foreach (BoxData boxData in boxDataList)
        {
            // Check if box is selected AND was selected in the current roll (same roll ID)
            // This distinguishes between same sum in different rolls
            if (!boxData.isPowerup && 
                boxData.number == number && 
                boxData.isSelected && 
                boxData.selectedInRollId == currentRollId)
            {
                return true;
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
            boxData.selectedInRollSum = -1;
            boxData.selectedInRollId = -1;
            
            if (boxData.highlightImage != null)
            {
                boxData.highlightImage.gameObject.SetActive(false);
            }
        }
        
        currentDiceSum = -1;
        lastRollDiceSum = -1;
        currentRollId = 0;
        UpdateBoxInteractability();
    }
    
    /// <summary>
    /// Gets the number of available (unselected) boxes for a given sum
    /// </summary>
    public int GetAvailableBoxCount(int sum)
    {
        int count = 0;
        foreach (BoxData boxData in boxDataList)
        {
            if (!boxData.isPowerup && !boxData.isSelected && boxData.number == sum)
            {
                count++;
            }
        }
        return count;
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

