using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Data class to hold leaderboard row information
/// </summary>
[System.Serializable]
public class LeaderboardRowData
{
    public int position;
    public string username;
    public int score;
    public Sprite avatarSprite;
    public bool isCustom;
    public string avatarKey; // URL or preset key from backend
    
    public LeaderboardRowData(int position, string username, int score, Sprite avatarSprite = null, bool isCustom = false, string avatarKey = "")
    {
        this.position = position;
        this.username = username;
        this.score = score;
        this.avatarSprite = avatarSprite;
        this.isCustom = isCustom;
        this.avatarKey = avatarKey;
    }
}

/// <summary>
/// Manages the UI display for a single leaderboard row
/// Handles different visual styles for top 3 positions (1st, 2nd, 3rd) and normal rows
/// Works with Enhanced Scroller for efficient scrolling
/// </summary>
public class LeaderboardRowUI : EnhancedScrollerCellView
{
    [Header("Background Images")]
    [SerializeField] private Image rowBackground; // Main row background image
    [SerializeField] public Sprite firstPlaceBackground; // Background for 1st place
    [SerializeField] public Sprite secondPlaceBackground; // Background for 2nd place
    [SerializeField] public Sprite thirdPlaceBackground; // Background for 3rd place
    [SerializeField] public Sprite normalRowBackground; // Background for 4th place and onwards
    
    [Header("Profile Background Images")]
    [SerializeField] private Image profileBackground; // Profile picture background/frame
    [SerializeField] public Sprite firstPlaceProfileBackground; // Profile background for 1st place
    [SerializeField] public Sprite secondPlaceProfileBackground; // Profile background for 2nd place
    [SerializeField] public Sprite thirdPlaceProfileBackground; // Profile background for 3rd place
    [SerializeField] public Sprite normalRowProfileBackground; // Profile background for normal rows
    
    [Header("Avatar")]
    [SerializeField] private Image avatarImage; // Player avatar image
    [SerializeField] private Image avatarImageCustom; // Player avatar image (custom/zoomed)
    
    [Header("Username Text")]
    [SerializeField] private TextMeshProUGUI usernameText; // Username text component
    [SerializeField] private Color firstPlaceUsernameColor = Color.white; // Username color for 1st place
    [SerializeField] private Color secondPlaceUsernameColor = Color.white; // Username color for 2nd place
    [SerializeField] private Color thirdPlaceUsernameColor = Color.white; // Username color for 3rd place
    [SerializeField] private Color normalRowUsernameColor = Color.white; // Username color for normal rows
    
    [Header("Score Text")]
    [SerializeField] private TextMeshProUGUI scoreText; // Score text component
    [SerializeField] private Color firstPlaceScoreColor = Color.white; // Score color for 1st place
    [SerializeField] private Color secondPlaceScoreColor = Color.white; // Score color for 2nd place
    [SerializeField] private Color thirdPlaceScoreColor = Color.white; // Score color for 3rd place
    [SerializeField] private Color normalRowScoreColor = Color.white; // Score color for normal rows
    
    [Header("Position Display")]
    [SerializeField] private Image badgeImage; // Badge image for positions 1, 2, 3
    [SerializeField] private TextMeshProUGUI positionText; // Position text for positions 4 and onwards
    [SerializeField] public Sprite firstPlaceBadge; // Badge sprite for 1st place
    [SerializeField] public Sprite secondPlaceBadge; // Badge sprite for 2nd place
    [SerializeField] public Sprite thirdPlaceBadge; // Badge sprite for 3rd place
    
    [Header("Arrow Image")]
    [SerializeField] private GameObject arrowImage; // Arrow image (only active for 1st place)
    
    private int currentPosition = -1;
    private LeaderboardRowData currentData;
    
