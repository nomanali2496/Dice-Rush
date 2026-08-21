using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EditProfilePanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button uploadAvatarButton; // Slot 1 in the avatar grid
    
    [Header("Preset Avatars (7 Slots)")]
    [SerializeField] private Button[] presetAvatarButtons = new Button[7];
    [SerializeField] private GameObject[] presetAvatarHighlights = new GameObject[7];
    [SerializeField] private GameObject customAvatarHighlight; // Highlight for the upload button

    [Header("Frames (8 Slots)")]
    [SerializeField] private Button[] frameButtons = new Button[8];
    [SerializeField] private GameObject[] frameHighlights = new GameObject[8];

    [Header("Settings")]
    [SerializeField] private int maxImageSize = 512;

    private int selectedAvatarIndex = -1; // -1 means custom, 0-6 means preset (which map to indices 0-6 in backend)
    private int selectedFrameIndex = 0;

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(ClosePanel);
        if (uploadAvatarButton != null) uploadAvatarButton.onClick.AddListener(OnUploadAvatarClicked);

        for (int i = 0; i < presetAvatarButtons.Length; i++)
        {
            int index = i;
            if (presetAvatarButtons[i] != null)
                presetAvatarButtons[i].onClick.AddListener(() => OnPresetAvatarClicked(index));
        }

        for (int i = 0; i < frameButtons.Length; i++)
        {
            int index = i;
            if (frameButtons[i] != null)
                frameButtons[i].onClick.AddListener(() => OnFrameClicked(index));
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
            if (selectedAvatarIndex >= presetAvatarButtons.Length) selectedAvatarIndex = 0;
        }
        else
        {
            selectedAvatarIndex = 0;
        }

        // Load frame from PlayerPrefs since it's not in PlayerModel yet
        selectedFrameIndex = PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
    }

    private void RefreshHighlights()
    {
        // Avatar Highlights
        if (customAvatarHighlight != null)
        {
            customAvatarHighlight.SetActive(selectedAvatarIndex == -1);
        }

        for (int i = 0; i < presetAvatarHighlights.Length; i++)
        {
            if (presetAvatarHighlights[i] != null)
            {
                presetAvatarHighlights[i].SetActive(i == selectedAvatarIndex);
            }
        }

        // Frame Highlights
        for (int i = 0; i < frameHighlights.Length; i++)
        {
            if (frameHighlights[i] != null)
            {
                frameHighlights[i].SetActive(i == selectedFrameIndex);
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
        Debug.Log($"Frame {index} selected and saved locally.");
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
