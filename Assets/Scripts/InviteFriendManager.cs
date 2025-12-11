using UnityEngine;
using System.Collections;
using TMPro;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Manages the invite friends list display using Enhanced Scroller
/// Handles data loading, cell creation, and scrolling for friend rows
/// </summary>
public class InviteFriendManager : MonoBehaviour, IEnhancedScrollerDelegate
{
    [Header("Enhanced Scroller References")]
    [SerializeField] private EnhancedScroller scroller; // The Enhanced Scroller component
    [SerializeField] private FriendRowUI cellViewPrefab; // Prefab for friend row cells
    
    [Header("Test Settings")]
    [SerializeField] private int testDataCount = 20; // Number of test friends to generate
    [SerializeField] private float cellSize = 100f; // Height of each friend row cell
    
    [Header("Test Avatars (Optional)")]
    [SerializeField] private Sprite[] testAvatars; // Optional test avatar sprites
    
    [Header("Test Names (Optional)")]
    [SerializeField] private string[] testNames = { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack" }; // Test friend names
    
    [Header("Total Invites Text")]
    [SerializeField] private TextMeshProUGUI totalInvitesText; // Text to display total invites count
    
    /// <summary>
    /// Internal representation of friends data
    /// </summary>
    private SmallList<FriendRowData> _friendsData;
    
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeFriendsList();
    }
    
    private void OnEnable()
    {
        // Reinitialize when GameObject becomes active
        if (isInitialized)
        {
            RefreshFriendsList();
        }
        else
        {
            InitializeFriendsList();
        }
    }
    
    /// <summary>
    /// Initializes the friends list for the first time
    /// </summary>
    private void InitializeFriendsList()
    {
        // Set this script as the scroller's delegate
        if (scroller != null)
        {
            scroller.Delegate = this;
        }
        else
        {
            Debug.LogError("[InviteFriendManager] Enhanced Scroller not assigned!");
            return;
        }
        
        // Load test data on start (use coroutine to wait for scroller initialization)
        StartCoroutine(LoadTestDataDelayed());
        isInitialized = true;
    }
    