    /// <summary>
    /// Sets the data for this cell view (called by Enhanced Scroller)
    /// </summary>
    /// <param name="data">The leaderboard row data</param>
    public void SetData(LeaderboardRowData data)
    {
        currentData = data;
        SetupRow(data.position, data.username, data.score, data.avatarSprite, data.isCustom, data.avatarKey);
    }
    
    /// <summary>
    /// Sets up the leaderboard row with the specified position, username, score, and avatar
    /// </summary>
    public void SetupRow(int position, string username, int score, Sprite avatarSprite = null, bool isCustom = false, string avatarKey = "")
    {
        currentPosition = position;
        
        // Setup backgrounds based on position
        SetupBackgrounds(position);
        
        // Setup profile background based on position
        SetupProfileBackground(position);
        
        // Setup avatar display
        if (!string.IsNullOrEmpty(avatarKey) && avatarKey.StartsWith("http"))
        {
            if (avatarImageCustom != null)
            {
                avatarImageCustom.sprite = null; // Clear old/reused cell sprite
                avatarImageCustom.gameObject.SetActive(true);
                
                // Fetch the remote image asynchronously
                if (BackendAuthService.Instance != null)
                {
                    string targetKey = avatarKey; // Closure-local copy
                    BackendAuthService.Instance.GetImage(targetKey, (tex) => {
                        if (tex != null && this != null && currentData != null && currentData.avatarKey == targetKey)
                        {
                            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            avatarImageCustom.sprite = s;
                            avatarImageCustom.rectTransform.localScale = Vector3.one * 2f;
                        }
                    });
                }
            }
            if (avatarImage != null)
                avatarImage.gameObject.SetActive(false);
        }
        else if (isCustom)
        {
            if (avatarImageCustom != null)
            {
                avatarImageCustom.sprite = avatarSprite;
                avatarImageCustom.rectTransform.localScale = Vector3.one * 2f; // Zoom effect for custom images
                avatarImageCustom.gameObject.SetActive(true);
            }
            if (avatarImage != null)
                avatarImage.gameObject.SetActive(false);
        }
        else
        {
            if (avatarImage != null)
            {
                avatarImage.sprite = avatarSprite;
                avatarImage.rectTransform.localScale = Vector3.one; // No zoom for presets
                avatarImage.gameObject.SetActive(true);
            }
            if (avatarImageCustom != null)
                avatarImageCustom.gameObject.SetActive(false);
        }
        
        // Setup username text
        if (usernameText != null)
        {
            usernameText.text = username;
            usernameText.color = GetUsernameColor(position);
        }
        
        // Setup score text
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
            scoreText.color = GetScoreColor(position);
        }
        
        // Setup position display (badge for top 3, text for others)
        SetupPositionDisplay(position);
        
