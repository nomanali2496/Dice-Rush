using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using TMPro;

public class LeaderboardManager : MonoBehaviour, IEnhancedScrollerDelegate
{
    [Header("Enhanced Scroller References")]
    [SerializeField] private EnhancedScroller scroller;
    [SerializeField] private LeaderboardRowUI cellViewPrefab;
    
    [Header("Test Settings")]
    [SerializeField] private int testDataCount = 50;
    [SerializeField] private float cellSize = 100f;
    
    [Header("Test Avatars (Optional)")]
    [SerializeField] private Sprite[] testAvatars;
    
    [Header("Top 3 Position Avatars")]
    [SerializeField] private Image firstPlaceAvatarImage;
    [SerializeField] private Image secondPlaceAvatarImage;
    [SerializeField] private Image thirdPlaceAvatarImage;
    
    [Header("External UI References")]
    [SerializeField] private MainMenuAvatarDisplay mainMenuAvatarDisplay;
    
    [Header("User's Fixed Row (Explicit Components)")]
    [SerializeField] private Image userFixedAvatar;
    [SerializeField] private Image userFixedBadge;
    [SerializeField] private TextMeshProUGUI userFixedName;
    [SerializeField] private TextMeshProUGUI userFixedScore;
    [SerializeField] private TextMeshProUGUI userFixedPositionText;
    [SerializeField] private Image userFixedRowBackground;
    [SerializeField] private Image userFixedProfileBackground;
    
    private SmallList<LeaderboardRowData> _leaderboardData;
    private LeaderboardRowData _userData;
    private bool isInitialized = false;
    
    // Filtered indices for the scroller — excludes the logged-in user's row
    private System.Collections.Generic.List<int> _filteredIndices = new System.Collections.Generic.List<int>();
    
