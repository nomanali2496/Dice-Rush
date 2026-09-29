using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays selected avatar in main menu.
/// PURE FRONTEND: No networking code, no UnityWebRequest.
/// Uses BackendAuthService.Instance.GetImage to fetch external URLs.
/// </summary>
public class MainMenuAvatarDisplay : MonoBehaviour
{
    [Header("Avatar Display")]
    [SerializeField] private Image avatarImageSprite;
    [SerializeField] private Image avatarImageCustom;
    [SerializeField] private Image frameImageSprite;
    
    [Header("Settings")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    [SerializeField] private int maxImageSize = 512;
    
    [Header("Custom Upload (Optional)")]
    [SerializeField] private Button selectCustomImageButton;
    
    [Header("Profile Details (Optional)")]
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    
    private string lastAvatarData = "";

    private void Start()
    {
        if (selectCustomImageButton != null)
            selectCustomImageButton.onClick.AddListener(OnSelectCustomImageClicked);
            
        PlayerModel.OnDataUpdated += OnPlayerModelUpdated;
        Refresh();
    }
    
    private void OnEnable()
    {
        if (autoRefreshOnEnable) Refresh();
    }
    
    private void OnDestroy() => PlayerModel.OnDataUpdated -= OnPlayerModelUpdated;

    private void OnPlayerModelUpdated()
    {
        Refresh(false);
    }

    /// <summary>
    /// Update display based on PlayerModel data.
    /// Purely asks BackendAuthService for URL images to avoid mixing logic.
    /// </summary>
    public void Refresh()
    {
        Refresh(false);
    }

    public void Refresh(bool force)
    {
        string avatarData = PlayerModel.Avatar;

        // Refresh profile details (like name) regardless of avatar change
        RefreshProfileDetails();

        // Skip redundant avatar updates unless forced
        if (!force && avatarData == lastAvatarData && GetCurrentAvatarSprite() != null) return;
        lastAvatarData = avatarData;

        if (string.IsNullOrEmpty(avatarData))
        {
            ShowPreset(0);
        }
        else if (avatarData.StartsWith("http"))
        {
            // PURE UI: Ask service to download the image. UI doesn't handle the request.
            BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ShowCustom(sprite);
                }
            }, force);
        }
        else if (int.TryParse(avatarData, out int index))
        {
            ShowPreset(index);
        }
        else
        {
            ShowPreset(0);
        }

        RefreshFrame();
    }

    private void RefreshProfileDetails()
    {
        if (playerNameInputField != null)
        {
            playerNameInputField.text = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "Guest";
            playerNameInputField.interactable = false;
        }

        if (levelText != null)
        {
            levelText.text = PlayerModel.Level > 0 ? PlayerModel.Level.ToString() : "1";
        }

        if (xpSlider != null)
        {
            // Simple XP logic: 1000 XP per level, remainder goes to slider
            // Adjust maxValue/value logic if the backend gives a specific format
            int xpRequirement = 1000;
            xpSlider.maxValue = xpRequirement;
            xpSlider.value = PlayerModel.Xp % xpRequirement;
        }
    }

    private void RefreshFrame()
    {
        if (frameImageSprite != null && AvatarSelector.Instance != null)
        {
            int frameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
                ? parsedFrame
                : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
                
            Sprite frameSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex);
            if (frameSprite != null)
            {
                frameImageSprite.sprite = frameSprite;
                frameImageSprite.gameObject.SetActive(true);
            }
            else
            {
                frameImageSprite.gameObject.SetActive(false);
            }
        }
    }

    private void ShowPreset(int index)
    {
        Sprite sprite = AvatarSelector.Instance != null ? AvatarSelector.Instance.GetAvatarSpriteByIndex(index) : null;
        if (avatarImageSprite != null)
        {
            avatarImageSprite.sprite = sprite;
            avatarImageSprite.rectTransform.localScale = Vector3.one;
            avatarImageSprite.gameObject.SetActive(true);
        }
        if (avatarImageCustom != null) avatarImageCustom.gameObject.SetActive(false);
        
        NotifyLeaderboard();
    }

    private void ShowCustom(Sprite sprite)
    {
        if (avatarImageCustom != null)
        {
            avatarImageCustom.sprite = sprite;
            avatarImageCustom.rectTransform.localScale = Vector3.one * UIConstants.CustomAvatarScale;
            avatarImageCustom.gameObject.SetActive(true);
        }
        if (avatarImageSprite != null) avatarImageSprite.gameObject.SetActive(false);
        
        NotifyLeaderboard();
    }



    public Sprite GetCurrentAvatarSprite()
    {
        if (avatarImageCustom != null && avatarImageCustom.gameObject.activeSelf) return avatarImageCustom.sprite;
        if (avatarImageSprite != null) return avatarImageSprite.sprite;
        return null;
    }

    public bool IsShowingCustomAvatar()
    {
        return avatarImageCustom != null && avatarImageCustom.gameObject.activeSelf;
    }

    private void NotifyLeaderboard()
    {
        LeaderboardManager[] managers = FindObjectsByType<LeaderboardManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var lbm in managers)
        {
            if (lbm != null) lbm.SyncAvatarFromMainMenu();
        }
    }

    private void OnSelectCustomImageClicked()
    {
        ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
    }

    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null) return;
        
        byte[] imageBytes = texture.EncodeToJPG(75);
        BackendAuthService.Instance.UploadAvatar(imageBytes, (success, result) => {
            if (!success) Debug.LogError("[MainMenuAvatarDisplay] Upload failed: " + result);
            else Refresh(true);
        });
    }
}
