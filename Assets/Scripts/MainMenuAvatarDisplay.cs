using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays selected avatar in main menu
/// Works even when AvatarSelector GameObject is inactive
/// </summary>
public class MainMenuAvatarDisplay : MonoBehaviour
{
    [Header("Avatar Display")]
    [Tooltip("Image component to show the selected avatar")]
    [SerializeField] private Image avatarImage;
    
    [Header("Avatar Sprites")]
    [Tooltip("Array of avatar sprites (must match AvatarSelector order)")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Settings")]
    [Tooltip("Auto-refresh when this GameObject becomes active")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    
    private void Start()
    {
        // If avatarImage not assigned, try to find it on this GameObject
        if (avatarImage == null)
        {
            avatarImage = GetComponent<Image>();
        }
        
        // Subscribe to PlayFab events to update when data loads
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
        
        // Try to load avatar immediately if data is available
        if (autoRefreshOnEnable)
        {
            // Wait a bit for PlayFab to initialize
            Invoke(nameof(LoadSelectedAvatar), 0.5f);
        }
    }
    
    private void OnEnable()
    {
        if (autoRefreshOnEnable)
        {
            // Refresh avatar when panel becomes active
            // Wait a bit to ensure PlayFab data is loaded
            Invoke(nameof(LoadSelectedAvatar), 0.3f);
        }
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
        // Update avatar when PlayFab data is loaded
        if (data != null)
        {
            Debug.Log("[MainMenuAvatarDisplay] PlayFab data loaded, updating avatar...");
            LoadSelectedAvatar();
        }
    }
    
    /// <summary>
    /// Load and display the selected avatar from PlayFab
    /// Works even if AvatarSelector GameObject is inactive
    /// </summary>
    public void LoadSelectedAvatar()
    {
        if (avatarImage == null)
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] Avatar Image not assigned!");
            return;
        }
        
        // First, check if there's a custom uploaded image
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                string imageUrl = PlayFabManager.Instance.currentPlayerData.imageUrl;
                
                // Check if it's a custom base64 image
                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("data:image"))
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
                            // Convert to sprite
                            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                            avatarImage.sprite = sprite;
                            Debug.Log("[MainMenuAvatarDisplay] ✓ Custom uploaded image displayed");
                            return; // Success!
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[MainMenuAvatarDisplay] Failed to load custom image: {e.Message}");
                    }
                }
            }
        }
        
        // If no custom image, load avatar sprite
        // Get the selected index from PlayFab
        int selectedIndex = 0;
        bool hasValidIndex = false;
        
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                selectedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
                // Check if this is a real saved value or just default 0
                // If player has a name, data is likely loaded
                if (!string.IsNullOrEmpty(PlayFabManager.Instance.currentPlayerData.playerName))
                {
                    hasValidIndex = true;
                    Debug.Log($"[MainMenuAvatarDisplay] ✓ Got avatar index from PlayFab: {selectedIndex} (player: {PlayFabManager.Instance.currentPlayerData.playerName})");
                }
                else
                {
                    Debug.LogWarning("[MainMenuAvatarDisplay] PlayerData exists but playerName is empty - data may not be fully loaded yet");
                }
            }
            else
            {
                Debug.LogWarning("[MainMenuAvatarDisplay] PlayerData is null! PlayFab data may not be loaded yet.");
            }
        }
        else
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] PlayFabManager not available or player not logged in!");
        }
        
        // If we don't have a valid index from PlayFab, try AvatarSelector
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (!hasValidIndex && selector != null)
        {
            selectedIndex = selector.GetSelectedAvatarIndex();
            hasValidIndex = true;
            Debug.Log($"[MainMenuAvatarDisplay] Got avatar index from AvatarSelector: {selectedIndex}");
        }
        else if (!hasValidIndex)
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] AvatarSelector not found, will use default index 0");
        }
        
        // Validate index
        if (selectedIndex < 0 || selectedIndex >= 8)
        {
            selectedIndex = 0;
            Debug.LogWarning($"[MainMenuAvatarDisplay] Invalid index, using default 0");
        }
        
        Debug.Log($"[MainMenuAvatarDisplay] Final selected index: {selectedIndex}, hasValidIndex: {hasValidIndex}");
        
        // Try to get sprite from AvatarSelector first (it has the sprites)
        if (selector != null)
        {
            // Get sprite by index directly (not by selected index)
            Sprite avatarSprite = selector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
                Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar displayed from AvatarSelector: Index {selectedIndex}");
                return; // Success!
            }
            else
            {
                Debug.LogWarning($"[MainMenuAvatarDisplay] Avatar sprite at index {selectedIndex} is null in AvatarSelector!");
            }
        }
        else
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] AvatarSelector not found in scene!");
        }
        
        // Fallback: Use local avatar sprites array
        if (avatarSprites != null && selectedIndex < avatarSprites.Length && avatarSprites[selectedIndex] != null)
        {
            avatarImage.sprite = avatarSprites[selectedIndex];
            Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar displayed from local sprites: Index {selectedIndex}");
        }
        else
        {
            Debug.LogError($"[MainMenuAvatarDisplay] Avatar sprite at index {selectedIndex} is null or not assigned! Make sure avatar sprites are assigned in Inspector.");
        }
    }
    
    /// <summary>
    /// Manually refresh the avatar display
    /// </summary>
    public void Refresh()
    {
        LoadSelectedAvatar();
    }
    
    /// <summary>
    /// Update avatar display immediately with provided index and sprites
    /// Called by AvatarSelector when selection changes
    /// </summary>
    public void UpdateFromAvatarSelector(int avatarIndex, Sprite[] sprites)
    {
        if (avatarImage == null)
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] Avatar Image not assigned!");
            return;
        }
        
        // Validate index
        if (avatarIndex < 0 || avatarIndex >= sprites.Length)
        {
            Debug.LogWarning($"[MainMenuAvatarDisplay] Invalid avatar index: {avatarIndex}");
            return;
        }
        
        // Update the display immediately
        if (sprites[avatarIndex] != null)
        {
            avatarImage.sprite = sprites[avatarIndex];
            Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar updated immediately to index {avatarIndex}");
        }
        else
        {
            Debug.LogWarning($"[MainMenuAvatarDisplay] Avatar sprite at index {avatarIndex} is null!");
        }
    }
    
    /// <summary>
    /// Try to update using AvatarSelector if it's available
    /// Falls back to PlayFabManager if AvatarSelector is inactive
    /// </summary>
    public void UpdateFromAvatarSelector()
    {
        // First try to use AvatarSelector's method (if it's active)
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (selector != null && selector.gameObject.activeSelf)
        {
            selector.UpdateMainMenuAvatarDisplay();
            Debug.Log("[MainMenuAvatarDisplay] Updated via AvatarSelector");
        }
        else
        {
            // Fallback to loading from PlayFabManager
            LoadSelectedAvatar();
            Debug.Log("[MainMenuAvatarDisplay] Updated via PlayFabManager (AvatarSelector inactive)");
        }
    }
    
    /// <summary>
    /// Update with custom uploaded image
    /// </summary>
    public void UpdateCustomImage(Sprite customSprite)
    {
        if (avatarImage != null && customSprite != null)
        {
            avatarImage.sprite = customSprite;
            Debug.Log("[MainMenuAvatarDisplay] Custom image updated");
        }
    }
}

