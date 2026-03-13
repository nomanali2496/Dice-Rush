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
    [SerializeField] private Image firstPlaceAvatarCustom;
    [SerializeField] private Image secondPlaceAvatarImage;
    [SerializeField] private Image secondPlaceAvatarCustom;
    [SerializeField] private Image thirdPlaceAvatarImage;
    [SerializeField] private Image thirdPlaceAvatarCustom;
    
    [Header("External UI References")]
    [SerializeField] private MainMenuAvatarDisplay mainMenuAvatarDisplay;
    
    [Header("User's Fixed Row (Explicit Components)")]
    [SerializeField] private Image userFixedAvatar;
    [SerializeField] private Image userFixedAvatarCustom;
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
            bool isRowCustom = false;
            if (i == 0) // Include logged-in user in test data
            {
                username = loggedInUsername;
                // REAL AVATAR SYNC: Use the actual sprite from main menu if available
                if (mainMenuAvatarDisplay != null)
                {
                    avatar = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
                    isRowCustom = mainMenuAvatarDisplay.IsShowingCustomAvatar();
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
            
            LeaderboardRowData data = new LeaderboardRowData(position, username, score, avatar, isRowCustom);
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
    
    /// <summary>
    /// Returns the best available avatar sprite for a leaderboard entry.
    /// If the entry belongs to the logged-in user, the live MainMenu avatar is
    /// always preferred so gallery/custom selections are reflected immediately.
    /// </summary>
    private Sprite GetAvatarForEntry(LeaderboardRowData entry)
    {
        if (entry == null) return null;

        // If this entry is the logged-in user, use the live MainMenu avatar
        if (entry == _userData && mainMenuAvatarDisplay != null)
        {
            Sprite live = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
            if (live != null) return live;
        }

        return entry.avatarSprite;
    }

    private void UpdateTop3Avatars()
    {
        if (_leaderboardData == null || _leaderboardData.Count == 0)
        {
            if (firstPlaceAvatarImage != null) firstPlaceAvatarImage.sprite = null;
            if (firstPlaceAvatarCustom != null) firstPlaceAvatarCustom.sprite = null;
            if (secondPlaceAvatarImage != null) secondPlaceAvatarImage.sprite = null;
            if (secondPlaceAvatarCustom != null) secondPlaceAvatarCustom.sprite = null;
            if (thirdPlaceAvatarImage != null) thirdPlaceAvatarImage.sprite = null;
            if (thirdPlaceAvatarCustom != null) thirdPlaceAvatarCustom.sprite = null;
            return;
        }

        if (firstPlaceAvatarImage != null || firstPlaceAvatarCustom != null)
        {
            LeaderboardRowData entry = _leaderboardData.Count > 0 ? _leaderboardData[0] : null;
            Sprite s = GetAvatarForEntry(entry);
            bool isC = (entry == _userData && mainMenuAvatarDisplay != null) ? mainMenuAvatarDisplay.IsShowingCustomAvatar() : (entry != null && entry.isCustom);
            SetTop3Slot(firstPlaceAvatarImage, firstPlaceAvatarCustom, s, isC);
        }

        if (secondPlaceAvatarImage != null || secondPlaceAvatarCustom != null)
        {
            LeaderboardRowData entry = _leaderboardData.Count > 1 ? _leaderboardData[1] : null;
            Sprite s = GetAvatarForEntry(entry);
            bool isC = (entry == _userData && mainMenuAvatarDisplay != null) ? mainMenuAvatarDisplay.IsShowingCustomAvatar() : (entry != null && entry.isCustom);
            SetTop3Slot(secondPlaceAvatarImage, secondPlaceAvatarCustom, s, isC);
        }

        if (thirdPlaceAvatarImage != null || thirdPlaceAvatarCustom != null)
        {
            LeaderboardRowData entry = _leaderboardData.Count > 2 ? _leaderboardData[2] : null;
            Sprite s = GetAvatarForEntry(entry);
            bool isC = (entry == _userData && mainMenuAvatarDisplay != null) ? mainMenuAvatarDisplay.IsShowingCustomAvatar() : (entry != null && entry.isCustom);
            SetTop3Slot(thirdPlaceAvatarImage, thirdPlaceAvatarCustom, s, isC);
        }
    }

    private void SetTop3Slot(Image spriteSlot, Image customSlot, Sprite sprite, bool isCustom)
    {
        if (isCustom)
        {
            if (customSlot != null)
            {
                customSlot.sprite = sprite;
                customSlot.rectTransform.localScale = Vector3.one * 2f;
                customSlot.gameObject.SetActive(sprite != null);
            }
            if (spriteSlot != null) spriteSlot.gameObject.SetActive(false);
        }
        else
        {
            if (spriteSlot != null)
            {
                spriteSlot.sprite = sprite;
                spriteSlot.rectTransform.localScale = Vector3.one;
                spriteSlot.gameObject.SetActive(sprite != null);
            }
            if (customSlot != null) customSlot.gameObject.SetActive(false);
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

        if ((userFixedAvatar != null || userFixedAvatarCustom != null) && mainMenuAvatarDisplay != null)
        {
            Sprite mainMenuSprite = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
            bool isC = mainMenuAvatarDisplay.IsShowingCustomAvatar();
            
            if (mainMenuSprite != null) 
            {
                if (isC)
                {
                    if (userFixedAvatarCustom != null)
                    {
                        userFixedAvatarCustom.sprite = mainMenuSprite;
                        userFixedAvatarCustom.rectTransform.localScale = Vector3.one * 2f;
                        userFixedAvatarCustom.gameObject.SetActive(true);
                    }
                    if (userFixedAvatar != null) userFixedAvatar.gameObject.SetActive(false);
                }
                else
                {
                    if (userFixedAvatar != null)
                    {
                        userFixedAvatar.sprite = mainMenuSprite;
                        userFixedAvatar.rectTransform.localScale = Vector3.one;
                        userFixedAvatar.gameObject.SetActive(true);
                    }
                    if (userFixedAvatarCustom != null) userFixedAvatarCustom.gameObject.SetActive(false);
                }
                Debug.Log($"[LeaderboardManager] Synced avatar from MainMenu (Custom: {isC})");
            }
            else
            {
                Debug.LogWarning("[LeaderboardManager] MainMenuAvatarDisplay returned null sprite!");
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

        // 1. Update the sprite on the user's data entry directly via object reference (most reliable)
        if (_userData != null)
        {
            _userData.avatarSprite = currentAvatar;
            _userData.isCustom = mainMenuAvatarDisplay.IsShowingCustomAvatar();
        }
        else if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            // Fallback: search by username if _userData ref isn't set yet
            string pName = PlayFabManager.Instance.currentPlayerData.playerName;
            if (string.IsNullOrEmpty(pName)) pName = "Guest User";
            if (_leaderboardData != null)
            {
                for (int i = 0; i < _leaderboardData.Count; i++)
                {
                    if (_leaderboardData[i] != null && _leaderboardData[i].username == pName)
                    {
                        _leaderboardData[i].avatarSprite = currentAvatar;
                        // Cache the reference for future calls
                        _userData = _leaderboardData[i];
                        break;
                    }
                }
            }
        }

        // 2. Update the fixed row
        UpdateUserFixedRow();
        
        // 3. Update top 3 visuals in case user is there
        UpdateTop3Avatars();

        // 4. Force scroller refresh to show the new image in the list immediately
        if (scroller != null && scroller.gameObject.activeInHierarchy)
        {
            scroller.ReloadData();
        }
    }
    
    public void AddTestEntry(string username, int score, Sprite avatar = null, bool isCustom = false)
    {
        if (_leaderboardData == null) _leaderboardData = new SmallList<LeaderboardRowData>();
        int position = _leaderboardData.Count + 1;
        _leaderboardData.Add(new LeaderboardRowData(position, username, score, avatar, isCustom));
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
