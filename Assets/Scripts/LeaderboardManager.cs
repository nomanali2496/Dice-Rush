using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Manages the leaderboard display using Enhanced Scroller
/// Handles data loading, cell creation, and scrolling for leaderboard rows
/// </summary>
public class LeaderboardManager : MonoBehaviour, IEnhancedScrollerDelegate
{
    [Header("Enhanced Scroller References")]
    [SerializeField] private EnhancedScroller scroller; // The Enhanced Scroller component
    [SerializeField] private LeaderboardRowUI cellViewPrefab; // Prefab for leaderboard row cells
    
    [Header("Test Settings")]
    [SerializeField] private int testDataCount = 50; // Number of test entries to generate
    [SerializeField] private float cellSize = 100f; // Height of each leaderboard row cell
    
    [Header("Test Avatars (Optional)")]
    [SerializeField] private Sprite[] testAvatars; // Optional test avatar sprites
    
    [Header("Top 3 Position Avatars")]
    [SerializeField] private Image firstPlaceAvatarImage; // Image to display 1st place avatar
    [SerializeField] private Image secondPlaceAvatarImage; // Image to display 2nd place avatar
    [SerializeField] private Image thirdPlaceAvatarImage; // Image to display 3rd place avatar
    
    /// <summary>
    /// Internal representation of leaderboard data
    /// </summary>
    private SmallList<LeaderboardRowData> _leaderboardData;
    
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeLeaderboard();
    }
    
    private void OnEnable()
    {
        // Reinitialize when GameObject becomes active
        if (isInitialized)
        {
            RefreshLeaderboard();
        }
        else
        {
            InitializeLeaderboard();
        }
    }
    
    /// <summary>
    /// Initializes the leaderboard for the first time
    /// </summary>
    private void InitializeLeaderboard()
    {
        // Set this script as the scroller's delegate
        if (scroller != null)
        {
            scroller.Delegate = this;
        }
        else
        {
            Debug.LogError("[LeaderboardManager] Enhanced Scroller not assigned!");
            return;
        }
        
        // Load test data on start (use coroutine to wait for scroller initialization)
        StartCoroutine(LoadTestDataDelayed());
        isInitialized = true;
    }
    
    /// <summary>
    /// Refreshes the leaderboard when GameObject becomes active again
    /// </summary>
    private void RefreshLeaderboard()
    {
        // Ensure delegate is set
        if (scroller != null)
        {
            scroller.Delegate = this;
            
            // Reload data if we have data (use coroutine to wait for scroller initialization)
            if (_leaderboardData != null && _leaderboardData.Count > 0)
            {
                StartCoroutine(ReloadDataDelayed());
            }
            else
            {
                // If no data, load test data
                StartCoroutine(LoadTestDataDelayed());
            }
        }
    }
    
    /// <summary>
    /// Coroutine to reload data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator ReloadDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            scroller.ReloadData();
        }
    }
    
    /// <summary>
    /// Coroutine to load test data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator LoadTestDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        LoadTestData();
    }
    
    /// <summary>
    /// Loads test leaderboard data for testing purposes
    /// </summary>
    public void LoadTestData()
    {
        if (_leaderboardData == null)
        {
            _leaderboardData = new SmallList<LeaderboardRowData>();
        }
        else
        {
            _leaderboardData.Clear();
        }
        
        // Generate test data with random scores
        System.Random random = new System.Random();
        
        for (int i = 0; i < testDataCount; i++)
        {
            int position = i + 1;
            string username = $"Player{position:D3}";
            int score = random.Next(100, 10000); // Random score between 100 and 10000
            
            // Optionally assign a random avatar from test avatars
            Sprite avatar = null;
            if (testAvatars != null && testAvatars.Length > 0)
            {
                avatar = testAvatars[random.Next(0, testAvatars.Length)];
            }
            
            _leaderboardData.Add(new LeaderboardRowData(position, username, score, avatar));
        }
        
        // Sort by score (descending) and update positions
        SortAndUpdatePositions();
        
        // Update top 3 position avatars
        UpdateTop3Avatars();
        
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
        
        Debug.Log($"[LeaderboardManager] Loaded {_leaderboardData.Count} test leaderboard entries");
    }
    
    /// <summary>
    /// Sorts leaderboard data by score (descending) and updates positions
    /// </summary>
    private void SortAndUpdatePositions()
    {
        // Sort by score descending
        for (int i = 0; i < _leaderboardData.Count - 1; i++)
        {
            for (int j = i + 1; j < _leaderboardData.Count; j++)
            {
                if (_leaderboardData[i].score < _leaderboardData[j].score)
                {
                    // Swap
                    LeaderboardRowData temp = _leaderboardData[i];
                    _leaderboardData[i] = _leaderboardData[j];
                    _leaderboardData[j] = temp;
                }
            }
        }
        
        // Update positions after sorting
        for (int i = 0; i < _leaderboardData.Count; i++)
        {
            _leaderboardData[i].position = i + 1;
        }
    }
    
    /// <summary>
    /// Updates the top 3 position avatar images
    /// </summary>
    private void UpdateTop3Avatars()
    {
        if (_leaderboardData == null || _leaderboardData.Count == 0)
        {
            // Clear all top 3 avatars if no data
            if (firstPlaceAvatarImage != null)
                firstPlaceAvatarImage.sprite = null;
            if (secondPlaceAvatarImage != null)
                secondPlaceAvatarImage.sprite = null;
            if (thirdPlaceAvatarImage != null)
                thirdPlaceAvatarImage.sprite = null;
            return;
        }
        
        // Update 1st place avatar
        if (firstPlaceAvatarImage != null)
        {
            if (_leaderboardData.Count > 0 && _leaderboardData[0].avatarSprite != null)
            {
                firstPlaceAvatarImage.sprite = _leaderboardData[0].avatarSprite;
                firstPlaceAvatarImage.enabled = true;
            }
            else
            {
                firstPlaceAvatarImage.sprite = null;
                firstPlaceAvatarImage.enabled = false;
            }
        }
        
        // Update 2nd place avatar
        if (secondPlaceAvatarImage != null)
        {
            if (_leaderboardData.Count > 1 && _leaderboardData[1].avatarSprite != null)
            {
                secondPlaceAvatarImage.sprite = _leaderboardData[1].avatarSprite;
                secondPlaceAvatarImage.enabled = true;
            }
            else
            {
                secondPlaceAvatarImage.sprite = null;
                secondPlaceAvatarImage.enabled = false;
            }
        }
        
        // Update 3rd place avatar
        if (thirdPlaceAvatarImage != null)
        {
            if (_leaderboardData.Count > 2 && _leaderboardData[2].avatarSprite != null)
            {
                thirdPlaceAvatarImage.sprite = _leaderboardData[2].avatarSprite;
                thirdPlaceAvatarImage.enabled = true;
            }
            else
            {
                thirdPlaceAvatarImage.sprite = null;
                thirdPlaceAvatarImage.enabled = false;
            }
        }
    }
    
    /// <summary>
    /// Adds a single leaderboard entry (for testing)
    /// </summary>
    public void AddTestEntry(string username, int score, Sprite avatar = null)
    {
        if (_leaderboardData == null)
        {
            _leaderboardData = new SmallList<LeaderboardRowData>();
        }
        
        int position = _leaderboardData.Count + 1;
        _leaderboardData.Add(new LeaderboardRowData(position, username, score, avatar));
        
        SortAndUpdatePositions();
        
        // Update top 3 position avatars
        UpdateTop3Avatars();
        
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
    /// Clears all leaderboard data
    /// </summary>
    public void ClearData()
    {
        if (_leaderboardData != null)
        {
            _leaderboardData.Clear();
        }
        
        // Clear top 3 avatars
        UpdateTop3Avatars();
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        Debug.Log("[LeaderboardManager] Leaderboard data cleared");
    }
    
    #region EnhancedScroller Handlers
    
    /// <summary>
    /// Returns the number of cells in the leaderboard
    /// </summary>
    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _leaderboardData != null ? _leaderboardData.Count : 0;
    }
    
    /// <summary>
    /// Returns the size of each cell (height for vertical scroller)
    /// </summary>
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellSize;
    }
    
    /// <summary>
    /// Gets the cell view for a specific data index
    /// </summary>
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        // Check if prefab is assigned
        if (cellViewPrefab == null)
        {
            Debug.LogError("[LeaderboardManager] Cell View Prefab is not assigned!");
            return null;
        }
        
        // Get a cell from the scroller (recycled or new)
        LeaderboardRowUI cellView = scroller.GetCellView(cellViewPrefab) as LeaderboardRowUI;
        
        if (cellView == null)
        {
            Debug.LogError("[LeaderboardManager] Failed to get LeaderboardRowUI cell view!");
            return null;
        }
        
        // Set the name for debugging
        cellView.name = $"Leaderboard Row {dataIndex}";
        
        // Set the data for this cell
        if (_leaderboardData != null && dataIndex < _leaderboardData.Count)
        {
            cellView.SetData(_leaderboardData[dataIndex]);
        }
        else
        {
            Debug.LogWarning($"[LeaderboardManager] Data index {dataIndex} is out of range!");
        }
        
        return cellView;
    }
    
    #endregion
    
    #region Test Methods
    
    /// <summary>
    /// Test method: Loads a small set of test data
    /// </summary>
    [ContextMenu("Load Small Test Data")]
    public void LoadSmallTestData()
    {
        testDataCount = 10;
        LoadTestData();
    }
    
    /// <summary>
    /// Test method: Loads a large set of test data
    /// </summary>
    [ContextMenu("Load Large Test Data")]
    public void LoadLargeTestData()
    {
        testDataCount = 100;
        LoadTestData();
    }
    
    /// <summary>
    /// Test method: Scrolls to top of leaderboard
    /// </summary>
    [ContextMenu("Scroll To Top")]
    public void ScrollToTop()
    {
        if (scroller != null)
        {
            scroller.JumpToDataIndex(0, 0f, 0f);
        }
    }
    
    /// <summary>
    /// Test method: Scrolls to a specific position (e.g., position 5)
    /// </summary>
    [ContextMenu("Scroll To Position 5")]
    public void ScrollToPosition5()
    {
        if (scroller != null && _leaderboardData != null && _leaderboardData.Count > 4)
        {
            scroller.JumpToDataIndex(4, 0f, 0f); // Index 4 = Position 5
        }
    }
    
    #endregion
    
    #region Scaling Support
    
    /// <summary>
    /// Sets the cell size and reloads the scroller data
    /// Used by LeaderboardRowScaler to apply scaled cell sizes
    /// </summary>
    public void SetCellSizeAndReload(float newCellSize)
    {
        cellSize = newCellSize;
        
        // Reload scroller if it's active and initialized
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Ensure delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            
            // Only reload if we have data
            if (_leaderboardData != null && _leaderboardData.Count > 0)
            {
                scroller.ReloadData();
            }
        }
        
        Debug.Log($"[LeaderboardManager] Cell size updated to {cellSize:F2}");
    }
    
    #endregion
}

