using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

/// <summary>
/// Controls the "Choose Your Mode" panel in Dice Rush / Dice Blitz.
/// Tailored for the simple hierarchy where each Mode item (Classic, Zombie, Snake) is its own Button & Image.
/// </summary>
public class ChooseYourModeManager : MonoBehaviour
{
    public enum GameMode
    {
        Classic,
        Zombie,
        Snake
    }

    public static ChooseYourModeManager Instance { get; private set; }

    /// <summary>
    /// Currently selected game mode across the application
    /// </summary>
    public static GameMode SelectedMode { get; private set; } = GameMode.Classic;

    /// <summary>
    /// Event fired whenever the selected game mode changes
    /// </summary>
    public static event Action<GameMode> OnGameModeSelected;

    [Header("Panel References")]
    [Tooltip("Root GameObject for the Choose Your Mode panel (defaults to this GameObject if null)")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Back button to close this panel and return to Main Menu")]
    [SerializeField] private Button backButton;

    [Header("Main Menu Play Button")]
    [Tooltip("Play button on the Main Menu screen that opens this panel (Auto-wired if assigned)")]
    [SerializeField] private Button mainMenuPlayButton;

    [Header("--- CLASSIC MODE ---")]
    [SerializeField] private bool isClassicUnlocked = true;
    [Tooltip("The 'Classic' GameObject with Button component in Hierarchy")]
    [SerializeField] private Button classicButton;
    [SerializeField] private Sprite classicUnlockedSprite;
    [SerializeField] private Sprite classicLockedSprite;

    [Header("--- ZOMBIE MODE ---")]
    [SerializeField] private bool isZombieUnlocked = false; // Locked by default
    [Tooltip("The 'Zombie' GameObject with Button component in Hierarchy")]
    [SerializeField] private Button zombieButton;
    [SerializeField] private Sprite zombieUnlockedSprite;
    [SerializeField] private Sprite zombieLockedSprite;

    [Header("--- SNAKE MODE ---")]
    [SerializeField] private bool isSnakeUnlocked = false; // Locked by default
    [Tooltip("The 'Snake' GameObject with Button component in Hierarchy")]
    [SerializeField] private Button snakeButton;
    [SerializeField] private Sprite snakeUnlockedSprite;
    [SerializeField] private Sprite snakeLockedSprite;

    [Header("Target Panels")]
    [Tooltip("Reference to SelectPlayerPanelController (opened when an unlocked mode is selected)")]
    [SerializeField] private SelectPlayerPanelController selectPlayerPanelController;

    [Tooltip("Reference to HorizontalLobbyManager (optional fallback)")]
    [SerializeField] private HorizontalLobbyManager lobbyManager;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private CanvasGroup canvasGroup;
    private RectTransform panelRectTransform;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        panelRectTransform = panelRoot.GetComponent<RectTransform>();
        canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null && panelRoot != null)
        {
            canvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        if (panelRectTransform != null)
        {
            originalScale = panelRectTransform.localScale;
        }
    }

    private void Start()
    {
        // Wire back button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // Wire Main Menu Play button
        if (mainMenuPlayButton != null)
        {
            mainMenuPlayButton.onClick.RemoveAllListeners();
            mainMenuPlayButton.onClick.AddListener(OpenPanel);
        }

        // Wire mode buttons (the entire Classic/Zombie/Snake GameObjects are buttons)
        if (classicButton != null)
        {
            classicButton.onClick.RemoveAllListeners();
            classicButton.onClick.AddListener(OnClassicModeClicked);
        }

        if (zombieButton != null)
        {
            zombieButton.onClick.RemoveAllListeners();
            zombieButton.onClick.AddListener(OnZombieModeClicked);
        }

        if (snakeButton != null)
        {
            snakeButton.onClick.RemoveAllListeners();
            snakeButton.onClick.AddListener(OnSnakeModeClicked);
        }

        // Auto-assign controllers if missing
        if (selectPlayerPanelController == null)
        {
            selectPlayerPanelController = FindFirstObjectByType<SelectPlayerPanelController>(FindObjectsInactive.Include);
        }

        if (lobbyManager == null)
        {
            lobbyManager = FindFirstObjectByType<HorizontalLobbyManager>(FindObjectsInactive.Include);
        }

        UpdateModeVisuals();
    }

    /// <summary>
    /// Updates mode button sprites based on unlocked/locked state
    /// </summary>
    public void UpdateModeVisuals()
    {
        UpdateModeButtonVisual(classicButton, isClassicUnlocked, classicUnlockedSprite, classicLockedSprite);
        UpdateModeButtonVisual(zombieButton, isZombieUnlocked, zombieUnlockedSprite, zombieLockedSprite);
        UpdateModeButtonVisual(snakeButton, isSnakeUnlocked, snakeUnlockedSprite, snakeLockedSprite);
    }

