using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles avatar and background selection with 8 buttons
/// Saves selection to PlayFab
/// </summary>
public class AvatarSelector : MonoBehaviour
{
    [Header("Selection Buttons (8 buttons)")]
    [SerializeField] private Button[] avatarButtons = new Button[8];
    
    [Header("Avatar Images (8 avatars)")]
    [Tooltip("The avatar images/sprites for each selection")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Background Images (8 backgrounds)")]
    [Tooltip("The background images/sprites for each selection")]
    [SerializeField] private Sprite[] backgroundSprites = new Sprite[8];
    
    [Header("Display References")]
    [Tooltip("Image component to display selected avatar")]
    [SerializeField] private Image selectedAvatarDisplay;
    
    [Tooltip("Image component to display selected background")]
    [SerializeField] private Image selectedBackgroundDisplay;
    
    [Header("Main Menu Display")]
    [Tooltip("Image component in main menu to show selected avatar")]
    [SerializeField] private Image mainMenuAvatarDisplay;
    
    [Header("Button Highlight")]
    [Tooltip("Optional: Visual indicator for selected button")]
    [SerializeField] private GameObject[] buttonHighlights = new GameObject[8];
    
    [Header("Custom Image Upload")]
    [Tooltip("Button to select custom image from device")]
    [SerializeField] private Button selectCustomImageButton;
    
    [Tooltip("Image component to display selected custom image")]
    [SerializeField] private Image customImageDisplay;
    
    [Header("Settings")]
    [SerializeField] private bool saveToPlayFabOnSelect = true;
    [SerializeField] private bool showSelectionFeedback = true;
    [SerializeField] private int maxImageSize = 512; // Max width/height for uploaded images
    
    private int currentSelectedIndex = 0;
    private Texture2D selectedCustomTexture;
    
    private void Start()
    {
        // Setup button listeners
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i; // Capture index for closure
            if (avatarButtons[i] != null)
            {
                avatarButtons[i].onClick.AddListener(() => OnAvatarButtonClicked(index));
            }
        }
        
        // Setup custom image button
        if (selectCustomImageButton != null)
        {
            selectCustomImageButton.onClick.AddListener(OnSelectCustomImageClicked);
        }
        
        // Subscribe to PlayFab events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
        
        // Load saved selection from PlayFab
        LoadSavedSelection();
        
        // Load custom image if exists
        LoadCustomImage();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        }
    }
    
    private void OnPlayerDataLoaded(PlayerData data)
    {
        // Refresh selection when PlayFab data is loaded
        // But don't update main menu (it's already showing correct avatar)
        if (data != null)
        {
            Debug.Log("[AvatarSelector] PlayFab data loaded, refreshing selection (without updating main menu)...");
            LoadSavedSelectionWithoutMainMenuUpdate();
        }
    }
    
    /// <summary>
    /// Called when an avatar button is clicked
    /// </summary>
    private void OnAvatarButtonClicked(int index)
    {
        if (index < 0 || index >= avatarButtons.Length)
        {
            Debug.LogWarning($"[AvatarSelector] Invalid button index: {index}");
            return;
        }
        
        Debug.Log($"[AvatarSelector] Avatar button {index} clicked");
        
        // IMPORTANT: Clear custom image when selecting a sprite avatar
        // This ensures the sprite is shown instead of the custom image
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            // Clear the custom image URL so sprite selection takes priority
            PlayFabManager.Instance.currentPlayerData.imageUrl = "";
            selectedCustomTexture = null; // Clear the texture reference
            Debug.Log("[AvatarSelector] Cleared custom image - sprite selection will be used");
        }
        
        // Update selection
        currentSelectedIndex = index;
        
        // Update UI
        UpdateSelectionDisplay();
        
        // Save to PlayFab
        if (saveToPlayFabOnSelect)
        {
            SaveSelectionToPlayFab();
        }
        
        // Show feedback
        if (showSelectionFeedback)
        {
            ShowSelectionFeedback(index);
        }
    }
    
    /// <summary>
    /// Update the display to show selected avatar and background
    /// </summary>
    private void UpdateSelectionDisplay()
    {
        // Update avatar display
        if (selectedAvatarDisplay != null && currentSelectedIndex < avatarSprites.Length)
        {
            if (avatarSprites[currentSelectedIndex] != null)
            {
                selectedAvatarDisplay.sprite = avatarSprites[currentSelectedIndex];
                Debug.Log($"[AvatarSelector] Updated avatar display to index {currentSelectedIndex}");
            }
        }
        
        // Update background display
        if (selectedBackgroundDisplay != null && currentSelectedIndex < backgroundSprites.Length)
        {
            if (backgroundSprites[currentSelectedIndex] != null)
            {
                selectedBackgroundDisplay.sprite = backgroundSprites[currentSelectedIndex];
                Debug.Log($"[AvatarSelector] Updated background display to index {currentSelectedIndex}");
            }
        }
        
        // Update main menu avatar display
        UpdateMainMenuAvatar();
        
        // Update button highlights
        UpdateButtonHighlights();
    }
    
    /// <summary>
    /// Update the main menu avatar display
    /// </summary>
    private void UpdateMainMenuAvatar()
    {
        // Update direct reference (if assigned)
        UpdateMainMenuAvatarDisplay();
        
        // Update all MainMenuAvatarDisplay scripts in scene (immediate update)
        UpdateMainMenuAvatarDisplayViaScript();
    }
    
    /// <summary>
    /// Public method to update main menu avatar display (via direct reference)
    /// </summary>
    public void UpdateMainMenuAvatarDisplay()
    {
        if (mainMenuAvatarDisplay == null)
        {
            return; // No main menu display assigned
        }
        
        // Use current selected index (immediate update)
        int avatarIndex = currentSelectedIndex;
        
        // Validate index
        if (avatarIndex < 0 || avatarIndex >= avatarSprites.Length)
        {
            avatarIndex = 0; // Default to first avatar
        }
        
        // Update the display immediately
        if (avatarSprites[avatarIndex] != null)
        {
            mainMenuAvatarDisplay.sprite = avatarSprites[avatarIndex];
            Debug.Log($"[AvatarSelector] Updated main menu avatar display (direct) to index {avatarIndex}");
        }
        else
        {
            Debug.LogWarning($"[AvatarSelector] Avatar sprite at index {avatarIndex} is null!");
        }
    }
    
    /// <summary>
    /// Update main menu avatar via MainMenuAvatarDisplay script
    /// This finds and updates all MainMenuAvatarDisplay scripts in the scene immediately
    /// </summary>
    private void UpdateMainMenuAvatarDisplayViaScript()
    {
        // Find all MainMenuAvatarDisplay scripts in the scene (even if inactive)
        MainMenuAvatarDisplay[] displays = FindObjectsOfType<MainMenuAvatarDisplay>(true);
        
        if (displays != null && displays.Length > 0)
        {
            foreach (MainMenuAvatarDisplay display in displays)
            {
                if (display != null)
                {
                    // Update immediately with current selected index and sprites
                    display.UpdateFromAvatarSelector(currentSelectedIndex, avatarSprites);
                    Debug.Log($"[AvatarSelector] Updated MainMenuAvatarDisplay script immediately with index {currentSelectedIndex}");
                }
            }
        }
    }
    
    /// <summary>
    /// Update button highlight indicators
    /// </summary>
    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < buttonHighlights.Length; i++)
        {
            if (buttonHighlights[i] != null)
            {
                // Show highlight only on selected button
                buttonHighlights[i].SetActive(i == currentSelectedIndex);
            }
        }
    }
    
    /// <summary>
    /// Show visual feedback when selection is made
    /// </summary>
    private void ShowSelectionFeedback(int index)
    {
        // Optional: Add animation, sound, or visual effect here
        Debug.Log($"[AvatarSelector] Selection feedback for index {index}");
        
        // Example: Scale animation on button
        if (avatarButtons[index] != null)
        {
            // You can add animation here if needed
            // LeanTween.scale(avatarButtons[index].gameObject, Vector3.one * 1.1f, 0.2f)
            //     .setEase(LeanTweenType.easeOutBack);
        }
    }
    
    /// <summary>
    /// Save selection to PlayFab
    /// </summary>
    private void SaveSelectionToPlayFab()
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[AvatarSelector] PlayFabManager not found! Cannot save selection.");
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[AvatarSelector] Player not logged in! Cannot save selection.");
            return;
        }
        
        Debug.Log($"[AvatarSelector] Saving selection to PlayFab - Avatar: {currentSelectedIndex}, Background: {currentSelectedIndex}");
        
        // Update player data
        if (PlayFabManager.Instance.currentPlayerData != null)
        {
            PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex = currentSelectedIndex;
            PlayFabManager.Instance.currentPlayerData.selectedBackgroundIndex = currentSelectedIndex;
            
            // IMPORTANT: Clear custom image URL when saving sprite selection
            // This ensures sprite takes priority over custom image
            if (!string.IsNullOrEmpty(PlayFabManager.Instance.currentPlayerData.imageUrl) && 
                PlayFabManager.Instance.currentPlayerData.imageUrl.StartsWith("data:image"))
            {
                PlayFabManager.Instance.currentPlayerData.imageUrl = "";
                selectedCustomTexture = null;
                Debug.Log("[AvatarSelector] Cleared custom image URL - sprite selection will be used");
            }
            
            // Save to PlayFab
            PlayFabManager.Instance.SavePlayerData(() =>
            {
                Debug.Log($"[AvatarSelector] ✓ Selection saved successfully to PlayFab!");
            });
        }
        else
        {
            Debug.LogError("[AvatarSelector] PlayerData is null! Cannot save selection.");
        }
    }
    
    /// <summary>
    /// Load saved selection from PlayFab
    /// </summary>
    private void LoadSavedSelection()
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[AvatarSelector] PlayFabManager not found! Cannot load selection.");
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[AvatarSelector] Player not logged in! Cannot load selection.");
            return;
        }
        
        if (PlayFabManager.Instance.currentPlayerData != null)
        {
            // Use avatar index, or default to 0
            int savedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
            
            // Validate index
            if (savedIndex >= 0 && savedIndex < avatarButtons.Length)
            {
                currentSelectedIndex = savedIndex;
                Debug.Log($"[AvatarSelector] Loaded saved selection: Index {savedIndex}");
            }
            else
            {
                currentSelectedIndex = 0;
                Debug.Log($"[AvatarSelector] Invalid saved index ({savedIndex}), using default (0)");
            }
            
            // Update display with loaded selection
            UpdateSelectionDisplay();
        }
        else
        {
            Debug.LogWarning("[AvatarSelector] PlayerData is null! Using default selection.");
            currentSelectedIndex = 0;
            UpdateSelectionDisplay();
        }
    }
    
    /// <summary>
    /// Manually refresh selection from PlayFab
    /// Call this after PlayFab data is loaded
    /// </summary>
    public void RefreshSelection()
    {
        LoadSavedSelection();
    }
    
    /// <summary>
    /// Get currently selected avatar index
    /// </summary>
    public int GetSelectedAvatarIndex()
    {
        return currentSelectedIndex;
    }
    
    /// <summary>
    /// Get currently selected background index
    /// </summary>
    public int GetSelectedBackgroundIndex()
    {
        return currentSelectedIndex;
    }
    
    /// <summary>
    /// Get selected avatar sprite
    /// </summary>
    public Sprite GetSelectedAvatarSprite()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < avatarSprites.Length)
        {
            return avatarSprites[currentSelectedIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Get selected background sprite
    /// </summary>
    public Sprite GetSelectedBackgroundSprite()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < backgroundSprites.Length)
        {
            return backgroundSprites[currentSelectedIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Get avatar sprites array (for MainMenuAvatarDisplay to use)
    /// </summary>
    public Sprite[] GetAvatarSprites()
    {
        return avatarSprites;
    }
    
    /// <summary>
    /// Get avatar sprite by index (for MainMenuAvatarDisplay to use)
    /// </summary>
    public Sprite GetAvatarSpriteByIndex(int index)
    {
        if (index >= 0 && index < avatarSprites.Length)
        {
            return avatarSprites[index];
        }
        return null;
    }
    
    /// <summary>
    /// Programmatically select an avatar (without saving)
    /// </summary>
    public void SelectAvatar(int index, bool saveToPlayFab = false)
    {
        if (index < 0 || index >= avatarButtons.Length)
        {
            Debug.LogWarning($"[AvatarSelector] Invalid index: {index}");
            return;
        }
        
        currentSelectedIndex = index;
        UpdateSelectionDisplay();
        
        if (saveToPlayFab)
        {
            SaveSelectionToPlayFab();
        }
    }
    
    private void OnEnable()
    {
        // Refresh selection when this panel becomes active
        // BUT don't update main menu display (it's already showing correct avatar)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            // First load custom image if it exists
            LoadCustomImage();
            // Then load sprite selection
            Invoke(nameof(LoadSavedSelectionWithoutMainMenuUpdate), 0.1f);
        }
    }
    
    /// <summary>
    /// Load saved selection without updating main menu (to avoid resetting it)
    /// </summary>
    private void LoadSavedSelectionWithoutMainMenuUpdate()
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[AvatarSelector] PlayFabManager not found! Cannot load selection.");
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[AvatarSelector] Player not logged in! Cannot load selection.");
            return;
        }
        
        if (PlayFabManager.Instance.currentPlayerData != null)
        {
            // Use avatar index, or default to 0
            int savedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
            
            // Validate index
            if (savedIndex >= 0 && savedIndex < avatarButtons.Length)
            {
                currentSelectedIndex = savedIndex;
                Debug.Log($"[AvatarSelector] Loaded saved selection: Index {savedIndex} (without updating main menu)");
            }
            else
            {
                currentSelectedIndex = 0;
                Debug.Log($"[AvatarSelector] Invalid saved index ({savedIndex}), using default (0)");
            }
            
            // Update local displays only (NOT main menu)
            UpdateLocalDisplaysOnly();
        }
        else
        {
            Debug.LogWarning("[AvatarSelector] PlayerData is null! Using default selection.");
            currentSelectedIndex = 0;
            UpdateLocalDisplaysOnly();
        }
    }
    
    /// <summary>
    /// Update only local displays (avatar selector panel), NOT main menu
    /// </summary>
    private void UpdateLocalDisplaysOnly()
    {
        // FIRST: Check if there's a custom uploaded image - if yes, show that instead of sprite
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                string imageUrl = PlayFabManager.Instance.currentPlayerData.imageUrl;
                
                // Check if it's a custom base64 image
                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("data:image"))
                {
                    // Custom image exists - make sure it's loaded and displayed
                    if (selectedCustomTexture != null)
                    {
                        // Convert to sprite if needed
                        Sprite customSprite = Sprite.Create(selectedCustomTexture, 
                            new Rect(0, 0, selectedCustomTexture.width, selectedCustomTexture.height), 
                            new Vector2(0.5f, 0.5f));
                        
                        // Update selected avatar display with custom image
                        if (selectedAvatarDisplay != null)
                        {
                            selectedAvatarDisplay.sprite = customSprite;
                            Debug.Log("[AvatarSelector] Showing custom image in selectedAvatarDisplay");
                        }
                        
                        // Update background display (in selector panel)
                        if (selectedBackgroundDisplay != null && currentSelectedIndex < backgroundSprites.Length)
                        {
                            if (backgroundSprites[currentSelectedIndex] != null)
                            {
                                selectedBackgroundDisplay.sprite = backgroundSprites[currentSelectedIndex];
                            }
                        }
                        
                        // Update button highlights
                        UpdateButtonHighlights();
                        
                        // DO NOT update main menu here - it's already showing the correct avatar!
                        return; // Exit early - custom image is displayed
                    }
                    else
                    {
                        // Custom image exists in PlayFab but texture not loaded - load it
                        LoadCustomImage();
                        return; // LoadCustomImage will update the display
                    }
                }
            }
        }
        
        // No custom image, show sprite selection
        // Update avatar display (in selector panel)
        if (selectedAvatarDisplay != null && currentSelectedIndex < avatarSprites.Length)
        {
            if (avatarSprites[currentSelectedIndex] != null)
            {
                selectedAvatarDisplay.sprite = avatarSprites[currentSelectedIndex];
                Debug.Log($"[AvatarSelector] Showing sprite at index {currentSelectedIndex} in selectedAvatarDisplay");
            }
        }
        
        // Update background display (in selector panel)
        if (selectedBackgroundDisplay != null && currentSelectedIndex < backgroundSprites.Length)
        {
            if (backgroundSprites[currentSelectedIndex] != null)
            {
                selectedBackgroundDisplay.sprite = backgroundSprites[currentSelectedIndex];
            }
        }
        
        // Update button highlights
        UpdateButtonHighlights();
        
        // DO NOT update main menu here - it's already showing the correct avatar!
    }
    
    /// <summary>
    /// Update main menu avatar display
    /// Call this from main menu script to refresh the display
    /// </summary>
    public void RefreshMainMenuAvatar()
    {
        UpdateMainMenuAvatar();
    }
    
    #region Custom Image Selection
    
    /// <summary>
    /// Called when select custom image button is clicked
    /// </summary>
    private void OnSelectCustomImageClicked()
    {
        Debug.Log("[AvatarSelector] Select custom image button clicked");
        
        // Use cross-platform image picker
        ImagePickerHelper.PickImage(OnImageSelected, maxImageSize);
    }
    
    /// <summary>
    /// Called when image is selected
    /// </summary>
    private void OnImageSelected(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("[AvatarSelector] Selected texture is null!");
            return;
        }
        
        selectedCustomTexture = texture;
        
        // Convert to sprite and display
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        
        // Update display
        if (customImageDisplay != null)
        {
            customImageDisplay.sprite = sprite;
            Debug.Log("[AvatarSelector] Custom image displayed");
        }
        
        // Update main menu display if assigned
        if (mainMenuAvatarDisplay != null)
        {
            mainMenuAvatarDisplay.sprite = sprite;
            Debug.Log("[AvatarSelector] Custom image updated in main menu");
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
            Debug.LogWarning("[AvatarSelector] PlayFabManager not found! Cannot save image.");
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[AvatarSelector] Player not logged in! Cannot save image.");
            return;
        }
        
        // Convert texture to PNG bytes
        byte[] imageBytes = texture.EncodeToPNG();
        
        // Convert to base64 string
        string base64Image = System.Convert.ToBase64String(imageBytes);
        
        Debug.Log($"[AvatarSelector] Saving custom image to PlayFab (size: {imageBytes.Length} bytes, base64 length: {base64Image.Length})");
        
        // Update player data
        if (PlayFabManager.Instance.currentPlayerData != null)
        {
            // Store as base64 string in imageUrl field
            PlayFabManager.Instance.currentPlayerData.imageUrl = "data:image/png;base64," + base64Image;
            
            // Save to PlayFab
            PlayFabManager.Instance.SavePlayerData(() =>
            {
                Debug.Log("[AvatarSelector] ✓ Custom image saved successfully to PlayFab!");
            });
        }
        else
        {
            Debug.LogError("[AvatarSelector] PlayerData is null! Cannot save image.");
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
                        Debug.Log("[AvatarSelector] Custom image loaded in customImageDisplay");
                    }
                    
                    // IMPORTANT: Update selected avatar display (the main display in selector panel)
                    if (selectedAvatarDisplay != null)
                    {
                        selectedAvatarDisplay.sprite = sprite;
                        Debug.Log("[AvatarSelector] ✓ Custom image loaded in selectedAvatarDisplay");
                    }
                    
                    // Update main menu display
                    if (mainMenuAvatarDisplay != null)
                    {
                        mainMenuAvatarDisplay.sprite = sprite;
                        Debug.Log("[AvatarSelector] Custom image loaded in mainMenuAvatarDisplay");
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
                    
                    Debug.Log("[AvatarSelector] ✓ Custom image loaded from PlayFab and displayed in all locations");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AvatarSelector] Failed to load custom image: {e.Message}");
            }
        }
    }
    
    #endregion
}

