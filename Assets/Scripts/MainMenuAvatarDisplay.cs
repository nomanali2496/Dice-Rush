using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays selected avatar in main menu.
/// PURE FRONTEND: No networking code, no UnityWebRequest.
/// Uses BackendAuthService.Instance.GetImage to fetch external URLs.
/// </summary>
public class MainMenuAvatarDisplay : MonoBehaviour
{
    [Header("Avatar Display")]
    [SerializeField] private Image avatarImageSprite;
    [SerializeField] private Image avatarImageCustom;
    
    [Header("Avatar Sprites")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Settings")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    
    private string lastAvatarData = "";

    private void Start()
    {
        PlayerModel.OnDataUpdated += Refresh;
        Refresh();
    }
    
    private void OnEnable()
    {
        if (autoRefreshOnEnable) Refresh();
    }
    
    private void OnDestroy() => PlayerModel.OnDataUpdated -= Refresh;

    /// <summary>
    /// Update display based on PlayerModel data.
    /// Purely asks BackendAuthService for URL images to avoid mixing logic.
    /// </summary>
    public void Refresh()
    {
        string avatarData = PlayerModel.Avatar;

        // Skip redundant updates
        if (avatarData == lastAvatarData && GetCurrentAvatarSprite() != null) return;
        lastAvatarData = avatarData;

        if (string.IsNullOrEmpty(avatarData))
        {
            ShowPreset(0);
        }
        else if (avatarData.StartsWith("http"))
        {
            // PURE UI: Ask service to download the image. UI doesn't handle the request.
            BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ShowCustom(sprite);
                }
            });
        }
        else if (int.TryParse(avatarData, out int index))
        {
            ShowPreset(index);
        }
        else
        {
            ShowPreset(0);
        }
    }

    private void ShowPreset(int index)
    {
        if (index < 0 || index >= avatarSprites.Length) index = 0;
        
        Sprite sprite = avatarSprites[index];
        if (avatarImageSprite != null)
        {
            avatarImageSprite.sprite = sprite;
            avatarImageSprite.rectTransform.localScale = Vector3.one;
            avatarImageSprite.gameObject.SetActive(true);
        }
        if (avatarImageCustom != null) avatarImageCustom.gameObject.SetActive(false);
        
        NotifyLeaderboard();
    }

    private void ShowCustom(Sprite sprite)
    {
        if (avatarImageCustom != null)
        {
            avatarImageCustom.sprite = sprite;
            avatarImageCustom.rectTransform.localScale = Vector3.one * 2f; // Zoom effect
            avatarImageCustom.gameObject.SetActive(true);
        }
        if (avatarImageSprite != null) avatarImageSprite.gameObject.SetActive(false);
        
        NotifyLeaderboard();
    }

    // Callbacks for immediate updates from other scripts
    public void UpdateFromAvatarSelector(int avatarIndex, Sprite[] sprites) => ShowPreset(avatarIndex);
    public void UpdateCustomImage(Sprite customSprite) => ShowCustom(customSprite);

    public Sprite GetCurrentAvatarSprite()
    {
        if (avatarImageCustom != null && avatarImageCustom.gameObject.activeSelf) return avatarImageCustom.sprite;
        if (avatarImageSprite != null) return avatarImageSprite.sprite;
        return null;
    }

    public bool IsShowingCustomAvatar()
    {
        return avatarImageCustom != null && avatarImageCustom.gameObject.activeSelf;
    }

    private void NotifyLeaderboard()
    {
        LeaderboardManager[] managers = FindObjectsOfType<LeaderboardManager>(true);
        foreach (var lbm in managers)
        {
            if (lbm != null) lbm.SyncAvatarFromMainMenu();
        }
    }
}
