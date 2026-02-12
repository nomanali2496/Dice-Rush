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
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI accountCashText; // Account cash display (aligned with other currency displays)
    [SerializeField] private TextMeshProUGUI accountCashTextShadow;
    [SerializeField] private Image profileImage;
    
    [Header("Gameplay UI References")]
    [SerializeField] private TextMeshProUGUI gameplayChipsText; // Chips display in gameplay screen
    [SerializeField] private TextMeshProUGUI gameplayCashText; // Cash display in gameplay screen
    
    [Header("Winner Screen UI References")]
    [SerializeField] private TextMeshProUGUI winnerScreenChipsText; // Chips display in winner screen
    [SerializeField] private TextMeshProUGUI winnerScreenCashText; // Cash display in winner screen
    
    [Header("Daily Reward UI References")]
    [SerializeField] private TextMeshProUGUI dailyRewardChipsText; // Chips display in daily reward panel
    [SerializeField] private TextMeshProUGUI dailyRewardCashText; // Cash display in daily reward panel
    
    [Header("1st Place Prize UI")]
    [SerializeField] private Image firstPlacePrizeIcon; // 1st place prize icon image (shows cash or chips sprite)
    [SerializeField] private TextMeshProUGUI firstPlacePrizeText; // 1st place prize text (shows cash or chips value)
    
    [Header("2nd Place Prize UI")]
    [SerializeField] private Image secondPlacePrizeIcon; // 2nd place prize icon image (shows cash or chips sprite)
    [SerializeField] private TextMeshProUGUI secondPlacePrizeText; // 2nd place prize text (shows cash or chips value)
    
    [Header("3rd Place Prize UI")]
    [SerializeField] private Image thirdPlacePrizeIcon; // 3rd place prize icon image (shows cash or chips sprite)
    [SerializeField] private TextMeshProUGUI thirdPlacePrizeText; // 3rd place prize text (shows cash or chips value)
    
    [Header("Buttons")]
    [SerializeField] private Button editNameButton; // Button to edit/save player name
    [SerializeField] private Button logoutButton;
    
    [Header("Custom Image Upload")]
    [Tooltip("Button to select custom image from device")]
    [SerializeField] private Button selectCustomImageButton;
    
    [Tooltip("Image component to display selected custom image")]
    [SerializeField] private Image customImageDisplay;
    
    [Tooltip("Image cropper UI component for cropping uploaded images")]
    [SerializeField] private ImageCropperUI imageCropperUI;
    
    [Header("Edit Mode Settings")]
    [SerializeField] private string editButtonText = "Edit";
    [SerializeField] private string saveButtonText = "Save";
    [SerializeField] private int maxImageSize = 512; // Max width/height for uploaded images
    
    [Header("Currency Formatting")]
    [SerializeField] private string cashPrefix = "Cash: $";
    [SerializeField] private string coinsPrefix = "Coins: ";
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
            Debug.Log($"[PlayerProfileUI] Formatted Cash: {FormatCash(data.cash)}, Formatted Coins: {FormatCoins(data.coins)}");
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
        Debug.Log($"[PlayerProfileUI] Formatting - Cash: {data.cash} -> {FormatCash(data.cash)}, Coins: {data.coins} -> {FormatCoins(data.coins)}");
        
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
        
        // Update cash (Main Menu) - cash is stored as float
        if (cashText != null)
        {
            cashText.text = cashPrefix + FormatCash(data.cash);
            Debug.Log($"[PlayerProfileUI] Set cashText to: {cashText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] cashText is null!");
        }
        
        // Update account cash display - cash is stored as float
        if (accountCashText != null)
        {
            accountCashText.text = cashPrefix + data.cash;
            accountCashTextShadow.text =  accountCashText.text;
        }
        
        // Update coins (Main Menu)
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + FormatCoins(data.coins);
            Debug.Log($"[PlayerProfileUI] Set coinsText to: {coinsText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] coinsText is null!");
        }
        
        // Update Gameplay UI
        UpdateGameplayCurrency(data.coins, data.cash);
        
        // Update Winner Screen UI
        UpdateWinnerScreenCurrency(data.coins, data.cash);
        
        // Update Daily Reward UI
        UpdateDailyRewardCurrency(data.coins, data.cash);
        
        // Update profile image
        if (profileImage == null)
        {
            Debug.LogWarning("[PlayerProfileUI] profileImage is not assigned in Inspector! Cannot display profile image.");
        }
        else if (!string.IsNullOrEmpty(data.imageUrl))
        {
            Debug.Log($"[PlayerProfileUI] Profile image URL found: {data.imageUrl}");
            
            // Check if it's a base64 image
            if (data.imageUrl.StartsWith("data:image"))
            {
                try
                {
                    // Extract base64 part
                    string base64Data = data.imageUrl.Substring(data.imageUrl.IndexOf(",") + 1);
                    byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                    
                    // Create texture
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        // Convert to sprite
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        profileImage.sprite = sprite;
                        Debug.Log("[PlayerProfileUI] ✓ Base64 profile image displayed");
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
            // Check if it's a URL (Facebook profile picture, etc.)
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
            // No image URL - load avatar sprite from selectedAvatarIndex (like MainMenuAvatarDisplay does)
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
        
        // Use formatting methods for consistency (even though 0 doesn't need formatting)
        if (cashText != null)
            cashText.text = cashPrefix + FormatCash(0f);
        
        if (accountCashText != null)
            accountCashText.text = cashPrefix + FormatCash(0f);
        
        if (coinsText != null)
            coinsText.text = coinsPrefix + FormatCoins(0);
        
        // Update Gameplay UI with default values
        UpdateGameplayCurrency(0, 0);
        
        // Update Winner Screen UI with default values
        UpdateWinnerScreenCurrency(0, 0);
        
        // Update Daily Reward UI with default values
        UpdateDailyRewardCurrency(0, 0);
    }
    
    /// <summary>
    /// Updates gameplay screen chips and cash displays - cash is stored as float
    /// </summary>
    private void UpdateGameplayCurrency(int chips, float cash)
    {
        if (gameplayChipsText != null)
        {
            gameplayChipsText.text = coinsPrefix + FormatCoins(chips);
        }
        
        if (gameplayCashText != null)
        {
            gameplayCashText.text = cashPrefix + FormatCash(cash);
        }
    }
    
    /// <summary>
    /// Updates winner screen chips and cash displays - cash is stored as float
    /// </summary>
    private void UpdateWinnerScreenCurrency(int chips, float cash)
    {
        if (winnerScreenChipsText != null)
        {
            winnerScreenChipsText.text = coinsPrefix + FormatCoins(chips);
        }
        
        if (winnerScreenCashText != null)
        {
            winnerScreenCashText.text = cashPrefix + FormatCash(cash);
        }
    }
    
    /// <summary>
    /// Updates daily reward panel chips and cash displays - cash is stored as float
    /// </summary>
    private void UpdateDailyRewardCurrency(int chips, float cash)
    {
        if (dailyRewardChipsText != null)
        {
            dailyRewardChipsText.text = coinsPrefix + FormatCoins(chips);
        }
        
        if (dailyRewardCashText != null)
        {
            dailyRewardCashText.text = cashPrefix + FormatCash(cash);
        }
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
    /// Load image from URL (for Facebook profile pictures, etc.)
    /// </summary>
    private System.Collections.IEnumerator LoadImageFromUrl(string url)
    {
        if (profileImage == null || string.IsNullOrEmpty(url))
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
                if (texture != null && profileImage != null)
                {
                    Sprite sprite = Sprite.Create(texture, 
                        new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f));
                    profileImage.sprite = sprite;
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
                // Fallback to avatar sprite if URL loading fails
                LoadAvatarSpriteFromIndex(PlayFabManager.Instance?.currentPlayerData?.selectedAvatarIndex ?? 0);
            }
        }
    }
    
    /// <summary>
    /// Load avatar sprite from selectedAvatarIndex (fallback when no imageUrl)
    /// Similar to MainMenuAvatarDisplay.LoadSelectedAvatar()
    /// </summary>
    private void LoadAvatarSpriteFromIndex(int selectedIndex)
    {
        if (profileImage == null)
        {
            Debug.LogWarning("[PlayerProfileUI] profileImage is not assigned! Cannot load avatar sprite.");
            return;
        }
        
        // Validate index
        if (selectedIndex < 0 || selectedIndex >= 8)
        {
            selectedIndex = 0;
            Debug.LogWarning($"[PlayerProfileUI] Invalid avatar index, using default 0");
        }
        
        Debug.Log($"[PlayerProfileUI] Loading avatar sprite from index: {selectedIndex}");
        
        // Try to get sprite from AvatarSelector first (it has the sprites)
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (selector != null)
        {
            Sprite avatarSprite = selector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                profileImage.sprite = avatarSprite;
                Debug.Log($"[PlayerProfileUI] ✓ Avatar sprite displayed from AvatarSelector: Index {selectedIndex}");
                return; // Success!
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
        
        // If we couldn't load from AvatarSelector, the sprite will remain as default
        // (which might be set in the Inspector)
        Debug.LogWarning($"[PlayerProfileUI] Could not load avatar sprite at index {selectedIndex}. Profile image will remain default.");
    }
    
    #region Public Helper Methods
    
    /// <summary>
    /// Manually force UI update with current PlayFab data
    /// Call this if UI is not updating automatically
    /// </summary>
    public void ForceUpdateUI()
    {
        Debug.Log("[PlayerProfileUI] ForceUpdateUI called manually");
        
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            Debug.Log("[PlayerProfileUI] Forcing UI update with current data");
            UpdateUI(PlayFabManager.Instance.currentPlayerData);
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] Cannot force update - no data available");
            RefreshPlayerData();
        }
    }
    
    /// <summary>
    /// Update displayed cash value (all screens) - cash is stored as float
    /// </summary>
    public void UpdateCashDisplay(float amount)
    {
        // Update Main Menu
        if (cashText != null)
        {
            cashText.text = cashPrefix + FormatCash(amount);
        }
        
        // Update Account Cash
        if (accountCashText != null)
        {
            accountCashText.text = cashPrefix + FormatCash(amount);
        }
        
        // Update Gameplay
        if (gameplayCashText != null)
        {
            gameplayCashText.text = cashPrefix + FormatCash(amount);
        }
        
        // Update Winner Screen
        if (winnerScreenCashText != null)
        {
            winnerScreenCashText.text = cashPrefix + FormatCash(amount);
        }
        
        // Update Daily Reward Panel
        if (dailyRewardCashText != null)
        {
            dailyRewardCashText.text = cashPrefix + FormatCash(amount);
        }
    }
    
    /// <summary>
    /// Update displayed coins value (all screens)
    /// </summary>
    public void UpdateCoinsDisplay(int amount)
    {
        // Update Main Menu
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + FormatCoins(amount);
        }
        
        // Update Gameplay
        if (gameplayChipsText != null)
        {
            gameplayChipsText.text = coinsPrefix + FormatCoins(amount);
        }
        
        // Update Winner Screen
        if (winnerScreenChipsText != null)
        {
            winnerScreenChipsText.text = coinsPrefix + FormatCoins(amount);
        }
        
        // Update Daily Reward Panel
        if (dailyRewardChipsText != null)
        {
            dailyRewardChipsText.text = coinsPrefix + FormatCoins(amount);
        }
    }
    
    /// <summary>
    /// Gets the 1st place prize icon image
    /// </summary>
    public Image GetFirstPlacePrizeIcon()
    {
        return firstPlacePrizeIcon;
    }
    
    /// <summary>
    /// Gets the 1st place prize text
    /// </summary>
    public TextMeshProUGUI GetFirstPlacePrizeText()
    {
        return firstPlacePrizeText;
    }
    
    /// <summary>
    /// Gets the 2nd place prize icon image
    /// </summary>
    public Image GetSecondPlacePrizeIcon()
    {
        return secondPlacePrizeIcon;
    }
    
    /// <summary>
    /// Gets the 2nd place prize text
    /// </summary>
    public TextMeshProUGUI GetSecondPlacePrizeText()
    {
        return secondPlacePrizeText;
    }
    
    /// <summary>
    /// Gets the 3rd place prize icon image
    /// </summary>
    public Image GetThirdPlacePrizeIcon()
    {
        return thirdPlacePrizeIcon;
    }
    
    /// <summary>
    /// Gets the 3rd place prize text
    /// </summary>
    public TextMeshProUGUI GetThirdPlacePrizeText()
    {
        return thirdPlacePrizeText;
    }
    
    /// <summary>
    /// Update all currency displays (chips and cash) across all screens
    /// </summary>
    public void UpdateAllCurrencyDisplays(int chips, float cash)
    {
        UpdateCoinsDisplay(chips);
        UpdateCashDisplay(cash);
    }
    
    /// <summary>
    /// Format cash value intelligently:
    /// - Values >= 1000: convert to "k" notation (e.g., "1k", "1.5k", "10k")
    /// - Values < 1000: 
    ///   - If 2 digits or less before decimal (e.g., 99.9): show decimals
    ///   - If 3+ digits before decimal (e.g., 999.9): round to integer (999)
    /// </summary>
    public static string FormatCash(float amount)
    {
        // Convert to "k" notation if >= 1000
        if (amount >= 1000f)
        {
            float kValue = amount / 1000f;
            // Check if it's a whole number in k (using epsilon for floating point comparison)
            if (Mathf.Abs(kValue - Mathf.RoundToInt(kValue)) < 0.001f)
            {
                return Mathf.RoundToInt(kValue).ToString() + "k";
            }
            else
            {
                // Show up to 1 decimal place for k notation, removing trailing zeros
                return kValue.ToString("0.#") + "k";
            }
        }
        
        // For values < 1000
        int wholeNumberPart = Mathf.FloorToInt(amount);
        
        // If 3+ digits before decimal point (>= 100), truncate to integer (don't round)
        if (wholeNumberPart >= 100)
        {
            return wholeNumberPart.ToString();
        }
        
        // If 2 digits or less before decimal point (< 100), show decimals if present
        // Check if it's a whole number
        if (Mathf.Abs(amount - Mathf.RoundToInt(amount)) < 0.001f)
        {
            // Whole number - show without decimals
            return Mathf.RoundToInt(amount).ToString();
        }
        else
        {
            // Has decimals - show up to 2 decimal places, removing trailing zeros
            return amount.ToString("0.##");
        }
    }
    
    /// <summary>
    /// Format coins/chips value intelligently:
    /// - Values >= 1000: convert to "k" notation (e.g., "1k", "1.5k", "10k")
    /// - Values < 1000: show as whole number
    /// </summary>
    public static string FormatCoins(int amount)
    {
        // Convert to "k" notation if >= 1000
        if (amount >= 1000)
        {
            float kValue = amount / 1000f;
            // Check if it's a whole number in k (using epsilon for floating point comparison)
            if (Mathf.Abs(kValue - Mathf.RoundToInt(kValue)) < 0.001f)
            {
                return Mathf.RoundToInt(kValue).ToString() + "k";
            }
            else
            {
                // Show up to 1 decimal place for k notation, removing trailing zeros
                return kValue.ToString("0.#") + "k";
            }
        }
        
        // For values < 1000, show as whole number
        return amount.ToString();
    }
    
    #endregion
    
    #region Custom Image Selection
    
    /// <summary>
    /// Called when select custom image button is clicked
    /// </summary>
    private void OnSelectCustomImageClicked()
    {
        Debug.Log("[PlayerProfileUI] Select custom image button clicked");
        
        // Use cross-platform image picker with crop functionality
        if (imageCropperUI != null)
        {
            ImagePickerHelper.PickImageWithCrop(OnImageSelected, imageCropperUI, maxImageSize);
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] ImageCropperUI not assigned! Using standard picker without crop.");
            ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
        }
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
        
        // Convert to sprite and display
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        
        // Update custom image display if assigned
        if (customImageDisplay != null)
        {
            customImageDisplay.sprite = sprite;
            Debug.Log("[PlayerProfileUI] Custom image displayed in customImageDisplay");
        }
        
        // Update profile image display
        if (profileImage != null)
        {
            profileImage.sprite = sprite;
            Debug.Log("[PlayerProfileUI] Custom image updated in profileImage");
        }
        
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
                    
                    // Update custom image display (if separate display exists)
                    if (customImageDisplay != null)
                    {
                        customImageDisplay.sprite = sprite;
                        Debug.Log("[PlayerProfileUI] Custom image loaded in customImageDisplay");
                    }
                    
                    // Update profile image display
                    if (profileImage != null)
                    {
                        profileImage.sprite = sprite;
                        Debug.Log("[PlayerProfileUI] ✓ Custom image loaded in profileImage");
                    }
                    
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