    /// <summary>
    /// Refreshes the friends list when GameObject becomes active again
    /// </summary>
    private void RefreshFriendsList()
    {
        // Update total invites text immediately
        UpdateTotalInvitesText();
        
        // Ensure delegate is set
        if (scroller != null)
        {
            scroller.Delegate = this;
            
            // Reload data if we have data (use coroutine to wait for scroller initialization)
            if (_friendsData != null && _friendsData.Count > 0)
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
        // Update total invites text
        UpdateTotalInvitesText();
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
    /// Loads test friends data for testing purposes
    /// </summary>
    public void LoadTestData()
    {
        if (_friendsData == null)
        {
            _friendsData = new SmallList<FriendRowData>();
        }
        else
        {
            _friendsData.Clear();
        }
        
        // Generate test data with random names, IDs, and online status
        System.Random random = new System.Random();
        
        for (int i = 0; i < testDataCount; i++)
        {
            // Generate friend name
            string friendName;
            if (testNames != null && testNames.Length > 0)
            {
                friendName = testNames[random.Next(0, testNames.Length)] + " " + (i + 1).ToString();
            }
            else
            {
                friendName = $"Friend{i + 1:D3}";
            }
            
            // Generate friend ID
            string friendId = "ID" + random.Next(10000, 99999).ToString();
            
            // Randomly assign online/offline status (70% chance online)
            bool isOnline = random.Next(0, 100) < 70;
            
            // Optionally assign a random avatar from test avatars
            Sprite avatar = null;
            if (testAvatars != null && testAvatars.Length > 0)
            {
                avatar = testAvatars[random.Next(0, testAvatars.Length)];
            }
            
            _friendsData.Add(new FriendRowData(friendName, friendId, avatar, isOnline));
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
        
        // Update total invites text
        UpdateTotalInvitesText();
        
        Debug.Log($"[InviteFriendManager] Loaded {_friendsData.Count} test friends");
    }
    
    /// <summary>
    /// Adds a single friend entry (for testing)
    /// </summary>
    public void AddFriend(string friendName, string friendId, Sprite avatar = null, bool isOnline = false)
    {
        if (_friendsData == null)
        {
            _friendsData = new SmallList<FriendRowData>();
        }
        
        _friendsData.Add(new FriendRowData(friendName, friendId, avatar, isOnline));
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            // Check if delegate is set
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        // Update total invites text
        UpdateTotalInvitesText();
    }
    
    /// <summary>
    /// Removes a friend by ID
    /// </summary>
    public void RemoveFriend(string friendId)
    {
        if (_friendsData == null) return;
        
        for (int i = _friendsData.Count - 1; i >= 0; i--)
        {
            if (_friendsData[i].friendId == friendId)
            {
                _friendsData.RemoveAt(i);
                break;
            }
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
        
        // Update total invites text
        UpdateTotalInvitesText();
    }
    
    /// <summary>
    /// Updates a friend's online status
    /// </summary>
    public void UpdateFriendOnlineStatus(string friendId, bool isOnline)
    {
        if (_friendsData == null) return;
        
        for (int i = 0; i < _friendsData.Count; i++)
        {
            if (_friendsData[i].friendId == friendId)
            {
                _friendsData[i].isOnline = isOnline;
                
                // Refresh the specific cell if scroller is active
                if (scroller != null && scroller.gameObject.activeInHierarchy)
                {
                    scroller.RefreshActiveCellViews();
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// Clears all friends data
    /// </summary>
    public void ClearData()
    {
        if (_friendsData != null)
        {
            _friendsData.Clear();
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
        
        // Update total invites text
        UpdateTotalInvitesText();
        
        Debug.Log("[InviteFriendManager] Friends data cleared");
    }
    
    /// <summary>
    /// Updates the total invites text based on current data
    /// </summary>
    private void UpdateTotalInvitesText()
    {
        if (totalInvitesText == null) return;
        
        int inviteCount = _friendsData != null ? _friendsData.Count : 0;
        
        if (inviteCount > 0)
        {
            // Show total invites count
            totalInvitesText.text = $"My Total Invites : {inviteCount}";
        }
        else
        {
            // Show no invites message
            totalInvitesText.text = "I have not any invite yet";
        }
    }
    
    #region EnhancedScroller Handlers
    
    /// <summary>
    /// Returns the number of cells in the friends list
    /// </summary>
    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _friendsData != null ? _friendsData.Count : 0;
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
            Debug.LogError("[InviteFriendManager] Cell View Prefab is not assigned!");
            return null;
        }
        
        // Get a cell from the scroller (recycled or new)
        FriendRowUI cellView = scroller.GetCellView(cellViewPrefab) as FriendRowUI;
        
        if (cellView == null)
        {
            Debug.LogError("[InviteFriendManager] Failed to get FriendRowUI cell view!");
            return null;
        }
        
        // Set the name for debugging
        cellView.name = $"Friend Row {dataIndex}";
        
        // Set the data for this cell
        if (_friendsData != null && dataIndex < _friendsData.Count)
        {
            cellView.SetData(_friendsData[dataIndex]);
        }
        else
        {
            Debug.LogWarning($"[InviteFriendManager] Data index {dataIndex} is out of range!");
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
        testDataCount = 5;
        LoadTestData();
    }
    
    /// <summary>
    /// Test method: Loads a large set of test data
    /// </summary>
    [ContextMenu("Load Large Test Data")]
    public void LoadLargeTestData()
    {
        testDataCount = 50;
        LoadTestData();
    }
    
    /// <summary>
    /// Test method: Scrolls to top of friends list
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
    /// Test method: Toggles online status of first friend
    /// </summary>
    [ContextMenu("Toggle First Friend Online Status")]
    public void ToggleFirstFriendStatus()
    {
        if (_friendsData != null && _friendsData.Count > 0)
        {
            bool newStatus = !_friendsData[0].isOnline;
            UpdateFriendOnlineStatus(_friendsData[0].friendId, newStatus);
            Debug.Log($"[InviteFriendManager] Toggled first friend status to: {(newStatus ? "Online" : "Offline")}");
        }
    }
    
    #endregion
}

