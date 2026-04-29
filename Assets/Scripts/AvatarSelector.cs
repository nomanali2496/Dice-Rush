using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles avatar and background selection with 8 buttons.
/// PURE FRONTEND: Handles button clicks and UI updates only.
/// Calls BackendAuthService for all network/data operations.
/// </summary>
public class AvatarSelector : MonoBehaviour
{
    [Header("Selection Buttons (8 buttons)")]
    [SerializeField] private Button[] avatarButtons = new Button[8];
    
    [Header("Avatar Images (8 avatars)")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Background Images (8 backgrounds)")]
    [SerializeField] private Sprite[] backgroundSprites = new Sprite[8];
    
    [Header("Display References")]
    [SerializeField] private Image selectedAvatarDisplay;
    [SerializeField] private Image selectedBackgroundDisplay;
    [SerializeField] private Image mainMenuAvatarDisplay;
    
    [Header("Button Highlight")]
    [SerializeField] private GameObject[] buttonHighlights = new GameObject[8];
    
    [Header("Custom Image Upload")]
    [SerializeField] private Button selectCustomImageButton;
    [SerializeField] private Image customImageDisplay;
    
    [Header("Watch Video UI")]
    [SerializeField] private TMP_Text watchVideoText;
    
    [Header("Settings")]
    [SerializeField] private bool showSelectionFeedback = true;
    [SerializeField] private int maxImageSize = 512; 
    
    private int currentSelectedIndex = 0;

    private void Start()
    {
        // Setup button listeners
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i; 
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.AddListener(() => OnAvatarButtonClicked(index));
        }
        
        if (selectCustomImageButton != null)
            selectCustomImageButton.onClick.AddListener(OnSelectCustomImageClicked);
        
        // Purely event-driven UI refresh
        PlayerModel.OnDataUpdated += RefreshUI;
        
        RefreshUI();
        SetupWatchVideoText();
    }
    
    private void OnDestroy() => PlayerModel.OnDataUpdated -= RefreshUI;
    private void OnEnable() => RefreshUI();

    /// <summary>
    /// Update UI based on PlayerModel state.
    /// No mixing: Networking is handled by asking BackendAuthService to fetch the image.
    /// </summary>
    public void RefreshUI()
    {
        string avatarData = PlayerModel.Avatar;

        // 1. Determine if we are showing a preset or a custom URL
        if (int.TryParse(avatarData, out int index))
        {
            currentSelectedIndex = index;
            ShowPreset(index);
        }
        else if (!string.IsNullOrEmpty(avatarData) && avatarData.StartsWith("http"))
        {
            currentSelectedIndex = -1;
            // ASYNC LOAD: We ask the backend for the image. UI doesn't know about UnityWebRequest.
            BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ApplyCustomSprite(sprite);
                }
            });
        }
        else
        {
            ShowPreset(0);
        }
        
        UpdateButtonHighlights();
    }

    private void ShowPreset(int index)
    {
        if (index < 0 || index >= avatarSprites.Length) index = 0;
        Sprite sprite = avatarSprites[index];

        if (selectedAvatarDisplay != null) selectedAvatarDisplay.sprite = sprite;
        if (mainMenuAvatarDisplay != null) mainMenuAvatarDisplay.sprite = sprite;

        UpdateExternalDisplays(sprite, index);
    }

    private void ApplyCustomSprite(Sprite sprite)
    {
        if (selectedAvatarDisplay != null) selectedAvatarDisplay.sprite = sprite;
        if (mainMenuAvatarDisplay != null) mainMenuAvatarDisplay.sprite = sprite;
        if (customImageDisplay != null) customImageDisplay.sprite = sprite;

        UpdateExternalDisplays(sprite, -1);
    }

    private void UpdateExternalDisplays(Sprite sprite, int index)
    {
        // Broadcast update to other UI scripts
        MainMenuAvatarDisplay[] displays = FindObjectsOfType<MainMenuAvatarDisplay>(true);
        foreach (var display in displays)
        {
            if (index >= 0) display.UpdateFromAvatarSelector(index, avatarSprites);
            else display.UpdateCustomImage(sprite);
        }

        PlayerProfileUI[] profileUIs = FindObjectsOfType<PlayerProfileUI>(true);
        foreach (var ui in profileUIs)
        {
            if (index >= 0) ui.UpdateFromAvatarSelector(index, avatarSprites);
            else ui.UpdateCustomImage(sprite);
        }
    }

    private void OnAvatarButtonClicked(int index)
    {
        if (index < 0 || index >= avatarButtons.Length) return;
        
        // PURE UI: Just trigger the service call
        BackendAuthService.Instance.UpdateProfile(PlayerModel.Name, index.ToString());

        if (showSelectionFeedback) ShowSelectionFeedback(index);
    }

    private void OnSelectCustomImageClicked()
    {
        ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
    }

    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null) return;
        
        // PURE UI: Prepare data and trigger service call
        byte[] imageBytes = texture.EncodeToJPG(75);
        BackendAuthService.Instance.UploadAvatar(imageBytes, (success, result) => {
            if (!success) Debug.LogError("[AvatarSelector] Upload failed: " + result);
        });
    }

    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < buttonHighlights.Length; i++)
        {
            if (buttonHighlights[i] != null)
                buttonHighlights[i].SetActive(i == currentSelectedIndex);
        }
    }

    private void ShowSelectionFeedback(int index) { }

    private void SetupWatchVideoText()
    {
        if (watchVideoText != null)
            watchVideoText.text = "Watch Video and Earn 10 <sprite name=\"chips\">";
    }

    // Public helper for other scripts
    public Sprite GetAvatarSpriteByIndex(int index) => (index >= 0 && index < avatarSprites.Length) ? avatarSprites[index] : null;
}