    private void Start()
    {
        InitializeLeaderboard();
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
            
            // IF ALREADY LOGGED IN: Sync immediately
            if (PlayFabManager.Instance.IsLoggedIn() && PlayFabManager.Instance.currentPlayerData != null)
            {
                Debug.Log("[LeaderboardManager] Player already logged in, performing initial sync.");
                UpdateUserFixedRow();
            }
        }
    }
    
    private void OnDestroy()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        }
    }
    
    private void OnPlayerDataLoaded(PlayerData data)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(UpdateFixedRowDelayed());
        }
    }
    
    private IEnumerator UpdateFixedRowDelayed()
    {
        yield return null;
        UpdateUserFixedRow();
    }
    
    private void OnEnable()
    {
        if (isInitialized)
        {
            RefreshLeaderboard();
        }
        else
        {
            InitializeLeaderboard();
        }
        
        // Extra sync: Wait a bit to ensure MainMenuAvatarDisplay has finished its 0.3s/0.5s loading
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SyncAvatarDelayed());
        }
    }
    
    private IEnumerator SyncAvatarDelayed()
    {
        yield return new WaitForSeconds(0.7f);
        UpdateUserFixedRow();
    }
    
    private void InitializeLeaderboard()
    {
        if (scroller != null)
        {
            scroller.Delegate = this;
        }
        else
        {
            Debug.LogError("[LeaderboardManager] Enhanced Scroller not assigned!");
            return;
        }
        StartCoroutine(LoadTestDataDelayed());
        isInitialized = true;
    }
    
    private void RefreshLeaderboard()
    {
        if (scroller != null)
        {
            scroller.Delegate = this;
            if (_leaderboardData != null && _leaderboardData.Count > 0)
            {
                StartCoroutine(ReloadDataDelayed());
            }
            else
            {
                StartCoroutine(LoadTestDataDelayed());
            }
        }
    }
    
    private IEnumerator ReloadDataDelayed()
    {
        yield return null;
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            scroller.ReloadData();
        }
    }
    
    private IEnumerator LoadTestDataDelayed()
    {
        yield return null;
        LoadTestData();
    }
    
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
        
        System.Random random = new System.Random();
        string loggedInUsername = "Guest User";
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null && !string.IsNullOrEmpty(PlayFabManager.Instance.currentPlayerData.playerName))
        {
            loggedInUsername = PlayFabManager.Instance.currentPlayerData.playerName;
        }
        
        for (int i = 0; i < testDataCount; i++)
        {
            int position = i + 1;
            string username;
            int score = random.Next(100, 10000);
            
            Sprite avatar = null;
            if (i == 4) 
            {
                username = loggedInUsername;
                // REAL AVATAR SYNC: Use the actual sprite from main menu if available
                if (mainMenuAvatarDisplay != null)
                {
                    avatar = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
                }
            }
            else 
            {
                username = $"Player{position:D3}";
            }
            
            if (avatar == null && testAvatars != null && testAvatars.Length > 0)
            {
                avatar = testAvatars[random.Next(0, testAvatars.Length)];
            }
            
            LeaderboardRowData data = new LeaderboardRowData(position, username, score, avatar);
            _leaderboardData.Add(data);
            if (username == loggedInUsername) _userData = data;
        }
        
        SortAndUpdatePositions();
        RebuildFilteredIndices();
        UpdateTop3Avatars();
        UpdateUserFixedRow();
        
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            if (scroller.Delegate == null) scroller.Delegate = this;
            scroller.ReloadData();
        }
    }
    
    private void SortAndUpdatePositions()
    {
        for (int i = 0; i < _leaderboardData.Count - 1; i++)
        {
            for (int j = i + 1; j < _leaderboardData.Count; j++)
            {
                if (_leaderboardData[i].score < _leaderboardData[j].score)
                {
                    LeaderboardRowData temp = _leaderboardData[i];
                    _leaderboardData[i] = _leaderboardData[j];
                    _leaderboardData[j] = temp;
                }
            }
        }
        for (int i = 0; i < _leaderboardData.Count; i++)
        {
            _leaderboardData[i].position = i + 1;
        }
    }
    
    /// <summary>
    /// Rebuilds the filtered index list used by the scroller,
    /// excluding the logged-in user's own row (it appears in the fixed row instead).
    /// </summary>
    private void RebuildFilteredIndices()
    {
        _filteredIndices.Clear();

        // Match the exact same fallback logic used in LoadTestData
        string loggedInUsername = "Guest User";
        if (PlayFabManager.Instance != null &&
            PlayFabManager.Instance.currentPlayerData != null &&
            !string.IsNullOrEmpty(PlayFabManager.Instance.currentPlayerData.playerName))
        {
            loggedInUsername = PlayFabManager.Instance.currentPlayerData.playerName;
        }

        for (int i = 0; i < (_leaderboardData != null ? _leaderboardData.Count : 0); i++)
        {
            if (_leaderboardData[i] == null) continue;

            // Skip by object reference (most reliable) OR by username match
            bool isUserRow = (_userData != null && _leaderboardData[i] == _userData)
                             || _leaderboardData[i].username == loggedInUsername;

            if (isUserRow) continue;

            _filteredIndices.Add(i);
        }
    }
    
    private void UpdateTop3Avatars()
    {
        if (_leaderboardData == null || _leaderboardData.Count == 0)
        {
            if (firstPlaceAvatarImage != null) firstPlaceAvatarImage.sprite = null;
            if (secondPlaceAvatarImage != null) secondPlaceAvatarImage.sprite = null;
            if (thirdPlaceAvatarImage != null) thirdPlaceAvatarImage.sprite = null;
            return;
        }
        
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
    
    private void UpdateUserFixedRow()
    {
        if (PlayFabManager.Instance == null || PlayFabManager.Instance.currentPlayerData == null) return;
        PlayerData playerData = PlayFabManager.Instance.currentPlayerData;
        
        if (_leaderboardData != null && !string.IsNullOrEmpty(playerData.playerName))
        {
            _userData = null;
            for (int i = 0; i < _leaderboardData.Count; i++)
            {
                if (_leaderboardData[i] != null && _leaderboardData[i].username == playerData.playerName)
                {
                    _userData = _leaderboardData[i];
                    break;
                }
            }
        }

        if (userFixedName != null) userFixedName.text = !string.IsNullOrEmpty(playerData.playerName) ? playerData.playerName : "Guest";
        if (userFixedScore != null) userFixedScore.text = (_userData != null) ? _userData.score.ToString() : "0";

        if (userFixedAvatar != null && mainMenuAvatarDisplay != null)
        {
            Sprite mainMenuSprite = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
            if (mainMenuSprite != null) 
            {
                userFixedAvatar.sprite = mainMenuSprite;
                Debug.Log($"[LeaderboardManager] Synced avatar from MainMenu: {mainMenuSprite.name}");
            }
            else
            {
                Debug.LogWarning("[LeaderboardManager] MainMenuAvatarDisplay returned null sprite! MainMenu might not be loaded yet.");
            }
        }

        if (cellViewPrefab != null)
        {
            if (_userData != null)
            {
                if (userFixedBadge != null)
                {
                    if (_userData.position <= 3)
                    {
                        userFixedBadge.gameObject.SetActive(true);
                        switch (_userData.position)
                        {
                            case 1: userFixedBadge.sprite = cellViewPrefab.firstPlaceBadge; break;
                            case 2: userFixedBadge.sprite = cellViewPrefab.secondPlaceBadge; break;
                            case 3: userFixedBadge.sprite = cellViewPrefab.thirdPlaceBadge; break;
                        }
                    }
                    else userFixedBadge.gameObject.SetActive(false);
                }

                if (userFixedPositionText != null)
                {
                    bool isNormalRank = _userData.position > 3;
                    userFixedPositionText.gameObject.SetActive(isNormalRank);
                    if (isNormalRank) userFixedPositionText.text = _userData.position.ToString();
                }

                if (userFixedRowBackground != null)
                {
                    switch (_userData.position)
                    {
                        case 1: userFixedRowBackground.sprite = cellViewPrefab.firstPlaceBackground; break;
                        case 2: userFixedRowBackground.sprite = cellViewPrefab.secondPlaceBackground; break;
                        case 3: userFixedRowBackground.sprite = cellViewPrefab.thirdPlaceBackground; break;
                        default: userFixedRowBackground.sprite = cellViewPrefab.normalRowBackground; break;
                    }
                }

                if (userFixedProfileBackground != null)
                {
                    switch (_userData.position)
                    {
                        case 1: userFixedProfileBackground.sprite = cellViewPrefab.firstPlaceProfileBackground; break;
                        case 2: userFixedProfileBackground.sprite = cellViewPrefab.secondPlaceProfileBackground; break;
                        case 3: userFixedProfileBackground.sprite = cellViewPrefab.thirdPlaceProfileBackground; break;
                        default: userFixedProfileBackground.sprite = cellViewPrefab.normalRowProfileBackground; break;
                    }
                }
            }
            else
            {
                if (userFixedBadge != null) userFixedBadge.gameObject.SetActive(false);
                if (userFixedPositionText != null)
                {
                    userFixedPositionText.gameObject.SetActive(true);
                    userFixedPositionText.text = "-";
                }
                if (userFixedRowBackground != null) userFixedRowBackground.sprite = cellViewPrefab.normalRowBackground;
                if (userFixedProfileBackground != null) userFixedProfileBackground.sprite = cellViewPrefab.normalRowProfileBackground;
            }
        }
    }
    
    /// <summary>
    /// Public method to force a sync from the Main Menu avatar display
    /// Called by AvatarSelector when user picks a new avatar
    /// </summary>
    public void SyncAvatarFromMainMenu()
    {
        if (mainMenuAvatarDisplay == null) return;
        
        Sprite currentAvatar = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
        if (currentAvatar == null) return;

        Debug.Log("[LeaderboardManager] External sync for gallery/preset avatar.");
        
        // 1. Update the fixed row
        UpdateUserFixedRow();
        
        // 2. Update the sprite in the actual data list (so the scrolling list shows it too)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            string pName = PlayFabManager.Instance.currentPlayerData.playerName;
            if (_leaderboardData != null && !string.IsNullOrEmpty(pName))
            {
                for (int i = 0; i < _leaderboardData.Count; i++)
                {
                    if (_leaderboardData[i].username == pName)
                    {
                        _leaderboardData[i].avatarSprite = currentAvatar;
                        break;
                    }
                }
            }
        }
        
        // 3. Update top 3 visuals in case user is there
        UpdateTop3Avatars();

        // 4. Force scroller refresh to show the new image in the list immediately
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            scroller.ReloadData();
        }
    }
    
    public void AddTestEntry(string username, int score, Sprite avatar = null)
    {
        if (_leaderboardData == null) _leaderboardData = new SmallList<LeaderboardRowData>();
        int position = _leaderboardData.Count + 1;
        _leaderboardData.Add(new LeaderboardRowData(position, username, score, avatar));
        SortAndUpdatePositions();
        RebuildFilteredIndices();
        UpdateTop3Avatars();
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            if (scroller.Delegate == null) scroller.Delegate = this;
            scroller.ReloadData();
        }
    }
    
    public void ClearData()
    {
        if (_leaderboardData != null) _leaderboardData.Clear();
        RebuildFilteredIndices();
        UpdateTop3Avatars();
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            if (scroller.Delegate == null) scroller.Delegate = this;
            scroller.ReloadData();
        }
    }
    
    public int GetNumberOfCells(EnhancedScroller scroller)
    {
        return _filteredIndices != null ? _filteredIndices.Count : 0;
    }
    
    public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
    {
        return cellSize;
    }
    
    public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
    {
        if (cellViewPrefab == null) return null;
        LeaderboardRowUI cellView = scroller.GetCellView(cellViewPrefab) as LeaderboardRowUI;
        if (cellView == null) return null;
        
        // Map the scroller's dataIndex → actual data index (user row excluded)
        int actualIndex = (_filteredIndices != null && dataIndex < _filteredIndices.Count)
                          ? _filteredIndices[dataIndex]
                          : dataIndex;
        
        cellView.name = $"Leaderboard Row {actualIndex}";
        if (_leaderboardData != null && actualIndex < _leaderboardData.Count)
        {
            cellView.SetData(_leaderboardData[actualIndex]);
        }
        return cellView;
    }
    
    public void SetCellSizeAndReload(float newCellSize)
    {
        cellSize = newCellSize;
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            if (scroller.Delegate == null) scroller.Delegate = this;
            if (_leaderboardData != null && _leaderboardData.Count > 0)
            {
                scroller.ReloadData();
            }
        }
    }
}
