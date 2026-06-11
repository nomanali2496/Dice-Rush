using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles avatar and background selection with 8 buttons.
/// PURE FRONTEND: Handles button clicks and UI updates only.
/// Calls BackendAuthService for all network/data operations.
/// </summary>
public class AvatarSelector : MonoBehaviour
{
    public static AvatarSelector Instance { get; private set; }

    [Header("Selection Buttons (8 buttons)")]
    [SerializeField] private Button[] avatarButtons = new Button[8];
    
    [Header("Avatar Images (8 avatars)")]
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    
    [Header("Background Images (8 backgrounds)")]
    [SerializeField] private Sprite[] backgroundSprites = new Sprite[8];
    
    [Header("Button Highlight")]
    [SerializeField] private GameObject[] buttonHighlights = new GameObject[8];
    

    [Header("Watch Video UI")]
    [SerializeField] private TMP_Text watchVideoText;
    
    [Header("Settings")]
    [SerializeField] private bool showSelectionFeedback = true;
    
    private int currentSelectedIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Setup button listeners
        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i; 
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.AddListener(() => OnAvatarButtonClicked(index));
        }
        // Purely event-driven UI refresh
        PlayerModel.OnDataUpdated += RefreshUI;
        
        RefreshUI();
        SetupWatchVideoText();
    }
    
    private void OnDestroy() => PlayerModel.OnDataUpdated -= RefreshUI;
    private void OnEnable() => RefreshUI();

    /// <summary>
    /// Update UI based on PlayerModel state.
    /// No mixing: Networking is handled by asking BackendAuthService to fetch the image.
    /// </summary>
    public void RefreshUI()
    {
        string avatarData = PlayerModel.Avatar;

        // 1. Determine if we are showing a preset or a custom URL
        if (int.TryParse(avatarData, out int index))
        {
            currentSelectedIndex = index;
            ShowPreset(index);
        }
        else if (!string.IsNullOrEmpty(avatarData) && avatarData.StartsWith("http"))
        {
            currentSelectedIndex = -1;
            // ASYNC LOAD: We ask the backend for the image. UI doesn't know about UnityWebRequest.
            BackendAuthService.Instance.GetImage(avatarData, (texture) => {
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    ApplyCustomSprite(sprite);
                }
            });
        }
        else
        {
            ShowPreset(0);
        }
        
        UpdateButtonHighlights();
    }

    private void ShowPreset(int index)
    {
        if (index < 0 || index >= avatarSprites.Length) index = 0;
    }

    private void ApplyCustomSprite(Sprite sprite)
    {
    }

    private void OnAvatarButtonClicked(int index)
    {
        if (index < 0 || index >= avatarButtons.Length) return;
        
        // PURE UI: Just trigger the service call
        BackendAuthService.Instance.UpdateProfile(PlayerModel.Name, index.ToString());

        if (showSelectionFeedback) ShowSelectionFeedback(index);
    }

    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < buttonHighlights.Length; i++)
        {
            if (buttonHighlights[i] != null)
                buttonHighlights[i].SetActive(i == currentSelectedIndex);
        }
    }

    private void ShowSelectionFeedback(int index) { }

    private void SetupWatchVideoText()
    {
        if (watchVideoText != null)
            watchVideoText.text = "Watch Video and Earn 10   <voffset=0.35em><sprite name=\"chips 1\"></voffset>";
    }

    // Public helper for other scripts
    public Sprite GetAvatarSpriteByIndex(int index) => (index >= 0 && index < avatarSprites.Length) ? avatarSprites[index] : null;
    public Sprite GetBackgroundSpriteByIndex(int index) => (index >= 0 && index < backgroundSprites.Length) ? backgroundSprites[index] : null;
}
