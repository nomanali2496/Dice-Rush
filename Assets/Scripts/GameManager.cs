using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages game state, player profiles, and CPU profiles
/// Handles profile image assignment from main menu and random CPU profile assignment
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Player Profile")]
    [SerializeField] private Image playerProfileImage; // Player's profile image display
    [SerializeField] private Image playerOutlineImage; // Player's outline image (for color assignment)
    [SerializeField] private MainMenuAvatarDisplay mainMenuAvatarDisplay; // Reference to main menu avatar display (to get user's selected avatar)
    
    [Header("CPU Profiles")]
    [SerializeField] private CPU[] cpuPlayers; // Array of CPU player scripts
    [SerializeField] private Image[] cpuProfileImages; // Array of CPU profile image displays (must match cpuPlayers length)
    [SerializeField] private Image[] cpuOutlineImages; // Array of CPU outline images (for color assignment, must match cpuPlayers length)
    [SerializeField] private int numberOfCPUs = 1; // Number of CPUs to use (1 or more)
    
    [Header("Profile Image Settings")]
    [SerializeField] private Sprite[] availableProfileSprites; // Available profile sprites for CPUs (random selection)
    [SerializeField] private bool excludePlayerAvatarFromCPU = true; // Don't use player's avatar for CPUs
    
    [Header("References")]
    [SerializeField] private NumberGrid numberGrid; // Reference to NumberGrid
    [SerializeField] private DicesController playerDicesController; // Reference to player's DicesController
    
    [Header("Gameplay Screen")]
    [SerializeField] private GameObject gameplayScreen; // Gameplay screen GameObject (set active when game starts)
    
    [Header("Timer")]
    [SerializeField] private TMPro.TextMeshProUGUI timerText; // Timer text display (TextMeshProUGUI)
    [SerializeField] private Text timerTextLegacy; // Timer text display (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private float gameTimer = 30f; // Game timer in seconds (30 seconds)
    
    [Header("Winner Screen")]
    [SerializeField] private GameObject winnerScreen; // Winner screen GameObject (set active when game ends)
    [SerializeField] private WinnerScreen winnerScreenScript; // Winner screen script for animations
    
    [Header("1st Place")]
    [SerializeField] private Image winnerProfileImage; // Winner's profile image display
    [SerializeField] private Image winnerOutlineImage; // Winner's outline image display
    [SerializeField] private Image winnerFrameImage; // Winner's frame image display
    [SerializeField] private TMPro.TextMeshProUGUI winnerNameText; // Winner's name text (TextMeshProUGUI)
    [SerializeField] private Text winnerNameTextLegacy; // Winner's name text (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private TMPro.TextMeshProUGUI winnerCashText; // Winner's cash prize text (TextMeshProUGUI)
    [SerializeField] private Text winnerCashTextLegacy; // Winner's cash prize text (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private TMPro.TextMeshProUGUI winnerChipsText; // Winner's chips prize text (TextMeshProUGUI)
    [SerializeField] private Text winnerChipsTextLegacy; // Winner's chips prize text (Legacy Text - use if not using TextMeshPro)
    
    [Header("2nd Place")]
    [SerializeField] private Image secondPlaceProfileImage; // 2nd place profile image display
    [SerializeField] private Image secondPlaceOutlineImage; // 2nd place outline image display
    [SerializeField] private Image secondPlaceFrameImage; // 2nd place frame image display
    [SerializeField] private TMPro.TextMeshProUGUI secondPlaceNameText; // 2nd place name text (TextMeshProUGUI)
    [SerializeField] private Text secondPlaceNameTextLegacy; // 2nd place name text (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private TMPro.TextMeshProUGUI secondPlaceCashText; // 2nd place cash prize text (TextMeshProUGUI)
    [SerializeField] private Text secondPlaceCashTextLegacy; // 2nd place cash prize text (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private TMPro.TextMeshProUGUI secondPlaceChipsText; // 2nd place chips prize text (TextMeshProUGUI)
    [SerializeField] private Text secondPlaceChipsTextLegacy; // 2nd place chips prize text (Legacy Text - use if not using TextMeshPro)
    
    [Header("3rd Place")]
    [SerializeField] private Image thirdPlaceProfileImage; // 3rd place profile image display
    [SerializeField] private Image thirdPlaceOutlineImage; // 3rd place outline image display
    [SerializeField] private Image thirdPlaceFrameImage; // 3rd place frame image display
    [SerializeField] private TMPro.TextMeshProUGUI thirdPlaceNameText; // 3rd place name text (TextMeshProUGUI)
    [SerializeField] private Text thirdPlaceNameTextLegacy; // 3rd place name text (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private TMPro.TextMeshProUGUI thirdPlaceCashText; // 3rd place cash prize text (TextMeshProUGUI)
    [SerializeField] private Text thirdPlaceCashTextLegacy; // 3rd place cash prize text (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private TMPro.TextMeshProUGUI thirdPlaceChipsText; // 3rd place chips prize text (TextMeshProUGUI)
    [SerializeField] private Text thirdPlaceChipsTextLegacy; // 3rd place chips prize text (Legacy Text - use if not using TextMeshPro)
    
    private Sprite playerSelectedAvatar; // Store player's selected avatar sprite
    private List<Sprite> availableCPUAvatars; // List of available avatars for CPUs
    private Color playerOutlineColor; // Store player's outline color
    private Color[] cpuOutlineColors; // Store CPU outline colors
    
    private float currentTimer; // Current timer value
    private bool isGameActive = false; // Whether the game is still active (starts as false, becomes true when gameplay screen is active)
    private Coroutine timerCoroutine; // Coroutine for timer countdown
    private bool timerStarted = false; // Flag to prevent multiple timer starts
    
    // Current lobby configuration
    private LobbyConfig currentLobbyConfig; // Current lobby configuration being played
    
    private void Start()
    {
        // Subscribe to PlayFab events to update when player data loads
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
        
        // Subscribe to NumberGrid event for when all boxes are selected
        if (numberGrid != null)
        {
            numberGrid.OnAllBoxesSelected += OnAllBoxesSelected;
        }
        
        // Wait a bit for PlayFab and MainMenuAvatarDisplay to load avatar
        StartCoroutine(InitializeProfilesDelayed());
        SetupCPUs();
        
        // Hide winner screen initially
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(false);
        }
        
        // Check if gameplay screen is active and start timer if it is
        if (gameplayScreen != null && gameplayScreen.activeSelf)
        {
            StartGameTimer();
        }
    }
    
    /// <summary>
    /// Called when gameplay screen becomes active - starts the timer
    /// Call this method when you activate the gameplay screen, or it will be called automatically via Update()
    /// </summary>
    public void OnGameplayScreenActivated()
    {
        if (gameplayScreen != null && gameplayScreen.activeSelf && !timerStarted)
        {
            Debug.Log("[GameManager] Gameplay screen activated, starting timer...");
            timerStarted = true;
            StartGameTimer();
        }
    }
    
    private void Update()
    {
        // Check if gameplay screen just became active (to start timer)
        if (gameplayScreen != null && gameplayScreen.activeSelf && !timerStarted)
        {
            Debug.Log("[GameManager] Gameplay screen is active, starting timer...");
            OnGameplayScreenActivated();
        }
    }
    
    /// <summary>
    /// Starts the game timer countdown
    /// </summary>
    private void StartGameTimer()
    {
        // Only start if gameplay screen is active
        if (gameplayScreen != null && !gameplayScreen.activeSelf)
        {
            Debug.LogWarning("[GameManager] Cannot start timer - gameplay screen is not active!");
            return;
        }
        
        currentTimer = gameTimer;
        isGameActive = true;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        timerCoroutine = StartCoroutine(TimerCountdown());
        Debug.Log($"[GameManager] Game timer started! Timer: {gameTimer} seconds");
    }
    
    /// <summary>
    /// Coroutine for timer countdown
    /// </summary>
    private System.Collections.IEnumerator TimerCountdown()
    {
        // Update timer display immediately
        UpdateTimerDisplay();
        
        while (currentTimer > 0 && isGameActive)
        {
            yield return new WaitForSeconds(1f);
            currentTimer -= 1f;
            
            // Update timer display
            UpdateTimerDisplay();
        }
        
        // Timer reached zero
        if (isGameActive)
        {
            currentTimer = 0f;
            UpdateTimerDisplay();
            OnTimerExpired();
        }
    }
    
    /// <summary>
    /// Updates the timer text display
    /// </summary>
    private void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(currentTimer);
        
        if (timerText != null)
        {
            timerText.text = seconds.ToString();
        }
        
        if (timerTextLegacy != null)
        {
            timerTextLegacy.text = seconds.ToString();
        }
    }
    
    /// <summary>
    /// Called when the timer expires
    /// </summary>
    private void OnTimerExpired()
    {
        isGameActive = false;
        timerStarted = false; // Reset flag so timer can be restarted for next game
        Debug.Log("[GameManager] Timer expired! Determining winner...");
        
        // Stop all CPU rolling
        if (cpuPlayers != null)
        {
            foreach (CPU cpu in cpuPlayers)
            {
                if (cpu != null)
                {
                    cpu.StopAutoRolling();
                }
            }
        }
        
        // Get rankings (top 3)
        var rankings = GetRankings();
        
        // Award prizes based on lobby configuration
        AwardPrizes(rankings);
        
        // Show winner screen with rankings
        ShowWinnerScreen(rankings);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        }
        
        if (numberGrid != null)
        {
            numberGrid.OnAllBoxesSelected -= OnAllBoxesSelected;
        }
    }
    
    /// <summary>
    /// Called when all boxes in the grid are selected
    /// </summary>
    private void OnAllBoxesSelected()
    {
        if (!isGameActive)
        {
            return; // Game already ended
        }
        
        isGameActive = false;
        timerStarted = false; // Reset flag so timer can be restarted if needed
        Debug.Log("[GameManager] All boxes selected! Determining winner...");
        
        // Stop timer
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Stop all CPU rolling
        if (cpuPlayers != null)
        {
            foreach (CPU cpu in cpuPlayers)
            {
                if (cpu != null)
                {
                    cpu.StopAutoRolling();
                }
            }
        }
        
        // Get rankings (top 3)
        var rankings = GetRankings();
        
        // Award prizes based on lobby configuration
        AwardPrizes(rankings);
        
        // Show winner screen with rankings
        ShowWinnerScreen(rankings);
    }
    
    /// <summary>
    /// Gets the top 3 rankings from selection counts
    /// Returns: List of tuples (PlayerType, cpuIndex, selectionCount) sorted by count (descending)
    /// Includes all players even if they have 0 selections
    /// </summary>
    private List<(PlayerType playerType, int cpuIndex, int count)> GetRankings()
    {
        if (numberGrid == null)
        {
            Debug.LogWarning("[GameManager] NumberGrid not assigned!");
            return new List<(PlayerType, int, int)>();
        }
        
        Dictionary<string, int> selectionCounts = numberGrid.GetSelectionCounts();
        List<(PlayerType playerType, int cpuIndex, int count)> rankings = new List<(PlayerType, int, int)>();
        
        // Convert dictionary to list of tuples
        foreach (var kvp in selectionCounts)
        {
            PlayerType playerType;
            int cpuIndex;
            
            if (kvp.Key == "Player")
            {
                playerType = PlayerType.Player;
                cpuIndex = -1;
            }
            else if (kvp.Key.StartsWith("CPU_"))
            {
                playerType = PlayerType.CPU;
                string indexStr = kvp.Key.Substring(4); // Remove "CPU_" prefix
                if (int.TryParse(indexStr, out cpuIndex))
                {
                    // cpuIndex is already set
                }
                else
                {
                    continue; // Skip invalid entries
                }
            }
            else
            {
                continue; // Skip invalid entries
            }
            
            rankings.Add((playerType, cpuIndex, kvp.Value));
        }
        
        // Add player with 0 selections if not already in rankings
        bool playerFound = false;
        foreach (var ranking in rankings)
        {
            if (ranking.playerType == PlayerType.Player)
            {
                playerFound = true;
                break;
            }
        }
        if (!playerFound)
        {
            rankings.Add((PlayerType.Player, -1, 0));
        }
        
        // Add all active CPUs with 0 selections if not already in rankings
        if (currentLobbyConfig != null && cpuPlayers != null)
        {
            int numberOfCPUs = currentLobbyConfig.GetNumberOfCPUs();
            for (int i = 0; i < numberOfCPUs; i++)
            {
                bool cpuFound = false;
                foreach (var ranking in rankings)
                {
                    if (ranking.playerType == PlayerType.CPU && ranking.cpuIndex == i)
                    {
                        cpuFound = true;
                        break;
                    }
                }
                if (!cpuFound)
                {
                    rankings.Add((PlayerType.CPU, i, 0));
                }
            }
        }
        
        // Sort by count (descending)
        rankings.Sort((a, b) => b.count.CompareTo(a.count));
        
        // Return top 3 (or less if there are fewer players)
        return rankings.Count > 3 ? rankings.GetRange(0, 3) : rankings;
    }
    
    /// <summary>
    /// Shows the winner screen with 1st, 2nd, and 3rd place information
    /// </summary>
    private void ShowWinnerScreen(List<(PlayerType playerType, int cpuIndex, int count)> rankings)
    {
        if (winnerScreen == null)
        {
            Debug.LogWarning("[GameManager] Winner screen not assigned!");
            return;
        }
        
        // Store lobby config before resetting (needed for animation)
        LobbyConfig configForAnimation = currentLobbyConfig;
        
        // Display 1st place
        if (rankings.Count > 0)
        {
            var firstPlace = rankings[0];
            float firstCash = currentLobbyConfig != null ? currentLobbyConfig.firstPlaceCash : 0f;
            int firstChips = currentLobbyConfig != null ? currentLobbyConfig.firstPlaceChips : 0;
            DisplayPlace(1, firstPlace.playerType, firstPlace.cpuIndex, firstPlace.count,
                        winnerProfileImage, winnerOutlineImage, winnerFrameImage, winnerNameText, winnerNameTextLegacy,
                        winnerCashText, winnerCashTextLegacy, winnerChipsText, winnerChipsTextLegacy,
                        firstCash, firstChips);
            Debug.Log($"[GameManager] 1st Place: {GetWinnerName(firstPlace.playerType, firstPlace.cpuIndex)} ({firstPlace.playerType}, CPU index: {firstPlace.cpuIndex}) with {firstPlace.count} selected boxes! Prize: {firstCash} cash, {firstChips} chips");
        }
        
        // Display 2nd place
        if (rankings.Count > 1)
        {
            var secondPlace = rankings[1];
            float secondCash = currentLobbyConfig != null ? currentLobbyConfig.secondPlaceCash : 0f;
            int secondChips = currentLobbyConfig != null ? currentLobbyConfig.secondPlaceChips : 0;
            DisplayPlace(2, secondPlace.playerType, secondPlace.cpuIndex, secondPlace.count,
                        secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy,
                        secondPlaceCashText, secondPlaceCashTextLegacy, secondPlaceChipsText, secondPlaceChipsTextLegacy,
                        secondCash, secondChips);
            Debug.Log($"[GameManager] 2nd Place: {GetWinnerName(secondPlace.playerType, secondPlace.cpuIndex)} ({secondPlace.playerType}, CPU index: {secondPlace.cpuIndex}) with {secondPlace.count} selected boxes! Prize: {secondCash} cash, {secondChips} chips");
        }
        else
        {
            // Hide 2nd place UI if no 2nd place
            HidePlace(secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy,
                     secondPlaceCashText, secondPlaceCashTextLegacy, secondPlaceChipsText, secondPlaceChipsTextLegacy);
        }
        
        // Display 3rd place
        if (rankings.Count > 2)
        {
            var thirdPlace = rankings[2];
            float thirdCash = currentLobbyConfig != null ? currentLobbyConfig.thirdPlaceCash : 0f;
            int thirdChips = currentLobbyConfig != null ? currentLobbyConfig.thirdPlaceChips : 0;
            DisplayPlace(3, thirdPlace.playerType, thirdPlace.cpuIndex, thirdPlace.count,
                        thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy,
                        thirdPlaceCashText, thirdPlaceCashTextLegacy, thirdPlaceChipsText, thirdPlaceChipsTextLegacy,
                        thirdCash, thirdChips);
            Debug.Log($"[GameManager] 3rd Place: {GetWinnerName(thirdPlace.playerType, thirdPlace.cpuIndex)} ({thirdPlace.playerType}, CPU index: {thirdPlace.cpuIndex}) with {thirdPlace.count} selected boxes! Prize: {thirdCash} cash, {thirdChips} chips");
        }
        else
        {
            // Hide 3rd place UI if no 3rd place
            HidePlace(thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy,
                     thirdPlaceCashText, thirdPlaceCashTextLegacy, thirdPlaceChipsText, thirdPlaceChipsTextLegacy);
        }
        
        // Reset game state before closing gameplay screen
        ResetGameState();
        
        // Hide gameplay screen
        if (gameplayScreen != null)
        {
            gameplayScreen.SetActive(false);
        }
        
        // Show winner screen
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(true);
        }
        
        // Trigger winner screen animation if player is in top 3 (use stored config)
        if (configForAnimation != null)
        {
            // Try to find WinnerScreen script if not assigned
            if (winnerScreenScript == null && winnerScreen != null)
            {
                winnerScreenScript = winnerScreen.GetComponent<WinnerScreen>();
            }
            
            if (winnerScreenScript != null)
            {
                winnerScreenScript.PlayAnimationIfPlayerInTop3(rankings, configForAnimation);
            }
            else
            {
                Debug.LogWarning("[GameManager] WinnerScreen script not found! Make sure WinnerScreen script is attached to the winner screen GameObject.");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] No lobby config available for winner screen animation!");
        }
    }
    
    /// <summary>
    /// Displays a place (1st, 2nd, or 3rd) with profile image, outline, frame, name, cash, and chips
    /// </summary>
    private void DisplayPlace(int placeNumber, PlayerType playerType, int cpuIndex, int selectionCount,
                              Image profileImage, Image outlineImage, Image frameImage, TMPro.TextMeshProUGUI nameText, Text nameTextLegacy,
                              TMPro.TextMeshProUGUI cashText, Text cashTextLegacy, TMPro.TextMeshProUGUI chipsText, Text chipsTextLegacy,
                              float cashPrize, int chipsPrize)
    {
        // Activate UI elements first (in case they were hidden in previous game)
        if (profileImage != null)
        {
            profileImage.gameObject.SetActive(true);
        }
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(true);
        }
        if (frameImage != null)
        {
            frameImage.gameObject.SetActive(true);
        }
        if (nameText != null)
        {
            nameText.gameObject.SetActive(true);
        }
        if (nameTextLegacy != null)
        {
            nameTextLegacy.gameObject.SetActive(true);
        }
        
        // Set profile image
        if (profileImage != null)
        {
            if (playerType == PlayerType.Player)
            {
                if (playerProfileImage != null && playerProfileImage.sprite != null)
                {
                    profileImage.sprite = playerProfileImage.sprite;
                }
            }
            else // CPU
            {
                if (cpuProfileImages != null && cpuIndex >= 0 && cpuIndex < cpuProfileImages.Length)
                {
                    if (cpuProfileImages[cpuIndex] != null && cpuProfileImages[cpuIndex].sprite != null)
                    {
                        profileImage.sprite = cpuProfileImages[cpuIndex].sprite;
                    }
                }
            }
        }
        
        // Set outline color
        if (outlineImage != null)
        {
            if (playerType == PlayerType.Player)
            {
                outlineImage.color = playerOutlineColor;
            }
            else // CPU
            {
                if (cpuOutlineColors != null && cpuIndex >= 0 && cpuIndex < cpuOutlineColors.Length)
                {
                    outlineImage.color = cpuOutlineColors[cpuIndex];
                }
            }
        }
        
        // Set name
        string playerName = GetWinnerName(playerType, cpuIndex);
        if (nameText != null)
        {
            nameText.text = playerName;
        }
        if (nameTextLegacy != null)
        {
            nameTextLegacy.text = playerName;
        }
        
        // Set cash prize
        if (cashText != null)
        {
            cashText.text = $"${cashPrize:F2}";
        }
        if (cashTextLegacy != null)
        {
            cashTextLegacy.text = $"${cashPrize:F2}";
        }
        
        // Set chips prize
        if (chipsText != null)
        {
            chipsText.text = chipsPrize.ToString("N0");
        }
        if (chipsTextLegacy != null)
        {
            chipsTextLegacy.text = chipsPrize.ToString("N0");
        }
    }
    
    /// <summary>
    /// Hides a place UI (for when there are fewer than 3 players)
    /// </summary>
    private void HidePlace(Image profileImage, Image outlineImage, Image frameImage, TMPro.TextMeshProUGUI nameText, Text nameTextLegacy,
                          TMPro.TextMeshProUGUI cashText, Text cashTextLegacy, TMPro.TextMeshProUGUI chipsText, Text chipsTextLegacy)
    {
        if (profileImage != null)
        {
            profileImage.gameObject.SetActive(false);
        }
        if (outlineImage != null)
        {
            outlineImage.gameObject.SetActive(false);
        }
        if (frameImage != null)
        {
            frameImage.gameObject.SetActive(false);
        }
        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
        }
        if (nameTextLegacy != null)
        {
            nameTextLegacy.gameObject.SetActive(false);
        }
        if (cashText != null)
        {
            cashText.gameObject.SetActive(false);
        }
        if (cashTextLegacy != null)
        {
            cashTextLegacy.gameObject.SetActive(false);
        }
        if (chipsText != null)
        {
            chipsText.gameObject.SetActive(false);
        }
        if (chipsTextLegacy != null)
        {
            chipsTextLegacy.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Gets the display name for a winner
    /// </summary>
    private string GetWinnerName(PlayerType playerType, int cpuIndex)
    {
        if (playerType == PlayerType.Player)
        {
            return "You";
        }
        else // CPU
        {
            // CPU names: Index 0 = Charles, Index 1 = John
            switch (cpuIndex)
            {
                case 0:
                    return "Charles";
                case 1:
                    return "John";
                default:
                    return $"CPU {cpuIndex + 1}";
            }
        }
    }
    
    /// <summary>
    /// Called when PlayFab player data is loaded
    /// </summary>
    private void OnPlayerDataLoaded(PlayerData data)
    {
        // Retry loading player profile image when data is loaded
        if (data != null)
        {
            StartCoroutine(RetryLoadPlayerProfile());
        }
    }
    
    /// <summary>
    /// Coroutine to retry loading player profile after PlayFab data loads
    /// </summary>
    private System.Collections.IEnumerator RetryLoadPlayerProfile()
    {
        yield return new WaitForSeconds(0.5f); // Wait for MainMenuAvatarDisplay to update
        LoadPlayerProfileImage();
    }
    
    /// <summary>
    /// Coroutine to initialize profiles after a delay (to allow MainMenuAvatarDisplay to load)
    /// </summary>
    private System.Collections.IEnumerator InitializeProfilesDelayed()
    {
        // Wait for PlayFab to initialize and MainMenuAvatarDisplay to load avatar
        yield return new WaitForSeconds(1.5f);
        
        InitializeProfiles();
    }
    
    /// <summary>
    /// Initializes player and CPU profiles
    /// </summary>
    private void InitializeProfiles()
    {
        // Get player's profile image from main menu
        LoadPlayerProfileImage();
        
        // Setup available CPU avatars
        SetupAvailableCPUAvatars();
        
        // Assign random profile images to CPUs
        AssignCPUProfileImages();
        
        // Assign random colors to outline images
        AssignOutlineColors();
    }
    
    /// <summary>
    /// Loads player's profile image from main menu
    /// </summary>
    private void LoadPlayerProfileImage()
    {
        if (playerProfileImage == null)
        {
            Debug.LogWarning("[GameManager] Player profile image not assigned!");
            return;
        }
        
        // Try to get avatar from MainMenuAvatarDisplay
        if (mainMenuAvatarDisplay != null)
        {
            // Use the public method to get the current avatar sprite
            Sprite avatarSprite = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay");
                return;
            }
            
            // Fallback: Try to get the Image component directly
            Image mainMenuImage = mainMenuAvatarDisplay.GetComponent<Image>();
            if (mainMenuImage != null && mainMenuImage.sprite != null)
            {
                playerSelectedAvatar = mainMenuImage.sprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay (component fallback)");
                return;
            }
            
            // Also try to find Image component in children
            Image childImage = mainMenuAvatarDisplay.GetComponentInChildren<Image>();
            if (childImage != null && childImage.sprite != null)
            {
                playerSelectedAvatar = childImage.sprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay (child fallback)");
                return;
            }
        }
        
        // Try to get from AvatarSelector
        AvatarSelector avatarSelector = FindObjectOfType<AvatarSelector>();
        if (avatarSelector != null)
        {
            // Try to get current selected avatar
            int selectedIndex = 0;
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
            {
                if (PlayFabManager.Instance.currentPlayerData != null)
                {
                    selectedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
                }
            }
            
            Sprite avatarSprite = avatarSelector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log($"[GameManager] Player profile image loaded from AvatarSelector (index: {selectedIndex})");
                return;
            }
        }
        
        // Try to get from PlayFabManager (custom uploaded image)
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
                            playerSelectedAvatar = sprite;
                            playerProfileImage.sprite = playerSelectedAvatar;
                            Debug.Log("[GameManager] Player profile image loaded from PlayFab custom image");
                            return;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[GameManager] Failed to load custom image: {e.Message}");
                    }
                }
            }
        }
        
        Debug.LogWarning("[GameManager] Could not load player profile image from main menu. Using default or assigned sprite.");
    }
    
    /// <summary>
    /// Sets up available CPU avatars (excluding player's avatar if option is enabled)
    /// </summary>
    private void SetupAvailableCPUAvatars()
    {
        availableCPUAvatars = new List<Sprite>();
        
        // Add all available profile sprites
        if (availableProfileSprites != null && availableProfileSprites.Length > 0)
        {
            foreach (Sprite sprite in availableProfileSprites)
            {
                if (sprite != null)
                {
                    // If exclude option is enabled, don't add player's avatar
                    if (excludePlayerAvatarFromCPU && playerSelectedAvatar != null)
                    {
                        if (sprite == playerSelectedAvatar)
                        {
                            continue; // Skip player's avatar
                        }
                    }
                    availableCPUAvatars.Add(sprite);
                }
            }
        }
        
        // If no sprites assigned, try to get from AvatarSelector
        if (availableCPUAvatars.Count == 0)
        {
            AvatarSelector avatarSelector = FindObjectOfType<AvatarSelector>();
            if (avatarSelector != null)
            {
                // Get all avatar sprites from AvatarSelector
                for (int i = 0; i < 8; i++) // Assuming 8 avatars
                {
                    Sprite sprite = avatarSelector.GetAvatarSpriteByIndex(i);
                    if (sprite != null)
                    {
                        // If exclude option is enabled, don't add player's avatar
                        if (excludePlayerAvatarFromCPU && playerSelectedAvatar != null)
                        {
                            if (sprite == playerSelectedAvatar)
                            {
                                continue; // Skip player's avatar
                            }
                        }
                        availableCPUAvatars.Add(sprite);
                    }
                }
            }
        }
        
        if (availableCPUAvatars.Count == 0)
        {
            Debug.LogWarning("[GameManager] No available CPU avatars found! CPUs will not have profile images.");
        }
    }
    
    /// <summary>
    /// Assigns random profile images to CPUs
    /// </summary>
    private void AssignCPUProfileImages()
    {
        if (cpuProfileImages == null || cpuProfileImages.Length == 0)
        {
            Debug.LogWarning("[GameManager] CPU profile images array is empty!");
            return;
        }
        
        if (availableCPUAvatars == null || availableCPUAvatars.Count == 0)
        {
            Debug.LogWarning("[GameManager] No available CPU avatars to assign!");
            return;
        }
        
        // Create a copy of available avatars to avoid duplicates
        List<Sprite> availableAvatars = new List<Sprite>(availableCPUAvatars);
        
        // Assign random avatars to each CPU
        for (int i = 0; i < cpuProfileImages.Length && i < numberOfCPUs; i++)
        {
            if (cpuProfileImages[i] == null)
            {
                continue;
            }
            
            if (availableAvatars.Count == 0)
            {
                // If we run out of unique avatars, reset the list
                availableAvatars = new List<Sprite>(availableCPUAvatars);
            }
            
            // Randomly select an avatar
            int randomIndex = Random.Range(0, availableAvatars.Count);
            Sprite selectedAvatar = availableAvatars[randomIndex];
            
            // Assign to CPU profile image
            cpuProfileImages[i].sprite = selectedAvatar;
            
            // Remove from available list to avoid duplicates (if we have enough avatars)
            if (availableAvatars.Count > numberOfCPUs)
            {
                availableAvatars.RemoveAt(randomIndex);
            }
            
            Debug.Log($"[GameManager] Assigned profile image to CPU {i + 1}");
        }
    }
    
    /// <summary>
    /// Sets up CPU players based on numberOfCPUs
    /// Enables/disables CPU GameObjects and their scripts based on the number needed
    /// </summary>
    private void SetupCPUs()
    {
        if (cpuPlayers == null || cpuPlayers.Length == 0)
        {
            Debug.LogWarning("[GameManager] CPU players array is empty!");
            return;
        }
        
        // Enable only the specified number of CPUs
        for (int i = 0; i < cpuPlayers.Length; i++)
        {
            if (cpuPlayers[i] != null)
            {
                if (i < numberOfCPUs)
                {
                    // Enable CPU GameObject
                    cpuPlayers[i].gameObject.SetActive(true);
                    // Set the CPU index
                    cpuPlayers[i].SetCPUIndex(i);
                    // Enable CPU script
                    cpuPlayers[i].SetCPUEnabled(true);
                    Debug.Log($"[GameManager] CPU {i + 1} GameObject enabled with index {i} and started rolling");
                }
                else
                {
                    // Disable CPU script first
                    cpuPlayers[i].SetCPUEnabled(false);
                    // Disable CPU GameObject
                    cpuPlayers[i].gameObject.SetActive(false);
                    Debug.Log($"[GameManager] CPU {i + 1} GameObject disabled");
                }
            }
        }
        
        Debug.Log($"[GameManager] Total CPUs active: {numberOfCPUs} out of {cpuPlayers.Length}");
    }
    
    /// <summary>
    /// Sets the number of CPUs to use
    /// </summary>
    public void SetNumberOfCPUs(int count)
    {
        numberOfCPUs = Mathf.Clamp(count, 1, cpuPlayers != null ? cpuPlayers.Length : 1);
        SetupCPUs();
        AssignCPUProfileImages(); // Reassign profile images
        Debug.Log($"[GameManager] Number of CPUs set to {numberOfCPUs}");
    }
    
    /// <summary>
    /// Manually refresh player profile image
    /// </summary>
    public void RefreshPlayerProfile()
    {
        LoadPlayerProfileImage();
    }
    
    /// <summary>
    /// Manually reassign CPU profile images
    /// </summary>
    public void ReassignCPUProfiles()
    {
        SetupAvailableCPUAvatars();
        AssignCPUProfileImages();
    }
    
    /// <summary>
    /// Gets the player's selected avatar sprite
    /// </summary>
    public Sprite GetPlayerAvatar()
    {
        return playerSelectedAvatar;
    }
    
    /// <summary>
    /// Gets the number of active CPUs
    /// </summary>
    public int GetNumberOfCPUs()
    {
        return numberOfCPUs;
    }
    
    /// <summary>
    /// Assigns random colors to outline images for player and CPUs (ensures no duplicates)
    /// </summary>
    private void AssignOutlineColors()
    {
        // Initialize CPU outline colors array
        cpuOutlineColors = new Color[cpuPlayers != null ? cpuPlayers.Length : 0];
        
        // Available colors: Red, Yellow, Green
        Color[] availableColors = new Color[]
        {
            new Color(0.7f, 0.1f, 0.1f, 1f), // Dark Red
            new Color(0.7f, 0.7f, 0.1f, 1f), // Dark Yellow
            new Color(0.1f, 0.7f, 0.1f, 1f)  // Dark Green
        };
        
        // Create a list of available colors to assign (ensures uniqueness)
        List<Color> availableColorsList = new List<Color>(availableColors);
        
        // Calculate total number of players (1 player + numberOfCPUs)
        int totalPlayers = 1 + numberOfCPUs;
        
        // Check if we have enough colors for all players
        if (totalPlayers > availableColors.Length)
        {
            Debug.LogWarning($"[GameManager] Not enough colors ({availableColors.Length}) for all players ({totalPlayers}). Some players will share colors.");
        }
        
        // Assign color to player outline (first player)
        if (playerOutlineImage != null)
        {
            if (availableColorsList.Count > 0)
            {
                int randomIndex = Random.Range(0, availableColorsList.Count);
                playerOutlineColor = availableColorsList[randomIndex]; // Store the color
                playerOutlineImage.color = playerOutlineColor;
                availableColorsList.RemoveAt(randomIndex); // Remove from available list
                Debug.Log($"[GameManager] Assigned color to player outline: {playerOutlineColor}");
            }
            else
            {
                // Fallback if no colors available
                playerOutlineColor = availableColors[0];
                playerOutlineImage.color = playerOutlineColor;
                Debug.LogWarning("[GameManager] No unique colors available, using default for player");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] Player outline image not assigned!");
        }
        
        // Assign unique colors to CPU outlines
        if (cpuOutlineImages != null && cpuOutlineImages.Length > 0)
        {
            for (int i = 0; i < cpuOutlineImages.Length && i < numberOfCPUs; i++)
            {
                if (cpuOutlineImages[i] != null)
                {
                    if (availableColorsList.Count > 0)
                    {
                        // Pick a random color from remaining available colors
                        int randomIndex = Random.Range(0, availableColorsList.Count);
                        Color selectedColor = availableColorsList[randomIndex];
                        cpuOutlineColors[i] = selectedColor; // Store the color
                        cpuOutlineImages[i].color = selectedColor;
                        availableColorsList.RemoveAt(randomIndex); // Remove from available list
                        Debug.Log($"[GameManager] Assigned unique color to CPU {i + 1} outline: {selectedColor}");
                    }
                    else
                    {
                        // Fallback: cycle through colors if we run out
                        Color fallbackColor = availableColors[i % availableColors.Length];
                        cpuOutlineColors[i] = fallbackColor;
                        cpuOutlineImages[i].color = fallbackColor;
                        Debug.LogWarning($"[GameManager] Using fallback color for CPU {i + 1}: {fallbackColor}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] CPU outline images array is empty or not assigned!");
        }
        
        // Update NumberGrid with the outline colors
        UpdateNumberGridColors();
    }
    
    /// <summary>
    /// Updates NumberGrid with player and CPU outline colors
    /// </summary>
    private void UpdateNumberGridColors()
    {
        if (numberGrid != null)
        {
            // Set player outline color
            numberGrid.SetPlayerOutlineColor(playerOutlineColor);
            
            // Set CPU outline colors
            if (cpuOutlineColors != null)
            {
                for (int i = 0; i < cpuOutlineColors.Length && i < numberOfCPUs; i++)
                {
                    numberGrid.SetCPUOutlineColor(i, cpuOutlineColors[i]);
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the outline color for a specific player type
    /// </summary>
    public Color GetOutlineColor(PlayerType playerType, int cpuIndex = 0)
    {
        if (playerType == PlayerType.Player)
        {
            return playerOutlineColor;
        }
        else // CPU
        {
            if (cpuOutlineColors != null && cpuIndex >= 0 && cpuIndex < cpuOutlineColors.Length)
            {
                return cpuOutlineColors[cpuIndex];
            }
            return Color.red; // Default fallback
        }
    }
    
    /// <summary>
    /// Gets a random darker color for outline images (red, yellow, green only)
    /// </summary>
    private Color GetRandomColor()
    {
        // Only three colors: Red, Yellow, Green
        Color[] colors = new Color[]
        {
            new Color(0.7f, 0.1f, 0.1f, 1f), // Dark Red
            new Color(0.7f, 0.7f, 0.1f, 1f), // Dark Yellow
            new Color(0.1f, 0.7f, 0.1f, 1f)  // Dark Green
        };
        
        // Randomly select one of the three colors
        int randomIndex = Random.Range(0, colors.Length);
        return colors[randomIndex];
    }
    
    /// <summary>
    /// Manually reassign outline colors
    /// </summary>
    public void ReassignOutlineColors()
    {
        AssignOutlineColors();
    }
    
    /// <summary>
    /// Resets game state when gameplay screen closes
    /// This ensures a clean state for the next game
    /// </summary>
    private void ResetGameState()
    {
        // Stop timer if running
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Reset flags
        isGameActive = false;
        timerStarted = false;
        currentTimer = gameTimer; // Reset to default timer value
        
        // Clear current lobby config (will be set when new game starts)
        currentLobbyConfig = null;
        
        Debug.Log("[GameManager] Game state reset");
    }
    
    /// <summary>
    /// Configures the game from a lobby configuration
    /// Sets number of CPUs, handles entry fee, and stores prize information
    /// </summary>
    public void ConfigureGameFromLobby(LobbyConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[GameManager] LobbyConfig is null! Using default settings.");
            return;
        }
        
        // Stop any running timer and reset flags first
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        isGameActive = false;
        timerStarted = false;
        currentTimer = gameTimer;
        
        // Reset winner screen UI to ensure clean state
        ResetWinnerScreenUI();
        
        // Stop all CPUs before resetting
        if (cpuPlayers != null)
        {
            foreach (CPU cpu in cpuPlayers)
            {
                if (cpu != null)
                {
                    cpu.StopAutoRolling();
                }
            }
            Debug.Log("[GameManager] All CPUs stopped");
        }
        
        // Reset and regenerate NumberGrid for new game
        if (numberGrid != null)
        {
            numberGrid.GenerateGrid();
            Debug.Log("[GameManager] NumberGrid regenerated for new game");
        }
        
        // Set new lobby configuration
        currentLobbyConfig = config;
        
        // Set number of CPUs based on lobby configuration
        int numberOfCPUs = config.GetNumberOfCPUs();
        
        // Wait a frame to ensure NumberGrid is fully regenerated before restarting CPUs
        StartCoroutine(ConfigureCPUsAfterDelay(numberOfCPUs));
        
        Debug.Log($"[GameManager] Game configured from Lobby {config.lobbyIndex + 1}: {config.numberOfPlayers} players ({numberOfCPUs} CPUs)");
        
        // Handle entry fee
        HandleEntryFee(config);
    }
    
    /// <summary>
    /// Resets the winner screen UI to ensure clean state for next game
    /// </summary>
    private void ResetWinnerScreenUI()
    {
        // Reset 1st place UI
        if (winnerProfileImage != null)
        {
            winnerProfileImage.gameObject.SetActive(true);
        }
        if (winnerOutlineImage != null)
        {
            winnerOutlineImage.gameObject.SetActive(true);
        }
        if (winnerFrameImage != null)
        {
            winnerFrameImage.gameObject.SetActive(true);
        }
        if (winnerNameText != null)
        {
            winnerNameText.gameObject.SetActive(true);
        }
        if (winnerNameTextLegacy != null)
        {
            winnerNameTextLegacy.gameObject.SetActive(true);
        }
        if (winnerCashText != null)
        {
            winnerCashText.gameObject.SetActive(true);
        }
        if (winnerCashTextLegacy != null)
        {
            winnerCashTextLegacy.gameObject.SetActive(true);
        }
        if (winnerChipsText != null)
        {
            winnerChipsText.gameObject.SetActive(true);
        }
        if (winnerChipsTextLegacy != null)
        {
            winnerChipsTextLegacy.gameObject.SetActive(true);
        }
        
        // Reset 2nd place UI
        if (secondPlaceProfileImage != null)
        {
            secondPlaceProfileImage.gameObject.SetActive(true);
        }
        if (secondPlaceOutlineImage != null)
        {
            secondPlaceOutlineImage.gameObject.SetActive(true);
        }
        if (secondPlaceFrameImage != null)
        {
            secondPlaceFrameImage.gameObject.SetActive(true);
        }
        if (secondPlaceNameText != null)
        {
            secondPlaceNameText.gameObject.SetActive(true);
        }
        if (secondPlaceNameTextLegacy != null)
        {
            secondPlaceNameTextLegacy.gameObject.SetActive(true);
        }
        if (secondPlaceCashText != null)
        {
            secondPlaceCashText.gameObject.SetActive(true);
        }
        if (secondPlaceCashTextLegacy != null)
        {
            secondPlaceCashTextLegacy.gameObject.SetActive(true);
        }
        if (secondPlaceChipsText != null)
        {
            secondPlaceChipsText.gameObject.SetActive(true);
        }
        if (secondPlaceChipsTextLegacy != null)
        {
            secondPlaceChipsTextLegacy.gameObject.SetActive(true);
        }
        
        // Reset 3rd place UI
        if (thirdPlaceProfileImage != null)
        {
            thirdPlaceProfileImage.gameObject.SetActive(true);
        }
        if (thirdPlaceOutlineImage != null)
        {
            thirdPlaceOutlineImage.gameObject.SetActive(true);
        }
        if (thirdPlaceFrameImage != null)
        {
            thirdPlaceFrameImage.gameObject.SetActive(true);
        }
        if (thirdPlaceNameText != null)
        {
            thirdPlaceNameText.gameObject.SetActive(true);
        }
        if (thirdPlaceNameTextLegacy != null)
        {
            thirdPlaceNameTextLegacy.gameObject.SetActive(true);
        }
        if (thirdPlaceCashText != null)
        {
            thirdPlaceCashText.gameObject.SetActive(true);
        }
        if (thirdPlaceCashTextLegacy != null)
        {
            thirdPlaceCashTextLegacy.gameObject.SetActive(true);
        }
        if (thirdPlaceChipsText != null)
        {
            thirdPlaceChipsText.gameObject.SetActive(true);
        }
        if (thirdPlaceChipsTextLegacy != null)
        {
            thirdPlaceChipsTextLegacy.gameObject.SetActive(true);
        }
        
        Debug.Log("[GameManager] Winner screen UI reset");
    }
    
    /// <summary>
    /// Coroutine to configure CPUs after a small delay to ensure NumberGrid is ready
    /// </summary>
    private System.Collections.IEnumerator ConfigureCPUsAfterDelay(int numberOfCPUs)
    {
        // Wait a frame to ensure NumberGrid is fully regenerated
        yield return null;
        
        // Now set number of CPUs (this will enable and start them)
        SetNumberOfCPUs(numberOfCPUs);
        
        Debug.Log($"[GameManager] CPUs configured and started after NumberGrid regeneration");
    }
    
    /// <summary>
    /// Handles entry fee payment (cash and/or chips)
    /// </summary>
    private void HandleEntryFee(LobbyConfig config)
    {
        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[GameManager] PlayFabManager not available! Cannot process entry fee.");
            return;
        }
        
        bool canAfford = true;
        
        // Check if player can afford cash entry fee
        if (config.entryFeeCash > 0f)
        {
            bool cashSpent = PlayFabManager.Instance.SpendCash(config.entryFeeCash);
            if (cashSpent)
            {
                Debug.Log($"[GameManager] Entry fee paid: {config.entryFeeCash} cash");
            }
            else
            {
                canAfford = false;
                Debug.LogWarning($"[GameManager] Player cannot afford entry fee: {config.entryFeeCash} cash (has: {PlayFabManager.Instance.currentPlayerData.cash})");
            }
        }
        
        // Check if player can afford chips entry fee
        if (config.entryFeeChips > 0)
        {
            bool chipsSpent = PlayFabManager.Instance.SpendCoins(config.entryFeeChips);
            if (chipsSpent)
            {
                Debug.Log($"[GameManager] Entry fee paid: {config.entryFeeChips} chips");
            }
            else
            {
                canAfford = false;
                Debug.LogWarning($"[GameManager] Player cannot afford entry fee: {config.entryFeeChips} chips (has: {PlayFabManager.Instance.currentPlayerData.coins})");
            }
        }
        
        if (!canAfford)
        {
            Debug.LogError("[GameManager] Player cannot afford entry fee! Game may not proceed correctly.");
            // Optionally: Close gameplay screen and show error message
        }
        
        // Update UI displays
        PlayerProfileUI playerProfileUI = FindObjectOfType<PlayerProfileUI>();
        if (playerProfileUI != null)
        {
            playerProfileUI.UpdateAllCurrencyDisplays(
                PlayFabManager.Instance.currentPlayerData.coins,
                PlayFabManager.Instance.currentPlayerData.cash
            );
        }
        
        // Refresh lobby play buttons after entry fee is deducted
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.RefreshAllPlayButtons();
        }
    }
    
    /// <summary>
    /// Awards prizes based on current lobby configuration and player rankings
    /// </summary>
    private void AwardPrizes(List<(PlayerType playerType, int cpuIndex, int count)> rankings)
    {
        if (currentLobbyConfig == null)
        {
            Debug.LogWarning("[GameManager] No lobby configuration set! Cannot award prizes.");
            return;
        }
        
        if (PlayFabManager.Instance == null || !PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[GameManager] PlayFabManager not available! Cannot award prizes.");
            return;
        }
        
        // Award prizes to top 3 players
        for (int i = 0; i < rankings.Count && i < 3; i++)
        {
            var ranking = rankings[i];
            
            // Only award prize to the player (not CPUs)
            if (ranking.playerType == PlayerType.Player)
            {
                float cashPrize = 0f;
                int chipsPrize = 0;
                
                switch (i)
                {
                    case 0: // 1st place
                        cashPrize = currentLobbyConfig.firstPlaceCash;
                        chipsPrize = currentLobbyConfig.firstPlaceChips;
                        break;
                    case 1: // 2nd place
                        cashPrize = currentLobbyConfig.secondPlaceCash;
                        chipsPrize = currentLobbyConfig.secondPlaceChips;
                        break;
                    case 2: // 3rd place
                        cashPrize = currentLobbyConfig.thirdPlaceCash;
                        chipsPrize = currentLobbyConfig.thirdPlaceChips;
                        break;
                }
                
                // Award cash prize
                if (cashPrize > 0f)
                {
                    PlayFabManager.Instance.AddCash(cashPrize);
                    Debug.Log($"[GameManager] Awarded {cashPrize} cash for {i + 1}st place");
                }
                
                // Award chips prize
                if (chipsPrize > 0)
                {
                    PlayFabManager.Instance.AddCoins(chipsPrize);
                    Debug.Log($"[GameManager] Awarded {chipsPrize} chips for {i + 1}st place");
                }
            }
        }
        
        // Don't update UI displays here - let WinnerScreen animation handle the count-up
        // The UI will be updated by WinnerScreen during the animation
        
        // Refresh lobby play buttons in case player can now afford lobbies they couldn't before
        // (This will happen after animation completes)
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            // Delay refresh until after animation completes
            StartCoroutine(RefreshLobbyButtonsDelayed());
        }
    }
    
    /// <summary>
    /// Refreshes lobby buttons after a delay (to allow animation to complete)
    /// </summary>
    private System.Collections.IEnumerator RefreshLobbyButtonsDelayed()
    {
        // Wait for animation to complete (particles + count-up)
        yield return new WaitForSeconds(3f); // Adjust based on your animation duration
        
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null)
        {
            lobbyManager.RefreshAllPlayButtons();
        }
    }
    
    /// <summary>
    /// Gets the current lobby configuration
    /// </summary>
    public LobbyConfig GetCurrentLobbyConfig()
    {
        return currentLobbyConfig;
    }
}

