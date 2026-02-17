using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;

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
    [SerializeField] private NumberGrid numberGrid; // Reference to NumberGrid (legacy - will be replaced by individual grids)
    [SerializeField] private DicesController playerDicesController; // Reference to player's DicesController
    [SerializeField] private GameObject numberGridPrefab; // Prefab for creating individual NumberGrid instances (if null, will use existing numberGrid as template)
    [SerializeField] private Transform gridsParent; // Parent transform for all NumberGrid instances (if null, will use gameplayScreen or this transform)
    
    [Header("Gameplay Screen")]
    [SerializeField] private GameObject gameplayScreen; // Gameplay screen GameObject (set active when game starts)
    [SerializeField] private Button quitButton; // Quit button (ends game when clicked)
    [SerializeField] private GameObject quitPanel; // Quit confirmation panel (shown when quit button is clicked)
    
    [Header("Timer")]
    [SerializeField] private TMPro.TextMeshProUGUI timerText; // Timer text display (TextMeshProUGUI)
    [SerializeField] private Text timerTextLegacy; // Timer text display (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private float gameTimer = 30f; // Game timer in seconds (30 seconds)
    
    [Header("Countdown")]
    [SerializeField] private TMPro.TextMeshProUGUI countdownText; // Countdown text display ("3", "2", "1")
    [SerializeField] private TMPro.TextMeshProUGUI countdownTextShadow; // Countdown text shadow display ("3", "2", "1")
    [SerializeField] private float countdownDuration = 1f; // Duration for each countdown number (in seconds)
    
    [Header("Winner Screen")]
    [SerializeField] private GameObject winnerScreen; // Winner screen GameObject (set active when game ends)
    [SerializeField] private WinnerScreen winnerScreenScript; // Winner screen script for animations
    
    [Header("1st Place")]
    [SerializeField] private Image winnerProfileImage; // Winner's profile image display
    [SerializeField] private Image winnerOutlineImage; // Winner's outline image display
    [SerializeField] private Image winnerFrameImage; // Winner's frame image display
    [SerializeField] private TMPro.TextMeshProUGUI winnerNameText; // Winner's name text (TextMeshProUGUI)
    [SerializeField] private Text winnerNameTextLegacy; // Winner's name text (Legacy Text - use if not using TextMeshPro)
    
    [Header("2nd Place")]
    [SerializeField] private Image secondPlaceProfileImage; // 2nd place profile image display
    [SerializeField] private Image secondPlaceOutlineImage; // 2nd place outline image display
    [SerializeField] private Image secondPlaceFrameImage; // 2nd place frame image display
    [SerializeField] private TMPro.TextMeshProUGUI secondPlaceNameText; // 2nd place name text (TextMeshProUGUI)
    [SerializeField] private Text secondPlaceNameTextLegacy; // 2nd place name text (Legacy Text - use if not using TextMeshPro)
    
    [Header("3rd Place")]
    [SerializeField] private Image thirdPlaceProfileImage; // 3rd place profile image display
    [SerializeField] private Image thirdPlaceOutlineImage; // 3rd place outline image display
    [SerializeField] private Image thirdPlaceFrameImage; // 3rd place frame image display
    [SerializeField] private TMPro.TextMeshProUGUI thirdPlaceNameText; // 3rd place name text (TextMeshProUGUI)
    [SerializeField] private Text thirdPlaceNameTextLegacy; // 3rd place name text (Legacy Text - use if not using TextMeshPro)
    
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
    
    // Individual NumberGrid instances (one per player/CPU)
    private NumberGrid playerNumberGrid; // Player's own NumberGrid instance
    private Dictionary<int, NumberGrid> cpuNumberGrids = new Dictionary<int, NumberGrid>(); // CPU NumberGrid instances by CPU index
    private List<NumberGrid> allNumberGrids = new List<NumberGrid>(); // All NumberGrid instances (for cleanup)
    
    private void Start()
    {
        // Subscribe to PlayFab events to update when player data loads
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
        
        // Subscribe to NumberGrid event for when all boxes are selected (legacy - will be updated when individual grids are created)
        if (numberGrid != null)
        {
            numberGrid.OnAllBoxesSelected += OnAllBoxesSelected;
            numberGrid.OnBoxSelected += OnBoxSelected; // Subscribe to box selection event
        }
        
        // Subscribe to quit button
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
        
        // Wait a bit for PlayFab and MainMenuAvatarDisplay to load avatar
        StartCoroutine(InitializeProfilesDelayed());
        SetupCPUs();
        
        // Hide winner screen initially
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(false);
        }
        
        // Hide quit panel initially
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
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
    /// Starts the game timer countdown (with "3, 2, 1" countdown first)
    /// </summary>
    private void StartGameTimer()
    {
        // Only start if gameplay screen is active
        if (gameplayScreen != null && !gameplayScreen.activeSelf)
        {
            Debug.LogWarning("[GameManager] Cannot start timer - gameplay screen is not active!");
            return;
        }
        
        // Start with "3, 2, 1" countdown first
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        timerCoroutine = StartCoroutine(CountdownBeforeStart());
        Debug.Log($"[GameManager] Starting '3, 2, 1' countdown before game timer...");
    }
    
    /// <summary>
    /// Coroutine for "3, 2, 1" countdown before game starts
    /// </summary>
    private System.Collections.IEnumerator CountdownBeforeStart()
    {
        // Make sure countdown text exists
        if (countdownText == null)
        {
            Debug.LogWarning("[GameManager] Countdown text not assigned! Skipping countdown and starting timer directly.");
            currentTimer = gameTimer;
            isGameActive = true;
            timerCoroutine = StartCoroutine(TimerCountdown());
            yield break;
        }
        
        // Hide timer text during countdown
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(false);
        
        // Show countdown text
        countdownText.gameObject.SetActive(true);
        countdownTextShadow.gameObject.SetActive(true);
        
        // Countdown from 3 to 1
        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            countdownTextShadow.text = i.ToString();
            
            // Play countdown sound effect using SoundManager
            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlayCountdown();
                Debug.Log($"[GameManager] Playing countdown sound for: {i}");
            }
            
            // Reset scale and alpha
            countdownText.transform.localScale = Vector3.zero;
            countdownText.alpha = 0f;
            countdownTextShadow.transform.localScale = Vector3.zero;
            countdownTextShadow.alpha = 0f;
            
            // Animate: Scale up and fade in
            countdownText.transform.DOScale(Vector3.one * 1.5f, countdownDuration * 0.3f).SetEase(DG.Tweening.Ease.OutBack);
            countdownText.DOFade(1f, countdownDuration * 0.3f);
            countdownTextShadow.transform.DOScale(Vector3.one * 1.5f, countdownDuration * 0.3f).SetEase(DG.Tweening.Ease.OutBack);
            countdownTextShadow.DOFade(1f, countdownDuration * 0.3f);
            
            yield return new WaitForSeconds(countdownDuration * 0.3f);
            
            // Animate: Scale down slightly and fade out
            countdownText.transform.DOScale(Vector3.one * 0.8f, countdownDuration * 0.7f).SetEase(DG.Tweening.Ease.InBack);
            countdownText.DOFade(0f, countdownDuration * 0.7f);
            countdownTextShadow.transform.DOScale(Vector3.one * 0.8f, countdownDuration * 0.7f).SetEase(DG.Tweening.Ease.InBack);
            countdownTextShadow.DOFade(0f, countdownDuration * 0.7f);
            
            yield return new WaitForSeconds(countdownDuration * 0.7f);
        }
        
        // Hide countdown text
        countdownText.gameObject.SetActive(false);
        countdownTextShadow.gameObject.SetActive(false);
        
        // Show timer text again
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(true);
        
        // Start gameplay music after countdown completes
        if (SettingManager.instance != null)
        {
            SettingManager.instance.OnCountdownComplete();
            Debug.Log("[GameManager] Countdown complete - signaled SettingManager to start gameplay music");
        }
        
        // Start powerup timers for all grids (player and CPUs) after countdown completes
        StartAllPowerupTimers();
        
        // Now start the actual game timer
        currentTimer = gameTimer;
        isGameActive = true;
        timerCoroutine = StartCoroutine(TimerCountdown());
        Debug.Log($"[GameManager] '3, 2, 1' countdown complete! Game timer started: {gameTimer} seconds");
    }
    
    /// <summary>
    /// Starts powerup timers for all grids (player and all CPUs)
    /// Called after "3, 2, 1" countdown completes
    /// </summary>
    private void StartAllPowerupTimers()
    {
        // Start player powerup timer
        if (playerNumberGrid != null)
        {
            playerNumberGrid.StartPowerupTimerFromGameManager();
            Debug.Log("[GameManager] Started powerup timer for player grid");
        }
        
        // Start all CPU powerup timers
        foreach (var kvp in cpuNumberGrids)
        {
            if (kvp.Value != null)
            {
                kvp.Value.StartPowerupTimerFromGameManager();
                Debug.Log($"[GameManager] Started powerup timer for CPU {kvp.Key} grid");
            }
        }
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
            numberGrid.OnBoxSelected -= OnBoxSelected; // Unsubscribe from box selection event
        }
        
        // Unsubscribe from quit button
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }
    }
    
    /// <summary>
    /// Called when a box is selected - updates outline image fill amounts
    /// </summary>
    private void OnBoxSelected(int number, GameObject boxObject)
    {
        UpdateOutlineFillAmounts();
    }
    
    /// <summary>
    /// Called when all boxes in a grid are selected (from any individual board)
    /// With individual boards, game ends when any player completes their board or timer expires
    /// </summary>
    private void OnAllBoxesSelected()
    {
        if (!isGameActive)
        {
            return; // Game already ended
        }
        
        isGameActive = false;
        timerStarted = false; // Reset flag so timer can be restarted if needed
        Debug.Log("[GameManager] All boxes selected on a board! Determining winner...");
        
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
        
        // Get rankings (top 3) - based on individual board selection counts
        var rankings = GetRankings();
        
        // Award prizes based on lobby configuration
        AwardPrizes(rankings);
        
        // Show winner screen with rankings
        ShowWinnerScreen(rankings);
    }
    
    /// <summary>
    /// Called when the quit button is clicked - player goes to lobby screen
    /// No prizes awarded, no winner screen shown to quitting player
    /// If only 2 players total: CPU wins instantly but player doesn't see winner screen
    /// If more than 2 players: CPUs continue playing
    /// </summary>
    private void OnQuitButtonClicked()
    {
        if (!isGameActive)
        {
            return; // Game already ended
        }
        
        Debug.Log("[GameManager] Player quit! Returning to lobby...");
        
        // Calculate total number of players (1 player + CPUs)
        int totalPlayers = 1; // Always 1 human player
        if (currentLobbyConfig != null)
        {
            totalPlayers += currentLobbyConfig.GetNumberOfCPUs();
        }
        
        Debug.Log($"[GameManager] Total players: {totalPlayers}");
        
        // Stop timer for the player
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Close quit panel if it's open
        if (quitPanel != null && quitPanel.activeSelf)
        {
            quitPanel.SetActive(false);
            Debug.Log("[GameManager] Quit panel closed");
        }
        
        // If only 2 players total (player + 1 CPU), end game for CPU
        // CPU wins but player doesn't see winner screen
        if (totalPlayers == 2)
        {
            Debug.Log("[GameManager] Only 2 players - ending game for CPU (player won't see winner screen)");
            
            isGameActive = false;
            timerStarted = false;
            
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
            
            // Reset game state
            ResetGameState();
        }
        else // More than 2 players - CPUs continue playing
        {
            Debug.Log("[GameManager] More than 2 players - CPUs continue playing");
            // Don't set isGameActive to false - game continues for CPUs
            // Don't stop CPU rolling - they keep playing
            // Don't reset game state - game is still active for others
        }
        
        // Hide gameplay screen - player returns to lobby
        if (gameplayScreen != null)
        {
            gameplayScreen.SetActive(false);
            Debug.Log("[GameManager] Gameplay screen hidden - player returned to lobby");
        }
        
        // Do NOT show winner screen - player goes straight to lobby
        // Do NOT award prizes - player gets nothing for quitting
    }
    
    /// <summary>
    /// Gets the top 3 rankings from individual board selection counts
    /// Each player/CPU has their own board, winner is determined by who selected most on their own board
    /// Returns: List of tuples (PlayerType, cpuIndex, selectionCount) sorted by count (descending)
    /// Includes all players even if they have 0 selections
    /// </summary>
    private List<(PlayerType playerType, int cpuIndex, int count)> GetRankings()
    {
        List<(PlayerType playerType, int cpuIndex, int count)> rankings = new List<(PlayerType, int, int)>();
        
        // Count player's selections from their own board
        int playerCount = 0;
        if (playerNumberGrid != null)
        {
            Dictionary<string, int> playerSelectionCounts = playerNumberGrid.GetSelectionCounts();
            // Sum all selections from player's board (should only be "Player" key, but sum all to be safe)
            foreach (var kvp in playerSelectionCounts)
            {
                playerCount += kvp.Value;
            }
        }
        rankings.Add((PlayerType.Player, -1, playerCount));
        
        // Count each CPU's selections from their own board
        if (currentLobbyConfig != null && cpuPlayers != null)
        {
            int numberOfCPUs = currentLobbyConfig.GetNumberOfCPUs();
            for (int i = 0; i < numberOfCPUs; i++)
            {
                int cpuCount = 0;
                if (cpuNumberGrids.ContainsKey(i) && cpuNumberGrids[i] != null)
                {
                    Dictionary<string, int> cpuSelectionCounts = cpuNumberGrids[i].GetSelectionCounts();
                    // Sum all selections from this CPU's board
                    foreach (var kvp in cpuSelectionCounts)
                    {
                        cpuCount += kvp.Value;
                    }
                }
                rankings.Add((PlayerType.CPU, i, cpuCount));
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
        
        // Ensure WinnerScreen script reference is initialized (needed for sprites)
        if (winnerScreenScript == null && winnerScreen != null)
        {
            winnerScreenScript = winnerScreen.GetComponent<WinnerScreen>();
        }
        
        // Ensure WinnerScreen script reference is initialized (needed for sprites and prize UI)
        if (winnerScreenScript == null && winnerScreen != null)
        {
            winnerScreenScript = winnerScreen.GetComponent<WinnerScreen>();
        }
        
        if (winnerScreenScript == null)
        {
            Debug.LogError("[GameManager] WinnerScreen script not found! Cannot display prize icons and texts. Make sure WinnerScreen script is attached to the winner screen GameObject.");
        }
        else
        {
            Debug.Log($"[GameManager] WinnerScreen script found successfully");
        }
        
        // Display 1st place (only if there's a prize)
        if (rankings.Count > 0)
        {
            var firstPlace = rankings[0];
            float firstCash = currentLobbyConfig != null ? currentLobbyConfig.firstPlaceCash : 0f;
            int firstChips = currentLobbyConfig != null ? currentLobbyConfig.firstPlaceChips : 0;
            // Get prize UI from WinnerScreen
            Image firstPrizeIcon = winnerScreenScript != null ? winnerScreenScript.GetFirstPlacePrizeIcon() : null;
            TMPro.TextMeshProUGUI firstPrizeText = winnerScreenScript != null ? winnerScreenScript.GetFirstPlacePrizeText() : null;
            
            // Check if there's any prize for 1st place
            if (firstCash > 0f || firstChips > 0)
            {
                if (winnerScreenScript != null)
                {
                    if (firstPrizeIcon == null)
                    {
                        Debug.LogError("[GameManager] First place prize icon is null! Please assign 'firstPlacePrizeIcon' in WinnerScreen component in Unity Inspector.");
                    }
                    if (firstPrizeText == null)
                    {
                        Debug.LogError("[GameManager] First place prize text is null! Please assign 'firstPlacePrizeText' in WinnerScreen component in Unity Inspector.");
                    }
                    if (firstPrizeIcon != null && firstPrizeText != null)
                    {
                        Debug.Log("[GameManager] First place prize UI references are valid");
                    }
                }
                
                DisplayPlace(1, firstPlace.playerType, firstPlace.cpuIndex, firstPlace.count,
                            winnerProfileImage, winnerOutlineImage, winnerFrameImage, winnerNameText, winnerNameTextLegacy,
                            firstPrizeIcon, firstPrizeText, null,
                            firstCash, firstChips);
                Debug.Log($"[GameManager] 1st Place: {GetWinnerName(firstPlace.playerType, firstPlace.cpuIndex)} ({firstPlace.playerType}, CPU index: {firstPlace.cpuIndex}) with {firstPlace.count} selected boxes! Prize: {firstCash} cash, {firstChips} chips");
            }
            else
            {
                // No prize for 1st place, hide it
                HidePlace(winnerProfileImage, winnerOutlineImage, winnerFrameImage, winnerNameText, winnerNameTextLegacy,
                         firstPrizeIcon, firstPrizeText, null);
                Debug.Log("[GameManager] No prize for 1st place (cash and chips are both 0), hiding 1st place UI");
            }
        }
        
        // Display 2nd place (only if there's a prize)
        if (rankings.Count > 1)
        {
            var secondPlace = rankings[1];
            float secondCash = currentLobbyConfig != null ? currentLobbyConfig.secondPlaceCash : 0f;
            int secondChips = currentLobbyConfig != null ? currentLobbyConfig.secondPlaceChips : 0;
            // Get prize UI from WinnerScreen
            Image secondPrizeIcon = winnerScreenScript != null ? winnerScreenScript.GetSecondPlacePrizeIcon() : null;
            TMPro.TextMeshProUGUI secondPrizeText = winnerScreenScript != null ? winnerScreenScript.GetSecondPlacePrizeText() : null;
            
            // Check if there's any prize for 2nd place
            if (secondCash > 0f || secondChips > 0)
            {
                DisplayPlace(2, secondPlace.playerType, secondPlace.cpuIndex, secondPlace.count,
                            secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy,
                            secondPrizeIcon, secondPrizeText, null,
                            secondCash, secondChips);
                Debug.Log($"[GameManager] 2nd Place: {GetWinnerName(secondPlace.playerType, secondPlace.cpuIndex)} ({secondPlace.playerType}, CPU index: {secondPlace.cpuIndex}) with {secondPlace.count} selected boxes! Prize: {secondCash} cash, {secondChips} chips");
            }
            else
            {
                // No prize for 2nd place, hide it
                HidePlace(secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy,
                         secondPrizeIcon, secondPrizeText, null);
                Debug.Log("[GameManager] No prize for 2nd place (cash and chips are both 0), hiding 2nd place UI");
            }
        }
        else
        {
            // Hide 2nd place UI if no 2nd place
            Image secondPrizeIconHide = winnerScreenScript != null ? winnerScreenScript.GetSecondPlacePrizeIcon() : null;
            TMPro.TextMeshProUGUI secondPrizeTextHide = winnerScreenScript != null ? winnerScreenScript.GetSecondPlacePrizeText() : null;
            HidePlace(secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy,
                     secondPrizeIconHide, secondPrizeTextHide, null);
        }
        
        // Display 3rd place (only if there's a prize)
        if (rankings.Count > 2)
        {
            var thirdPlace = rankings[2];
            float thirdCash = currentLobbyConfig != null ? currentLobbyConfig.thirdPlaceCash : 0f;
            int thirdChips = currentLobbyConfig != null ? currentLobbyConfig.thirdPlaceChips : 0;
            // Get prize UI from WinnerScreen
            Image thirdPrizeIcon = winnerScreenScript != null ? winnerScreenScript.GetThirdPlacePrizeIcon() : null;
            TMPro.TextMeshProUGUI thirdPrizeText = winnerScreenScript != null ? winnerScreenScript.GetThirdPlacePrizeText() : null;
            
            // Check if there's any prize for 3rd place
            if (thirdCash > 0f || thirdChips > 0)
            {
                if (winnerScreenScript != null)
                {
                    if (thirdPrizeIcon == null)
                    {
                        Debug.LogError("[GameManager] Third place prize icon is null! Please assign 'thirdPlacePrizeIcon' in WinnerScreen component in Unity Inspector.");
                    }
                    if (thirdPrizeText == null)
                    {
                        Debug.LogError("[GameManager] Third place prize text is null! Please assign 'thirdPlacePrizeText' in WinnerScreen component in Unity Inspector.");
                    }
                }
                
                DisplayPlace(3, thirdPlace.playerType, thirdPlace.cpuIndex, thirdPlace.count,
                            thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy,
                            thirdPrizeIcon, thirdPrizeText, null,
                            thirdCash, thirdChips);
                Debug.Log($"[GameManager] 3rd Place: {GetWinnerName(thirdPlace.playerType, thirdPlace.cpuIndex)} ({thirdPlace.playerType}, CPU index: {thirdPlace.cpuIndex}) with {thirdPlace.count} selected boxes! Prize: {thirdCash} cash, {thirdChips} chips");
            }
            else
            {
                // No prize for 3rd place, hide it
                HidePlace(thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy,
                         thirdPrizeIcon, thirdPrizeText, null);
                Debug.Log("[GameManager] No prize for 3rd place (cash and chips are both 0), hiding 3rd place UI");
            }
        }
        else
        {
            // Hide 3rd place UI if no 3rd place
            Image thirdPrizeIconHide = winnerScreenScript != null ? winnerScreenScript.GetThirdPlacePrizeIcon() : null;
            TMPro.TextMeshProUGUI thirdPrizeTextHide = winnerScreenScript != null ? winnerScreenScript.GetThirdPlacePrizeText() : null;
            HidePlace(thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy,
                     thirdPrizeIconHide, thirdPrizeTextHide, null);
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
    /// Gets the WinnerScreen script reference (ensures it's initialized)
    /// </summary>
    private WinnerScreen GetWinnerScreenScript()
    {
        if (winnerScreenScript != null)
        {
            return winnerScreenScript;
        }
        
        if (winnerScreen != null)
        {
            winnerScreenScript = winnerScreen.GetComponent<WinnerScreen>();
            return winnerScreenScript;
        }
        
        return null;
    }
    
    /// <summary>
    /// Displays a place (1st, 2nd, or 3rd) with profile image, outline, frame, name, prize icon, and prize text
    /// </summary>
    private void DisplayPlace(int placeNumber, PlayerType playerType, int cpuIndex, int selectionCount,
                              Image profileImage, Image outlineImage, Image frameImage, TMPro.TextMeshProUGUI nameText, Text nameTextLegacy,
                              Image prizeIcon, TMPro.TextMeshProUGUI prizeText, Text prizeTextLegacy,
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
        
        // Get sprites from WinnerScreen
        WinnerScreen winnerScreenComponent = GetWinnerScreenScript();
        Sprite cashSprite = winnerScreenComponent != null ? winnerScreenComponent.GetCashSprite() : null;
        Sprite chipsSprite = winnerScreenComponent != null ? winnerScreenComponent.GetChipsSprite() : null;
        
        Debug.Log($"[GameManager] DisplayPlace - Place: {placeNumber}, Cash: {cashPrize}, Chips: {chipsPrize}");
        Debug.Log($"[GameManager] PrizeIcon: {prizeIcon != null}, PrizeText: {prizeText != null}");
        Debug.Log($"[GameManager] CashSprite: {cashSprite != null}, ChipsSprite: {chipsSprite != null}");
        
        // Set prize icon and text (only one prize type exists at a time - cash OR chips)
        if (cashPrize > 0f)
        {
            // Show cash prize
            if (prizeIcon != null)
            {
                if (cashSprite != null)
                {
                    prizeIcon.sprite = cashSprite;
                    prizeIcon.gameObject.SetActive(true);
                    Debug.Log($"[GameManager] Set cash sprite to prize icon for place {placeNumber}");
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Cash sprite is null! Cannot set prize icon for place {placeNumber}");
                    prizeIcon.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] Prize icon is null for place {placeNumber}!");
            }
            
            // Always set prize text if it exists (even if sprite is missing)
            if (prizeText != null)
            {
                prizeText.text = "$" + PlayerProfileUI.FormatCash(cashPrize);
                prizeText.gameObject.SetActive(true);
                Debug.Log($"[GameManager] Set prize text to ${PlayerProfileUI.FormatCash(cashPrize)} for place {placeNumber}");
            }
            else
            {
                Debug.LogError($"[GameManager] Prize text is null for place {placeNumber}! Make sure it's assigned in PlayerProfileUI.");
            }
        }
        else if (chipsPrize > 0)
        {
            // Show chips prize
            if (prizeIcon != null)
            {
                if (chipsSprite != null)
                {
                    prizeIcon.sprite = chipsSprite;
                    prizeIcon.gameObject.SetActive(true);
                    Debug.Log($"[GameManager] Set chips sprite to prize icon for place {placeNumber}");
                }
                else
                {
                    Debug.LogWarning($"[GameManager] Chips sprite is null! Cannot set prize icon for place {placeNumber}");
                    prizeIcon.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] Prize icon is null for place {placeNumber}!");
            }
            
            // Always set prize text if it exists (even if sprite is missing)
            if (prizeText != null)
            {
                prizeText.text = PlayerProfileUI.FormatCoins(chipsPrize);
                prizeText.gameObject.SetActive(true);
                Debug.Log($"[GameManager] Set prize text to {PlayerProfileUI.FormatCoins(chipsPrize)} chips for place {placeNumber}");
            }
            else
            {
                Debug.LogError($"[GameManager] Prize text is null for place {placeNumber}! Make sure it's assigned in PlayerProfileUI.");
            }
        }
        else
        {
            // Hide prize icon and text if no prize
            if (prizeIcon != null)
            {
                prizeIcon.gameObject.SetActive(false);
            }
            if (prizeText != null)
            {
                prizeText.gameObject.SetActive(false);
            }
            Debug.Log($"[GameManager] No prize for place {placeNumber}, hiding icon and text");
        }
    }
    
    /// <summary>
    /// Hides a place UI (for when there are fewer than 3 players)
    /// </summary>
    private void HidePlace(Image profileImage, Image outlineImage, Image frameImage, TMPro.TextMeshProUGUI nameText, Text nameTextLegacy,
                          Image prizeIcon, TMPro.TextMeshProUGUI prizeText, Text prizeTextLegacy)
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
        if (prizeIcon != null)
        {
            prizeIcon.gameObject.SetActive(false);
        }
        if (prizeText != null)
        {
            prizeText.gameObject.SetActive(false);
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
        
        // Available colors from hex values
        Color[] availableColors = new Color[]
        {
            ParseHexColor("#7026BA"), // Purple
            ParseHexColor("#260E45"), // Dark Purple
            ParseHexColor("#FF66FF"), // Magenta/Pink
            ParseHexColor("#B165FF"), // Light Purple
            ParseHexColor("#86E21B"), // Green
            ParseHexColor("#FFD24D")  // Yellow
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
    /// Gets a random color from available color palette
    /// </summary>
    private Color GetRandomColor()
    {
        Color[] colors = new Color[]
        {
            ParseHexColor("#FF00C8"), // Purple
            ParseHexColor("#FF6A00"), // Dark Purple
            ParseHexColor("#FFE100"), // Magenta/Pink
            ParseHexColor("#0DFF00"), // Light Purple
            ParseHexColor("#00F6FF"), // Green
            ParseHexColor("#FF0000")  // Yellow
        };
        
        // Randomly select one of the colors
        int randomIndex = Random.Range(0, colors.Length);
        return colors[randomIndex];
    }
    
    /// <summary>
    /// Parses a hex color string to Unity Color
    /// </summary>
    private Color ParseHexColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        
        // Fallback if parsing fails
        Debug.LogWarning($"[GameManager] Failed to parse hex color: {hex}, using white as fallback");
        return Color.white;
    }
    
    /// <summary>
    /// Manually reassign outline colors
    /// </summary>
    public void ReassignOutlineColors()
    {
        AssignOutlineColors();
    }
    
    /// <summary>
    /// Updates outline image fill amounts based on current selection counts from individual boards
    /// Each selection fills the outline by 4% (0.04)
    /// </summary>
    private void UpdateOutlineFillAmounts()
    {
        const float FILL_PER_SELECTION = 0.04f; // 4% per selection
        
        // Update player outline fill from player's own board
        if (playerOutlineImage != null && playerNumberGrid != null)
        {
            Dictionary<string, int> playerSelectionCounts = playerNumberGrid.GetSelectionCounts();
            int playerCount = 0;
            // Sum all selections from player's board
            foreach (var kvp in playerSelectionCounts)
            {
                playerCount += kvp.Value;
            }
            float fillAmount = playerCount * FILL_PER_SELECTION;
            fillAmount = Mathf.Clamp01(fillAmount); // Clamp between 0 and 1
            playerOutlineImage.fillAmount = fillAmount;
        }
        
        // Update CPU outline fills from each CPU's own board
        if (cpuOutlineImages != null)
        {
            for (int i = 0; i < cpuOutlineImages.Length && i < numberOfCPUs; i++)
            {
                if (cpuOutlineImages[i] != null)
                {
                    int cpuCount = 0;
                    if (cpuNumberGrids.ContainsKey(i) && cpuNumberGrids[i] != null)
                    {
                        Dictionary<string, int> cpuSelectionCounts = cpuNumberGrids[i].GetSelectionCounts();
                        // Sum all selections from this CPU's board
                        foreach (var kvp in cpuSelectionCounts)
                        {
                            cpuCount += kvp.Value;
                        }
                    }
                    float fillAmount = cpuCount * FILL_PER_SELECTION;
                    fillAmount = Mathf.Clamp01(fillAmount); // Clamp between 0 and 1
                    cpuOutlineImages[i].fillAmount = fillAmount;
                }
            }
        }
    }
    
    /// <summary>
    /// Resets outline image fill amounts to 0
    /// </summary>
    private void ResetOutlineFillAmounts()
    {
        // Reset player outline fill
        if (playerOutlineImage != null)
        {
            playerOutlineImage.fillAmount = 0f;
        }
        
        // Reset CPU outline fills
        if (cpuOutlineImages != null)
        {
            for (int i = 0; i < cpuOutlineImages.Length; i++)
            {
                if (cpuOutlineImages[i] != null)
                {
                    cpuOutlineImages[i].fillAmount = 0f;
                }
            }
        }
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
        
        // Reset outline fill amounts
        ResetOutlineFillAmounts();
        
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
        
        // Reset outline fill amounts for new game
        ResetOutlineFillAmounts();
        
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
        
        // Set new lobby configuration
        currentLobbyConfig = config;
        
        // Set number of CPUs based on lobby configuration
        int numberOfCPUs = config.GetNumberOfCPUs();
        
        // Create individual NumberGrid instances for player and each CPU
        CreateIndividualNumberGrids(numberOfCPUs);
        
        // Wait a frame to ensure NumberGrids are fully created before restarting CPUs
        StartCoroutine(ConfigureCPUsAfterDelay(numberOfCPUs));
        
        Debug.Log($"[GameManager] Game configured from Lobby {config.lobbyIndex }: {config.numberOfPlayers} players ({numberOfCPUs} CPUs) Name = {config.lobbyName}  ");
        
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
        // Ensure WinnerScreen script reference is initialized
        if (winnerScreenScript == null && winnerScreen != null)
        {
            winnerScreenScript = winnerScreen.GetComponent<WinnerScreen>();
        }
        
        if (winnerScreenScript != null)
        {
            Image firstPrizeIconReset = winnerScreenScript.GetFirstPlacePrizeIcon();
            TMPro.TextMeshProUGUI firstPrizeTextReset = winnerScreenScript.GetFirstPlacePrizeText();
            if (firstPrizeIconReset != null)
            {
                firstPrizeIconReset.gameObject.SetActive(true);
            }
            if (firstPrizeTextReset != null)
            {
                firstPrizeTextReset.gameObject.SetActive(true);
            }
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
        if (winnerScreenScript != null)
        {
            Image secondPrizeIconReset = winnerScreenScript.GetSecondPlacePrizeIcon();
            TMPro.TextMeshProUGUI secondPrizeTextReset = winnerScreenScript.GetSecondPlacePrizeText();
            if (secondPrizeIconReset != null)
            {
                secondPrizeIconReset.gameObject.SetActive(true);
            }
            if (secondPrizeTextReset != null)
            {
                secondPrizeTextReset.gameObject.SetActive(true);
            }
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
        if (winnerScreenScript != null)
        {
            Image thirdPrizeIconReset = winnerScreenScript.GetThirdPlacePrizeIcon();
            TMPro.TextMeshProUGUI thirdPrizeTextReset = winnerScreenScript.GetThirdPlacePrizeText();
            if (thirdPrizeIconReset != null)
            {
                thirdPrizeIconReset.gameObject.SetActive(true);
            }
            if (thirdPrizeTextReset != null)
            {
                thirdPrizeTextReset.gameObject.SetActive(true);
            }
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
        PlayerProfileUI playerProfileUI = FindObjectOfType<PlayerProfileUI>(true);
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
    
    /// <summary>
    /// Creates individual NumberGrid instances for player and each CPU
    /// Each player/CPU gets their own independent board
    /// </summary>
    private void CreateIndividualNumberGrids(int numberOfCPUs)
    {
        // Clean up existing individual grids first
        DestroyIndividualNumberGrids();
        
        // Determine parent transform for grids
        Transform parentTransform = gridsParent != null ? gridsParent : 
                                   (gameplayScreen != null ? gameplayScreen.transform : transform);
        
        // Validate that we have either a prefab or existing numberGrid to use as template
        if (numberGridPrefab == null && numberGrid == null)
        {
            Debug.LogError("[GameManager] Cannot create individual grids - no NumberGrid prefab or existing NumberGrid available!");
            return;
        }
        
        // Disable the original numberGrid from scene (if it exists) since we're replacing it with individual grids
        if (numberGrid != null)
        {
            numberGrid.gameObject.SetActive(false);
            // Unsubscribe from original grid events
            numberGrid.OnAllBoxesSelected -= OnAllBoxesSelected;
            numberGrid.OnBoxSelected -= OnBoxSelected;
            Debug.Log("[GameManager] Disabled original NumberGrid from scene (replaced with individual grids)");
        }
        
        // Create player's NumberGrid
        playerNumberGrid = CreateNumberGridInstance("PlayerGrid", parentTransform);
        if (playerNumberGrid != null)
        {
            allNumberGrids.Add(playerNumberGrid);
            
            // Assign timer UI references from original NumberGrid to PlayerGrid
            if (numberGrid != null)
            {
                AssignTimerUIReferences(numberGrid, playerNumberGrid);
            }
            
            // Subscribe to player grid events
            playerNumberGrid.OnAllBoxesSelected += OnAllBoxesSelected;
            playerNumberGrid.OnBoxSelected += OnBoxSelected;
            // Ensure player grid is active and visible
            playerNumberGrid.gameObject.SetActive(true);
            // Hide CPU grids visually (only show player's grid)
            // Player grid stays visible at default position
            Debug.Log("[GameManager] Created player's NumberGrid");
        }
        
        // Create CPU NumberGrids (hidden off-screen or in separate area)
        for (int i = 0; i < numberOfCPUs; i++)
        {
            NumberGrid cpuGrid = CreateNumberGridInstance($"CPUGrid_{i}", parentTransform);
            if (cpuGrid != null)
            {
                cpuNumberGrids[i] = cpuGrid;
                allNumberGrids.Add(cpuGrid);
                
                // Subscribe to CPU grid events (game ends if any board is completed)
                cpuGrid.OnAllBoxesSelected += OnAllBoxesSelected;
                cpuGrid.OnBoxSelected += OnBoxSelected;
                
                // Hide CPU grids from view (position off-screen or disable renderer)
                // We can position them far off-screen or disable the GameObject
                cpuGrid.gameObject.SetActive(false); // Hide CPU grids completely
                // Alternative: Position off-screen: cpuGrid.transform.localPosition = new Vector3(10000 + i * 1000, 0, 0);
                
                Debug.Log($"[GameManager] Created CPU {i}'s NumberGrid (hidden)");
            }
        }
        
        // Assign grids to DicesController and CPUs
        AssignGridsToControllers();
    }
    
    /// <summary>
    /// Creates a NumberGrid instance from prefab or existing template
    /// </summary>
    private NumberGrid CreateNumberGridInstance(string name, Transform parent)
    {
        GameObject instance = null;
        bool templateWasDisabled = false;
        
        // Use prefab if available, otherwise duplicate the existing numberGrid GameObject
        if (numberGridPrefab != null)
        {
            instance = Instantiate(numberGridPrefab, parent);
        }
        else if (numberGrid != null)
        {
            // Check if template is disabled - we'll need to temporarily enable it or handle it differently
            templateWasDisabled = !numberGrid.gameObject.activeSelf;
            
            // If template is disabled, temporarily enable it for cloning, then disable it again
            if (templateWasDisabled)
            {
                numberGrid.gameObject.SetActive(true);
            }
            
            // Create a duplicate of the existing NumberGrid GameObject
            instance = Instantiate(numberGrid.gameObject, parent);
            
            // Restore template's original state
            if (templateWasDisabled)
            {
                numberGrid.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"[GameManager] Cannot create NumberGrid instance: {name} - no prefab or existing NumberGrid available!");
            return null;
        }
        
        instance.name = name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        
        // Always ensure the new instance is active (even if template was disabled)
        instance.SetActive(true);
        
        NumberGrid grid = instance.GetComponent<NumberGrid>();
        if (grid != null)
        {
            // Regenerate grid for this instance (creates new random layout)
            grid.GenerateGrid();
            return grid;
        }
        
        Debug.LogError($"[GameManager] Failed to create NumberGrid instance: {name} - component not found!");
        return null;
    }
    
    /// <summary>
    /// Destroys all individual NumberGrid instances
    /// </summary>
    private void DestroyIndividualNumberGrids()
    {
        // Unsubscribe from events before destroying
        foreach (NumberGrid grid in allNumberGrids)
        {
            if (grid != null)
            {
                grid.OnAllBoxesSelected -= OnAllBoxesSelected;
                grid.OnBoxSelected -= OnBoxSelected;
            }
        }
        
        // Destroy all grid instances
        foreach (NumberGrid grid in allNumberGrids)
        {
            if (grid != null && grid.gameObject != null)
            {
                Destroy(grid.gameObject);
            }
        }
        
        allNumberGrids.Clear();
        cpuNumberGrids.Clear();
        playerNumberGrid = null;
        
        Debug.Log("[GameManager] Destroyed all individual NumberGrid instances");
    }
    
    /// <summary>
    /// Assigns timer UI references (slider and text) from source NumberGrid to target NumberGrid
    /// </summary>
    private void AssignTimerUIReferences(NumberGrid sourceGrid, NumberGrid targetGrid)
    {
        if (sourceGrid == null || targetGrid == null)
        {
            Debug.LogWarning("[GameManager] Cannot assign timer UI references - source or target grid is null!");
            return;
        }
        
        // Use reflection to get and set the SerializeField references
        var sourceType = typeof(NumberGrid);
        
        // Get powerupTimerSlider field
        var sliderField = sourceType.GetField("powerupTimerSlider", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (sliderField != null)
        {
            var sliderValue = sliderField.GetValue(sourceGrid);
            sliderField.SetValue(targetGrid, sliderValue);
            Debug.Log("[GameManager] Assigned powerupTimerSlider to PlayerGrid");
        }
        else
        {
            Debug.LogWarning("[GameManager] Could not find powerupTimerSlider field in NumberGrid!");
        }
        
        // Get powerupTimerText field
        var textField = sourceType.GetField("powerupTimerText", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (textField != null)
        {
            var textValue = textField.GetValue(sourceGrid);
            textField.SetValue(targetGrid, textValue);
            Debug.Log("[GameManager] Assigned powerupTimerText to PlayerGrid");
        }
        else
        {
            Debug.LogWarning("[GameManager] Could not find powerupTimerText field in NumberGrid!");
        }
    }
    
    /// <summary>
    /// Assigns NumberGrid instances to DicesController and CPU scripts
    /// </summary>
    private void AssignGridsToControllers()
    {
        // Assign player's grid to DicesController
        if (playerDicesController != null && playerNumberGrid != null)
        {
            // Use reflection to set the numberGrid field in DicesController
            var field = typeof(DicesController).GetField("numberGrid", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(playerDicesController, playerNumberGrid);
                Debug.Log("[GameManager] Assigned player's NumberGrid to DicesController");
            }
            else
            {
                Debug.LogWarning("[GameManager] Could not find numberGrid field in DicesController!");
            }
        }
        
        // Assign CPU grids to CPU scripts
        if (cpuPlayers != null)
        {
            for (int i = 0; i < cpuPlayers.Length; i++)
            {
                if (cpuPlayers[i] != null && cpuNumberGrids.ContainsKey(i))
                {
                    // Use reflection to set the numberGrid field in CPU
                    var field = typeof(CPU).GetField("numberGrid", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(cpuPlayers[i], cpuNumberGrids[i]);
                        Debug.Log($"[GameManager] Assigned CPU {i}'s NumberGrid to CPU script");
                    }
                    else
                    {
                        Debug.LogWarning($"[GameManager] Could not find numberGrid field in CPU {i}!");
                    }
                }
            }
        }
    }
}

