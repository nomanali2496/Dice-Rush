using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfilePanelUI : MonoBehaviour
{
    public static ProfilePanelUI Instance { get; private set; }
    [Header("Profile Card Info")]
    [SerializeField] private Image profileIcon;
    [SerializeField] private Image frameIcon; // Added for frame
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private TMP_Text playerLikesText;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button copyIdButton;
    [SerializeField] private Button addFriendButton;
    
    [Header("Self View Edit Elements")]
    [Tooltip("Elements like pencil icons that should only show for self")]
    [SerializeField] private GameObject[] editElements;
    [SerializeField] private Button editProfileIconButton;
    [SerializeField] private Button editPlayerNameButton;
    [SerializeField] private GameObject editProfilePanel;

    [Header("Player Stats (Text Manager)")]
    [SerializeField] private TMP_Text totalWinsText;
    [SerializeField] private TMP_Text currentChipsText;
    [SerializeField] private TMP_Text gamesPlayedText;
    [SerializeField] private TMP_Text winStreakText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text leagueText;
    [SerializeField] private TMP_Text countryText;
    [SerializeField] private TMP_Text loginStreakText;

    private string currentPlayerId = "";
    private bool isSelfView = true;
    private bool isNameEditMode = false;
    private string originalPlayerName = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);
        if (copyIdButton != null) copyIdButton.onClick.AddListener(OnCopyIdClicked);
        if (addFriendButton != null) addFriendButton.onClick.AddListener(OnAddFriendClicked);
        if (editProfileIconButton != null) editProfileIconButton.onClick.AddListener(OnEditProfileIconClicked);
        if (editPlayerNameButton != null) editPlayerNameButton.onClick.AddListener(OnEditPlayerNameClicked);

        PlayerModel.OnDataUpdated += OnPlayerDataUpdated;
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackButtonClicked);
        if (copyIdButton != null) copyIdButton.onClick.RemoveListener(OnCopyIdClicked);
        if (addFriendButton != null) addFriendButton.onClick.RemoveListener(OnAddFriendClicked);
        if (editProfileIconButton != null) editProfileIconButton.onClick.RemoveListener(OnEditProfileIconClicked);
        if (editPlayerNameButton != null) editPlayerNameButton.onClick.RemoveListener(OnEditPlayerNameClicked);

        PlayerModel.OnDataUpdated -= OnPlayerDataUpdated;
    }

    private void OnPlayerDataUpdated()
    {
        if (isSelfView && gameObject.activeInHierarchy)
        {
            SetupForSelf();
        }
    }

    /// <summary>
    /// Setup the profile panel for the local player (Self View)
    /// </summary>
    public void SetupForSelf()
    {
        isSelfView = true;
        ToggleEditElements(true);
        if (addFriendButton != null) addFriendButton.gameObject.SetActive(false);

        currentPlayerId = PlayerModel.Id;
        
        if (playerNameInputField != null) 
        {
            playerNameInputField.text = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "Guest";
            playerNameInputField.interactable = false;
        }
        if (playerIdText != null) playerIdText.text = currentPlayerId;
        
        if (totalWinsText != null) totalWinsText.text = PlayerModel.Wins.ToString();
        if (currentChipsText != null) currentChipsText.text = PlayerModel.Balance.ToString();
        if (gamesPlayedText != null) gamesPlayedText.text = (PlayerModel.Wins + PlayerModel.Losses).ToString();
        
        // Mock data for unimplemented fields
        if (playerLikesText != null) playerLikesText.text = "1000";
        if (winStreakText != null) winStreakText.text = "0";
        if (totalScoreText != null) totalScoreText.text = "1,000";
        if (rankText != null) rankText.text = "#1000";
        if (leagueText != null) leagueText.text = "Gold League";
        if (countryText != null) countryText.text = "Pakistan";
        if (loginStreakText != null) loginStreakText.text = "1";

        // Load Avatar
        UpdateAvatarDisplay(PlayerModel.Avatar);

        // Load Frame
        if (frameIcon != null)
        {
            int savedFrameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
                ? parsedFrame
                : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
            if (AvatarSelector.Instance != null)
            {
                Sprite frameSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(savedFrameIndex);
                if (frameSprite != null)
                {
                    frameIcon.sprite = frameSprite;
                    frameIcon.gameObject.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Open the profile panel and fetch data for another player.
    /// </summary>
    public void OpenPublicProfile(string userId)
    {
        gameObject.SetActive(true);
        isSelfView = false;
        ToggleEditElements(false);
        if (addFriendButton != null) 
        {
            if (userId == PlayerModel.Id)
            {
                addFriendButton.gameObject.SetActive(false);
            }
            else
            {
                addFriendButton.gameObject.SetActive(true);
                addFriendButton.interactable = false; // Disable until we know if it's already liked
            }
        }
        
        currentPlayerId = userId;
        
        // Show loading state if applicable (can reset text to "...")
        if (playerNameInputField != null) playerNameInputField.text = "Loading...";
        
        BackendAuthService.Instance.GetPublicProfile(userId, (success, response) => 
        {
            if (success && response != null && response.data != null)
            {
                var data = response.data;
                if (playerNameInputField != null) 
                {
                    playerNameInputField.text = data.name;
                    playerNameInputField.interactable = false;
                }
                if (playerIdText != null) playerIdText.text = data.id;
                
                if (totalWinsText != null) totalWinsText.text = data.wins.ToString();
                // Since this is coin-based, chips map to gamesPlayed or similar, but wait, data doesn't have chips in public profile. 
                // We will leave currentChipsText untouched or hide it.
                if (gamesPlayedText != null) gamesPlayedText.text = data.gamesPlayed.ToString();
                
                if (playerLikesText != null) playerLikesText.text = data.likesReceived.ToString();
                if (winStreakText != null) winStreakText.text = data.winStreak.ToString();
                if (totalScoreText != null) totalScoreText.text = data.xp.ToString(); // using xp as totalScore for now
                if (rankText != null) rankText.text = "#-"; // Rank requires leaderboard context, keeping placeholder
                if (leagueText != null) leagueText.text = data.league;
                if (countryText != null) countryText.text = data.country;
                if (loginStreakText != null) loginStreakText.text = "-"; 
                
                if (addFriendButton != null)
                {
                    addFriendButton.interactable = !data.alreadyLiked;
                }

                UpdateAvatarDisplay(data.avatar);
                
                // Update frame
                if (frameIcon != null && AvatarSelector.Instance != null)
                {
                    int frameIndex = int.TryParse(data.profileFrame, out int parsedFrame) ? parsedFrame : 0;
                    Sprite frameSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex);
                    if (frameSprite != null)
                    {
                        frameIcon.sprite = frameSprite;
                        frameIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        frameIcon.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (playerNameInputField != null) playerNameInputField.text = "Error loading";
            }
        });
    }

    private void ToggleEditElements(bool state)
    {
        if (editElements == null) return;
        foreach (var element in editElements)
        {
            if (element != null) element.SetActive(state);
        }
    }

    private void UpdateAvatarDisplay(string avatarData)
    {
        if (profileIcon == null) return;

        if (string.IsNullOrEmpty(avatarData))
        {
            ShowPreset(0);
        }
        else if (avatarData.StartsWith("http"))
        {
            if (BackendAuthService.Instance != null)
            {
                BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                    if (texture != null && profileIcon != null)
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        profileIcon.sprite = sprite;
                    }
                });
            }
        }
        else if (int.TryParse(avatarData, out int index))
        {
            ShowPreset(index);
        }
        else
        {
            ShowPreset(0);
        }
    }

    private void ShowPreset(int index)
    {
        if (AvatarSelector.Instance != null)
        {
            Sprite sprite = AvatarSelector.Instance.GetAvatarSpriteByIndex(index);
            if (sprite != null && profileIcon != null) profileIcon.sprite = sprite;
        }
    }

    private void OnCopyIdClicked()
    {
        if (!string.IsNullOrEmpty(currentPlayerId))
        {
            GUIUtility.systemCopyBuffer = currentPlayerId;
            Debug.Log($"Copied Player ID: {currentPlayerId} to clipboard.");
        }
    }

    private void OnAddFriendClicked()
    {
        if (string.IsNullOrEmpty(currentPlayerId) || isSelfView) return;
        
        if (addFriendButton != null) addFriendButton.interactable = false;
        
        BackendAuthService.Instance.LikeProfile(currentPlayerId, (success, response) => 
        {
            if (success && response != null)
            {
                if (playerLikesText != null) playerLikesText.text = response.likesReceived.ToString();
                if (addFriendButton != null) addFriendButton.interactable = !response.alreadyLiked;
            }
            else
            {
                if (addFriendButton != null) addFriendButton.interactable = true; // Re-enable on failure
                Debug.LogWarning("Failed to like profile");
            }
        });
    }

    private void OnBackButtonClicked()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (isSelfView)
        {
            SetupForSelf();
        }
    }

    private void OnEditProfileIconClicked()
    {
        if (editProfilePanel != null)
        {
            editProfilePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("EditProfilePanel reference is missing in ProfilePanelUI!");
        }
    }

    private void OnEditPlayerNameClicked()
    {
        if (playerNameInputField == null) return;
        
        if (!isNameEditMode)
        {
            isNameEditMode = true;
            originalPlayerName = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "Guest";
            playerNameInputField.interactable = true;
            playerNameInputField.ActivateInputField();
        }
        else
        {
            string newName = playerNameInputField.text.Trim();
            if (!string.IsNullOrEmpty(newName) && newName != originalPlayerName && newName.Length >= 3)
            {
                if (BackendAuthService.Instance != null)
                    BackendAuthService.Instance.UpdateProfile(newName);
            }
            else
            {
                Debug.LogWarning($"[ProfilePanelUI] Name edit ignored or invalid. New: '{newName}', Old: '{originalPlayerName}', Length: {newName.Length}");
                playerNameInputField.text = originalPlayerName; // Revert if invalid
            }
            
            isNameEditMode = false;
            playerNameInputField.interactable = false;
        }
    }
}

/// <summary>
/// Simple data structure for other player's profile data
/// </summary>
public class OtherProfileData
{
    public string id;
    public string name;
    public string avatarUrl;
    public int wins;
    public int chips;
    public int gamesPlayed;
    public int likes;
    public int winStreak;
    public int totalScore;
    public int rank;
    public string league;
    public string country;
    public int loginStreak;
}
