using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays selected avatar in main menu
/// Works even when AvatarSelector GameObject is inactive
/// </summary>
public class MainMenuAvatarDisplay : MonoBehaviour
{
    [Header("Avatar Display")]
    [Tooltip("Image shown when a PRESET avatar is selected (from the avatar buttons)")]
    [SerializeField] private Image avatarImageSprite;
    
    [Tooltip("Image shown when a CUSTOM photo is picked from the gallery — size this larger than the mask in the Inspector for the zoom effect")]
    [SerializeField] private Image avatarImageCustom;
    
    [Header("Avatar Sprites")]
    [Tooltip("Array of avatar sprites (must match AvatarSelector order)")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Settings")]
    [Tooltip("Auto-refresh when this GameObject becomes active")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    
    private void Start()
    {
        // Hide the custom image slot by default — sprite slot is shown until a custom image is loaded
        if (avatarImageCustom != null)
            avatarImageCustom.gameObject.SetActive(false);
        if (avatarImageSprite != null)
            avatarImageSprite.gameObject.SetActive(true);
        
        // If neither slot is assigned, try to find Image on this GameObject as fallback
        if (avatarImageSprite == null && avatarImageCustom == null)
        {
            avatarImageSprite = GetComponent<Image>();
            if (avatarImageSprite != null)
                Debug.Log("[MainMenuAvatarDisplay] Auto-assigned avatarImageSprite from GameObject");
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
    
    private bool isLoadingUrlImage = false; // Track if we're currently loading a URL image
    private string currentImageUrl = ""; // Track the current image URL being loaded/displayed
    
    private void OnPlayerDataLoaded(PlayerData data)
    {
        if (data != null)
        {
            Debug.Log("[MainMenuAvatarDisplay] PlayFab data loaded, updating avatar...");
            
            if (isLoadingUrlImage)
            {
                Debug.Log("[MainMenuAvatarDisplay] URL image is currently loading, skipping reload");
                return;
            }
            
            // If same URL is already displayed in the sprite slot, skip
            if (!string.IsNullOrEmpty(data.imageUrl) && 
                (data.imageUrl.StartsWith("http://") || data.imageUrl.StartsWith("https://")) &&
                data.imageUrl == currentImageUrl && avatarImageSprite != null && avatarImageSprite.sprite != null)
            {
                Debug.Log("[MainMenuAvatarDisplay] Same URL image already displayed, skipping reload");
                return;
            }
            
            LoadSelectedAvatar();
        }
    }
    
    /// <summary>
    /// Load and display the selected avatar from PlayFab
    /// Works even if AvatarSelector GameObject is inactive
    /// </summary>
    public void LoadSelectedAvatar()
    {
        // Check for custom uploaded or URL image first
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                string imageUrl = PlayFabManager.Instance.currentPlayerData.imageUrl;
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Base64 custom image — show in custom slot
                    if (imageUrl.StartsWith("data:image"))
                    {
                        try
                        {
                            string base64Data = imageUrl.Substring(imageUrl.IndexOf(",") + 1);
                            byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                            Texture2D texture = new Texture2D(2, 2);
                            if (texture.LoadImage(imageBytes))
                            {
                                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                                ShowCustomAvatar(sprite);
                                Debug.Log("[MainMenuAvatarDisplay] ✓ Custom uploaded image displayed in custom slot");
                                return;
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[MainMenuAvatarDisplay] Failed to load custom image: {e.Message}");
                        }
                    }
                    // URL image (Facebook/Google) — show in sprite slot, no zoom
                    else if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                    {
                        if (imageUrl != currentImageUrl)
                        {
                            Debug.Log($"[MainMenuAvatarDisplay] Loading image from URL: {imageUrl}");
                            currentImageUrl = imageUrl;
                            isLoadingUrlImage = true;
                            StartCoroutine(LoadImageFromUrl(imageUrl));
                            return;
                        }
                        else
                        {
                            Debug.Log("[MainMenuAvatarDisplay] Same URL image already loaded, skipping");
                            return;
                        }
                    }
                }
            }
        }
        
        // No custom image — load preset avatar sprite
        int selectedIndex = 0;
        bool hasValidIndex = false;
        
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                selectedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
                if (!string.IsNullOrEmpty(PlayFabManager.Instance.currentPlayerData.playerName))
                {
                    hasValidIndex = true;
                    Debug.Log($"[MainMenuAvatarDisplay] ✓ Got avatar index from PlayFab: {selectedIndex}");
                }
                else
                {
                    Debug.LogWarning("[MainMenuAvatarDisplay] PlayerData exists but playerName is empty");
                }
            }
            else
            {
                Debug.LogWarning("[MainMenuAvatarDisplay] PlayerData is null!");
            }
        }
        else
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] PlayFabManager not available or not logged in!");
        }
        
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (!hasValidIndex && selector != null)
        {
            selectedIndex = selector.GetSelectedAvatarIndex();
            hasValidIndex = true;
            Debug.Log($"[MainMenuAvatarDisplay] Got avatar index from AvatarSelector: {selectedIndex}");
        }
        else if (!hasValidIndex)
        {
            Debug.LogWarning("[MainMenuAvatarDisplay] AvatarSelector not found, using default index 0");
        }
        
        if (selectedIndex < 0 || selectedIndex >= 8)
        {
            selectedIndex = 0;
            Debug.LogWarning($"[MainMenuAvatarDisplay] Invalid index, using default 0");
        }
        
        Debug.Log($"[MainMenuAvatarDisplay] Final selected index: {selectedIndex}");
        
        // Try AvatarSelector sprites first
        if (selector != null)
        {
            Sprite avatarSprite = selector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                ShowSpriteAvatar(avatarSprite);
                Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar displayed from AvatarSelector: Index {selectedIndex}");
                return;
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
        
        // Fallback: local sprites array
        if (avatarSprites != null && selectedIndex < avatarSprites.Length && avatarSprites[selectedIndex] != null)
        {
            ShowSpriteAvatar(avatarSprites[selectedIndex]);
            Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar displayed from local sprites: Index {selectedIndex}");
        }
        else
        {
            Debug.LogError($"[MainMenuAvatarDisplay] Avatar sprite at index {selectedIndex} is null or not assigned!");
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
        if (avatarIndex < 0 || avatarIndex >= sprites.Length)
        {
            Debug.LogWarning($"[MainMenuAvatarDisplay] Invalid avatar index: {avatarIndex}");
            return;
        }
        
        if (sprites[avatarIndex] != null)
        {
            // Preset sprite selected from avatar buttons
            ShowSpriteAvatar(sprites[avatarIndex]);
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
    /// Show a custom gallery-picked image: activates avatarImageCustom, hides avatarImageSprite.
    /// </summary>
    private void ShowCustomAvatar(Sprite sprite)
    {
        if (avatarImageCustom != null)
        {
            avatarImageCustom.sprite = sprite;
            avatarImageCustom.rectTransform.localScale = Vector3.one * 2f; // 2x zoom to fill the mask
            avatarImageCustom.gameObject.SetActive(true);
        }
        if (avatarImageSprite != null)
            avatarImageSprite.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Show a preset avatar sprite: activates avatarImageSprite, hides avatarImageCustom.
    /// </summary>
    private void ShowSpriteAvatar(Sprite sprite)
    {
        if (avatarImageSprite != null)
        {
            avatarImageSprite.sprite = sprite;
            avatarImageSprite.rectTransform.localScale = Vector3.one; // ensure no leftover zoom
            avatarImageSprite.gameObject.SetActive(true);
        }
        if (avatarImageCustom != null)
            avatarImageCustom.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Gets the current avatar sprite being displayed
    /// </summary>
    public Sprite GetCurrentAvatarSprite()
    {
        // Return whichever slot is active
        if (avatarImageCustom != null && avatarImageCustom.gameObject.activeSelf)
            return avatarImageCustom.sprite;
        if (avatarImageSprite != null)
            return avatarImageSprite.sprite;
        return null;
    }
    
    /// <summary>
    /// Update with custom uploaded image — shows in the custom slot
    /// </summary>
    public void UpdateCustomImage(Sprite customSprite)
    {
        if (customSprite != null)
        {
            ShowCustomAvatar(customSprite);
            Debug.Log("[MainMenuAvatarDisplay] Custom image updated in custom slot");
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
        
        Debug.Log($"[MainMenuAvatarDisplay] Starting to load image from URL: {url}");
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                if (texture != null)
                {
                    // URL image (Facebook/Google) — show in sprite slot, no zoom
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ShowSpriteAvatar(sprite);
                    Debug.Log($"[MainMenuAvatarDisplay] ✓ Image loaded from URL successfully! Size: {texture.width}x{texture.height}");
                    isLoadingUrlImage = false;
                }
                else
                {
                    Debug.LogWarning("[MainMenuAvatarDisplay] Failed to create texture from downloaded image");
                    isLoadingUrlImage = false;
                    LoadAvatarSpriteFallback();
                }
            }
            else
            {
                Debug.LogWarning($"[MainMenuAvatarDisplay] Failed to load image from URL: {url}. Error: {www.error}");
                isLoadingUrlImage = false;
                LoadAvatarSpriteFallback();
            }
        }
    }
    
    /// <summary>
    /// Load avatar sprite (fallback when URL image fails or no URL available)
    /// </summary>
    private void LoadAvatarSpriteFallback()
    {
        int selectedIndex = 0;
        bool hasValidIndex = false;
        
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                selectedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
                if (!string.IsNullOrEmpty(PlayFabManager.Instance.currentPlayerData.playerName))
                    hasValidIndex = true;
            }
        }
        
        AvatarSelector selector = FindObjectOfType<AvatarSelector>();
        if (!hasValidIndex && selector != null)
        {
            selectedIndex = selector.GetSelectedAvatarIndex();
            hasValidIndex = true;
        }
        
        if (selectedIndex < 0 || selectedIndex >= 8)
            selectedIndex = 0;
        
        if (selector != null)
        {
            Sprite avatarSprite = selector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                ShowSpriteAvatar(avatarSprite);
                Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar sprite displayed (fallback): Index {selectedIndex}");
                return;
            }
        }
        
        if (avatarSprites != null && selectedIndex < avatarSprites.Length && avatarSprites[selectedIndex] != null)
        {
            ShowSpriteAvatar(avatarSprites[selectedIndex]);
            Debug.Log($"[MainMenuAvatarDisplay] ✓ Avatar sprite displayed from local array (fallback): Index {selectedIndex}");
        }
    }
}

