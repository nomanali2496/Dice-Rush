using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EditProfilePanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [UnityEngine.Serialization.FormerlySerializedAs("uploadAvatarButton")]
    [SerializeField] private Button uploadButton; // Maps to "Upload" in hierarchy
    [UnityEngine.Serialization.FormerlySerializedAs("topAvatarPreview")]
    [SerializeField] private Image avatarPreview; // Maps to "Button" in hierarchy
    [UnityEngine.Serialization.FormerlySerializedAs("topFramePreview")]
    [SerializeField] private Image framePreview; // Maps to "2" in hierarchy
    
    [Header("AvatarManager (Avatars)")]
    [UnityEngine.Serialization.FormerlySerializedAs("presetAvatarButtons")]
    [SerializeField] private Button[] avatarManagerButtons = new Button[7];
    [UnityEngine.Serialization.FormerlySerializedAs("presetAvatarHighlights")]
    [SerializeField] private GameObject[] avatarManagerHighlights = new GameObject[7];
    [UnityEngine.Serialization.FormerlySerializedAs("customAvatarHighlight")]
    [SerializeField] private GameObject uploadHighlight; // Highlight for the upload button

    [Header("AvatarFrameManager (Frames)")]
    [UnityEngine.Serialization.FormerlySerializedAs("frameButtons")]
    [SerializeField] private Button[] avatarFrameManagerButtons = new Button[8];
    [UnityEngine.Serialization.FormerlySerializedAs("frameHighlights")]
    [SerializeField] private GameObject[] avatarFrameManagerHighlights = new GameObject[8];

    [Header("Settings")]
    [SerializeField] private int maxImageSize = 512;

    private int selectedAvatarIndex = -1; // -1 means custom, 0-6 means preset (which map to indices 0-6 in backend)
    private int selectedFrameIndex = 0;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(ClosePanel);
        if (uploadButton != null) uploadButton.onClick.AddListener(OnUploadAvatarClicked);

        for (int i = 0; i < avatarManagerButtons.Length; i++)
        {
            int index = i;
            if (avatarManagerButtons[i] != null)
            {
                avatarManagerButtons[i].onClick.AddListener(() => OnPresetAvatarClicked(index));
                
                // Automatically set the sprite from AvatarSelector
                if (AvatarSelector.Instance != null)
                {
                    Image btnImage = avatarManagerButtons[i].GetComponent<Image>();
                    if (btnImage != null)
                    {
                        Sprite spr = AvatarSelector.Instance.GetAvatarSpriteByIndex(index);
                        if (spr != null) btnImage.sprite = spr;
                    }
                }
            }
        }

        for (int i = 0; i < avatarFrameManagerButtons.Length; i++)
        {
            int index = i;
            if (avatarFrameManagerButtons[i] != null)
            {
                avatarFrameManagerButtons[i].onClick.AddListener(() => OnFrameClicked(index));
                
                // Automatically set the sprite from AvatarSelector
                if (AvatarSelector.Instance != null)
                {
                    Image btnImage = avatarFrameManagerButtons[i].GetComponent<Image>();
                    if (btnImage != null)
                    {
                        Sprite spr = AvatarSelector.Instance.GetBackgroundSpriteByIndex(index);
                        if (spr != null) btnImage.sprite = spr;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        LoadCurrentSelections();
        RefreshHighlights();
    }

    private void LoadCurrentSelections()
    {
        // Check current avatar from PlayerModel
        string currentAvatar = PlayerModel.Avatar;
        if (!string.IsNullOrEmpty(currentAvatar) && currentAvatar.StartsWith("http"))
        {
            selectedAvatarIndex = -1; // Custom
        }
        else if (int.TryParse(currentAvatar, out int parsedIndex))
        {
            // Map the parsed index directly (assuming backend index matches our 0-6 presets)
            selectedAvatarIndex = parsedIndex;
            if (selectedAvatarIndex >= avatarManagerButtons.Length) selectedAvatarIndex = 0;
        }
        else
        {
            selectedAvatarIndex = 0;
        }

        // Load frame from PlayerPrefs since it's not in PlayerModel yet
        selectedFrameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
            ? parsedFrame
            : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
    }

    private void RefreshHighlights()
    {
        // Avatar Highlights
        if (uploadHighlight != null)
        {
            uploadHighlight.SetActive(selectedAvatarIndex == -1);
        }

        for (int i = 0; i < avatarManagerHighlights.Length; i++)
        {
            if (avatarManagerHighlights[i] != null)
            {
                avatarManagerHighlights[i].SetActive(i == selectedAvatarIndex);
            }
        }

        // Frame Highlights
        for (int i = 0; i < avatarFrameManagerHighlights.Length; i++)
        {
            if (avatarFrameManagerHighlights[i] != null)
            {
                avatarFrameManagerHighlights[i].SetActive(i == selectedFrameIndex);
            }
        }

        // Update Top Previews instantly
        if (AvatarSelector.Instance != null)
        {
            if (framePreview != null)
            {
                framePreview.sprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(selectedFrameIndex);
            }
            
            if (avatarPreview != null && selectedAvatarIndex >= 0)
            {
                avatarPreview.sprite = AvatarSelector.Instance.GetAvatarSpriteByIndex(selectedAvatarIndex);
            }
        }
    }

    private void OnPresetAvatarClicked(int index)
    {
        selectedAvatarIndex = index;
        RefreshHighlights();

        // Save immediately to backend (BackendAuthService automatically updates PlayerModel upon success)
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.UpdateProfile(PlayerModel.Name, index.ToString());
        }
    }

    private void OnFrameClicked(int index)
    {
        selectedFrameIndex = index;
        RefreshHighlights();
        
        // Save locally for now
        PlayerPrefs.SetInt("SelectedProfileFrameIndex", index);
        PlayerPrefs.Save();
        PlayerModel.SetProfileFrame(index.ToString());
        Debug.Log($"Frame {index} selected and saved locally.");

        // Save immediately to backend (BackendAuthService automatically updates PlayerModel upon success)
        if (BackendAuthService.Instance != null)
        {
            // Send null for avatar so it only updates the frame
            BackendAuthService.Instance.UpdateProfile(PlayerModel.Name, null, index.ToString());
        }
    }

    private void OnUploadAvatarClicked()
    {
        ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
    }

    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null) return;
        
        byte[] imageBytes = texture.EncodeToJPG(75);
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.UploadAvatar(imageBytes, (success, result) => {
                if (success && !string.IsNullOrEmpty(result) && result.StartsWith("http"))
                {
                    selectedAvatarIndex = -1;
                    RefreshHighlights();
                }
            });
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);

        // Refresh profile panel if open in background
        ProfilePanelUI profileUI = Object.FindFirstObjectByType<ProfilePanelUI>();
        if (profileUI != null && profileUI.gameObject.activeInHierarchy)
        {
            profileUI.SetupForSelf();
        }
    }
}
