using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    [System.Serializable]
    public class ProfileItemData
    {
        public string userId;
        public string username;
        public int score;
        public Sprite avatarSprite;

        public ProfileItemData(string id, string name, int scoreValue, Sprite avatar = null)
        {
            userId = id;
            username = name;
            score = scoreValue;
            avatarSprite = avatar;
        }
    }

    /// <summary>
    /// Controller for the Profile Dropdown UI component (Dice Blitz UI)
    /// Manages expandable profile list, backend data loading, smooth DOTween collapse/expand, and arrow rotation.
    /// </summary>
    public class ProfileDropdownController : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Containers & References")]
        [Tooltip("Parent RectTransform containing profile row instances")]
        [SerializeField] private RectTransform itemsContainer;

        [Tooltip("Prefab for single profile row (with ProfileDropdownRowUI script or child text components)")]
        [SerializeField] private GameObject profileRowPrefab;

        [Tooltip("Expandable panel transform (for animated height collapse/expand)")]
        [SerializeField] private RectTransform expandableContentPanel;

        [Tooltip("Background panel transform to resize alongside content")]
        [SerializeField] private RectTransform backgroundPanel;

        [Tooltip("The minimum (collapsed) height of the background panel")]
        [SerializeField] private float backgroundCollapsedHeight = 80f;

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

        [Header("Dropdown State Settings")]
        [Tooltip("Minimum number of profiles to show when dropdown is collapsed")]
        [SerializeField] private int minProfilesWhenCollapsed = 3;

        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        [Header("Sample Data (Used if Offline / Testing)")]
        [SerializeField] private List<ProfileItemData> sampleProfiles = new List<ProfileItemData>()
        {
            new ProfileItemData("", "Omega Shenron", 1000),
            new ProfileItemData("", "Muhammad Usama", 700),
            new ProfileItemData("", "Noman Ali", 900)
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

        public void OnPointerClick(PointerEventData eventData)
        {
            // Jab aap puray object pe touch/click karenge tab bhi toggle hoga
            ToggleDropdown();
        }

        private void Start()
        {
            // Auto-setup Layout Group to fix any overlapping issues from the inspector
            if (itemsContainer != null)
            {
                var vlg = itemsContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vlg == null) vlg = itemsContainer.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                
                vlg.childControlHeight = false;
                vlg.childControlWidth = true; // Allow width control if needed, but height must be false to keep prefab height
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.spacing = 15f;

                var csf = itemsContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (csf == null) csf = itemsContainer.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                
                // CRITICAL: Vertical fit must be PreferredSize for the dropdown to size correctly
                csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            }

            RecalculateHeight();
            
            // Set initial state
            if (expandableContentPanel != null && expandableContentPanel != itemsContainer)
            {
                expandableContentPanel.sizeDelta = new Vector2(expandableContentPanel.sizeDelta.x, GetCollapsedHeight());
            }
            if (backgroundPanel != null)
            {
                backgroundPanel.sizeDelta = new Vector2(backgroundPanel.sizeDelta.x, backgroundCollapsedHeight + GetCollapsedHeight());
            }
            
            UpdateToggleVisuals(false);
        }

        private void OnEnable()
        {
            if (fetchFromBackendOnStart && BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
            {
                FetchBackendProfiles();
            }
            else
            {
                // Fallback to sample data if offline
                SetProfiles(GetDynamicSampleProfiles());
            }
        }

        private int GetTargetProfileCount()
        {
            int target = maxProfilesToFetch;

            // 1. If actively playing, get exact player count from the current match
            if (GameManager.Instance != null && GameManager.Instance.IsActivelyPlaying)
            {
                var matchData = GameManager.Instance.GetCurrentMatchData();
                if (matchData != null && matchData.playerAvatars != null)
                {
                    // playerAvatars contains all players in the game (including local)
                    return matchData.playerAvatars.Count;
                }
            }

            // 2. Otherwise try to get selected mode player count from Lobby
            var lobbyManager = FindFirstObjectByType<HorizontalLobbyManager>(FindObjectsInactive.Include);
            if (lobbyManager != null && lobbyManager.SelectedPlayerCount > 0)
            {
                target = lobbyManager.SelectedPlayerCount;
            }
            return target;
        }

        private List<ProfileItemData> GetDynamicSampleProfiles()
        {
            int target = GetTargetProfileCount();
            List<ProfileItemData> dynamicList = new List<ProfileItemData>();
            
            for (int i = 0; i < target; i++)
            {
                if (sampleProfiles != null && sampleProfiles.Count > 0)
                {
                    dynamicList.Add(sampleProfiles[i % sampleProfiles.Count]);
                }
                else
                {
                    dynamicList.Add(new ProfileItemData("", "Player " + (i + 1), Random.Range(100, 1000)));
                }
            }
            return dynamicList;
        }

        /// <summary>
        /// Fetches top profiles from BackendAuthService leaderboard
        /// </summary>
        public void FetchBackendProfiles()
        {
            if (BackendAuthService.Instance == null) return;

            int fetchCount = GetTargetProfileCount();

            BackendAuthService.Instance.GetLeaderboard(fetchCount, "all_time", "earnings", (success, response) =>
            {
                if (success && response != null && response.leaderboard != null && response.leaderboard.Count > 0)
                {
                    List<ProfileItemData> backendData = new List<ProfileItemData>();
                    for (int i = 0; i < fetchCount; i++)
                    {
                        if (i < response.leaderboard.Count)
                        {
                            var item = response.leaderboard[i];
                            backendData.Add(new ProfileItemData(item.userId, item.name, item.wins));
                        }
                        else
                        {
                            // If backend returned less than requested, fill with sample or dummy data
                            if (sampleProfiles != null && sampleProfiles.Count > 0)
                            {
                                backendData.Add(sampleProfiles[i % sampleProfiles.Count]);
                            }
                            else
                            {
                                backendData.Add(new ProfileItemData("dummy_id", "Player " + (i + 1), Random.Range(100, 1000)));
                            }
                        }
                    }
                    SetProfiles(backendData);
                }
                else
                {
                    // Fallback if backend fetch fails
                    SetProfiles(GetDynamicSampleProfiles());
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
                GameObject rowObj = Instantiate(profileRowPrefab, itemsContainer, false);
                spawnedRowObjects.Add(rowObj);

                Button rowButton = rowObj.GetComponent<Button>();
                if (rowButton == null) rowButton = rowObj.AddComponent<Button>();
                rowButton.onClick.AddListener(() => {
                    if (ProfilePanelUI.Instance != null && !string.IsNullOrEmpty(profile.userId))
                    {
                        ProfilePanelUI.Instance.OpenPublicProfile(profile.userId);
                    }
                });

                // Try binding via ProfileDropdownRowUI script
                ProfileDropdownRowUI rowUI = rowObj.GetComponent<ProfileDropdownRowUI>();
                if (rowUI != null)
                {
                    rowUI.SetData(profile.username, profile.score, profile.avatarSprite);
                }
                else
                {
                    // Smart Fallback: Find components even if they are not named correctly
                    var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
                    var images = rowObj.GetComponentsInChildren<Image>();

                    TextMeshProUGUI nameText = texts.Length > 0 ? texts[0] : null;
                    TextMeshProUGUI scoreText = texts.Length > 1 ? texts[1] : null;
                    
                    // The first image is usually the background, so we try to get the second one for avatar
                    Image avatarImg = images.Length > 1 ? images[1] : (images.Length > 0 ? images[0] : null);

                    if (nameText != null) nameText.text = profile.username;
                    if (scoreText != null) scoreText.text = profile.score.ToString("N0");
                    if (avatarImg != null && profile.avatarSprite != null)
                    {
                        avatarImg.sprite = profile.avatarSprite;
                        
                        // Default to -1 (Current player's frame) for now, unless the backend sends other players' frames
                        int frameIndex = -1;
                        if (frameIndex == -1)
                        {
                            frameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
                                ? parsedFrame
                                : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
                        }

                        // Try to find explicitly named Frame Image in prefab
                        Image frameImg = null;
                        foreach(var img in images) {
                            if (img.gameObject.name.Contains("Frame")) {
                                frameImg = img;
                                break;
                            }
                        }

                        if (frameImg != null && AvatarSelector.Instance != null)
                        {
                            Sprite fSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex);
                            if (fSprite != null)
                            {
                                frameImg.sprite = fSprite;
                                frameImg.gameObject.SetActive(true);
                            }
                            else
                            {
                                frameImg.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            // Fallback to overlay if no explicit frame object
                            UIProfileHelper.ApplyFrameToImage(avatarImg, frameIndex);
                        }
                    }
                }
            }

            RecalculateHeight();
        }

        private void RecalculateHeight()
        {
            if (itemsContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(itemsContainer);
            }
            Canvas.ForceUpdateCanvases();
            
            if (expandableContentPanel != null)
            {
                fullPanelHeight = itemsContainer != null ? itemsContainer.rect.height : expandableContentPanel.rect.height;
                
                if (isExpanded)
                {
                    float targetHeight = Mathf.Max(fullPanelHeight, GetCollapsedHeight());
                    if (expandableContentPanel != itemsContainer)
                    {
                        expandableContentPanel.sizeDelta = new Vector2(expandableContentPanel.sizeDelta.x, targetHeight);
                    }
                    
                    if (backgroundPanel != null)
                    {
                        float bgTargetHeight = targetHeight + backgroundCollapsedHeight;
                        backgroundPanel.sizeDelta = new Vector2(backgroundPanel.sizeDelta.x, bgTargetHeight);
                    }
                }
            }
            else if (itemsContainer != null)
            {
                fullPanelHeight = itemsContainer.rect.height;
            }
        }

        private float GetCollapsedHeight()
        {
            if (itemsContainer == null || itemsContainer.childCount == 0) return 0f;
            int count = Mathf.Min(minProfilesWhenCollapsed, itemsContainer.childCount);
            
            float height = 0f;
            var layoutGroup = itemsContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;
            float paddingTop = layoutGroup != null ? layoutGroup.padding.top : 0f;
            float paddingBottom = layoutGroup != null ? layoutGroup.padding.bottom : 0f;

            height += paddingTop;
            for(int i = 0; i < count; i++)
            {
                RectTransform rt = itemsContainer.GetChild(i) as RectTransform;
                if(rt != null) height += rt.rect.height;
                if(i < count - 1) height += spacing;
            }
            height += paddingBottom;
            return height;
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
            RecalculateHeight();
            float collapsedHeight = GetCollapsedHeight();
            float targetHeight = open ? Mathf.Max(fullPanelHeight, collapsedHeight) : collapsedHeight;
            
            if (expandableContentPanel != null && expandableContentPanel != itemsContainer)
            {
                expandableContentPanel.DOKill();
                expandableContentPanel.DOSizeDelta(new Vector2(expandableContentPanel.sizeDelta.x, targetHeight), animationDuration)
                    .SetEase(animationEase)
                    .SetLink(expandableContentPanel.gameObject);
            }

            if (backgroundPanel != null)
            {
                backgroundPanel.DOKill();
                float bgTargetHeight = open ? (targetHeight + backgroundCollapsedHeight) : (collapsedHeight + backgroundCollapsedHeight);
                backgroundPanel.DOSizeDelta(new Vector2(backgroundPanel.sizeDelta.x, bgTargetHeight), animationDuration)
                    .SetEase(animationEase)
                    .SetLink(backgroundPanel.gameObject);
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