        // Setup arrow image (only for 1st place)
        SetupArrowImage(position);
    }
    
    /// <summary>
    /// Sets up the row background based on position
    /// </summary>
    private void SetupBackgrounds(int position)
    {
        if (rowBackground == null) return;
        
        switch (position)
        {
            case 1:
                if (firstPlaceBackground != null)
                    rowBackground.sprite = firstPlaceBackground;
                break;
            case 2:
                if (secondPlaceBackground != null)
                    rowBackground.sprite = secondPlaceBackground;
                break;
            case 3:
                if (thirdPlaceBackground != null)
                    rowBackground.sprite = thirdPlaceBackground;
                break;
            default:
                if (normalRowBackground != null)
                    rowBackground.sprite = normalRowBackground;
                break;
        }
    }
    
    /// <summary>
    /// Sets up the profile background based on position
    /// </summary>
    private void SetupProfileBackground(int position)
    {
        if (profileBackground == null) return;
        
        switch (position)
        {
            case 1:
                if (firstPlaceProfileBackground != null)
                    profileBackground.sprite = firstPlaceProfileBackground;
                break;
            case 2:
                if (secondPlaceProfileBackground != null)
                    profileBackground.sprite = secondPlaceProfileBackground;
                break;
            case 3:
                if (thirdPlaceProfileBackground != null)
                    profileBackground.sprite = thirdPlaceProfileBackground;
                break;
            default:
                if (normalRowProfileBackground != null)
                    profileBackground.sprite = normalRowProfileBackground;
                break;
        }
    }
    
    /// <summary>
    /// Gets the username color based on position
    /// </summary>
    private Color GetUsernameColor(int position)
    {
        switch (position)
        {
            case 1:
                return firstPlaceUsernameColor;
            case 2:
                return secondPlaceUsernameColor;
            case 3:
                return thirdPlaceUsernameColor;
            default:
                return normalRowUsernameColor;
        }
    }
    
    /// <summary>
    /// Gets the score color based on position
    /// </summary>
    private Color GetScoreColor(int position)
    {
        switch (position)
        {
            case 1:
                return firstPlaceScoreColor;
            case 2:
                return secondPlaceScoreColor;
            case 3:
                return thirdPlaceScoreColor;
            default:
                return normalRowScoreColor;
        }
    }
    
    /// <summary>
    /// Sets up position display - badge for top 3, text for others
    /// </summary>
    private void SetupPositionDisplay(int position)
    {
        if (position <= 3)
        {
            // Show badge, hide text
            if (badgeImage != null)
            {
                badgeImage.gameObject.SetActive(true);
                
                switch (position)
                {
                    case 1:
                        if (firstPlaceBadge != null)
                            badgeImage.sprite = firstPlaceBadge;
                        break;
                    case 2:
                        if (secondPlaceBadge != null)
                            badgeImage.sprite = secondPlaceBadge;
                        break;
                    case 3:
                        if (thirdPlaceBadge != null)
                            badgeImage.sprite = thirdPlaceBadge;
                        break;
                }
            }
            
            if (positionText != null)
            {
                positionText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Show text, hide badge
            if (badgeImage != null)
            {
                badgeImage.gameObject.SetActive(false);
            }
            
            if (positionText != null)
            {
                positionText.gameObject.SetActive(true);
                positionText.text = position.ToString();
                positionText.color = normalRowUsernameColor;
            }
        }
    }
    
    /// <summary>
    /// Sets up arrow image - only active for 1st place
    /// </summary>
    private void SetupArrowImage(int position)
    {
        if (arrowImage != null)
        {
            arrowImage.SetActive(position == 1);
        }
    }
    
    /// <summary>
    /// Gets the current position of this row
    /// </summary>
    public int GetCurrentPosition()
    {
        return currentPosition;
    }
    
    /// <summary>
    /// Updates only the score without changing other properties
    /// </summary>
    public void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = newScore.ToString();
        }
    }
    
    /// <summary>
    /// Updates only the username without changing other properties
    /// </summary>
    public void UpdateUsername(string newUsername)
    {
        if (usernameText != null)
        {
            usernameText.text = newUsername;
        }
    }
    
    /// <summary>
    /// Updates only the avatar without changing other properties
    /// </summary>
    public void UpdateAvatar(Sprite newAvatar)
    {
        if (newAvatar == null) return;

        bool isCustom = currentData != null && currentData.isCustom;

        if (isCustom)
        {
            if (avatarImageCustom != null)
            {
                avatarImageCustom.sprite = newAvatar;
                avatarImageCustom.rectTransform.localScale = Vector3.one * 2f;
                avatarImageCustom.gameObject.SetActive(true);
            }
            if (avatarImage != null) avatarImage.gameObject.SetActive(false);
        }
        else
        {
            if (avatarImage != null)
            {
                avatarImage.sprite = newAvatar;
                avatarImage.rectTransform.localScale = Vector3.one;
                avatarImage.gameObject.SetActive(true);
            }
            if (avatarImageCustom != null) avatarImageCustom.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Gets the current data for this row
    /// </summary>
    public LeaderboardRowData GetCurrentData()
    {
        return currentData;
    }
}

