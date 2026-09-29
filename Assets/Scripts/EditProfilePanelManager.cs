using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditProfilePanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button uploadButton;
    
    [Tooltip("The main image where the avatar is displayed (Dp&Frame in hierarchy)")]
    [SerializeField] private Image dpImage; 
    
    [Tooltip("The image where the frame is displayed (Frame show in hierarchy)")]
    [SerializeField] private Image frameShowImage; 
    
    [Header("Preset Avatars (6 buttons)")]
    [Tooltip("Assign the 6 avatar buttons here")]
    [SerializeField] private Button[] presetAvatarButtons = new Button[6];
    
    [Header("Preset Frames (8 buttons)")]
    [Tooltip("Assign the 8 frame buttons here")]
    [SerializeField] private Button[] presetFrameButtons = new Button[8];
    
    private void Start()
    {
        // Setup Back Button
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                PlayTapSound();
                gameObject.SetActive(false);
            });
        }
        
        // Setup Upload Button
        if (uploadButton != null)
        {
            uploadButton.onClick.AddListener(OnUploadClicked);
        }
        
        // Setup Avatar Buttons
        for (int i = 0; i < presetAvatarButtons.Length; i++)
        {
            int index = i; // capture index for closure
            if (presetAvatarButtons[i] != null)
            {
                presetAvatarButtons[i].onClick.AddListener(() => OnAvatarSelected(index));
            }
        }
        
        // Setup Frame Buttons
        for (int i = 0; i < presetFrameButtons.Length; i++)
        {
            int index = i; // capture index for closure
            if (presetFrameButtons[i] != null)
            {
                presetFrameButtons[i].onClick.AddListener(() => OnFrameSelected(index));
            }
        }
        
        // Subscribe to data updates
        PlayerModel.OnDataUpdated += RefreshDisplay;
        
        // Initial refresh
        RefreshDisplay();
    }
    
    private void OnDestroy()
    {
        PlayerModel.OnDataUpdated -= RefreshDisplay;
    }
    
    private void OnEnable()
    {
        RefreshDisplay();
    }
    
    private void RefreshDisplay()
    {
        // 1. Refresh Avatar Image
        string avatarData = PlayerModel.Avatar;
        if (int.TryParse(avatarData, out int aIndex))
        {
            if (AvatarSelector.Instance != null && dpImage != null)
            {
                dpImage.sprite = AvatarSelector.Instance.GetAvatarSpriteByIndex(aIndex);
            }
        }
        else if (!string.IsNullOrEmpty(avatarData) && avatarData.StartsWith("http"))
        {
            if (BackendAuthService.Instance != null && dpImage != null)
            {
                BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                    if (texture != null && dpImage != null)
                    {
                        dpImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }
                });
            }
        }
        else
        {
            // Default avatar fallback
            if (AvatarSelector.Instance != null && dpImage != null)
            {
                dpImage.sprite = AvatarSelector.Instance.GetAvatarSpriteByIndex(0);
            }
        }
        
        // 2. Refresh Frame Image
        int fIndex = 0;
        if (int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame))
        {
            fIndex = parsedFrame;
        }
        else
        {
            fIndex = PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
        }
        
        if (AvatarSelector.Instance != null && frameShowImage != null)
        {
            Sprite frameSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(fIndex);
            if (frameSprite != null)
            {
                frameShowImage.sprite = frameSprite;
                frameShowImage.gameObject.SetActive(true);
            }
            else
            {
                frameShowImage.gameObject.SetActive(false);
            }
        }
    }
    
    private void OnAvatarSelected(int index)
    {
        PlayTapSound();
        
        // Assuming the 6 buttons correspond directly to avatars 0 to 5 in AvatarSelector
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.UpdateProfile(PlayerModel.Name, index.ToString(), null);
        }
        
        // Optimistic UI update
        if (AvatarSelector.Instance != null && dpImage != null)
        {
            dpImage.sprite = AvatarSelector.Instance.GetAvatarSpriteByIndex(index);
        }
    }
    
    private void OnFrameSelected(int index)
    {
        PlayTapSound();
        
        // Save locally immediately for fast feedback
        PlayerPrefs.SetInt("SelectedProfileFrameIndex", index);
        PlayerModel.SetProfileFrame(index.ToString());
        
        // Sync with backend
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.UpdateProfile(PlayerModel.Name, null, index.ToString());
        }
    }
    
    private const int MAX_IMAGE_SIZE = 512;

    private void OnUploadClicked()
    {
        PlayTapSound();
        
        // Use the existing ImagePickerHelper used in the project
        ImagePickerHelper.PickImage(OnImageSelected, MAX_IMAGE_SIZE);
    }
    
    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null) return;
        
        byte[] imageBytes = texture.EncodeToJPG(75);
        
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.UploadAvatar(imageBytes, (success, result) => {
                if (success)
                {
                    // Update display immediately with the selected local texture
                    if (dpImage != null)
                    {
                        dpImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    }
                    Debug.Log("[EditProfilePanelManager] Avatar uploaded successfully.");
                }
                else
                {
                    Debug.LogError("[EditProfilePanelManager] Upload failed: " + result);
                }
            });
        }
    }
    
    private void PlayTapSound()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
    }

    /// <summary>
    /// Connect this method to a "Reset Avatar" button in the inspector.
    /// </summary>
    public void ResetAvatarToDefault()
    {
        PlayTapSound();
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.ResetAvatar((success, msg) => {
                if (success)
                {
                    Debug.Log("[EditProfilePanelManager] Avatar reset successfully: " + msg);
                    // The backend typically sets the avatar to "0" or clears it
                    if (dpImage != null && AvatarSelector.Instance != null)
                    {
                        dpImage.sprite = AvatarSelector.Instance.GetAvatarSpriteByIndex(0);
                    }
                }
                else
                {
                    Debug.LogError("[EditProfilePanelManager] Avatar reset failed: " + msg);
                }
            });
        }
    }
}
