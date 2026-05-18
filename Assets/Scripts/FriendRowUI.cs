using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Data class to hold friend row information
/// </summary>
[System.Serializable]
public class FriendRowData
{
    public string friendName;
    public string friendId;
    public Sprite avatarSprite;
    public bool isOnline;
    public string avatarKey;
    
    public FriendRowData(string friendName, string friendId, Sprite avatarSprite = null, bool isOnline = false, string avatarKey = "")
    {
        this.friendName = friendName;
        this.friendId = friendId;
        this.avatarSprite = avatarSprite;
        this.isOnline = isOnline;
        this.avatarKey = avatarKey;
    }
}

/// <summary>
/// Manages the UI display for a single friend row
/// Works with Enhanced Scroller for efficient scrolling
/// </summary>
public class FriendRowUI : EnhancedScrollerCellView
{
    [Header("Avatar")]
    [SerializeField] private Image avatarImage; // Friend avatar image
    
    [Header("Name Text")]
    [SerializeField] private TextMeshProUGUI nameText; // Friend name text component
    
    [Header("ID Text")]
    [SerializeField] private TextMeshProUGUI idText; // Friend ID text component
    
    [Header("Online/Offline Status")]
    [SerializeField] private Image statusImage; // Single image to show online/offline status
    [SerializeField] private Sprite onlineSprite; // Sprite to show when friend is online
    [SerializeField] private Sprite offlineSprite; // Sprite to show when friend is offline
    
    [Header("Line Separator")]
    [SerializeField] private RectTransform lineImage; // Line separator image
    
    private FriendRowData currentData;
    private static FriendRowScaler cachedScaler; // Cache the scaler to avoid FindObjectOfType every time
    
    /// <summary>
    /// Sets the data for this cell view (called by Enhanced Scroller)
    /// </summary>
    /// <param name="data">The friend row data</param>
    public void SetData(FriendRowData data)
    {
        currentData = data;
        
        // Apply scaling every time (Enhanced Scroller recycles cells)
        ApplyScaling();
        
        SetupRow(data.friendName, data.friendId, data.avatarSprite, data.isOnline, data.avatarKey);
    }
    
    /// <summary>
    /// Applies scaling to all row elements using FriendRowScaler
    /// </summary>
    private void ApplyScaling()
    {
        // Find or use cached FriendRowScaler
        if (cachedScaler == null)
        {
            cachedScaler = FindObjectOfType<FriendRowScaler>();
        }
        
        if (cachedScaler != null)
        {
            // Get RectTransforms
            RectTransform profileRect = avatarImage != null ? avatarImage.GetComponent<RectTransform>() : null;
            RectTransform statusRect = statusImage != null ? statusImage.GetComponent<RectTransform>() : null;
            
            // Scale all elements
            cachedScaler.ScaleRowElements(profileRect, statusRect, nameText, idText, lineImage);
        }
        else
        {
            Debug.LogWarning("[FriendRowUI] FriendRowScaler not found in scene! Elements will not be scaled.");
        }
    }
    
    /// <summary>
    /// Sets up the friend row with the specified name, ID, avatar, and online status
    /// This method can still be called directly if not using Enhanced Scroller
    /// </summary>
    /// <param name="friendName">Friend's display name</param>
    /// <param name="friendId">Friend's ID</param>
    /// <param name="avatarSprite">Friend's avatar sprite (optional)</param>
    /// <param name="isOnline">Whether the friend is online</param>
    /// <param name="avatarKey">The remote or preset key for the avatar</param>
    public void SetupRow(string friendName, string friendId, Sprite avatarSprite = null, bool isOnline = false, string avatarKey = "")
    {
        // Setup avatar
        if (avatarImage != null)
        {
            if (!string.IsNullOrEmpty(avatarKey) && avatarKey.StartsWith("http"))
            {
                avatarImage.sprite = null; // Clear old/stale
                avatarImage.enabled = true;

                if (BackendAuthService.Instance != null)
                {
                    string targetKey = avatarKey;
                    BackendAuthService.Instance.GetImage(targetKey, (tex) => {
                        if (tex != null && this != null && currentData != null && currentData.avatarKey == targetKey)
                        {
                            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            avatarImage.sprite = s;
                            avatarImage.enabled = true;
                        }
                    });
                }
            }
            else if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
                avatarImage.enabled = true;
            }
            else
            {
                avatarImage.sprite = null;
                avatarImage.enabled = false;
            }
        }
        
        // Setup name text
        if (nameText != null)
        {
            nameText.text = friendName;
        }
        
        // Setup ID text
        if (idText != null)
        {
            idText.text = friendId;
        }
        
        // Setup online/offline status
        SetupOnlineStatus(isOnline);
    }
    
    /// <summary>
    /// Sets up the online/offline status display
    /// </summary>
    private void SetupOnlineStatus(bool isOnline)
    {
        if (statusImage != null)
        {
            if (isOnline)
            {
                // Show online sprite
                if (onlineSprite != null)
                {
                    statusImage.sprite = onlineSprite;
                    statusImage.enabled = true;
                }
                else
                {
                    statusImage.enabled = false;
                }
            }
            else
            {
                // Show offline sprite
                if (offlineSprite != null)
                {
                    statusImage.sprite = offlineSprite;
                    statusImage.enabled = true;
                }
                else
                {
                    statusImage.enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// Updates only the avatar without changing other properties
    /// </summary>
    public void UpdateAvatar(Sprite newAvatar)
    {
        if (avatarImage != null)
        {
            if (newAvatar != null)
            {
                avatarImage.sprite = newAvatar;
                avatarImage.enabled = true;
            }
            else
            {
                avatarImage.sprite = null;
                avatarImage.enabled = false;
            }
        }
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.avatarSprite = newAvatar;
        }
    }
    
    /// <summary>
    /// Updates only the name without changing other properties
    /// </summary>
    public void UpdateName(string newName)
    {
        if (nameText != null)
        {
            nameText.text = newName;
        }
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.friendName = newName;
        }
    }
    
    /// <summary>
    /// Updates only the ID without changing other properties
    /// </summary>
    public void UpdateId(string newId)
    {
        if (idText != null)
        {
            idText.text = newId;
        }
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.friendId = newId;
        }
    }
    
    /// <summary>
    /// Updates only the online status without changing other properties
    /// </summary>
    public void UpdateOnlineStatus(bool isOnline)
    {
        SetupOnlineStatus(isOnline);
        
        // Update current data if exists
        if (currentData != null)
        {
            currentData.isOnline = isOnline;
        }
    }
    
    /// <summary>
    /// Gets the current data for this row
    /// </summary>
    public FriendRowData GetCurrentData()
    {
        return currentData;
    }
    
    /// <summary>
    /// Gets whether the friend is currently online
    /// </summary>
    public bool IsOnline()
    {
        return currentData != null ? currentData.isOnline : false;
    }
}

