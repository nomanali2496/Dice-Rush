using UnityEngine;

/// <summary>
/// Global repository for preset avatar and frame sprites.
/// PURE DATA CONTAINER: UI selection is now handled by EditProfilePanelManager.
/// </summary>
public class AvatarSelector : MonoBehaviour
{
    public static AvatarSelector Instance { get; private set; }
    
    [Header("Avatar Images (Avatars)")]
    [Tooltip("Array of preset avatar sprites")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Background Images (Frames)")]
    [Tooltip("Array of preset frame/background sprites")]
    [SerializeField] private Sprite[] backgroundSprites = new Sprite[8];
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // Public helper for other scripts to fetch sprites
    public Sprite GetAvatarSpriteByIndex(int index)
    {
        return (index >= 0 && index < avatarSprites.Length) ? avatarSprites[index] : null;
    }

    public Sprite GetBackgroundSpriteByIndex(int index)
    {
        return (index >= 0 && index < backgroundSprites.Length) ? backgroundSprites[index] : null;
    }
}
