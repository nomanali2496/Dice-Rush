using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace UI
{
    [System.Serializable]
    public class ProfileItemData
    {
        public string username;
        public int score;
        public Sprite avatarSprite;

        public ProfileItemData(string name, int scoreValue, Sprite avatar = null)
        {
            username = name;
            score = scoreValue;
            avatarSprite = avatar;
        }
    }

    /// <summary>
    /// Controller for the Profile Dropdown UI component (Dice Rush / Dice Blitz UI)
    /// Manages expandable profile list, backend data loading, smooth DOTween collapse/expand, and arrow rotation.
    /// </summary>
    public class ProfileDropdownController : MonoBehaviour
    {
        [Header("UI Containers & References")]
        [Tooltip("Parent RectTransform containing profile row instances")]
        [SerializeField] private RectTransform itemsContainer;

        [Tooltip("Prefab for single profile row (with ProfileDropdownRowUI script or child text components)")]
        [SerializeField] private GameObject profileRowPrefab;

        [Tooltip("Expandable panel transform (for animated height collapse/expand)")]
        [SerializeField] private RectTransform expandableContentPanel;

        [Header("Dropdown Toggle Controls")]
        [Tooltip("Button to expand or collapse the dropdown")]
        [SerializeField] private Button toggleDropdownButton;

        [Tooltip("Text component inside toggle button (e.g. View All Profiles)")]
        [SerializeField] private TextMeshProUGUI toggleButtonText;

        [Tooltip("Arrow icon transform to rotate when opened/closed")]
        [SerializeField] private Transform arrowIconTransform;

        [Header("Labels & Text Settings")]
        [SerializeField] private string collapsedButtonText = "View All Profiles";
        [SerializeField] private string expandedButtonText = "Hide Profiles";

        [Header("Backend Data Integration")]
        [Tooltip("Fetch top profiles automatically from BackendAuthService on Start")]
        [SerializeField] private bool fetchFromBackendOnStart = true;
        [SerializeField] private int maxProfilesToFetch = 10;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        [Header("Sample Data (Used if Offline / Testing)")]
        [SerializeField] private List<ProfileItemData> sampleProfiles = new List<ProfileItemData>()
        {
            new ProfileItemData("Omega Shenron", 1000),
            new ProfileItemData("Muhammad Usama", 700),
            new ProfileItemData("Noman Ali", 900)
        };

        private bool isExpanded = false;
        private float fullPanelHeight;
        private List<GameObject> spawnedRowObjects = new List<GameObject>();

        private void Awake()
        {
            if (toggleDropdownButton != null)
            {
                toggleDropdownButton.onClick.AddListener(ToggleDropdown);
            }
        }

        private void Start()
        {
            if (fetchFromBackendOnStart && BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
            {
                FetchBackendProfiles();
            }
            else
            {
                // Fallback to sample data if offline
                if (sampleProfiles != null && sampleProfiles.Count > 0)
                {
                    SetProfiles(sampleProfiles);
                }
            }

            RecalculateHeight();
            UpdateToggleVisuals(false);
        }

        /// <summary>
        /// Fetches top profiles from BackendAuthService leaderboard
        /// </summary>
        public void FetchBackendProfiles()
        {
            if (BackendAuthService.Instance == null) return;

            BackendAuthService.Instance.GetLeaderboard(maxProfilesToFetch, (success, response) =>
            {
                if (success && response != null && response.leaderboard != null && response.leaderboard.Count > 0)
                {
                    List<ProfileItemData> backendData = new List<ProfileItemData>();
                    foreach (var item in response.leaderboard)
                    {
                        backendData.Add(new ProfileItemData(item.name, item.wins));
                    }
                    SetProfiles(backendData);
                }
                else
                {
                    // Fallback if backend fetch fails
                    if (sampleProfiles != null && sampleProfiles.Count > 0)
                    {
                        SetProfiles(sampleProfiles);
                    }
                }
            });
        }

        /// <summary>
        /// Dynamically populates the dropdown with a list of profiles
        /// </summary>
        public void SetProfiles(List<ProfileItemData> profiles)
        {
            ClearRows();

            if (profileRowPrefab == null || itemsContainer == null)
            {
                Debug.LogWarning("[ProfileDropdownController] Prefab or ItemsContainer is missing!");
                return;
            }

            foreach (var profile in profiles)
            {
                GameObject rowObj = Instantiate(profileRowPrefab, itemsContainer);
                spawnedRowObjects.Add(rowObj);

                // Try binding via ProfileDropdownRowUI script
                ProfileDropdownRowUI rowUI = rowObj.GetComponent<ProfileDropdownRowUI>();
                if (rowUI != null)
                {
                    rowUI.SetData(profile.username, profile.score, profile.avatarSprite);
                }
                else
                {
                    // Fallback to transform lookup
                    var avatarImg = rowObj.transform.Find("AvatarImage")?.GetComponent<Image>();
                    var nameText = rowObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    var scoreText = rowObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

                    if (nameText != null) nameText.text = profile.username;
                    if (scoreText != null) scoreText.text = profile.score.ToString("N0");
                    if (avatarImg != null && profile.avatarSprite != null)
                    {
                        avatarImg.sprite = profile.avatarSprite;
                    }
                }
            }

            RecalculateHeight();
        }

        private void RecalculateHeight()
        {
            Canvas.ForceUpdateCanvases();
            if (expandableContentPanel != null)
            {
                fullPanelHeight = expandableContentPanel.rect.height;
            }
        }

        /// <summary>
        /// Clears spawned row items
        /// </summary>
        private void ClearRows()
        {
            foreach (var row in spawnedRowObjects)
            {
                if (row != null) Destroy(row);
            }
            spawnedRowObjects.Clear();
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
            // Sound feedback
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlayTap();
            }

            // Rotate Arrow Icon (0 deg -> collapsed, 180 deg -> expanded)
            if (arrowIconTransform != null)
            {
                arrowIconTransform.DOKill();
                arrowIconTransform.DORotate(new Vector3(0, 0, open ? 180f : 0f), animationDuration)
                    .SetEase(animationEase)
                    .SetLink(arrowIconTransform.gameObject);
            }

            // Expand/Collapse Panel height
            if (expandableContentPanel != null)
            {
                expandableContentPanel.DOKill();
                float targetHeight = open ? fullPanelHeight : 0f;
                expandableContentPanel.DOSizeDelta(new Vector2(expandableContentPanel.sizeDelta.x, targetHeight), animationDuration)
                    .SetEase(animationEase)
                    .SetLink(expandableContentPanel.gameObject);
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
