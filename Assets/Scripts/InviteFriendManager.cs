using UnityEngine;
using System.Collections;
using TMPro;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using UnityEngine.Networking;

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
        
        // Load real data
        StartCoroutine(LoadRealFriendsDataDelayed());
        isInitialized = true;
    }
    
    private void RefreshFriendsList()
    {
        // Update total invites text immediately
        UpdateTotalInvitesText();
        
        // Ensure delegate is set
        if (scroller != null)
        {
            scroller.Delegate = this;
            StartCoroutine(LoadRealFriendsDataDelayed());
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
    /// Coroutine to load real friends data after waiting a frame (ensures scroller is initialized)
    /// </summary>
    private IEnumerator LoadRealFriendsDataDelayed()
    {
        yield return null; // Wait one frame for scroller to initialize
        LoadRealFriendsData();
    }
    
    /// <summary>
    /// Loads friends data from the backend API.
    /// </summary>
    public void LoadFriendsData()
    {
        Debug.Log("[InviteFriendManager] Loading live friends data...");
        LoadRealFriendsData();
    }
    
    /// <summary>
    /// Loads the real friends from the backend. Falls back to test data if offline or failed.
    /// </summary>
    public void LoadRealFriendsData()
    {
        if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            Debug.Log("[InviteFriendManager] Requesting real invite history from backend...");
            BackendAuthService.Instance.GetInviteHistory((success, list) =>
            {
                if (success && list != null)
                {
                    PopulateRealFriendsData(list);
                }
                else
                {
                    Debug.LogWarning("[InviteFriendManager] Backend invite history fetch failed. Falling back to test data.");
                    LoadTestData();
                }
            });
        }
        else
        {
            Debug.Log("[InviteFriendManager] No active JWT or BackendAuthService missing. Loading test data.");
            LoadTestData();
        }
    }

    /// <summary>
    /// Populates real friends list data parsed from the backend API.
    /// </summary>
    private void PopulateRealFriendsData(System.Collections.Generic.List<BackendAuthService.InviteHistoryItem> list)
    {
        if (_friendsData == null)
        {
            _friendsData = new SmallList<FriendRowData>();
        }
        else
        {
            _friendsData.Clear();
        }

        foreach (var item in list)
        {
            string friendName = item.name;
            string friendId = item.userId;
            bool isOnline = item.isOnline;
            
            // Map preset or URL
            Sprite avatarSprite = null;
            string avatarKey = item.avatar;

            if (string.IsNullOrEmpty(avatarKey) || !avatarKey.StartsWith("http"))
            {
                // Preset key
                if (avatarKey == "avatarGirl" && testAvatars != null && testAvatars.Length > 1)
                {
                    avatarSprite = testAvatars[1];
                }
                else if (testAvatars != null && testAvatars.Length > 0)
                {
                    avatarSprite = testAvatars[0];
                }
            }

            _friendsData.Add(new FriendRowData(friendName, friendId, avatarSprite, isOnline, avatarKey));
        }

        // Reload the scroller with new data (only if scroller is active and initialized)
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            if (scroller.Delegate == null)
            {
                scroller.Delegate = this;
            }
            scroller.ReloadData();
        }
        
        // Update total invites text
        UpdateTotalInvitesText();
        
        Debug.Log($"[InviteFriendManager] Loaded {_friendsData.Count} real friends");
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
    
    #region Image Loading
    
    /// <summary>
    /// Loads an image from a URL or base64 data URI and converts it to a Sprite
    /// </summary>
    private IEnumerator LoadImageFromURL(string imageUrl, string userId, System.Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            onComplete?.Invoke(null);
            yield break;
        }
        
        // Check if it's a base64 data URI (custom uploaded image)
        if (imageUrl.StartsWith("data:image"))
        {
            try
            {
                // Extract base64 part
                string base64Data = imageUrl.Substring(imageUrl.IndexOf(",") + 1);
                byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                
                // Create texture
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                {
                    // Convert to sprite
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    
                    Debug.Log($"[InviteFriendManager] Successfully loaded base64 image for {userId}");
                    onComplete?.Invoke(sprite);
                    yield break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InviteFriendManager] Failed to load base64 image for {userId}: {e.Message}");
            }
        }
        
        // Try loading as regular URL
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                if (texture != null)
                {
                    // Create sprite from texture
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    
                    Debug.Log($"[InviteFriendManager] Successfully loaded image from URL for {userId}");
                    onComplete?.Invoke(sprite);
                }
                else
                {
                    Debug.LogWarning($"[InviteFriendManager] Failed to create texture from image URL: {imageUrl}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogWarning($"[InviteFriendManager] Failed to load image from URL: {imageUrl}. Error: {www.error}");
                onComplete?.Invoke(null);
            }
        }
    }
    
    /// <summary>
    /// Updates the avatar for a specific friend by their ID
    /// </summary>
    private void UpdateFriendAvatar(string userId, Sprite newAvatar)
    {
        if (_friendsData == null) return;
        
        for (int i = 0; i < _friendsData.Count; i++)
        {
            if (_friendsData[i].friendId == userId)
            {
                _friendsData[i].avatarSprite = newAvatar;
                
                // Refresh the specific cell if scroller is active
                if (scroller != null && scroller.gameObject.activeInHierarchy)
                {
                    scroller.RefreshActiveCellViews();
                }
                break;
            }
        }
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
    
    #region Scaling Support
    
    /// <summary>
    /// Sets the cell size and reloads the scroller (called by FriendRowScaler)
    /// </summary>
    public void SetCellSizeAndReload(float newCellSize)
    {
        cellSize = newCellSize;
        Debug.Log($"[InviteFriendManager] Cell size updated to {cellSize:F2}");
        
        // Reload scroller to apply new cell size
        if (scroller != null && scroller.gameObject.activeInHierarchy && _friendsData != null)
        {
            scroller.ReloadData();
            Debug.Log($"[InviteFriendManager] Scroller reloaded with new cell size");
        }
    }
    
    #endregion
}

