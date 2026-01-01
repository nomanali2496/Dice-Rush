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
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Image profileImage;
    
    [Header("Gameplay UI References")]
    [SerializeField] private TextMeshProUGUI gameplayChipsText; // Chips display in gameplay screen
    [SerializeField] private TextMeshProUGUI gameplayCashText; // Cash display in gameplay screen
    
    [Header("Winner Screen UI References")]
    [SerializeField] private TextMeshProUGUI winnerScreenChipsText; // Chips display in winner screen
    [SerializeField] private TextMeshProUGUI winnerScreenCashText; // Cash display in winner screen
    
    [Header("Buttons")]
    [SerializeField] private Button editNameButton; // Button to edit/save player name
    [SerializeField] private Button logoutButton;
    
    [Header("Edit Mode Settings")]
    [SerializeField] private string editButtonText = "Edit";
    [SerializeField] private string saveButtonText = "Save";
    
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
    
    private void Start()
    {
        // Add button listeners
        if (editNameButton != null)
            editNameButton.onClick.AddListener(OnEditNameClicked);
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
        
        // Setup input field - start in read-only mode
        SetEditMode(false);
    }
    
    private void OnEnable()
    {
        Debug.Log("[PlayerProfileUI] ===== PANEL ENABLED =====");
        
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
        
        // Subscribe to future events
        PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
        
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
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] emailText is null!");
        }
        
        // Update cash (Main Menu) - cash is stored as float
        if (cashText != null)
        {
            cashText.text = cashPrefix + data.cash.ToString("F2");
            Debug.Log($"[PlayerProfileUI] Set cashText to: {cashText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] cashText is null!");
        }
        
        // Update coins (Main Menu)
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + data.coins.ToString("N0");
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
        
        // Update profile image
        if (profileImage != null && !string.IsNullOrEmpty(data.imageUrl))
        {
            // If you want to load the image from URL, you'll need to implement that
            // For now, we'll just log it
            Debug.Log($"[PlayerProfileUI] Profile image URL: {data.imageUrl}");
            // StartCoroutine(LoadImageFromUrl(data.imageUrl));
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
        
        if (cashText != null)
            cashText.text = cashPrefix + "0";
        
        if (coinsText != null)
            coinsText.text = coinsPrefix + "0";
        
        // Update Gameplay UI with default values
        UpdateGameplayCurrency(0, 0);
        
        // Update Winner Screen UI with default values
        UpdateWinnerScreenCurrency(0, 0);
    }
    
    /// <summary>
    /// Updates gameplay screen chips and cash displays - cash is stored as float
    /// </summary>
    private void UpdateGameplayCurrency(int chips, float cash)
    {
        if (gameplayChipsText != null)
        {
            gameplayChipsText.text = coinsPrefix + chips.ToString("N0");
        }
        
        if (gameplayCashText != null)
        {
            gameplayCashText.text = cashPrefix + cash.ToString("F1");
        }
    }
    
    /// <summary>
    /// Updates winner screen chips and cash displays - cash is stored as float
    /// </summary>
    private void UpdateWinnerScreenCurrency(int chips, float cash)
    {
        if (winnerScreenChipsText != null)
        {
            winnerScreenChipsText.text = coinsPrefix + chips.ToString("N0");
        }
        
        if (winnerScreenCashText != null)
        {
            winnerScreenCashText.text = cashPrefix + cash.ToString("F1");
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
    
    // Optional: Load image from URL
    /*
    private System.Collections.IEnumerator LoadImageFromUrl(string url)
    {
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        
        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
            if (profileImage != null)
            {
                profileImage.sprite = Sprite.Create(texture, 
                    new Rect(0, 0, texture.width, texture.height), 
                    new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            Debug.LogError("Failed to load profile image: " + request.error);
        }
    }
    */
    
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
            cashText.text = cashPrefix + amount.ToString("F2");
        }
        
        // Update Gameplay
        if (gameplayCashText != null)
        {
            gameplayCashText.text = cashPrefix + amount.ToString("F2");
        }
        
        // Update Winner Screen
        if (winnerScreenCashText != null)
        {
            winnerScreenCashText.text = cashPrefix + amount.ToString("F2");
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
            coinsText.text = coinsPrefix + amount.ToString("N0");
        }
        
        // Update Gameplay
        if (gameplayChipsText != null)
        {
            gameplayChipsText.text = coinsPrefix + amount.ToString("N0");
        }
        
        // Update Winner Screen
        if (winnerScreenChipsText != null)
        {
            winnerScreenChipsText.text = coinsPrefix + amount.ToString("N0");
        }
    }
    
    /// <summary>
    /// Update all currency displays (chips and cash) across all screens
    /// </summary>
    public void UpdateAllCurrencyDisplays(int chips, float cash)
    {
        UpdateCoinsDisplay(chips);
        UpdateCashDisplay(cash);
    }
    
    #endregion
}

