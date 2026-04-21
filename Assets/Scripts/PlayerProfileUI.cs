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
        
        // Subscribe to PlayFab events early (like MainMenuAvatarDisplay does)
        // This ensures we don't miss events that fire during initialization
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
            PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
            Debug.Log("[PlayerProfileUI] Subscribed to PlayFab events in Start()");
        }
        
        // Load custom image if exists
        LoadCustomImage();
    }
    
    private void OnEnable()
    {
        Debug.Log("[PlayerProfileUI] ===== PANEL ENABLED =====");
        
        // Load custom image if exists
        LoadCustomImage();
        
        // Wait a tiny bit to ensure PlayFabManager is fully initialized
        StartCoroutine(InitializeUIAfterDelay());
    }
    
    private System.Collections.IEnumerator InitializeUIAfterDelay()
    {
        // Wait one frame to ensure everything is set up
        yield return null;
        
        Debug.Log("[PlayerProfileUI] Initializing UI after delay...");
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] PlayFabManager not found!");
            UpdateUIWithDefaultData();
            yield break;
        }
        
        // Subscribe to future events (if not already subscribed in Start)
        // Note: We unsubscribe first to avoid duplicate subscriptions
        PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
        Debug.Log("[PlayerProfileUI] Subscribed to PlayFab events in OnEnable()");
        
        Debug.Log($"[PlayerProfileUI] IsLoggedIn: {PlayFabManager.Instance.IsLoggedIn()}");
        
        // Check if player is logged in and data exists
        if (PlayFabManager.Instance.IsLoggedIn())
        {
            PlayerData data = PlayFabManager.Instance.currentPlayerData;
            
            Debug.Log($"[PlayerProfileUI] Checking data - playerName: '{data?.playerName}', cash: {data?.cash}, coins: {data?.coins}");
            
            if (data != null && !string.IsNullOrEmpty(data.playerName))
            {
                // Data was already loaded while this panel was inactive!
                Debug.Log("[PlayerProfileUI] ✓✓✓ DATA FOUND! Displaying immediately! ✓✓✓");
                Debug.Log($"[PlayerProfileUI] Updating UI with: Name={data.playerName}, Email={data.email}, Cash={data.cash}, Coins={data.coins}");
                UpdateUI(data);
            }
            else
            {
                // Data not loaded yet, request it
                Debug.Log("[PlayerProfileUI] No data yet, requesting from PlayFab...");
                UpdateUIWithDefaultData();
                
                // Wait a bit then check again (data might be loading)
                yield return new WaitForSeconds(0.5f);
                
                // Check again after waiting
                data = PlayFabManager.Instance.currentPlayerData;
                if (data != null && !string.IsNullOrEmpty(data.playerName))
                {
                    Debug.Log("[PlayerProfileUI] ✓ Data arrived! Displaying now!");
                    UpdateUI(data);
                    
                }
                else
                {
                    Debug.Log("[PlayerProfileUI] Still no data, loading from PlayFab...");
                    PlayFabManager.Instance.LoadPlayerData();
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] Player not logged in!");
            UpdateUIWithDefaultData();
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events when disabled
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        }
    }
    
    private void RefreshPlayerData()
    {
        Debug.Log("[PlayerProfileUI] Manual RefreshPlayerData called");
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] PlayFabManager not found!");
            UpdateUIWithDefaultData();
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[PlayerProfileUI] Player not logged in!");
            UpdateUIWithDefaultData();
            return;
        }
        
        // Request fresh data from PlayFab
        Debug.Log("[PlayerProfileUI] Requesting fresh data from PlayFab...");
        PlayFabManager.Instance.LoadPlayerData();
    }
    
    private void OnPlayerDataLoaded(PlayerData data)
    {
        Debug.Log("[PlayerProfileUI] OnPlayerDataLoaded called!");
        if (data != null)
        {
            Debug.Log($"[PlayerProfileUI] Received data - Name: {data.playerName}, Cash: {data.cash}, Coins: {data.coins}");
            Debug.Log($"[PlayerProfileUI] Formatted Cash: {CurrencyDisplayUI.FormatCash(data.cash)}, Formatted Coins: {CurrencyDisplayUI.FormatCoins(data.coins)}");
        }
        UpdateUI(data);
    }
    
    private void OnPlayerDataSaved(bool success)
    {
        if (success)
        {
            Debug.Log("Player data saved successfully!");
            // Optionally show a notification
        }
        else
        {
            Debug.LogWarning("Failed to save player data!");
        }
    }
    
    private void UpdateUI(PlayerData data)
    {
        Debug.Log("[PlayerProfileUI] UpdateUI called");
        
        if (data == null)
        {
            Debug.LogWarning("[PlayerProfileUI] Data is null, showing defaults");
            UpdateUIWithDefaultData();
            return;
        }
        
        Debug.Log($"[PlayerProfileUI] Updating UI with: Name={data.playerName}, Email={data.email}, Cash={data.cash}, Coins={data.coins}");
        Debug.Log($"[PlayerProfileUI] Formatting - Cash: {data.cash} -> {CurrencyDisplayUI.FormatCash(data.cash)}, Coins: {data.coins} -> {CurrencyDisplayUI.FormatCoins(data.coins)}");
        
        // Update name - Support InputField
        // Only update if not in edit mode (to avoid interrupting user's typing)
        if (!isEditMode)
        {
            string playerName = !string.IsNullOrEmpty(data.playerName) ? data.playerName : "Guest";
            
            if (playerNameInputField != null)
            {
                playerNameInputField.text = playerName;
                Debug.Log($"[PlayerProfileUI] Set playerNameInputField to: {playerNameInputField.text}");
            }
            else
            {
                Debug.LogWarning("[PlayerProfileUI] playerNameInputField is null! Assign it in Inspector!");
            }
        }
        else
        {
            Debug.Log("[PlayerProfileUI] Skipping name update - user is in edit mode");
        }
        
        // Update email
        if (emailText != null)
        {
            emailText.text = !string.IsNullOrEmpty(data.email) ? data.email : "No email";
            helpAndSupportEmailText.text = emailText.text;
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] emailText is null!");
        }
        

        // Update profile image
        if (!string.IsNullOrEmpty(data.imageUrl))
        {
            Debug.Log($"[PlayerProfileUI] Profile image URL found: {data.imageUrl}");
            
            // Check if it's a base64 image (custom gallery pick)
            if (data.imageUrl.StartsWith("data:image"))
            {
                try
                {
                    string base64Data = data.imageUrl.Substring(data.imageUrl.IndexOf(",") + 1);
                    byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        ShowCustomImage(sprite);
                        Debug.Log("[PlayerProfileUI] ✓ Base64 profile image displayed in custom slot");
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerProfileUI] Failed to load image from base64 data");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlayerProfileUI] Failed to load base64 image: {e.Message}");
                }
            }
            // URL image (Facebook/Google) — show in sprite slot, no zoom needed
            else if (data.imageUrl.StartsWith("http://") || data.imageUrl.StartsWith("https://"))
            {
                Debug.Log($"[PlayerProfileUI] Loading profile image from URL: {data.imageUrl}");
                StartCoroutine(LoadImageFromUrl(data.imageUrl));
            }
            else
            {
                Debug.LogWarning($"[PlayerProfileUI] Unknown image URL format: {data.imageUrl}");
            }
        }
        else
        {
            // No image URL — show preset avatar sprite
            Debug.Log("[PlayerProfileUI] No image URL in data, loading avatar sprite from selectedAvatarIndex");
            LoadAvatarSpriteFromIndex(data.selectedAvatarIndex);
        }
        AvatarDisplay.SetActive(true);
        Debug.Log("[PlayerProfileUI] UI Update Complete!");
    }
    
    private void UpdateUIWithDefaultData()
    {
        if (playerNameInputField != null)
            playerNameInputField.text = "Guest";
        
        if (emailText != null)
            emailText.text = "Not logged in";
        helpAndSupportEmailText.text = emailText.text;
        

    }
    
    /// <summary>
    /// Called when Edit/Save button is clicked
    /// </summary>
    private void OnEditNameClicked()
    {
        if (playerNameInputField == null)
        {
            Debug.LogError("[PlayerProfileUI] playerNameInputField is null! Cannot edit name.");
            return;
        }
        
        if (!isEditMode)
        {
            // Enter Edit Mode
            EnterEditMode();
        }
        else
        {
            // Save the name
            SavePlayerName();
        }
    }
    
    /// <summary>
    /// Enter edit mode - enable input field
    /// </summary>
    private void EnterEditMode()
    {
        isEditMode = true;
        
        // Store original name
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            originalPlayerName = PlayFabManager.Instance.currentPlayerData.playerName;
        }
        
        // Enable input field for editing
        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = true;
            playerNameInputField.readOnly = false;
            playerNameInputField.Select(); // Focus the input field
            playerNameInputField.ActivateInputField(); // Show keyboard on mobile
        }
        
        // Update button text
        UpdateEditButtonText();
        
        Debug.Log("[PlayerProfileUI] Entered edit mode");
    }
    
    /// <summary>
    /// Exit edit mode - disable input field
    /// </summary>
    private void ExitEditMode()
    {
        isEditMode = false;
        
        // Disable input field (read-only mode)
        if (playerNameInputField != null)
        {
            playerNameInputField.interactable = false;
            playerNameInputField.readOnly = true;
        }
        
        // Update button text
        UpdateEditButtonText();
        
        Debug.Log("[PlayerProfileUI] Exited edit mode");
    }
    
    /// <summary>
    /// Update the edit button text based on current mode
    /// </summary>
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
    
    /// <summary>
    /// Save the updated player name to PlayFab
    /// </summary>
    private void SavePlayerName()
    {
        if (playerNameInputField == null)
        {
            Debug.LogError("[PlayerProfileUI] playerNameInputField is null!");
            return;
        }
        
        string newName = playerNameInputField.text.Trim();
        
        // Validate name
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("[PlayerProfileUI] Player name cannot be empty!");
            // Restore original name
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
            {
                playerNameInputField.text = PlayFabManager.Instance.currentPlayerData.playerName;
            }
            ExitEditMode();
            return;
        }
        
        if (newName.Length < 3)
        {
            Debug.LogWarning("[PlayerProfileUI] Player name must be at least 3 characters!");
            // Restore original name
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
            {
                playerNameInputField.text = PlayFabManager.Instance.currentPlayerData.playerName;
            }
            ExitEditMode();
            return;
        }
        
        // Check if name actually changed
        if (newName == originalPlayerName)
        {
            Debug.Log("[PlayerProfileUI] Name unchanged, exiting edit mode");
            ExitEditMode();
            return;
        }
        
        // Update player data
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.Log($"[PlayerProfileUI] Saving new player name: '{newName}'");
            
            // Update local data
            PlayFabManager.Instance.currentPlayerData.playerName = newName;
            
            // Save to PlayFab
            PlayFabManager.Instance.SavePlayerData(() =>
            {
                Debug.Log($"[PlayerProfileUI] ✓ Player name saved successfully to PlayFab: '{newName}'");
                ExitEditMode();
            });
        }
        else
        {
            Debug.LogError("[PlayerProfileUI] Cannot save - player not logged in!");
            ExitEditMode();
        }
    }
    
    /// <summary>
    /// Set edit mode (enable/disable input field)
    /// </summary>
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
        // Clear offer panel timer immediately when logout happens
        OfferPanelManager offerPanelManager = FindObjectOfType<OfferPanelManager>();
        if (offerPanelManager != null)
        {
            offerPanelManager.OnUserLoggedOut();
            Debug.Log("[PlayerProfileUI] Cleared offer panel timer on logout");
        }
        
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.Logout();
            Debug.Log("[PlayerProfileUI] Player logged out");
        }
        
        // Show loading panel
        if (splashScreenPanel != null)
        {
            splashScreenPanel.SetActive(true);
            Debug.Log("[PlayerProfileUI] Showing loading panel");
        }
        
        // Hide settings panel
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[PlayerProfileUI] Hiding settings panel");
        }
        
        // Hide main menu panel
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
            Debug.Log("[PlayerProfileUI] Hiding main menu panel");
        }
        
        // Wait a moment then show login screen
        StartCoroutine(ShowLoginScreenAfterDelay());
    }
    
    /// <summary>
    /// Coroutine to show login screen after a brief delay
    /// </summary>
    private System.Collections.IEnumerator ShowLoginScreenAfterDelay()
    {
        // Wait a brief moment to show loading panel
        yield return new WaitForSeconds(0.5f);
        
        // Hide loading panel
        if (splashScreenPanel != null)
        {
            splashScreenPanel.SetActive(false);
            Debug.Log("[PlayerProfileUI] Hiding loading panel");
        }
        
        // Show login screen via AuthenticationManager
        AuthenticationManager authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.ShowLoginRegisterPanel();
            Debug.Log("[PlayerProfileUI] Showing login screen");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] AuthenticationManager not found! Cannot show login screen.");
        }
    }
    
    /// <summary>
    /// Show a custom gallery-picked image: activates profileImageCustom, hides profileImageSprite.
    /// </summary>
    private void ShowCustomImage(Sprite sprite)
    {
        if (profileImageCustom != null)
        {
            // Preferred path: dedicated custom slot
            profileImageCustom.sprite = sprite;
            profileImageCustom.rectTransform.localScale = Vector3.one * 2f;
            profileImageCustom.gameObject.SetActive(true);
            
            if (profileImageSprite != null)
                profileImageSprite.gameObject.SetActive(false);
        }
        else if (profileImageSprite != null)
        {
            // Fallback: no custom slot assigned — reuse sprite slot with 2x zoom
            Debug.LogWarning("[PlayerProfileUI] profileImageCustom not assigned! Showing custom image in profileImageSprite with 2x zoom.");
            profileImageSprite.sprite = sprite;
            profileImageSprite.rectTransform.localScale = Vector3.one * 2f;
            profileImageSprite.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[PlayerProfileUI] Neither profileImageCustom nor profileImageSprite is assigned!");
        }
        
        // Also update the legacy customImageDisplay slot if assigned
        if (customImageDisplay != null)
            customImageDisplay.sprite = sprite;
    }
    
    /// <summary>
    /// Show a preset avatar sprite: activates profileImageSprite, hides profileImageCustom.
    /// </summary>
    private void ShowSpriteImage(Sprite sprite)
    {
        if (profileImageSprite != null)
        {
            profileImageSprite.sprite = sprite;
            profileImageSprite.rectTransform.localScale = Vector3.one; // ensure no leftover zoom
            profileImageSprite.gameObject.SetActive(true);
        }
        if (profileImageCustom != null)
            profileImageCustom.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Called by AvatarSelector when a preset avatar button is tapped.
    /// Shows the selected sprite in the sprite slot (hides the custom slot).
    /// </summary>
    public void UpdateFromAvatarSelector(int avatarIndex, Sprite[] sprites)
    {
        if (sprites == null || avatarIndex < 0 || avatarIndex >= sprites.Length)
        {
            Debug.LogWarning($"[PlayerProfileUI] UpdateFromAvatarSelector: invalid index {avatarIndex}");
            return;
        }
        
        Sprite avatarSprite = sprites[avatarIndex];
        if (avatarSprite != null)
        {
            ShowSpriteImage(avatarSprite);
            Debug.Log($"[PlayerProfileUI] ✓ Avatar updated to index {avatarIndex} via AvatarSelector");
        }
        else
        {
            Debug.LogWarning($"[PlayerProfileUI] Avatar sprite at index {avatarIndex} is null!");
        }
    }
    
    /// <summary>
    /// Called by AvatarSelector when a custom gallery image is picked from the main menu.
    /// Shows the image in the custom slot (zoomed).
    /// </summary>
    public void UpdateCustomImage(Sprite customSprite)
    {
        if (customSprite != null)
        {
            ShowCustomImage(customSprite);
            Debug.Log("[PlayerProfileUI] ✓ Custom image updated via external call");
        }
    }
    
    /// <summary>
    /// Load image from URL (for Facebook profile pictures, etc.)
    /// </summary>
    private System.Collections.IEnumerator LoadImageFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            yield break;
        }
        
        Debug.Log($"[PlayerProfileUI] Starting to load profile image from URL: {url}");
        
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, 
                        new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f));
                    // URL images (Facebook/Google) — show in sprite slot, no zoom
                    ShowSpriteImage(sprite);
                    Debug.Log($"[PlayerProfileUI] ✓ Profile image loaded from URL successfully! Size: {texture.width}x{texture.height}");
                }
                else
                {
                    Debug.LogWarning("[PlayerProfileUI] Failed to create texture from downloaded image");
                }
            }
            else
            {
                Debug.LogError($"[PlayerProfileUI] Failed to load profile image from URL: {url}. Error: {request.error}");
                LoadAvatarSpriteFromIndex(PlayFabManager.Instance?.currentPlayerData?.selectedAvatarIndex ?? 0);
            }
        }
    }
    
    /// <summary>
    /// Load avatar sprite from selectedAvatarIndex — shows in the sprite slot (no zoom).
    /// </summary>
    private void LoadAvatarSpriteFromIndex(int selectedIndex)
    {
        // Validate index
        if (selectedIndex < 0 || selectedIndex >= 8)
        {
            selectedIndex = 0;
            Debug.LogWarning($"[PlayerProfileUI] Invalid avatar index, using default 0");
        }
        
        Debug.Log($"[PlayerProfileUI] Loading avatar sprite from index: {selectedIndex}");
        
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (selector != null)
        {
            Sprite avatarSprite = selector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                ShowSpriteImage(avatarSprite);
                Debug.Log($"[PlayerProfileUI] ✓ Avatar sprite displayed from AvatarSelector: Index {selectedIndex}");
                return;
            }
            else
            {
                Debug.LogWarning($"[PlayerProfileUI] Avatar sprite at index {selectedIndex} is null in AvatarSelector!");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] AvatarSelector not found in scene!");
        }
        
        Debug.LogWarning($"[PlayerProfileUI] Could not load avatar sprite at index {selectedIndex}.");
    }
    
    
    #region Custom Image Selection
    
    /// <summary>
    /// Called when select custom image button is clicked
    /// </summary>
    private void OnSelectCustomImageClicked()
    {
        Debug.Log("[PlayerProfileUI] Select custom image button clicked");
        ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
    }
    
    /// <summary>
    /// Called when image is selected
    /// </summary>
    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("[PlayerProfileUI] Selected texture is null!");
            return;
        }
        
        selectedCustomTexture = texture;
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        
        // Show in the custom image slot (zoomed) — hides the sprite slot
        ShowCustomImage(sprite);
        Debug.Log("[PlayerProfileUI] Custom image displayed in custom slot");
        
        // Update all MainMenuAvatarDisplay scripts
        MainMenuAvatarDisplay[] displays = FindObjectsOfType<MainMenuAvatarDisplay>(true);
        foreach (MainMenuAvatarDisplay display in displays)
        {
            if (display != null)
            {
                display.UpdateCustomImage(sprite);
            }
        }
        
        // Convert to base64 and save to PlayFab
        SaveCustomImageToPlayFab(texture);
    }
    
    /// <summary>
    /// Save custom image to PlayFab as base64 string
    /// </summary>
    private void SaveCustomImageToPlayFab(Texture2D texture)
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] PlayFabManager not found! Cannot save image.");
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[PlayerProfileUI] Player not logged in! Cannot save image.");
            return;
        }
        
        // Convert texture to PNG bytes
        byte[] imageBytes = texture.EncodeToPNG();
        
        // Convert to base64 string
        string base64Image = System.Convert.ToBase64String(imageBytes);
        
        Debug.Log($"[PlayerProfileUI] Saving custom image to PlayFab (size: {imageBytes.Length} bytes, base64 length: {base64Image.Length})");
        
        // Update player data
        if (PlayFabManager.Instance.currentPlayerData != null)
        {
            // Store as base64 string in imageUrl field
            PlayFabManager.Instance.currentPlayerData.imageUrl = "data:image/png;base64," + base64Image;
            
            // Save to PlayFab
            PlayFabManager.Instance.SavePlayerData(() =>
            {
                Debug.Log("[PlayerProfileUI] ✓ Custom image saved successfully to PlayFab!");
            });
        }
        else
        {
            Debug.LogError("[PlayerProfileUI] PlayerData is null! Cannot save image.");
        }
    }
    
    /// <summary>
    /// Load custom image from PlayFab
    /// </summary>
    private void LoadCustomImage()
    {
        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn())
        {
            return;
        }
        
        if (PlayFabManager.Instance.currentPlayerData == null)
        {
            return;
        }
        
        string imageUrl = PlayFabManager.Instance.currentPlayerData.imageUrl;
        
        if (string.IsNullOrEmpty(imageUrl))
        {
            return;
        }
        
        // Check if it's a base64 image
        if (imageUrl.StartsWith("data:image"))
        {
            try
            {
                // Extract base64 part
                string base64Data = imageUrl.Substring(imageUrl.IndexOf(",") + 1);
                byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                
                // Create texture
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                {
                    // Store texture
                    selectedCustomTexture = texture;
                    
                    // Convert to sprite
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    
                    // Show in the custom image slot (hides the sprite slot)
                    ShowCustomImage(sprite);
                    
                    // Update all MainMenuAvatarDisplay scripts
                    MainMenuAvatarDisplay[] displays = FindObjectsOfType<MainMenuAvatarDisplay>(true);
                    foreach (MainMenuAvatarDisplay display in displays)
                    {
                        if (display != null)
                        {
                            display.UpdateCustomImage(sprite);
                        }
                    }
                    
                    Debug.Log("[PlayerProfileUI] ✓ Custom image loaded from PlayFab and displayed in all locations");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerProfileUI] Failed to load custom image: {e.Message}");
            }
        }
    }
    
    #endregion
}

