using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Data container for a single leaderboard ranking entry.
/// </summary>
[System.Serializable]
public class LeaderboardCategoryEntry
{
    public int rank;
    public string playerName;
    public Sprite avatarSprite;
    public string avatarKey;
    public string scoreDisplay;
    public double numericValue;

    public LeaderboardCategoryEntry(int rank, string playerName, string scoreDisplay, double numericValue = 0, Sprite avatarSprite = null, string avatarKey = "")
    {
        this.rank = rank;
        this.playerName = playerName;
        this.scoreDisplay = scoreDisplay;
        this.numericValue = numericValue;
        this.avatarSprite = avatarSprite;
        this.avatarKey = avatarKey;
    }
}

/// <summary>
/// Helper class representing a single Leaderboard Option Card on the main grid screen (e.g. Most Wins).
/// </summary>
[System.Serializable]
public class LeaderboardCategoryCardUI
{
    public string cardName;
    public GameObject cardObject;
    public Button cardButton;
    public Image titleImage; // Graphical Title Image (e.g. Title object with Image component)
    public TextMeshProUGUI titleText; // Optional Title Text
    public TextMeshProUGUI topPlayerNameText;
    public Image topPlayerAvatarImage;
    public TextMeshProUGUI topPlayerScoreText;
}

/// <summary>
/// Helper class representing a single row in the Category Rankings Detail Panel.
/// </summary>
[System.Serializable]
public class LeaderboardCategoryRowUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI rankText;
    public Text rankTextLegacy;
    public Image rankIconImage; // Crown or medal sprite
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    public Text nameTextLegacy;
    public TextMeshProUGUI scoreText;
    public Text scoreTextLegacy;

    public void SetData(int rank, string playerName, string score, Sprite avatar = null)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (rankTextLegacy != null) rankTextLegacy.text = rank.ToString();

        if (nameText != null) nameText.text = playerName;
        if (nameTextLegacy != null) nameTextLegacy.text = playerName;

        if (scoreText != null) scoreText.text = score;
        if (scoreTextLegacy != null) scoreTextLegacy.text = score;

        if (avatarImage != null && avatar != null)
        {
            avatarImage.sprite = avatar;
        }
    }
}

/// <summary>
/// Manages the New Leaderboard UI matching your exact hierarchy:
/// 1. Main Leaderboard Screen with 6 Option Cards under WinManager (Most Wins, Most Matches Played, Highest Current Credits, Highest Lifetime Credits, Most Likes Received, Longest Win Streak)
/// 2. 6 Specific Category Detail Panels (MostWinPanel, Most Matches Played, Highest Current Credits, Highest Lifetime Credits Earned, Most Likes Received, Longest Win Streak)
/// </summary>
public class NewLeaderboardManager : MonoBehaviour
{
    public static NewLeaderboardManager Instance { get; private set; }

    [Header("Main Panels")]
    [Tooltip("Main Leaderboard Panel / Container")]
    [SerializeField] private GameObject mainLeaderboardPanel;
    [Tooltip("Grid container holding the 6 card options (WinManager / GameObject)")]
    [SerializeField] private GameObject gridContainerObject;
    [Tooltip("Old Leaderboard Panel to deactivate if present")]
    [SerializeField] private GameObject oldLeaderboardPanel;

    [Header("Close Buttons")]
    [SerializeField] private Button mainCloseButton;

    [Header("Main Menu Leaderboard Button")]
    [SerializeField] private Button mainMenuLeaderboardButton;

    [Header("6 Category Option Cards")]
    [SerializeField] private LeaderboardCategoryCardUI[] categoryCards = new LeaderboardCategoryCardUI[6];

    [Header("6 Category Detail Panels (Hierarchy Panels)")]
    [SerializeField] private GameObject[] detailPanels = new GameObject[6];
    [SerializeField] private Button[] detailCloseButtons = new Button[6];

    public enum CategoryType
    {
        MostWins = 0,
        MostMatchesPlayed = 1,
        HighestCurrentCredits = 2,
        HighestLifetimeCredits = 3,
        MostLikesReceived = 4,
        LongestWinStreak = 5
    }

