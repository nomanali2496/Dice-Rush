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
    
    private List<GameObject> instantiatedBoxes = new List<GameObject>(); // List to store all instantiated boxes
    private const int MIN_NUMBER = 2;
    private const int MAX_NUMBER = 12;
    private const int POWERUP_COUNT = 3;
    
    private void Start()
    {
        GenerateGrid();
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

