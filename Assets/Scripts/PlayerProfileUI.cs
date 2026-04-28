using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays and manages player profile information
/// </summary>
public class PlayerProfileUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerNameInputField; // InputField for editing player name
    [SerializeField] private TextMeshProUGUI emailText;
    [SerializeField] private TextMeshProUGUI helpAndSupportEmailText;
    [Tooltip("Image shown when a PRESET avatar is selected (from the avatar buttons)")]
    [SerializeField] private Image profileImageSprite;
    
    [Tooltip("Image shown when a CUSTOM photo is picked from the gallery — size this larger than the mask in the Inspector for the zoom effect")]
    [SerializeField] private Image profileImageCustom;
    
    [Header("Buttons")]
    [SerializeField] private Button editNameButton; // Button to edit/save player name
    [SerializeField] private Button logoutButton;
    
    [Header("Custom Image Upload")]
    [Tooltip("Button to select custom image from device")]
    [SerializeField] private Button selectCustomImageButton;
    
    [Tooltip("Image component to display selected custom image (same as profileImageCustom, optional separate slot)")]
    [SerializeField] private Image customImageDisplay;
    
    [Header("Edit Mode Settings")]
    [SerializeField] private string editButtonText = "Edit";
    [SerializeField] private string saveButtonText = "Save";
    [SerializeField] private int maxImageSize = 512; // Max width/height for uploaded images
    
    [SerializeField] private GameObject AvatarDisplay;
    
    [Header("Panel References for Logout")]
    [SerializeField] private GameObject settingsPanel; // Settings panel to hide on logout
    [SerializeField] private GameObject mainMenuPanel; // Main menu panel to hide on logout
    [SerializeField] private GameObject splashScreenPanel; // Loading panel to show on logout
    
    private bool isEditMode = false;
    private string originalPlayerName = ""; // Store original name when entering edit mode
    private Texture2D selectedCustomTexture; // Store selected custom texture
    
    private void Start()
    {
        // Hide the custom image slot by default — sprite slot is shown until a custom image is loaded
        if (profileImageCustom != null)
            profileImageCustom.gameObject.SetActive(false);
        if (profileImageSprite != null)
            profileImageSprite.gameObject.SetActive(true);
        
        // Add button listeners
        if (editNameButton != null)
            editNameButton.onClick.AddListener(OnEditNameClicked);
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
        
        // Setup custom image button
        if (selectCustomImageButton != null)
        {
            selectCustomImageButton.onClick.AddListener(OnSelectCustomImageClicked);
        }
        
        // Setup input field - start in read-only mode
        SetEditMode(false);
        
        // Subscribe to Backend PlayerModel events
        PlayerModel.OnDataUpdated -= RefreshUIFromBackend;
        PlayerModel.OnDataUpdated += RefreshUIFromBackend;
        Debug.Log("[PlayerProfileUI] Subscribed to PlayerModel events in Start()");
        
        // Load custom image if exists
        LoadCustomImage();
    }
    
    private void OnEnable()
    {
        Debug.Log("[PlayerProfileUI] ===== PANEL ENABLED =====");
        
        // Load custom image if exists
        LoadCustomImage();
        
        StartCoroutine(InitializeUIAfterDelay());
    }
    
    private System.Collections.IEnumerator InitializeUIAfterDelay()
    {
        // Wait one frame to ensure everything is set up
        yield return null;
        
        Debug.Log("[PlayerProfileUI] Initializing UI after delay...");
        
        // Ensure Backend PlayerModel is subscribed
        PlayerModel.OnDataUpdated -= RefreshUIFromBackend;
        PlayerModel.OnDataUpdated += RefreshUIFromBackend;
        
        // Refresh immediately if data exists
        RefreshUIFromBackend();
    }
    
    private void OnDisable()
    {
        PlayerModel.OnDataUpdated -= RefreshUIFromBackend;
    }
    
    private void OnDestroy()
    {
        PlayerModel.OnDataUpdated -= RefreshUIFromBackend;
    }
    
    private void RefreshUIFromBackend()
    {
        Debug.Log("[PlayerProfileUI] RefreshUIFromBackend called");
        
        if (string.IsNullOrEmpty(PlayerModel.Id)) 
        {
            UpdateUIWithDefaultData();
            return; 
        }

        // Update name
        if (!isEditMode && playerNameInputField != null)
        {
            playerNameInputField.text = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "Guest";
        }

        // Update email
        if (emailText != null)
        {
            emailText.text = !string.IsNullOrEmpty(PlayerModel.Email) ? PlayerModel.Email : "No email";
            if (helpAndSupportEmailText != null)
                helpAndSupportEmailText.text = emailText.text;
        }

        // Update profile image from avatar string
        if (!string.IsNullOrEmpty(PlayerModel.Avatar))
        {
            // For now, we assume the avatar string is an index for the preset list
            int avatarIndex = 0;
            if (int.TryParse(PlayerModel.Avatar, out int index))
            {
                avatarIndex = index;
            }
            LoadAvatarSpriteFromIndex(avatarIndex);
        }

        AvatarDisplay.SetActive(true);
    }

    private void UpdateUIWithDefaultData()
    {
        if (playerNameInputField != null)
            playerNameInputField.text = "Guest";
        
        if (emailText != null)
            emailText.text = "Not logged in";
        if (helpAndSupportEmailText != null)
            helpAndSupportEmailText.text = emailText.text;
    }
    
    private void OnEditNameClicked()
    {
        if (playerNameInputField == null)
        {
            Debug.LogError("[PlayerProfileUI] playerNameInputField is null! Cannot edit name.");
            return;
        }
        
        if (!isEditMode)
        {
            EnterEditMode();
        }
        else
        {
            SavePlayerName();
        }
    }
    
    private void EnterEditMode()
    {
        isEditMode = true;
        originalPlayerName = PlayerModel.Name;
        
        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = true;
            playerNameInputField.readOnly = false;
            playerNameInputField.Select();
            playerNameInputField.ActivateInputField();
        }
        
        UpdateEditButtonText();
        Debug.Log("[PlayerProfileUI] Entered edit mode");
    }
    
    private void ExitEditMode()
    {
        isEditMode = false;
        
        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = false;
            playerNameInputField.readOnly = true;
        }
        
        UpdateEditButtonText();
        Debug.Log("[PlayerProfileUI] Exited edit mode");
    }
    
    private void UpdateEditButtonText()
    {
        if (editNameButton != null)
        {
            TextMeshProUGUI buttonText = editNameButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isEditMode ? saveButtonText : editButtonText;
            }
        }
    }
    
    private void SavePlayerName()
    {
        if (playerNameInputField == null) return;
        
        string newName = playerNameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("[PlayerProfileUI] Player name cannot be empty!");
            playerNameInputField.text = PlayerModel.Name;
            ExitEditMode();
            return;
        }
        
        if (newName.Length < 3)
        {
            Debug.LogWarning("[PlayerProfileUI] Player name must be at least 3 characters!");
            playerNameInputField.text = PlayerModel.Name;
            ExitEditMode();
            return;
        }
        
        if (newName == originalPlayerName)
        {
            Debug.Log("[PlayerProfileUI] Name unchanged, exiting edit mode");
            ExitEditMode();
            return;
        }
        
        Debug.Log($"[PlayerProfileUI] Saving new player name: '{newName}'");
        
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.UpdateProfile(newName, (success, message) => 
            {
                if (success)
                {
                    Debug.Log("[PlayerProfileUI] ✓ Name updated successfully on backend!");
                }
                else
                {
                    Debug.LogError($"[PlayerProfileUI] Failed to update name: {message}");
                    // Revert UI to old name if failed
                    if (playerNameInputField != null)
                    {
                        playerNameInputField.text = PlayerModel.Name;
                    }
                }
                ExitEditMode();
            });
        }
        else
        {
            Debug.LogError("[PlayerProfileUI] BackendAuthService not found!");
            ExitEditMode();
        }
    }
    
    private void SetEditMode(bool editMode)
    {
        isEditMode = editMode;
        
        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = editMode;
            playerNameInputField.readOnly = !editMode;
        }
        
        UpdateEditButtonText();
    }
    
    private void OnLogoutClicked()
    {
        OfferPanelManager offerPanelManager = FindObjectOfType<OfferPanelManager>();
        if (offerPanelManager != null)
        {
            offerPanelManager.OnUserLoggedOut();
        }
        
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.ClearJWT();
            Debug.Log("[PlayerProfileUI] Player logged out (Backend JWT cleared)");
        }
        
        if (splashScreenPanel != null) splashScreenPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        
        StartCoroutine(ShowLoginScreenAfterDelay());
    }
    
    private System.Collections.IEnumerator ShowLoginScreenAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (splashScreenPanel != null) splashScreenPanel.SetActive(false);
        
        AuthenticationManager authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.ShowLoginRegisterPanel();
        }
    }
    
    private void ShowCustomImage(Sprite sprite)
    {
        if (profileImageCustom != null)
        {
            profileImageCustom.sprite = sprite;
            profileImageCustom.rectTransform.localScale = Vector3.one * 2f;
            profileImageCustom.gameObject.SetActive(true);
            
            if (profileImageSprite != null)
                profileImageSprite.gameObject.SetActive(false);
        }
        else if (profileImageSprite != null)
        {
            profileImageSprite.sprite = sprite;
            profileImageSprite.rectTransform.localScale = Vector3.one * 2f;
            profileImageSprite.gameObject.SetActive(true);
        }
        
        if (customImageDisplay != null)
            customImageDisplay.sprite = sprite;
    }
    
    private void ShowSpriteImage(Sprite sprite)
    {
        if (profileImageSprite != null)
        {
            profileImageSprite.sprite = sprite;
            profileImageSprite.rectTransform.localScale = Vector3.one;
            profileImageSprite.gameObject.SetActive(true);
        }
        if (profileImageCustom != null)
            profileImageCustom.gameObject.SetActive(false);
    }
    
    public void UpdateFromAvatarSelector(int avatarIndex, Sprite[] sprites)
    {
        if (sprites == null || avatarIndex < 0 || avatarIndex >= sprites.Length) return;
        
        Sprite avatarSprite = sprites[avatarIndex];
        if (avatarSprite != null)
        {
            ShowSpriteImage(avatarSprite);
        }
    }
    
    public void UpdateCustomImage(Sprite customSprite)
    {
        if (customSprite != null)
        {
            ShowCustomImage(customSprite);
        }
    }
    
    private System.Collections.IEnumerator LoadImageFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) yield break;
        
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ShowSpriteImage(sprite);
                }
            }
            else
            {
                LoadAvatarSpriteFromIndex(0);
            }
        }
    }
    
    private void LoadAvatarSpriteFromIndex(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= 8) selectedIndex = 0;
        
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (selector != null)
        {
            Sprite avatarSprite = selector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                ShowSpriteImage(avatarSprite);
            }
        }
    }
    
    #region Custom Image Selection
    
    private void OnSelectCustomImageClicked()
    {
        ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
    }
    
    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null) return;
        
        selectedCustomTexture = texture;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        ShowCustomImage(sprite);
        
        MainMenuAvatarDisplay[] displays = FindObjectsOfType<MainMenuAvatarDisplay>(true);
        foreach (MainMenuAvatarDisplay display in displays)
        {
            if (display != null) display.UpdateCustomImage(sprite);
        }
    }
    
    private void LoadCustomImage()
    {
        // Placeholder
    }
    
    #endregion
}