    private void UpdateModeButtonVisual(Button button, bool isUnlocked, Sprite unlockedSprite, Sprite lockedSprite)
    {
        if (button == null) return;

        Image img = button.image != null ? button.image : button.GetComponent<Image>();
        if (img != null)
        {
            Sprite targetSprite = isUnlocked ? unlockedSprite : lockedSprite;
            if (targetSprite != null)
            {
                img.sprite = targetSprite;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateModeVisuals();
    }
#endif

    /// <summary>
    /// Opens the Choose Your Mode panel with animation and sound
    /// </summary>
    public void OpenPanel()
    {
        PlayTapSound();

        if (panelRoot == null) return;

        // Ensure SelectPlayerPanel is hidden when ChooseYourMode panel opens
        if (selectPlayerPanelController == null)
        {
            selectPlayerPanelController = FindFirstObjectByType<SelectPlayerPanelController>(FindObjectsInactive.Include);
        }
        if (selectPlayerPanelController != null)
        {
            selectPlayerPanelController.HidePanel();
        }

        UpdateModeVisuals();
        panelRoot.SetActive(true);

        if (useAnimation && panelRectTransform != null)
        {
            panelRectTransform.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();

            panelRectTransform.localScale = originalScale * 0.8f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            panelRectTransform.DOScale(originalScale, animationDuration).SetEase(showEase).SetUpdate(true);
            if (canvasGroup != null) canvasGroup.DOFade(1f, animationDuration).SetUpdate(true);
        }
        else
        {
            if (panelRectTransform != null) panelRectTransform.localScale = originalScale;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        Debug.Log("[ChooseYourModeManager] Panel Opened.");
    }

    /// <summary>
    /// Closes the Choose Your Mode panel
    /// </summary>
    public void ClosePanel()
    {
        PlayTapSound();

        if (panelRoot == null || !panelRoot.activeSelf) return;

        if (useAnimation && panelRectTransform != null)
        {
            panelRectTransform.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();

            panelRectTransform.DOScale(originalScale * 0.8f, animationDuration).SetEase(hideEase).SetUpdate(true);
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, animationDuration).SetUpdate(true).OnComplete(() =>
                {
                    panelRoot.SetActive(false);
                    panelRectTransform.localScale = originalScale;
                });
            }
            else
            {
                panelRoot.SetActive(false);
                panelRectTransform.localScale = originalScale;
            }
        }
        else
        {
            panelRoot.SetActive(false);
        }

        Debug.Log("[ChooseYourModeManager] Panel Closed.");
    }

    public void OnBackButtonClicked()
    {
        ClosePanel();
    }

    public void OnClassicModeClicked()
    {
        if (!isClassicUnlocked)
        {
            ShowLockedNotification("Classic Mode");
            return;
        }

        SelectMode(GameMode.Classic);
    }

    public void OnZombieModeClicked()
    {
        if (!isZombieUnlocked)
        {
            ShowLockedNotification("Zombie Mode");
            return;
        }

        SelectMode(GameMode.Zombie);
    }

    public void OnSnakeModeClicked()
    {
        if (!isSnakeUnlocked)
        {
            ShowLockedNotification("Snake Mode");
            return;
        }

        SelectMode(GameMode.Snake);
    }

    private void ShowLockedNotification(string modeName)
    {
        PlayTapSound();
        Debug.Log($"[ChooseYourModeManager] {modeName} is currently locked (Coming Soon).");

        NotificationPanel notification = FindFirstObjectByType<NotificationPanel>(FindObjectsInactive.Include);
        if (notification != null)
        {
            notification.Show($"{modeName} is coming soon! Stay tuned.");
        }
    }

    private void SelectMode(GameMode mode)
    {
        PlayTapSound();

        SelectedMode = mode;
        Debug.Log($"[ChooseYourModeManager] Mode Selected: {mode}");

        OnGameModeSelected?.Invoke(mode);

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (selectPlayerPanelController == null)
        {
            selectPlayerPanelController = FindFirstObjectByType<SelectPlayerPanelController>(FindObjectsInactive.Include);
        }

        if (selectPlayerPanelController != null)
        {
            selectPlayerPanelController.ShowPanel();
        }
        else if (lobbyManager != null)
        {
            lobbyManager.OnLobbyOpened();
        }
        else
        {
            Debug.LogWarning("[ChooseYourModeManager] Neither SelectPlayerPanelController nor HorizontalLobbyManager found to proceed!");
        }
    }

    /// <summary>
    /// Dynamically unlock or lock a mode at runtime
    /// </summary>
    public void SetModeUnlocked(GameMode mode, bool unlocked)
    {
        switch (mode)
        {
            case GameMode.Classic:
                isClassicUnlocked = unlocked;
                break;
            case GameMode.Zombie:
                isZombieUnlocked = unlocked;
                break;
            case GameMode.Snake:
                isSnakeUnlocked = unlocked;
                break;
        }

        UpdateModeVisuals();
    }

    public bool IsModeUnlocked(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Classic:
                return isClassicUnlocked;
            case GameMode.Zombie:
                return isZombieUnlocked;
            case GameMode.Snake:
                return isSnakeUnlocked;
            default:
                return false;
        }
    }

    private void PlayTapSound()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
    }
}