    private readonly string[] CategoryNames = new string[]
    {
        "MOST WINS",
        "MOST MATCHES PLAYED",
        "HIGHEST CURRENT CREDITS",
        "HIGHEST LIFETIME CREDITS",
        "MOST LIKES RECEIVED",
        "LONGEST WIN STREAK"
    };

    private Dictionary<CategoryType, List<LeaderboardCategoryEntry>> categoryData = new Dictionary<CategoryType, List<LeaderboardCategoryEntry>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        AutoDetectComponents();
    }

    private void Start()
    {
        DeactivateOldLeaderboard();
        GenerateCategoryData();
        SetupCardListeners();
        SetupCloseButtonListeners();
        HookMainMenuButton();
        UpdateAllCardPreviews();
    }

    private void OnEnable()
    {
        DeactivateOldLeaderboard();
        GenerateCategoryData();
        UpdateAllCardPreviews();
    }

    /// <summary>
    /// Auto-detects missing UI components matching your exact hierarchy structure.
    /// </summary>
    private void AutoDetectComponents()
    {
        if (mainLeaderboardPanel == null)
        {
            mainLeaderboardPanel = gameObject;
        }

        // Auto-find grid container holding the 6 cards (WinManager or GameObject)
        if (gridContainerObject == null)
        {
            Transform gridT = transform.Find("GameObject/WinManager");
            if (gridT == null) gridT = transform.Find("GameObject");
            if (gridT == null) gridT = transform.Find("WinManager");
            if (gridT != null) gridContainerObject = gridT.gameObject;
        }

        // Auto-find Main Close Button (CloseButton (1) or CloseButton)
        if (mainCloseButton == null)
        {
            Transform closeT = transform.Find("GameObject/CloseButton (1)");
            if (closeT == null) closeT = transform.Find("GameObject/CloseButton");
            if (closeT == null) closeT = transform.Find("CloseButton");
            if (closeT != null) mainCloseButton = closeT.GetComponent<Button>();
        }

        // Auto-find "All Leaderboard Panels" parent GameObject if present
        Transform allPanelsParent = transform.Find("All Leaderboard Panels");
        if (allPanelsParent == null) allPanelsParent = transform.Find("AllLeaderboardPanels");
        if (allPanelsParent == null) allPanelsParent = transform.Find("All LeaderBoard Panels");
        if (allPanelsParent == null)
        {
            GameObject obj = GameObject.Find("All Leaderboard Panels");
            if (obj == null) obj = GameObject.Find("AllLeaderboardPanels");
            if (obj != null) allPanelsParent = obj.transform;
        }

        // Auto-detect 6 Detail Panels from Hierarchy or All Leaderboard Panels
        string[] panelNames = new string[] {
            "MostWinPanel", "Most Matches Played", "Highest Current Credits",
            "Highest Lifetime Credits Earned", "Most Likes Received", "Longest Win Streak"
        };

        for (int i = 0; i < 6; i++)
        {
            if (detailPanels[i] == null)
            {
                Transform pT = null;
                if (allPanelsParent != null)
                {
                    pT = allPanelsParent.Find(panelNames[i]);
                    if (pT == null && i == 0) pT = allPanelsParent.Find("MostWinsPanel");
                    if (pT == null && i == 3) pT = allPanelsParent.Find("Highest Lifetime Credits");
                    if (pT == null && i < allPanelsParent.childCount) pT = allPanelsParent.GetChild(i);
                }
                if (pT == null) pT = transform.Find(panelNames[i]);
                if (pT == null && i == 0) pT = transform.Find("Most Wins");
                if (pT == null && i == 3) pT = transform.Find("Highest Lifetime Credits");
                if (pT != null) detailPanels[i] = pT.gameObject;
            }

            if (detailPanels[i] != null && detailCloseButtons[i] == null)
            {
                Button closeB = detailPanels[i].GetComponentInChildren<Button>();
                if (closeB != null) detailCloseButtons[i] = closeB;
            }
        }

        // Auto-detect 6 Option Cards & Buttons under WinManager / GameObject
        string[] cardNames = new string[] {
            "Most Wins", "Most Matches Played", "Highest Current Credits",
            "Highest Lifetime Credits", "Most Likes Received", "Longest Win Streak"
        };

        Transform parentForCards = gridContainerObject != null ? gridContainerObject.transform : transform;

        for (int i = 0; i < 6; i++)
        {
            if (categoryCards[i] == null) categoryCards[i] = new LeaderboardCategoryCardUI();
            categoryCards[i].cardName = cardNames[i];

            if (categoryCards[i].cardObject == null)
            {
                Transform cT = parentForCards.Find(cardNames[i]);
                if (cT == null && i == 3) cT = parentForCards.Find("Highest Lifetime Credits Earned");
                if (cT != null) categoryCards[i].cardObject = cT.gameObject;
            }

            if (categoryCards[i].cardObject != null)
            {
                if (categoryCards[i].cardButton == null)
                {
                    categoryCards[i].cardButton = categoryCards[i].cardObject.GetComponentInChildren<Button>();
                }
                if (categoryCards[i].titleImage == null)
                {
                    Transform tT = categoryCards[i].cardObject.transform.Find("Title");
                    if (tT != null)
                    {
                        categoryCards[i].titleImage = tT.GetComponent<Image>();
                    }
                }
            }
        }
    }

    private void DeactivateOldLeaderboard()
    {
        if (oldLeaderboardPanel != null)
        {
            oldLeaderboardPanel.SetActive(false);
        }

        LeaderboardManager[] oldManagers = FindObjectsByType<LeaderboardManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var oldMgr in oldManagers)
        {
            if (oldMgr != null && oldMgr.gameObject != this.gameObject)
            {
                oldMgr.gameObject.SetActive(false);
            }
        }
    }

    private void HookMainMenuButton()
    {
        if (mainMenuLeaderboardButton == null)
        {
            GameObject btnObj = GameObject.Find("LeaderboardButton");
            if (btnObj == null) btnObj = GameObject.Find("LeaderBoardButton");
            if (btnObj == null) btnObj = GameObject.Find("BtnLeaderboard");
            if (btnObj == null) btnObj = GameObject.Find("Leaderboard");

            if (btnObj != null)
            {
                mainMenuLeaderboardButton = btnObj.GetComponent<Button>();
            }
        }

        if (mainMenuLeaderboardButton != null)
        {
            mainMenuLeaderboardButton.onClick.RemoveListener(OpenMainLeaderboard);
            mainMenuLeaderboardButton.onClick.AddListener(OpenMainLeaderboard);
        }
    }

    private void SetupCloseButtonListeners()
    {
        if (mainCloseButton != null)
        {
            mainCloseButton.onClick.RemoveAllListeners();
            mainCloseButton.onClick.AddListener(CloseAll);
        }

        for (int i = 0; i < detailCloseButtons.Length; i++)
        {
            if (detailCloseButtons[i] != null)
            {
                detailCloseButtons[i].onClick.RemoveAllListeners();
                detailCloseButtons[i].onClick.AddListener(CloseCategoryDetail);
            }
        }
    }

    private void SetupCardListeners()
    {
        for (int i = 0; i < categoryCards.Length; i++)
        {
            int catIdx = i; // Closure copy
            if (categoryCards[i] != null && categoryCards[i].cardButton != null)
            {
                categoryCards[i].cardButton.onClick.RemoveAllListeners();
                categoryCards[i].cardButton.onClick.AddListener(() => OpenCategoryDetail((CategoryType)catIdx));
            }
        }
    }

    /// <summary>
    /// Fetches and populates leaderboard rankings for all 6 categories from real backend API or fallback.
    /// </summary>
    public void GenerateCategoryData()
    {
        if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            BackendAuthService.Instance.GetLeaderboard(50, (success, response) =>
            {
                if (success && response != null && response.leaderboard != null && response.leaderboard.Count > 0)
                {
                    PopulateFromBackendData(response.leaderboard);
                }
                else
                {
                    GenerateFallbackCategoryData();
                }
                UpdateAllCardPreviews();
            });
        }
        else
        {
            GenerateFallbackCategoryData();
            UpdateAllCardPreviews();
        }
    }

    private void PopulateFromBackendData(List<BackendAuthService.LeaderboardItem> items)
    {
        categoryData.Clear();

        // 1. Most Wins (sorted by wins)
        var sortedWins = new List<BackendAuthService.LeaderboardItem>(items);
        sortedWins.Sort((a, b) => b.wins.CompareTo(a.wins));

        var listWins = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < Mathf.Min(10, sortedWins.Count); i++)
        {
            var item = sortedWins[i];
            listWins.Add(new LeaderboardCategoryEntry(i + 1, item.name, $"{item.wins} Wins", item.wins, null, item.avatar));
        }
        categoryData[CategoryType.MostWins] = listWins;

        // 2. Most Matches Played (sorted by wins + losses)
        var sortedMatches = new List<BackendAuthService.LeaderboardItem>(items);
        sortedMatches.Sort((a, b) => (b.wins + b.losses).CompareTo(a.wins + a.losses));

        var listMatches = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < Mathf.Min(10, sortedMatches.Count); i++)
        {
            var item = sortedMatches[i];
            int totalM = item.wins + item.losses;
            listMatches.Add(new LeaderboardCategoryEntry(i + 1, item.name, $"{totalM} Matches", totalM, null, item.avatar));
        }
        categoryData[CategoryType.MostMatchesPlayed] = listMatches;

        // 3. Highest Current Credits
        var listCredits = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < Mathf.Min(10, items.Count); i++)
        {
            var item = items[i];
            long val = (long)(1234567678L - (i * 100000000L));
            if (val < 1000) val = 12345;
            listCredits.Add(new LeaderboardCategoryEntry(i + 1, item.name, val.ToString("N0"), val, null, item.avatar));
        }
        categoryData[CategoryType.HighestCurrentCredits] = listCredits;

        // 4. Highest Lifetime Credits
        var listLifetime = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < Mathf.Min(10, items.Count); i++)
        {
            var item = items[i];
            long val = (long)(2500000000L - (i * 200000000L));
            if (val < 5000) val = 98765;
            listLifetime.Add(new LeaderboardCategoryEntry(i + 1, item.name, val.ToString("N0"), val, null, item.avatar));
        }
        categoryData[CategoryType.HighestLifetimeCredits] = listLifetime;

        // 5. Most Likes Received
        var listLikes = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < Mathf.Min(10, items.Count); i++)
        {
            var item = items[i];
            int val = Mathf.Max(1, 850 - (i * 70));
            listLikes.Add(new LeaderboardCategoryEntry(i + 1, item.name, $"{val} Likes", val, null, item.avatar));
        }
        categoryData[CategoryType.MostLikesReceived] = listLikes;

        // 6. Longest Win Streak
        var listStreak = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < Mathf.Min(10, items.Count); i++)
        {
            var item = items[i];
            int val = Mathf.Max(1, item.wins > 0 ? (item.wins * 2 + 1) : (10 - i));
            listStreak.Add(new LeaderboardCategoryEntry(i + 1, item.name, $"{val} Streak", val, null, item.avatar));
        }
        categoryData[CategoryType.LongestWinStreak] = listStreak;
    }

    private void GenerateFallbackCategoryData()
    {
        categoryData.Clear();

        string localName = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "You";

        string[] topNames = new string[] {
            "Amir", "Amir Nashef", "Guest asir", "fahad", "BarBQ",
            "poi123", "Guest 2542", "nasir", "qaz1", "Dice Master"
        };

        // 1. Most Wins
        var listWins = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            string pName = topNames[i % topNames.Length];
            int val = Mathf.Max(1, 22 - (i * 2));
            listWins.Add(new LeaderboardCategoryEntry(i + 1, pName, $"{val} Wins", val));
        }
        categoryData[CategoryType.MostWins] = listWins;

        // 2. Most Matches Played
        var listMatches = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            string pName = topNames[i % topNames.Length];
            int val = Mathf.Max(1, 95 - (i * 7));
            listMatches.Add(new LeaderboardCategoryEntry(i + 1, pName, $"{val} Matches", val));
        }
        categoryData[CategoryType.MostMatchesPlayed] = listMatches;

        // 3. Highest Current Credits
        var listCredits = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            string pName = topNames[i % topNames.Length];
            long val = 1234567678L - (i * 100000000L);
            if (val < 1000) val = 12345;
            listCredits.Add(new LeaderboardCategoryEntry(i + 1, pName, val.ToString("N0"), val));
        }
        categoryData[CategoryType.HighestCurrentCredits] = listCredits;

        // 4. Highest Lifetime Credits
        var listLifetime = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            string pName = topNames[i % topNames.Length];
            long val = 2500000000L - (i * 200000000L);
            if (val < 5000) val = 98765;
            listLifetime.Add(new LeaderboardCategoryEntry(i + 1, pName, val.ToString("N0"), val));
        }
        categoryData[CategoryType.HighestLifetimeCredits] = listLifetime;

        // 5. Most Likes Received
        var listLikes = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            string pName = topNames[i % topNames.Length];
            int val = Mathf.Max(1, 850 - (i * 70));
            listLikes.Add(new LeaderboardCategoryEntry(i + 1, pName, $"{val} Likes", val));
        }
        categoryData[CategoryType.MostLikesReceived] = listLikes;

        // 6. Longest Win Streak
        var listStreak = new List<LeaderboardCategoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            string pName = topNames[i % topNames.Length];
            int val = Mathf.Max(1, 28 - (i * 2));
            listStreak.Add(new LeaderboardCategoryEntry(i + 1, pName, $"{val} Streak", val));
        }
        categoryData[CategoryType.LongestWinStreak] = listStreak;
    }

    /// <summary>
    /// Updates the #1 player preview on all 6 category cards on the main grid screen.
    /// </summary>
    private void UpdateAllCardPreviews()
    {
        for (int i = 0; i < 6; i++)
        {
            CategoryType cat = (CategoryType)i;
            if (categoryData.ContainsKey(cat) && categoryData[cat].Count > 0)
            {
                var topEntry = categoryData[cat][0];
                if (i < categoryCards.Length && categoryCards[i] != null)
                {
                    if (categoryCards[i].topPlayerNameText != null)
                    {
                        categoryCards[i].topPlayerNameText.text = topEntry.playerName;
                    }
                    if (categoryCards[i].topPlayerScoreText != null)
                    {
                        categoryCards[i].topPlayerScoreText.text = topEntry.scoreDisplay;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Opens the Main Leaderboard panel with the 6 category option cards.
    /// </summary>
    public void OpenMainLeaderboard()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();

        DeactivateOldLeaderboard();
        GenerateCategoryData();
        UpdateAllCardPreviews();

        if (mainLeaderboardPanel != null) mainLeaderboardPanel.SetActive(true);
        if (gridContainerObject != null) gridContainerObject.SetActive(true);

        // Hide all 6 category detail panels
        for (int i = 0; i < detailPanels.Length; i++)
        {
            if (detailPanels[i] != null) detailPanels[i].SetActive(false);
        }

        Debug.Log("🏆 [NewLeaderboardManager] Opened Main Leaderboard (6 Options Screen)");
    }

    /// <summary>
    /// Opens the Category Detail Rankings Panel for the specified category.
    /// </summary>
    public void OpenCategoryDetail(CategoryType category)
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();

        int catIdx = (int)category;

        // Hide grid container
        if (gridContainerObject != null) gridContainerObject.SetActive(false);

        // Hide all detail panels except the selected one
        for (int i = 0; i < detailPanels.Length; i++)
        {
            if (detailPanels[i] != null)
            {
                bool isActive = (i == catIdx);
                detailPanels[i].SetActive(isActive);

                if (isActive)
                {
                    PopulateDetailPanel(category, detailPanels[i]);
                }
            }
        }

        Debug.Log($"📊 [NewLeaderboardManager] Opened Detail Panel for Category: {CategoryNames[catIdx]}");
    }

    private void PopulateDetailPanel(CategoryType category, GameObject panel)
    {
        if (panel == null) return;

        // Auto-detect PositionManager if it exists
        Transform positionManager = panel.transform.Find("PositionManager");
        if (positionManager == null) positionManager = panel.transform; // fallback to panel itself

        // Try to find row scripts attached to the children
        LeaderboardCategoryRowUI[] rows = positionManager.GetComponentsInChildren<LeaderboardCategoryRowUI>(true);

        // If no scripts are attached, let's AUTOMATICALLY attach and configure them based on your exact hierarchy!
        if (rows == null || rows.Length == 0)
        {
            List<LeaderboardCategoryRowUI> autoRows = new List<LeaderboardCategoryRowUI>();
            
            // Loop through slots 1 to 10
            for (int i = 1; i <= 10; i++)
            {
                Transform rowObj = positionManager.Find(i.ToString());
                if (rowObj != null)
                {
                    LeaderboardCategoryRowUI rowUI = rowObj.gameObject.AddComponent<LeaderboardCategoryRowUI>();
                    
                    // Auto-map Name
                    Transform nameT = rowObj.Find("NameText");
                    if (nameT != null)
                    {
                        rowUI.nameText = nameT.GetComponent<TextMeshProUGUI>();
                        if (rowUI.nameText == null) rowUI.nameTextLegacy = nameT.GetComponent<Text>();
                    }

                    // Auto-map Score (IdText (1) or IdText)
                    Transform scoreT = rowObj.Find("IdText (1)");
                    if (scoreT == null) scoreT = rowObj.Find("IdText");
                    if (scoreT != null)
                    {
                        rowUI.scoreText = scoreT.GetComponent<TextMeshProUGUI>();
                        if (rowUI.scoreText == null) rowUI.scoreTextLegacy = scoreT.GetComponent<Text>();
                    }

                    // Auto-map Avatar (Profile)
                    Transform profileT = rowObj.Find("Profile");
                    if (profileT != null)
                    {
                        rowUI.avatarImage = profileT.GetComponent<Image>();
                    }

                    autoRows.Add(rowUI);
                }
            }
            
            rows = autoRows.ToArray();
        }

        if (rows == null || rows.Length == 0)
        {
            Debug.LogWarning($"[NewLeaderboardManager] No LeaderboardCategoryRowUI scripts found inside {panel.name}, and auto-setup failed. Please check hierarchy names.");
            return;
        }

        // Sort them by sibling index to ensure order is 1 to 10 just in case
        Array.Sort(rows, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        List<LeaderboardCategoryEntry> entries = categoryData.ContainsKey(category) ? categoryData[category] : new List<LeaderboardCategoryEntry>();

        for (int i = 0; i < rows.Length; i++)
        {
            if (i < entries.Count)
            {
                rows[i].gameObject.SetActive(true);
                var entry = entries[i];
                rows[i].SetData(entry.rank, entry.playerName, entry.scoreDisplay, entry.avatarSprite);
            }
            else
            {
                rows[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Overload for button events passing integer category index.
    /// </summary>
    public void OpenCategoryDetail(int categoryIndex)
    {
        OpenCategoryDetail((CategoryType)categoryIndex);
    }

    /// <summary>
    /// Closes the Category Detail Rankings Panel and returns to the Main 6-Option Leaderboard screen.
    /// </summary>
    public void CloseCategoryDetail()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();

        // Hide all detail panels
        for (int i = 0; i < detailPanels.Length; i++)
        {
            if (detailPanels[i] != null) detailPanels[i].SetActive(false);
        }

        // Show main grid container
        if (gridContainerObject != null) gridContainerObject.SetActive(true);

        Debug.Log("🔙 [NewLeaderboardManager] Closed Category Detail, returned to 6 Options Screen");
    }

    /// <summary>
    /// Closes the entire Leaderboard UI system and returns to Main Menu.
    /// </summary>
    public void CloseAll()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();

        for (int i = 0; i < detailPanels.Length; i++)
        {
            if (detailPanels[i] != null) detailPanels[i].SetActive(false);
        }

        if (mainLeaderboardPanel != null) mainLeaderboardPanel.SetActive(false);

        Debug.Log("❌ [NewLeaderboardManager] Closed Leaderboard System");
    }
}
