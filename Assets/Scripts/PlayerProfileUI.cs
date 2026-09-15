using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays and manages player profile information.
/// PURE FRONTEND: Handles button clicks and UI text/image assignments only.
/// All API calls and image downloads are delegated to BackendAuthService.
/// </summary>
public class PlayerProfileUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TextMeshProUGUI emailText;
    [SerializeField] private TextMeshProUGUI helpAndSupportEmailText;
    [SerializeField] private Image profileImageSprite;
    [SerializeField] private Image profileImageCustom;
    [SerializeField] private Image profileBackgroundDisplay;
    [SerializeField] private Image profileFrameDisplay;
    
    [Header("Buttons")]
    [SerializeField] private Button editNameButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button selectCustomImageButton;
    
    [Header("Edit Mode Settings")]
    [SerializeField] private string editButtonText = "Edit";
    [SerializeField] private string saveButtonText = "Save";
    [SerializeField] private int maxImageSize = 512;
    [SerializeField] private GameObject AvatarDisplay;
    
    [Header("Logout Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject splashScreenPanel;
    
    private bool isEditMode = false;
    private string originalPlayerName = "";

    private void Start()
    {
        if (editNameButton != null) editNameButton.onClick.AddListener(OnEditNameClicked);
        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutClicked);
        if (selectCustomImageButton != null) selectCustomImageButton.onClick.AddListener(OnSelectCustomImageClicked);
        
        SetEditMode(false);
        PlayerModel.OnDataUpdated += RefreshUI;
        RefreshUI();
    }
    
    private void OnEnable() => RefreshUI();
    private void OnDestroy() => PlayerModel.OnDataUpdated -= RefreshUI;

    /// <summary>
    /// Update Profile UI based on PlayerModel state.
    /// Purely delegates networking to BackendAuthService.
    /// </summary>
    public void RefreshUI()
    {
        if (string.IsNullOrEmpty(PlayerModel.Id)) 
        {
            UpdateUIWithDefaultData();
            return; 
        }

        if (!isEditMode && playerNameInputField != null)
            playerNameInputField.text = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "Guest";

        if (emailText != null)
        {
            emailText.text = !string.IsNullOrEmpty(PlayerModel.Email) ? PlayerModel.Email : "No email";
            if (helpAndSupportEmailText != null) helpAndSupportEmailText.text = emailText.text;
        }

        UpdateAvatarDisplay(PlayerModel.Avatar);
        UpdateFrameDisplay();
        if (AvatarDisplay != null) AvatarDisplay.SetActive(true);
    }

    private void UpdateAvatarDisplay(string avatarData)
    {
        if (string.IsNullOrEmpty(avatarData))
        {
            ShowPreset(0);
        }
        else if (avatarData.StartsWith("http"))
        {
            if (profileImageSprite != null) profileImageSprite.gameObject.SetActive(false);
            
            // PURE UI: Delegate download to service
            BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ShowCustom(sprite);
                }
            });
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

    private void UpdateFrameDisplay()
    {
        if (profileFrameDisplay == null || AvatarSelector.Instance == null) return;

        int frameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
            ? parsedFrame
            : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
        Sprite frameSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex);
        if (frameSprite != null)
        {
            profileFrameDisplay.sprite = frameSprite;
            profileFrameDisplay.gameObject.SetActive(true);
        }
    }

    private void ShowPreset(int index)
    {
        if (index < 0 || index >= 8) index = 0;
        
        Sprite sprite = AvatarSelector.Instance != null ? AvatarSelector.Instance.GetAvatarSpriteByIndex(index) : null;

        if (profileImageSprite != null)
        {
            profileImageSprite.sprite = sprite;
            // Scale override removed to respect Editor settings
            profileImageSprite.gameObject.SetActive(true);
        }
        if (profileImageCustom != null) profileImageCustom.gameObject.SetActive(false);
        
        if (profileBackgroundDisplay != null)
        {
            Sprite bgSprite = AvatarSelector.Instance != null ? AvatarSelector.Instance.GetBackgroundSpriteByIndex(index) : null;
            profileBackgroundDisplay.sprite = bgSprite;
        }
    }

    private void ShowCustom(Sprite sprite)
    {
        if (profileImageCustom != null)
        {
            profileImageCustom.sprite = sprite;
            // Scale override removed to respect Editor settings
            profileImageCustom.gameObject.SetActive(true);
            if (profileImageSprite != null) profileImageSprite.gameObject.SetActive(false);
        }
    }

    private void UpdateUIWithDefaultData()
    {
        if (playerNameInputField != null) playerNameInputField.text = "Guest";
        if (emailText != null) emailText.text = "Not logged in";
    }

    private void OnEditNameClicked()
    {
        if (playerNameInputField == null) return;
        
        if (!isEditMode)
        {
            isEditMode = true;
            originalPlayerName = PlayerModel.Name;
            playerNameInputField.interactable = true;
            playerNameInputField.ActivateInputField();
        }
        else
        {
            string newName = playerNameInputField.text.Trim();
            if (!string.IsNullOrEmpty(newName) && newName != originalPlayerName)
                BackendAuthService.Instance.UpdateProfile(newName); // PURE UI: Just trigger the service
            
            isEditMode = false;
            playerNameInputField.interactable = false;
        }
        UpdateEditButtonText();
    }

    private void OnLogoutClicked()
    {
        // 1. Show splash/loading immediately
        if (splashScreenPanel != null) splashScreenPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // 2. Call the backend to clear session
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.Logout((success, message) => {
                // Local cleanup is handled inside the Logout service method
                ShowLoginScreen();
            });
        }
        else
        {
            // Fallback if service is missing
            BackendAuthService.ClearJWT();
            ShowLoginScreen();
        }
    }

    private void ShowLoginScreen()
    {
        if (splashScreenPanel != null) splashScreenPanel.SetActive(false);
        AuthenticationManager auth = FindFirstObjectByType<AuthenticationManager>();
        if (auth != null) auth.ShowLoginRegisterPanel();
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
            if (success && !string.IsNullOrEmpty(result) && result.StartsWith("http"))
            {
                BackendAuthService.Instance.GetImage(result, (tex) => {
                    if (tex != null)
                    {
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        ShowCustom(sprite);
                    }
                }, true);
            }
        });
    }

    private void SetEditMode(bool editMode) { /* Helper */ }
    private void UpdateEditButtonText()
    {
        if (editNameButton != null)
        {
            TextMeshProUGUI text = editNameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = isEditMode ? saveButtonText : editButtonText;
        }
    }
}
