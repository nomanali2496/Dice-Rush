using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace UI
{
    [System.Serializable]
    public class LivePlayerScoreData
    {
        public string userId;
        public string username;
        public int score;
        public Sprite avatarSprite;
        public bool isLocalPlayer;

        public LivePlayerScoreData(string id, string name, int currentScore, Sprite avatar = null, bool isLocal = false)
        {
            userId = id;
            username = name;
            score = currentScore;
            avatarSprite = avatar;
            isLocalPlayer = isLocal;
        }
    }

    /// <summary>
    /// Live Gameplay Dropdown Leaderboard (Placed at Top-Left Corner of Gameplay Screen)
    /// Shows real-time rankings and coins/scores of all active players during match.
    /// </summary>
    public class GameplayProfileDropdown : MonoBehaviour
    {
        public static GameplayProfileDropdown Instance { get; private set; }

        [Header("UI Containers & References")]
        [Tooltip("Parent RectTransform containing profile row instances")]
        [SerializeField] private RectTransform itemsContainer;

        [Tooltip("Prefab for single player row (with ProfileDropdownRowUI attached)")]
        [SerializeField] private GameObject profileRowPrefab;

        [Tooltip("Expandable content panel transform (for animated collapse/expand)")]
        [SerializeField] private RectTransform expandableContentPanel;

        [Tooltip("Background panel transform to resize alongside content")]
        [SerializeField] private RectTransform backgroundPanel;

        [Tooltip("The minimum (collapsed) height of the background panel")]
        [SerializeField] private float backgroundCollapsedHeight = 80f;

        [Header("Toggle Controls")]
        [Tooltip("Button to expand or collapse the gameplay dropdown")]
        [SerializeField] private Button toggleButton;

        [Tooltip("Text component inside toggle button (e.g. Live Standings / Scores)")]
        [SerializeField] private TextMeshProUGUI toggleButtonText;

        [Tooltip("Arrow icon transform to rotate on toggle")]
        [SerializeField] private Transform arrowIconTransform;

        [Header("Labels & Text Settings")]
        [SerializeField] private string collapsedButtonText = "View All Profiles";
        [SerializeField] private string expandedButtonText = "Hide Profiles";

        [Header("Live Update Settings")]
        [Tooltip("Interval in seconds to update live player scores")]
        [SerializeField] private float updateIntervalSeconds = 0.5f;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        private bool isExpanded = false;
        private float fullPanelHeight;
        private float updateTimer = 0f;
        private List<GameObject> spawnedRowObjects = new List<GameObject>();
        private List<LivePlayerScoreData> currentMatchPlayers = new List<LivePlayerScoreData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleDropdown);
            }
        }

        private void Start()
        {
            // Position top-left anchor check
            EnsureTopLeftAnchor();

            // Initial height calculation
            RecalculateHeight();
            UpdateToggleVisuals(false);

            // Collapse by default so it doesn't obstruct initial gameplay view
            if (expandableContentPanel != null)
            {
                expandableContentPanel.sizeDelta = new Vector2(expandableContentPanel.sizeDelta.x, 0);
            }
            if (backgroundPanel != null)
            {
                backgroundPanel.sizeDelta = new Vector2(backgroundPanel.sizeDelta.x, backgroundCollapsedHeight);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsActivelyPlaying)
            {
                return;
            }

            updateTimer += Time.deltaTime;
            if (updateTimer >= updateIntervalSeconds)
            {
                updateTimer = 0f;
                UpdateLiveScores();
            }
        }

        /// <summary>
        /// Ensures RectTransform is anchored to Top-Left of screen
        /// </summary>
        private void EnsureTopLeftAnchor()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
            }
        }

        /// <summary>
        /// Collects live player scores from GameManager and refreshes dropdown ranking
        /// </summary>
        public void UpdateLiveScores()
        {
            if (GameManager.Instance == null) return;

            StartGameData matchData = GameManager.Instance.GetCurrentMatchData();
            List<LivePlayerScoreData> updatedPlayers = new List<LivePlayerScoreData>();

            // 1. Add Local Player
            string localName = string.IsNullOrEmpty(PlayerModel.Name) ? "You" : PlayerModel.Name;
            int localScore = ScoreFeedbackUIManager.Instance != null ? ScoreFeedbackUIManager.Instance.CurrentScore : 0;
            Sprite localAvatar = GameManager.Instance.GetAvatarSprite(PlayerModel.SelectedAvatarIndex.ToString());

            updatedPlayers.Add(new LivePlayerScoreData(PlayerModel.Id, $"{localName} (You)", localScore, localAvatar, true));

            // 2. Add Opponents / CPUs from match data
            if (matchData != null && matchData.playerAvatars != null)
            {
                foreach (var pair in matchData.playerAvatars)
                {
                    if (pair.Key == PlayerModel.Id) continue; // Skip local player

                    string oppId = pair.Key;
                    string oppName = !string.IsNullOrEmpty(pair.Value.name) ? pair.Value.name : $"Player {oppId}";
                    Sprite oppAvatar = GameManager.Instance.GetAvatarSprite(pair.Value.avatar);

                    int oppScore = 0;
                    if (matchData.gameState != null && matchData.gameState.ContainsKey(oppId))
                    {
                        oppScore = matchData.gameState[oppId].score;
                    }

                    updatedPlayers.Add(new LivePlayerScoreData(oppId, oppName, oppScore, oppAvatar, false));
                }
            }

            // 3. Sort descending by score (highest coins/score first)
            currentMatchPlayers = updatedPlayers.OrderByDescending(p => p.score).ToList();

            // 4. Render rows
            RenderPlayerRows();
        }

        /// <summary>
        /// Renders sorted player rows into the dropdown container
        /// </summary>
        private void RenderPlayerRows()
        {
            ClearRows();

            if (profileRowPrefab == null || itemsContainer == null) return;

            for (int i = 0; i < currentMatchPlayers.Count; i++)
            {
                var player = currentMatchPlayers[i];
                GameObject rowObj = Instantiate(profileRowPrefab, itemsContainer);
                spawnedRowObjects.Add(rowObj);

                ProfileDropdownRowUI rowUI = rowObj.GetComponent<ProfileDropdownRowUI>();
                if (rowUI != null)
                {
                    rowUI.SetData(player.username, player.score, player.avatarSprite, i + 1);
                }
                else
                {
                    var nameText = rowObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    var scoreText = rowObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
                    var avatarImg = rowObj.transform.Find("AvatarImage")?.GetComponent<Image>();

                    if (nameText != null) nameText.text = $"#{i + 1} {player.username}";
                    if (scoreText != null) scoreText.text = player.score.ToString("N0");
                    if (avatarImg != null && player.avatarSprite != null) avatarImg.sprite = player.avatarSprite;
                }
            }

            RecalculateHeight();
        }

        private void RecalculateHeight()
        {
            Canvas.ForceUpdateCanvases();
            if (expandableContentPanel != null && isExpanded)
            {
                fullPanelHeight = itemsContainer != null ? itemsContainer.rect.height : expandableContentPanel.rect.height;
            }
        }

        private void ClearRows()
        {
            foreach (var row in spawnedRowObjects)
            {
                if (row != null) Destroy(row);
            }
            spawnedRowObjects.Clear();
            
            // Clear any dummy UI objects in hierarchy
            if (itemsContainer != null)
            {
                foreach (Transform child in itemsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Toggles dropdown open / closed state
        /// </summary>
        public void ToggleDropdown()
        {
            isExpanded = !isExpanded;
            AnimateDropdown(isExpanded);
        }

        private void AnimateDropdown(bool open)
        {
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlayTap();
            }

            if (arrowIconTransform != null)
            {
                arrowIconTransform.DOKill();
                arrowIconTransform.DORotate(new Vector3(0, 0, open ? 180f : 0f), animationDuration)
                    .SetEase(animationEase)
                    .SetLink(arrowIconTransform.gameObject);
            }

            if (expandableContentPanel != null)
            {
                expandableContentPanel.DOKill();
                RecalculateHeight();
                float targetHeight = open ? Mathf.Max(fullPanelHeight, 150f) : 0f;
                expandableContentPanel.DOSizeDelta(new Vector2(expandableContentPanel.sizeDelta.x, targetHeight), animationDuration)
                    .SetEase(animationEase)
                    .SetLink(expandableContentPanel.gameObject);
                    
                if (backgroundPanel != null)
                {
                    backgroundPanel.DOKill();
                    float bgTargetHeight = open ? (targetHeight + backgroundCollapsedHeight) : backgroundCollapsedHeight;
                    backgroundPanel.DOSizeDelta(new Vector2(backgroundPanel.sizeDelta.x, bgTargetHeight), animationDuration)
                        .SetEase(animationEase)
                        .SetLink(backgroundPanel.gameObject);
                }
            }

            UpdateToggleVisuals(open);
        }

        private void UpdateToggleVisuals(bool open)
        {
            if (toggleButtonText != null)
            {
                toggleButtonText.text = open ? expandedButtonText : collapsedButtonText;
            }
        }
    }
}
