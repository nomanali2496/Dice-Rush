#pragma warning disable 0414
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
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [Header("Player Profile")]
    [SerializeField] private Image playerProfileImage; // Player's profile image display
    [SerializeField] private Image playerProfileImageCustom; // Player's profile image (custom/zoomed)
    [SerializeField] private Image playerFrameImage; // Player's profile frame image
    [SerializeField] private Image playerOutlineImage; // Player's outline image (for color assignment)
    [SerializeField] private MainMenuAvatarDisplay mainMenuAvatarDisplay; // Reference to main menu avatar display (to get user's selected avatar)
    
    [Header("Opponent Profiles")]
    [SerializeField] private Image[] opponentProfileImages; // Array of profile image displays for opponents
    [SerializeField] private Image[] opponentFrameImages; // Array of profile frame images for opponents
    [SerializeField] private Image[] opponentOutlineImages; // Array of outline images for opponents
    
    [Header("Profile Image Settings")]
    [SerializeField] private Sprite[] availableProfileSprites; // Available profile sprites for CPUs (random selection)
    [SerializeField] private bool excludePlayerAvatarFromCPU = true; // Don't use player's avatar for CPUs
    
    // Recovery & Persistent Rankings state
    private Dictionary<int, string> lastKnownPlayerIds = new Dictionary<int, string>();
    
    // Server state tracking
    private Dictionary<string, int> networkPlayerScores = new Dictionary<string, int>();
    private Dictionary<string, double> networkPlayerTimes = new Dictionary<string, double>();
    
    [Header("References")]
    [SerializeField] private NumberGrid numberGrid; // Reference to NumberGrid (legacy - will be replaced by individual grids)
    [SerializeField] private DicesController playerDicesController; // Reference to player's DicesController
    [SerializeField] private GameObject numberGridPrefab; // Prefab for creating individual NumberGrid instances (if null, will use existing numberGrid as template)
    [SerializeField] private Transform gridsParent; // Parent transform for all NumberGrid instances (if null, will use gameplayScreen or this transform)
    
    [Header("Gameplay Screen")]
    [SerializeField] private GameObject gameplayScreen; // Gameplay screen GameObject (set active when game starts)
    [SerializeField] private Button quitButton; // Quit button (ends game when clicked)
    [SerializeField] private GameObject quitPanel; // Quit confirmation panel (shown when quit button is clicked)
    
    [SerializeField] private TMPro.TextMeshProUGUI timerText; // Timer text display (TextMeshProUGUI)
    [SerializeField] private Text timerTextLegacy; // Timer text display (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private float gameTimer = 120f; // Game timer in seconds (120 seconds)
    
    private float currentTimer; // Current timer value
    private Coroutine timerCoroutine; // Reference to the timer coroutine
    private bool isGameActive = false; // Flag to track if game is active
    public bool IsGameActive => isGameActive;
    public bool IsGameplayScreenActive => gameplayScreen != null && gameplayScreen.activeSelf;
    public bool IsQuitPanelActive => quitPanel != null && quitPanel.activeInHierarchy;
    public bool IsDiceGameplayActive => IsGameplayScreenActive && isGameActive && !winnerScreenShown && !isWaitingForServer && !IsQuitPanelActive;
    // True only when the player is mid-match (screen active AND winner results not yet shown).
    // Use this instead of IsGameplayScreenActive when deciding to sync/reconnect a live match.
    public bool IsActivelyPlaying => IsGameplayScreenActive && !winnerScreenShown;
    private bool timerStarted = false; // Flag to prevent multiple timer starts
    private float initialGameTimer; // Original game timer value to reset back to

    public const string GAME_EXTENSION_KEY = "GameExtensionCount"; 
    
    // Current lobby configuration
    private LobbyConfig currentLobbyConfig; // Current lobby configuration being played
    private StartGameData currentMatchData; // Current match data from socket
    
    [Header("Dynamic Backgrounds")]
    [SerializeField] private InfiniteBackgroundScroller backgroundScroller;
    [SerializeField] private Color[] lobbyBackgroundColors = new Color[] 
    {
        new Color(0.05f, 0.3f, 0.5f),   // Monaco Riviera (Blue)
        new Color(0.0f, 0.45f, 0.05f),  // Atlantic Boardwalk (Green)
        new Color(0.45f, 0.05f, 0.05f), // Macau Dragon (Red)
        new Color(0.15f, 0.15f, 0.15f), // Vegas Neon (Black)
        new Color(0.45f, 0.35f, 0.15f), // Singapore Marina (Gold)
        new Color(0.35f, 0.35f, 0.35f)  // London Royal (Gray)
    };
    
    // Win conditions
    private int currentWinConditionTarget = 50; // Target score or boxes to win

    [Header("Winner Screen")]
    [SerializeField] private GameObject winnerScreen; // Winner screen GameObject (set active when game ends)
    [SerializeField] private WinnerScreen winnerScreenScript; // Winner screen script for animations
    [SerializeField] private GameObject loserScoreScreen; // Screen shown for 4th, 5th, 6th place
    [SerializeField] private LoserScoreScreen loserScoreScreenScript; // Script for loser screen
    [SerializeField] private Button winnerScreenLobbyButton; // Lobby button on winner screen
    [SerializeField] private GameObject calculateLoadingScreen; // Loading screen shown while calculating results
    [SerializeField] private GameObject forfeitLoadingScreen; // Loading screen shown when user forfeits
    
    private float lastAwardedCash = 0f;
    private int lastAwardedChips = 0;
    private bool prizesAwardedForThisMatch = false; // Prevents infinite loop/duplicate prizes
    private bool winnerScreenShown = false; // Track if we've already displayed the results
    private bool isWaitingForServer = false; // Track if we are waiting for server gameOver
    private bool localPlayerForfeited = false; // Prevents winner UI after this user forfeits
    private Coroutine winnerScreenDelayCoroutine;
    private int localMatchScore = 0; // The persistent running score for the local player match
    
    public (float cash, int chips) GetLastAwardedPrize() => (lastAwardedCash, lastAwardedChips);
    
    [Header("Countdown")]
    [SerializeField] private TMPro.TextMeshProUGUI countdownText; // Countdown text display ("3", "2", "1")
    [SerializeField] private TMPro.TextMeshProUGUI countdownTextShadow; // Countdown text shadow display ("3", "2", "1")
    [SerializeField] private float countdownDuration = 1f; // Duration for each countdown number (in seconds)
    
    [SerializeField] private Color playerOutlineColor = Color.white;
    [SerializeField] private Color[] opponentOutlineColors;
    
    [Header("UI Panels")]
    
    private Sprite playerSelectedAvatar; // Store player's selected avatar sprite
    private List<Sprite> availableOpponentAvatars; // List of available avatars for opponents
    
    [Header("Network Caching")]
    private Dictionary<int, string> cachedNetworkNames = new Dictionary<int, string>();
    private Dictionary<int, Sprite> cachedNetworkAvatars = new Dictionary<int, Sprite>(); // Quality 2: Custom Gallery Images
    private Dictionary<int, int> cachedNetworkAvatarIndices = new Dictionary<int, int>(); // Quality 1: Preset Avatar 0-7

    // Constants for profile slot states
    private const float CUSTOM_AVATAR_SCALE = 2.0f;
    private bool wasInRoomLastFrame = false;
    
    // Individual NumberGrid instances (one per player/opponent)
    private NumberGrid playerNumberGrid; // Player's own NumberGrid instance
    private Dictionary<int, NumberGrid> opponentNumberGrids = new Dictionary<int, NumberGrid>(); // Opponent NumberGrid instances by index
    private List<NumberGrid> allNumberGrids = new List<NumberGrid>(); // All NumberGrid instances (for cleanup)
    
    // Cache refresh timing
    private float cacheRefreshInterval = 2.0f;
    private float lastCacheRefreshTime = 0f;
    
    // Tie-breaker and Disconnection persistence
    private Dictionary<int, double> playerLastScoreTime = new Dictionary<int, double>();
    private Coroutine retryCoroutine;
    private Coroutine reconnectSyncCoroutine;

    // Events for separate frontend/backend handling
    public static System.Action OnGameStarted;
    public static System.Action OnGameEnded;
    
    private void Start()
    {
        // Store initial game timer for resetting later
        initialGameTimer = gameTimer;

        // Subscriptions removed as we are using custom backend
        
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

        // Subscribe to winner screen lobby button
        if (winnerScreenLobbyButton != null)
        {
            winnerScreenLobbyButton.onClick.AddListener(OnWinnerLobbyButtonClicked);
        }
        
        // Initialize profiles
        StartCoroutine(InitializeProfilesDelayed());
        
        // Hide screens initially
        if (winnerScreen != null)
            winnerScreen.SetActive(false);
            
        if (calculateLoadingScreen != null)
            calculateLoadingScreen.SetActive(false);
            
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
               // Hide quit panel initially
        if (quitPanel != null)
            quitPanel.SetActive(false);

        // Subscribe to Socket events
        MatchmakingModel.OnNumberLockedEvent += HandleServerNumberLocked;
        MatchmakingModel.OnGameOverEvent += HandleServerGameOver;
        MatchmakingModel.OnCountdownEvent += HandleServerCountdown;
        MatchmakingModel.OnDiceRolledEvent += HandleServerDiceRolled;
        MatchmakingModel.OnTapResultEvent += HandleServerTapResult;
    }

    private void HandleServerGameOver(GameOverData data)
    {
        if (serverGameOverTimeoutCoroutine != null)
        {
            StopCoroutine(serverGameOverTimeoutCoroutine);
            serverGameOverTimeoutCoroutine = null;
        }

        if (data == null) return;

        bool localForfeitPending = MatchmakingModel.ConsumeLocalForfeitPending();
        if (localPlayerForfeited || localForfeitPending)
        {
            Debug.Log("[GameManager] Local player forfeited. Consuming server gameOver without showing winner screen.");
            CompleteForfeitCleanup();
            return;
        }
        
        // If the player manually quit, ignore the server gameOver.
        // BUT if the local timer just expired and we are waiting for the server, process it!
        if (!isGameActive && !isWaitingForServer)
        {
            Debug.Log("[GameManager] Game already inactive and not waiting for server, ignoring gameOver event.");
            return;
        }

        Debug.Log($"<color=#00FFFF>⬇️ [GameManager] Server GameOver received. Winner: {data.winnerId}, Reason: {data.reason}</color>");

        // Notify listeners (like SettingManager) that gameplay music should stop
        OnGameEnded?.Invoke();
        
        // Refresh profile from backend to sync final balance and winnings
        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.GetProfile(null);
        }
        
        // Hide the "Calculating Results" screen if it was shown
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);

        // If we already showed the screen (e.g. in Solo mode or duplicate packet), ignore
        if (winnerScreenShown) 
        {
            Debug.Log("[GameManager] Winner screen already shown. Skipping server gameOver trigger.");
            return;
        }

        winnerScreenShown = true;
        isGameActive = false;
        timerStarted = false;
        isWaitingForServer = false;

        // Clear recovery persistence locally so the lobby doesn't prompt for rejoin
        PlayerModel.UpdateData(
            PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
            PlayerModel.Wins, PlayerModel.Losses, PlayerModel.Balance,
            PlayerModel.CashBalance, PlayerModel.BonusBalance, PlayerModel.WinningBalance,
            PlayerModel.Coins, PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level, PlayerModel.Role,
            PlayerModel.SelectedAvatarIndex, null, null
        );

        // Stop local timer coroutine
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        // Convert server rankings to the format expected by ShowWinnerScreen
        // List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)>
        var rankings = new List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)>();

        // Server is authoritative — use its earnings/coinsEarnings directly.
        lastAwardedCash  = (float)data.earnings.GetValueOrDefault(PlayerModel.Id, 0);
        lastAwardedChips = (int)(data.coinsEarnings?.GetValueOrDefault(PlayerModel.Id, 0) ?? 0);

        // Ensure data.players is populated with all known players from the match
        if (data.players == null) data.players = new System.Collections.Generic.Dictionary<string, PlayerAvatarInfo>();
        if (currentMatchData != null && currentMatchData.playerAvatars != null)
        {
            foreach (var kvp in currentMatchData.playerAvatars)
            {
                if (!data.players.ContainsKey(kvp.Key))
                {
                    data.players[kvp.Key] = kvp.Value;
                }
            }
        }

        // Build rankings from ALL known players
        var allPlayerIds = new System.Collections.Generic.HashSet<string>();
        if (data.players != null)
        {
            foreach (var key in data.players.Keys) allPlayerIds.Add(key);
        }
        if (data.scores != null)
        {
            foreach (var key in data.scores.Keys) allPlayerIds.Add(key);
        }

        int oppIdx = 0;
        foreach (string userId in allPlayerIds)
        {
            int score = (data.scores != null && data.scores.TryGetValue(userId, out int s)) ? s : 0;
            
            PlayerType type = (userId == PlayerModel.Id) ? PlayerType.Player : PlayerType.Network;
            double time = (userId == data.winnerId) ? 0 : 999.0;

            int index = (type == PlayerType.Player) ? -1 : oppIdx++;
            rankings.Add((type, index, score, time, userId));
        }

        // Sort rankings by score (descending)
        rankings.Sort((a, b) => b.count.CompareTo(a.count));


        // Show loading screen first
        if (calculateLoadingScreen != null)
        {
            calculateLoadingScreen.SetActive(true);
            Debug.Log("[GameManager] Show calculateLoadingScreen before winner screen");
        }

        // Show the winner screen after a short delay
        winnerScreenDelayCoroutine = StartCoroutine(ShowWinnerScreenWithDelay(rankings, data));
    }

    private void CompleteForfeitCleanup()
    {
        OnGameEnded?.Invoke();

        if (forfeitTimeoutCoroutine != null)
        {
            StopCoroutine(forfeitTimeoutCoroutine);
            forfeitTimeoutCoroutine = null;
        }

        if (winnerScreenDelayCoroutine != null)
        {
            StopCoroutine(winnerScreenDelayCoroutine);
            winnerScreenDelayCoroutine = null;
        }

        if (forfeitLoadingScreen != null) forfeitLoadingScreen.SetActive(false);
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);
        if (winnerScreen != null) winnerScreen.SetActive(false);
        if (loserScoreScreen != null) loserScoreScreen.SetActive(false);
        if (gameplayScreen != null) gameplayScreen.SetActive(false);

        PlayerModel.UpdateData(
            PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
            PlayerModel.Wins, PlayerModel.Losses, PlayerModel.Balance,
            PlayerModel.CashBalance, PlayerModel.BonusBalance, PlayerModel.WinningBalance,
            PlayerModel.Coins, PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level, PlayerModel.Role,
            PlayerModel.SelectedAvatarIndex, null, null
        );

        ResetGameState();
        localPlayerForfeited = false;

        HorizontalLobbyManager lobbyManager = HorizontalLobbyManager.Instance != null 
            ? HorizontalLobbyManager.Instance 
            : FindFirstObjectByType<HorizontalLobbyManager>(FindObjectsInactive.Include);
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyOpened();
        }
    }

    /// <summary>
    /// Waits for a short duration with the loading screen active before showing the winner screen
    /// </summary>
    private System.Collections.IEnumerator ShowWinnerScreenWithDelay(List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)> rankings, GameOverData data)
    {
        // Wait for 0.8 seconds to calculate results before showing winner screen
        yield return new WaitForSeconds(0.8f);

        // Hide loading screen immediately after calculation delay
        if (calculateLoadingScreen != null)
        {
            calculateLoadingScreen.SetActive(false);
        }
        if (forfeitLoadingScreen != null)
        {
            forfeitLoadingScreen.SetActive(false);
        }

        if (localPlayerForfeited)
        {
            winnerScreenDelayCoroutine = null;
            yield break;
        }

        // Check if the local player is in the top 3
        bool isLocalPlayerInTop3 = false;
        int localPlayerScore = 0;
        for (int i = 0; i < rankings.Count; i++)
        {
            if (rankings[i].playerType == PlayerType.Player)
            {
                if (i < 3) isLocalPlayerInTop3 = true;
                localPlayerScore = rankings[i].count;
                break;
            }
        }

        // Hide 3D dice when match completes and result screen opens
        if (playerDicesController != null)
        {
            playerDicesController.HideDices();
        }
        DiceVisibilityManager dvm = FindFirstObjectByType<DiceVisibilityManager>();
        if (dvm != null)
        {
            dvm.ForceHideDices();
        }

        if (!isLocalPlayerInTop3)
        {
            // Player is not in top 3, show Loser Score Screen instead
            if (loserScoreScreen != null && loserScoreScreenScript != null)
            {
                loserScoreScreen.SetActive(true);
                loserScoreScreenScript.Setup(localPlayerScore);
                winnerScreenShown = true;
            }
            else
            {
                OnWinnerLobbyButtonClicked(); // Fallback if screen isn't set
            }
            yield break;
        }

        // Show the panel first so Awake/Start runs
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(true);
            winnerScreenShown = true;
        }

        // Setup the new WinnerScreen logic
        if (winnerScreenScript != null)
        {
            winnerScreenScript.SetupWinnerResults(rankings, data);

            // NEW: Trigger the prize animation for the local player if they won something
            winnerScreenScript.PlayAnimationIfPlayerInTop3(rankings, currentLobbyConfig);
        }

        winnerScreenDelayCoroutine = null;
    }

    private void HandleServerNumberLocked(NumberLockedData data)
    {
        if (data != null)
        {
            if (data.userId != PlayerModel.Id)
            {
                networkPlayerScores[data.userId] = data.score;
                networkPlayerTimes[data.userId] = GetEstimatedServerTime();
                Debug.Log($"[GameManager] Sync: Player {data.userId} updated score to: {data.score}");
            }
            else
            {
                // We keep track of the local player's true server score but DO NOT force the UI to update immediately.
                networkPlayerScores[data.userId] = data.score;
                Debug.Log($"[GameManager] Silent Sync: Local player true server score is: {data.score}");
            }
        }
    }

    private void HandleServerDiceRolled(DiceRolledData data)
    {
        // When the dice rolls, the previous round is OVER. No more pending rapid taps.
        // If there is a discrepancy between our optimistic score and the server's true score,
        // it means a tap was dropped/rejected silently. We correct the UI now.
        if (networkPlayerScores.TryGetValue(PlayerModel.Id, out int trueServerScore))
        {
            if (localMatchScore != trueServerScore)
            {
                Debug.Log($"[GameManager] Dice Roll Correction: UI Score ({localMatchScore}) differs from Server Score ({trueServerScore}). Correcting UI.");
                localMatchScore = trueServerScore;
                
                if (ScoreFeedbackUIManager.Instance != null)
                {
                    ScoreFeedbackUIManager.Instance.SetScore(localMatchScore);
                }
            }
        }
    }

    private void HandleServerTapResult(TapResultData data)
    {
        if (data != null)
        {
            // Silently update the local player's true server score (especially important for -50 wrong tap penalties)
            networkPlayerScores[PlayerModel.Id] = data.score;
            Debug.Log($"[GameManager] Silent Sync: Local player true server score updated from TapResult to: {data.score}");
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
        if (gameplayScreen != null && gameplayScreen.activeSelf && !timerStarted && !winnerScreenShown && !isWaitingForServer)
        {
            Debug.Log("[GameManager] Gameplay screen is active, starting timer...");
            OnGameplayScreenActivated();
        }
    }


    /// <summary>
    /// Called when a pending reward is claimed (match won while away)
    /// Shows the winner screen UI even when returning after a disconnect.
    /// </summary>
    private void OnOfflineRewardClaimed(string message, float cash, int chips, string rankingsJson)
    {
        Debug.Log($"[GameManager] 🏆 Offline reward DETECTED: '{message}' | Cash: {cash} | Chips: {chips}");

        // NEW: Hide the loading screen now that we are ready to show the winner screen
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);
        
        // 0. Force Profile Image Load FIRST so it's ready for DisplayPlace
        LoadPlayerProfileImage();
        
        // Safety: If game is already active, don't show the lobby popup
        if (isGameActive) 
        {
             Debug.LogWarning($"[GameManager] ⚠️ Offline reward BLOCKED: isGameActive is {isGameActive}. We only show this in the Lobby.");
             return;
        }

        // Validate the GameObject exists
        if (winnerScreen == null)
        {
            Debug.LogError("[GameManager] ❌ ERROR: 'winnerScreen' GameObject is not assigned in the Inspector!");
            return;
        }

        // Ensure WinnerScreen script reference is initialized
        WinnerScreen ws = GetWinnerScreenScript();
        if (ws == null)
        {
            Debug.LogError("[GameManager] ❌ ERROR: Could not find WinnerScreen script on the assigned GameObject!");
            return;
        }

        // 1. Setup prize tracking for animation
        lastAwardedCash = cash;
        lastAwardedChips = chips;
        prizesAwardedForThisMatch = true;
        
        // Activate visuals
        if (winnerScreen != null) winnerScreen.SetActive(true);

        // Trigger animation
        if (ws != null) {
            ws.PlayAnimationIfPlayerInTop3(null, currentLobbyConfig);
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
        
        // Initialize participant tracking for this match
        networkPlayerScores.Clear();
        networkPlayerTimes.Clear();
        
        // Reset official prize tracking for the NEW match
        lastAwardedCash = 0f;
        lastAwardedChips = 0;
        prizesAwardedForThisMatch = false;
        
        // Reset score UI ONLY if this is a fresh game (not a recovery)
        if (currentMatchData == null || currentMatchData.timeRemaining <= 0)
        {
            localMatchScore = 0; 
            if (ScoreFeedbackUIManager.Instance != null)
            {
                ScoreFeedbackUIManager.Instance.ResetScore();
            }
        }
        else
        {
            // For recovery, ensure the UI is showing the current recovered score
            if (ScoreFeedbackUIManager.Instance != null)
            {
                ScoreFeedbackUIManager.Instance.SetScore(localMatchScore);
            }
        }
        
        long currentTimeMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bool isRecovery = currentMatchData != null && (currentMatchData.timeRemaining > 0 || currentTimeMs >= currentMatchData.gameplayStartsAt);

        if (isRecovery)
        {
            // Skip countdown for recovery
            if (currentMatchData != null && currentMatchData.gameplayStartsAt > 0)
            {
                double elapsedSeconds = (currentTimeMs - currentMatchData.gameplayStartsAt) / 1000.0;
                float durationToUse = gameTimer; // FORCE Inspector timer
                currentTimer = Mathf.Max(0, durationToUse - (float)elapsedSeconds);
            }
            else
            {
                currentTimer = gameTimer;
            }

            isGameActive = true;
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(TimerCountdown());
            
            // Ensure UI is correct for active game
            if (timerText != null) timerText.gameObject.SetActive(true);
            if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(true);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            
            if (playerDicesController != null) 
            {
                playerDicesController.ResetRollingState();
            }
            
            Debug.Log($"[GameManager] Match recovery detected. Skipping countdown. Adjusted Timer: {currentTimer}");
        }
        else
        {
            // We are either in countdown or waiting for it
            if (currentMatchData != null && currentMatchData.gameplayStartsAt > 0)
            {
                // We are in the countdown phase!
                float secondsUntilStart = (float)(currentMatchData.gameplayStartsAt - currentTimeMs) / 1000f;
                Debug.Log($"[GameManager] Rejoined during countdown. Seconds until start: {secondsUntilStart}");
                
                if (secondsUntilStart > 0)
                {
                    StartCoroutine(HandleRejoinedCountdown(secondsUntilStart));
                }
                else
                {
                    StartCoroutine(FinishCountdownSequence());
                }
            }
            else
            {
                // Fresh start - run local countdown
                if (timerText != null) timerText.gameObject.SetActive(false);
                if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(false);
                if (playerDicesController != null) playerDicesController.SetRollButtonInteractable(false);
                Debug.Log("[GameManager] Match started locally. Starting local 3,2,1 countdown...");
                StartCoroutine(LocalCountdownSequence());
            }
        }

        Debug.Log("[GameManager] Starting match. Score reset to 0.");
    }
    
    private System.Collections.IEnumerator HandleRejoinedCountdown(float secondsToWait)
    {
        // Setup countdown UI
        if (countdownText != null && countdownTextShadow != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownTextShadow.gameObject.SetActive(true);
            
            // Just show "..." or a generic message while waiting for the exact start if a number isn't already set
            if (string.IsNullOrEmpty(countdownText.text) || countdownText.text == "..." || !int.TryParse(countdownText.text, out _))
            {
                countdownText.text = "...";
                countdownTextShadow.text = "...";
            }
        }
        
        // Hide game timer
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(false);
        if (playerDicesController != null) playerDicesController.SetRollButtonInteractable(false);

        // Wait for the remaining duration
        yield return new WaitForSeconds(secondsToWait);
        
        // Finalize start
        StartCoroutine(FinishCountdownSequence());
    }

    private System.Collections.IEnumerator LocalCountdownSequence()
    {
        for (int i = 3; i >= 1; i--)
        {
            HandleServerCountdown(new CountdownData { count = i });
            yield return new WaitForSeconds(1f);
        }
    }

    private void HandleServerCountdown(CountdownData data)
    {
        if (data == null) return;
        Debug.Log($"[GameManager] Server Countdown: {data.count}");
        
        if (countdownText != null && countdownTextShadow != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownTextShadow.gameObject.SetActive(true);
            
            if (data.count >= 1)
            {
                countdownText.text = data.count.ToString();
                countdownTextShadow.text = data.count.ToString();
                
                if (SoundManager.instance != null) SoundManager.instance.PlayCountdown();
                
                countdownText.transform.localScale = Vector3.zero;
                countdownText.alpha = 0f;
                countdownTextShadow.transform.localScale = Vector3.zero;
                countdownTextShadow.alpha = 0f;
                
                float countdownScale = 1.5f * GetGameplayReferenceScale(countdownText.transform);
                countdownText.transform.DOScale(Vector3.one * countdownScale, 0.3f).SetEase(DG.Tweening.Ease.OutBack).SetLink(countdownText.gameObject);
                countdownText.DOFade(1f, 0.3f).SetLink(countdownText.gameObject);
                countdownTextShadow.transform.DOScale(Vector3.one * countdownScale, 0.3f).SetEase(DG.Tweening.Ease.OutBack).SetLink(countdownTextShadow.gameObject);
                countdownTextShadow.DOFade(1f, 0.3f).SetLink(countdownTextShadow.gameObject);
            }
            
            if (data.count == 1)
            {
                // After "1" finishes, initiate the start sequence
                StartCoroutine(FinishCountdownSequence());
            }
        }
    }
    
    private System.Collections.IEnumerator FinishCountdownSequence()
    {
        yield return new WaitForSeconds(1.0f);
        
        // Optionally flash "GO!" here if desired before hiding
        
        // Hide countdown text
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            countdownTextShadow.gameObject.SetActive(false);
        }
        
        // Show timer text again
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(true);
        
        // Start gameplay music
        if (SettingManager.instance != null) SettingManager.instance.OnCountdownComplete();
        
        OnGameStarted?.Invoke();
        StartAllPowerupTimers();
        
        if (playerDicesController != null) 
        {
            playerDicesController.SetRollButtonInteractable(true);
            playerDicesController.ShowDices();
            playerDicesController.ResetRollingState();
            playerDicesController.ClearDiceSumDisplay();
        }
        
        currentTimer = gameTimer;
        isGameActive = true;
        
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerCountdown());
        Debug.Log($"[GameManager] Server countdown complete! Game timer started: {gameTimer} seconds");
    }

    private float GetGameplayReferenceScale(Transform source)
    {
        GamePlayReferenceScaler scaler = source != null ? source.GetComponentInParent<GamePlayReferenceScaler>() : null;
        return scaler != null ? scaler.CurrentScale : 1f;
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
        
        // Start all opponent powerup timers
        foreach (var kvp in opponentNumberGrids)
        {
            if (kvp.Value != null)
            {
                kvp.Value.StartPowerupTimerFromGameManager();
                Debug.Log($"[GameManager] Started powerup timer for Opponent {kvp.Key} grid");
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
            // Update more frequently for smoother display
            yield return new WaitForSeconds(0.25f);
            
            if (currentMatchData != null && currentMatchData.gameplayStartsAt > 0)
            {
                // Sync to backend absolute start time
                double elapsedSeconds = (System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - currentMatchData.gameplayStartsAt) / 1000.0;
                float durationToUse = gameTimer; // FORCE Inspector timer
                currentTimer = Mathf.Max(0, durationToUse - (float)elapsedSeconds);
            }
            else
            {
                // Fallback for offline/legacy
                currentTimer -= 0.25f;
            }
            
            // Update display
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
    
    private bool wasOfflineLastFrame = false;
    private NetworkReachability lastKnownReachability = NetworkReachability.NotReachable;

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
        timerStarted = false; 
        Debug.Log("[GameManager] Match ended! Determining winner...");
        
        // Stop timer
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        // Multiplayer: Wait for server (Always required now)
        Debug.Log("[GameManager] Local timer expired. Waiting for server 'gameOver' event...");
        
        // Debug Socket State
        if (SocketManager.Instance != null && SocketManager.Instance.Socket != null) {
            Debug.Log($"[GameManager] Socket Status: {SocketManager.Instance.Socket.IsOpen} | ID: {SocketManager.Instance.Socket.Id}");
        } else {
            Debug.LogWarning("[GameManager] SocketManager or Socket is NULL at game end!");
        }

        isWaitingForServer = true;
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(true);
        StartServerGameOverTimeout();
    }

    /// <summary>
    /// Called when the lobby/finish button on the winner screen is clicked
    /// Leaves the room and cleans up game state
    /// </summary>
    public void OnWinnerLobbyButtonClicked()
    {
        Debug.Log("[GameManager] Winner lobby button clicked.");
        
        // Safety: Ensure gameplay music is stopped
        OnGameEnded?.Invoke();

        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);
        if (forfeitLoadingScreen != null) forfeitLoadingScreen.SetActive(false);

        // Return to lobby by disabling gameplay screen
        if (gameplayScreen != null)
        {
            gameplayScreen.SetActive(false);
        }
        
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(false);
        }
        
        if (loserScoreScreen != null)
        {
            loserScoreScreen.SetActive(false);
        }
        
        ResetGameState();

        HorizontalLobbyManager lobbyManager = HorizontalLobbyManager.Instance != null 
            ? HorizontalLobbyManager.Instance 
            : FindFirstObjectByType<HorizontalLobbyManager>(FindObjectsInactive.Include);
        if (lobbyManager != null)
        {
            lobbyManager.OnLobbyOpened();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from Socket events
        MatchmakingModel.OnNumberLockedEvent -= HandleServerNumberLocked;
        MatchmakingModel.OnGameOverEvent -= HandleServerGameOver;
        MatchmakingModel.OnCountdownEvent -= HandleServerCountdown;
        MatchmakingModel.OnDiceRolledEvent -= HandleServerDiceRolled;
        MatchmakingModel.OnTapResultEvent -= HandleServerTapResult;

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
    private void OnBoxSelected(NumberGrid grid, int number, GameObject boxObject)
    {
        UpdateOutlineFillAmounts();

        // 1. Identify who this board belongs to
        int ownerKey = -1; // Default to local player
        if (grid != playerNumberGrid)
        {
            // Check Opponents
            foreach (var kvp in opponentNumberGrids)
            {
                if (kvp.Value == grid) { ownerKey = kvp.Key; break; }
            }
            // If not local and not CPU, it could be a network grid? 
            // In your current architecture, human opponents use scores from server properties instead of grids.
        }

        // 2. Record the time of this score achievement for tie-breakers
        // FIX: Use estimated server time so offline selections use the SAME scale as online ones.
        double currentTotalTime = GetEstimatedServerTime();
        playerLastScoreTime[ownerKey] = currentTotalTime;

        // 3. Update local score for local player
        if (ownerKey == -1) 
        {
            // Play Correct Sound
            if (SoundManager.instance != null) SoundManager.instance.PlayCorrect();

            // NEW SCORING: 
            // - Selecting correct number: +100
            // - Selecting icon (if number is -1): +200
            if (number < 0) {
                localMatchScore += 200;
                Debug.Log($"[GameManager] 💎 Icon Selected! +200 Points (Total: {localMatchScore})");
                
                // Trigger visual feedback
                if (ScoreFeedbackUIManager.Instance != null && boxObject != null)
                {
                    ScoreFeedbackUIManager.Instance.ShowScoreFeedback(200, boxObject.transform.position);
                }
            } else {
                localMatchScore += 100;
                Debug.Log($"[GameManager] ✅ Correct Number! +100 Points (Total: {localMatchScore})");

                if (ScoreFeedbackUIManager.Instance != null && boxObject != null)
                {
                    ScoreFeedbackUIManager.Instance.ShowScoreFeedback(100, boxObject.transform.position);
                }
            }
        }
    }

    /// <summary>
    /// Called when a wrong box is clicked - subtracts 50 points
    /// </summary>
    private void OnWrongBoxSelected(NumberGrid grid, int number, GameObject boxObject)
    {
        // Only penalize if it's the player's own board and match is active
        if (grid == playerNumberGrid && isGameActive)
        {
            localMatchScore -= 50;
            Debug.Log($"[GameManager] ❌ Wrong Number Clicked! -50 Points (Total: {localMatchScore})");

            // Play Wrong Sound
            if (SoundManager.instance != null) SoundManager.instance.PlayWrong();

            // Trigger visual feedback
            if (ScoreFeedbackUIManager.Instance != null && boxObject != null)
            {
                ScoreFeedbackUIManager.Instance.ShowScoreFeedback(-50, boxObject.transform.position);
            }
        }
    }

    /// <summary>
    /// Helper to get the current total score of the local player from their board
    /// </summary>
    /// <summary>
    /// Helper to get the current total score of the local player from their board
    /// </summary>
    public int GetCurrentLocalScore()
    {
        if (playerNumberGrid == null) return 0;
        int currentScore = 0;
        Dictionary<string, int> counts = playerNumberGrid.GetSelectionCounts();
        foreach (var kvp in counts) currentScore += kvp.Value;
        return currentScore;
    }

    /// <summary>
    /// Returns the estimated server time using local wall-clock.
    /// Ensures tie-breaker scales match.
    /// </summary>
    public double GetEstimatedServerTime()
    {
        return (double)Time.time;
    }

    /// <summary>
    /// Called when another player finishes their board first
    /// </summary>
    public void OnGameFinishedByOtherPlayer()
    {
        if (isGameActive)
        {
            Debug.Log("[GameManager] Match ended by another player! Showing winner screen.");
            OnAllBoxesSelected();
        }
    }

    /// <summary>
    /// Called when all boxes in a grid are selected
    /// </summary>
    private void OnAllBoxesSelected()
    {
        if (!isGameActive)
        {
            return; // Game already ended
        }
        
        isGameActive = false;
        timerStarted = false; 
        Debug.Log("[GameManager] All boxes selected on a board! Determining winner...");
        
        // Stop timer
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Multiplayer: Wait for server (Always required now)
        Debug.Log("[GameManager] Board complete locally. Waiting for server 'gameOver' event...");
        isWaitingForServer = true;
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(true);
        StartServerGameOverTimeout();
    }
    
    /// <summary>
    /// Called when the quit button is clicked - player goes to lobby screen
    /// No prizes awarded, no winner screen shown to quitting player
    /// If only 2 players total: CPU wins instantly but player doesn't see winner screen
    /// If more than 2 players: CPUs continue playing
    /// </summary>
    private Coroutine forfeitTimeoutCoroutine;

    /// <summary>
    /// Called when the player quits/forfeits an active game.
    /// Handles forfeiting the match with the server and returning to the lobby safely.
    /// </summary>
    private void OnQuitButtonClicked()
    {
        // 0. If we are currently searching for a match, cancel it immediately
        if (MatchmakingModel.IsSearching)
        {
            MatchmakingModel.CancelSearch();
            
            // Also ensure the waiting panel is hidden if we quit during matchmaking
            HorizontalLobbyManager lobbyManager = FindFirstObjectByType<HorizontalLobbyManager>();
            if (lobbyManager != null)
            {
                lobbyManager.HideWaitingPanel();
            }
        }

        Debug.Log("[GameManager] Player forfeit/quit requested.");
        localPlayerForfeited = true;
        
        // Hide quit popup panel if active
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }

        // Show loading screen while waiting for server to acknowledge forfeit
        if (forfeitLoadingScreen != null)
        {
            forfeitLoadingScreen.SetActive(true);
        }

        // Notify listeners (like SettingManager) that gameplay music should stop
        OnGameEnded?.Invoke();

        // Get roomId from currentMatchData or PlayerModel fallback
        string roomId = (currentMatchData != null && !string.IsNullOrEmpty(currentMatchData.roomId)) 
            ? currentMatchData.roomId 
            : PlayerModel.ActiveRoomId;

        // Notify server about forfeit if room ID is valid
        if (!string.IsNullOrEmpty(roomId))
        {
            MatchmakingModel.ForfeitMatch(roomId);
        }
        else
        {
            Debug.LogWarning("[GameManager] No active RoomId found for forfeit. Proceeding to immediate cleanup.");
        }

        // Safety fallback: Ensure we return to lobby after 2 seconds even if server gameOver doesn't arrive
        if (forfeitTimeoutCoroutine != null) StopCoroutine(forfeitTimeoutCoroutine);
        forfeitTimeoutCoroutine = StartCoroutine(ForfeitSafetyTimeoutRoutine(2.0f));
    }

    private System.Collections.IEnumerator ForfeitSafetyTimeoutRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("[GameManager] Forfeit safety timeout reached. Completing forfeit cleanup.");
        forfeitTimeoutCoroutine = null;
        CompleteForfeitCleanup();
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
    /// Resets the winner screen UI to ensure clean state for next game
    /// </summary>
    private void ResetWinnerScreenUI()
    {
        if (winnerScreenScript != null) winnerScreenScript.ResetUI();
    }
    
    /// <summary>
    /// Called when player data is loaded
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
    /// Coroutine to retry loading player profile after data loads
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
        // Wait for backend to initialize and MainMenuAvatarDisplay to load avatar
        yield return new WaitForSeconds(1.5f);
        
        InitializeProfiles();
    }
    
    /// <summary>
    private void InitializeProfiles()
    {
        LoadPlayerProfileImage();
        
        // Opponents will be set up during ConfigureGameFromLobby when matchData arrives
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
        }

        // Load Player Frame
        if (playerFrameImage != null)
        {
            int savedFrameIndex = PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
            Sprite frameSprite = AvatarSelector.Instance != null ? AvatarSelector.Instance.GetBackgroundSpriteByIndex(savedFrameIndex) : null;
            if (frameSprite != null)
            {
                playerFrameImage.sprite = frameSprite;
                playerFrameImage.gameObject.SetActive(true);
            }
            else
            {
                playerFrameImage.gameObject.SetActive(false);
            }
        }

        // 1. Check if backend URL is available custom avatar URL (v2.3 contract)
        if (!string.IsNullOrEmpty(PlayerModel.Avatar) && PlayerModel.Avatar.StartsWith("http"))
        {
            if (playerProfileImageCustom != null)
            {
                playerProfileImageCustom.gameObject.SetActive(true);
                AssignAvatarAsync(playerProfileImageCustom, PlayerModel.Avatar);
            }
            if (playerProfileImage != null) playerProfileImage.gameObject.SetActive(false);
            Debug.Log("[GameManager] Player profile image loaded from Backend URL");
            return;
        }
        
        // 2. Fallback to MainMenuAvatarDisplay (Local Gallery/Presets)
        if (mainMenuAvatarDisplay != null)
        {
            Sprite avatarSprite = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
            bool isCustom = mainMenuAvatarDisplay.IsShowingCustomAvatar();

            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                
                if (isCustom)
                {
                    if (playerProfileImageCustom != null)
                    {
                        playerProfileImageCustom.sprite = playerSelectedAvatar;
                        playerProfileImageCustom.rectTransform.localScale = Vector3.one * UIConstants.CustomAvatarScale;
                        playerProfileImageCustom.gameObject.SetActive(true);
                    }
                    if (playerProfileImage != null) playerProfileImage.gameObject.SetActive(false);
                }
                else
                {
                    if (playerProfileImage != null)
                    {
                        playerProfileImage.sprite = playerSelectedAvatar;
                        playerProfileImage.rectTransform.localScale = Vector3.one;
                        playerProfileImage.gameObject.SetActive(true);
                    }
                    if (playerProfileImageCustom != null) playerProfileImageCustom.gameObject.SetActive(false);
                }
                return;
            }
        }
        
        // 3. Fallback to AvatarSelector
        AvatarSelector avatarSelector = FindFirstObjectByType<AvatarSelector>();
        if (avatarSelector != null)
        {
            int selectedIndex = PlayerModel.SelectedAvatarIndex;
            Sprite avatarSprite = avatarSelector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                if (playerProfileImage != null) playerProfileImage.sprite = playerSelectedAvatar;
                return;
            }
        }
    }
    
    
    
    /// <summary>
    /// Sets the number of active opponents based on server data
    /// </summary>
    private void SetUpOpponentsFromMatch(StartGameData matchData)
    {
        if (matchData == null || matchData.playerAvatars == null) return;

        int opponentCount = 0;
        foreach (var pair in matchData.playerAvatars)
        {
            if (pair.Key == PlayerModel.Id) continue; // Skip local player
            
            if (opponentCount < opponentProfileImages.Length)
            {
                // Set name and avatar for opponent
                string opponentId = pair.Key;
                PlayerAvatarInfo info = pair.Value;
                
                // Show the profile image
                if (opponentCount < opponentProfileImages.Length)
                {
                    opponentProfileImages[opponentCount].gameObject.SetActive(true);
                    AssignAvatarAsync(opponentProfileImages[opponentCount], info.avatar);
                    
                    if (opponentFrameImages != null && opponentCount < opponentFrameImages.Length && opponentFrameImages[opponentCount] != null)
                    {
                        if (!string.IsNullOrEmpty(info.frame) && int.TryParse(info.frame, out int frameIndex))
                        {
                            Sprite frameSprite = AvatarSelector.Instance != null ? AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex) : null;
                            if (frameSprite != null)
                            {
                                opponentFrameImages[opponentCount].sprite = frameSprite;
                                opponentFrameImages[opponentCount].gameObject.SetActive(true);
                            }
                            else
                            {
                                opponentFrameImages[opponentCount].gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            opponentFrameImages[opponentCount].gameObject.SetActive(false);
                        }
                    }

                    if (opponentOutlineImages[opponentCount] != null)
                    {
                        opponentOutlineImages[opponentCount].gameObject.SetActive(true);
                    }
                }
                
                opponentCount++;
            }
        }
        
        // Hide remaining slots
        for (int i = opponentCount; i < opponentProfileImages.Length; i++)
        {
            if (opponentProfileImages[i] != null) opponentProfileImages[i].gameObject.SetActive(false);
            if (opponentFrameImages != null && i < opponentFrameImages.Length && opponentFrameImages[i] != null) opponentFrameImages[i].gameObject.SetActive(false);
            if (opponentOutlineImages[i] != null) opponentOutlineImages[i].gameObject.SetActive(false);
        }
    }

    public Sprite GetAvatarSprite(string avatarKey)
    {
        // If it's a URL, we can't return a sprite immediately (use AssignAvatarAsync instead)
        if (string.IsNullOrEmpty(avatarKey) || avatarKey.StartsWith("http"))
        {
            return (availableProfileSprites != null && availableProfileSprites.Length > 0) ? availableProfileSprites[0] : null;
        }

        // Try to parse index if it's a number
        if (int.TryParse(avatarKey, out int index) && availableProfileSprites != null && index >= 0 && index < availableProfileSprites.Length)
            return availableProfileSprites[index];
        
        // Fallback to presets by name (if applicable)
        if (avatarKey == "avatarBoy" && availableProfileSprites.Length > 0) return availableProfileSprites[0];
        if (avatarKey == "avatarGirl" && availableProfileSprites.Length > 1) return availableProfileSprites[1];

        // Fallback to first sprite
        return (availableProfileSprites != null && availableProfileSprites.Length > 0) ? availableProfileSprites[0] : null;
    }

    /// <summary>
    /// Assigns an avatar to an Image component. Handles both local presets and remote URLs (v2.3 contract).
    /// </summary>
    public void AssignAvatarAsync(Image target, string avatarKey, bool zoomIfLocalCustom = false)
    {
        if (target == null) return;

        if (!string.IsNullOrEmpty(avatarKey) && avatarKey.StartsWith("http"))
        {
            // Remote URL - Download via BackendAuthService
            if (BackendAuthService.Instance != null)
            {
                BackendAuthService.Instance.GetImage(avatarKey, (tex) => {
                    if (tex != null && target != null)
                    {
                        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        target.sprite = s;
                        // Scale override removed to respect Editor settings
                    }
                });
            }
        }
        else
        {
            // Local preset
            target.sprite = GetAvatarSprite(avatarKey);
            // Scale override removed to respect Editor settings
        }
    }

    
    /// <summary>
    /// Manually refresh player profile image
    /// </summary>
    public void RefreshPlayerProfile()
    {
        LoadPlayerProfileImage();
    }
    
    /// <summary>
    /// Assigns random colors to outline images for player and opponents
    /// </summary>
    private void AssignOutlineColors()
    {
        // Colors will be assigned to player and opponents based on match data
        
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
        
        // Calculate total number of players from match data
        int totalPlayers = (currentMatchData != null && currentMatchData.playerAvatars != null) 
                          ? currentMatchData.playerAvatars.Count 
                          : 1;
        
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
        
        // Assign unique colors to Opponent outlines
        if (opponentOutlineImages != null && opponentOutlineImages.Length > 0)
        {
            int opponentCount = 0;
            if (currentMatchData != null && currentMatchData.playerAvatars != null)
            {
                foreach (var pair in currentMatchData.playerAvatars)
                {
                    if (pair.Key == PlayerModel.Id) continue; // Skip local player
                    
                    if (opponentCount < opponentOutlineImages.Length && opponentCount < opponentOutlineColors.Length)
                    {
                        if (opponentOutlineImages[opponentCount] != null)
                        {
                            if (availableColorsList.Count > 0)
                            {
                                int randomIndex = Random.Range(0, availableColorsList.Count);
                                Color selectedColor = availableColorsList[randomIndex];
                                opponentOutlineColors[opponentCount] = selectedColor;
                                opponentOutlineImages[opponentCount].color = selectedColor;
                                availableColorsList.RemoveAt(randomIndex);
                                Debug.Log($"[GameManager] Assigned color to Opponent {opponentCount + 1}: {selectedColor}");
                            }
                        }
                        opponentCount++;
                    }
                }
            }
        }
        else
        {
            Debug.Log("[GameManager] Opponent outline images array is empty or not assigned.");
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
            
            // Set opponent outline colors
            if (opponentOutlineColors != null)
            {
                for (int i = 0; i < opponentOutlineColors.Length; i++)
                {
                    numberGrid.SetCPUOutlineColor(i, opponentOutlineColors[i]);
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the outline color for a specific player type
    /// </summary>
    public Color GetOutlineColor(PlayerType playerType, int opponentIndex = 0)
    {
        if (playerType == PlayerType.Player)
        {
            return playerOutlineColor;
        }
        else // Opponent
        {
            if (opponentOutlineColors != null && opponentIndex >= 0 && opponentIndex < opponentOutlineColors.Length)
            {
                return opponentOutlineColors[opponentIndex];
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
    public Color ParseHexColor(string hex)
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
        
        // Update Opponent outline fills from each opponent's board
        if (opponentOutlineImages != null)
        {
            for (int i = 0; i < opponentOutlineImages.Length; i++)
            {
                if (opponentOutlineImages[i] != null && opponentOutlineImages[i].gameObject.activeSelf)
                {
                    int oppCount = 0;
                    if (opponentNumberGrids.ContainsKey(i) && opponentNumberGrids[i] != null)
                    {
                        Dictionary<string, int> oppSelectionCounts = opponentNumberGrids[i].GetSelectionCounts();
                        foreach (var kvp in oppSelectionCounts)
                        {
                            oppCount += kvp.Value;
                        }
                    }
                    float fillAmount = oppCount * FILL_PER_SELECTION;
                    fillAmount = Mathf.Clamp01(fillAmount);
                    opponentOutlineImages[i].fillAmount = fillAmount;
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
        
        // Reset Opponent outline fills
        if (opponentOutlineImages != null)
        {
            for (int i = 0; i < opponentOutlineImages.Length; i++)
            {
                if (opponentOutlineImages[i] != null)
                {
                    opponentOutlineImages[i].fillAmount = 0f;
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
        winnerScreenShown = false;
        isWaitingForServer = false;
        prizesAwardedForThisMatch = false;

        gameTimer = initialGameTimer;
        currentTimer = gameTimer;

        // Clear current lobby config (will be set when new game starts)
        currentLobbyConfig = null;
        currentMatchData = null;

        // Clear per-match score state so stale data never bleeds into the next match
        networkPlayerScores.Clear();
        networkPlayerTimes.Clear();
        localMatchScore = 0;
        
        // Reset outline fill amounts
        ResetOutlineFillAmounts();
        
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);
        if (forfeitLoadingScreen != null) forfeitLoadingScreen.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);

        if (playerDicesController == null)
        {
            playerDicesController = FindFirstObjectByType<DicesController>();
        }
        if (playerDicesController != null)
        {
            playerDicesController.ResetRollingState();
            playerDicesController.ClearDiceSumDisplay();
            playerDicesController.HideDices();
        }
        DiceVisibilityManager dvm = FindFirstObjectByType<DiceVisibilityManager>();
        if (dvm != null)
        {
            dvm.ForceHideDices();
        }
        
        // Reset score UI
        if (ScoreFeedbackUIManager.Instance != null)
        {
            ScoreFeedbackUIManager.Instance.ResetScore();
        }

        // Disconnect socket when returning to lobby/resetting game state
        // [MODIFIED]: We no longer disconnect here. The socket stays alive in the lobby
        // to allow for instant rematching without reconnection delays.
        // Disconnection is now handled by the Back button in HorizontalLobbyManager.
        if (SocketManager.Instance != null && MatchmakingModel.IsForfeitPending())
        {
            // If we just sent a forfeit, give the socket a moment to actually send the packet 
            // but DO NOT completely disconnect. Just clear the pending state.
            // Let the server clean up the match state, but keep the connection open for the next match.
            // (The actual DisconnectDelayed call was removed here)
        }
    }
    
    /// <summary>
    /// Configures the game from a lobby configuration
    /// Sets number of CPUs, handles entry fee, and stores prize information
    /// </summary>
    public void ConfigureGameFromLobby(LobbyConfig config, int overrideCPUs = -1, StartGameData matchData = null)
    {
        Debug.Log($"[GameManager] Configuring game for lobby: {config.lobbyId}, matchData provided: {matchData != null}");
        localPlayerForfeited = false;
        MatchmakingModel.ClearLocalForfeitPending();
        
        // If we are recovering a match from the prompt, the gameplayScreen is already hidden. We must ensure it's shown.
        if (gameplayScreen != null && !gameplayScreen.activeSelf)
        {
            gameplayScreen.SetActive(true);
        }
        
        currentLobbyConfig = config;
        
        if (config == null)
        {
            Debug.LogWarning("[GameManager] LobbyConfig is null! Using default settings.");
            return;
        }

        // Set dynamic background color based on lobby index
        if (backgroundScroller != null && lobbyBackgroundColors != null && lobbyBackgroundColors.Length > 0)
        {
            int colorIndex = Mathf.Clamp(config.lobbyIndex, 0, lobbyBackgroundColors.Length - 1);
            backgroundScroller.SetBackgroundColor(lobbyBackgroundColors[colorIndex]);
        }
        
        // Stop any running timer and reset flags first
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        isGameActive = false;
        timerStarted = false;
        // gameTimer will be updated below from currentLobbyConfig
        
        // Reset winner screen UI to ensure clean state
        ResetWinnerScreenUI();
        
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);
        if (forfeitLoadingScreen != null) forfeitLoadingScreen.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);
        
        // Reset outline fill amounts for new game
        ResetOutlineFillAmounts();
        
        // Ensure playerDicesController reference is populated and cleanly reset for new game
        if (playerDicesController == null)
        {
            playerDicesController = FindFirstObjectByType<DicesController>();
        }
        if (playerDicesController != null)
        {
            playerDicesController.ResetRollingState();
            playerDicesController.ClearDiceSumDisplay();
            // Let DiceVisibilityManager handle showing the dice when gameplay actually starts
        }
        
        Debug.Log("[GameManager] Board and dice state reset for new match.");
        
        // Set new lobby configuration
        currentLobbyConfig = config;
        
        if (matchData == null)
        {
            int reqPlayers = (config != null && config.numberOfPlayers > 0) ? config.numberOfPlayers : 3;
            Debug.LogWarning($"[GameManager] matchData is null! Auto-generating fallback matchData for {reqPlayers} players.");
            matchData = CreateFallbackMatchData(reqPlayers, config != null ? (config.entryFeeCash > 0 ? config.entryFeeCash : config.entryFeeChips) : 100f);
        }
        currentMatchData = matchData;

        Debug.Log($"[GameManager] ✅ Server match initialized. Room: {matchData.roomId}, Duration: {matchData.duration}s");

        // Save for recovery in case of crash/background
        // Handled by backend profile state now

        // Update game duration from server data
        if (matchData.timeRemaining > 0)
        {
            this.gameTimer = matchData.timeRemaining;
            Debug.Log($"[GameManager] Match recovery: Setting timer to remaining time: {matchData.timeRemaining}s");
        }
        else
        {
            // FORCE 120 seconds as requested, ignoring matchData.duration from backend
            this.gameTimer = 120f;
            Debug.Log("[GameManager] Forced game timer to 120 seconds.");
        }
        
        this.currentTimer = this.gameTimer;
        
        // Setup Opponents and Grids
        SetUpOpponentsFromMatch(matchData);
        CreateIndividualNumberGrids(matchData);
        
        // Handle Match Recovery Score sync
        if (matchData.gameState != null && matchData.gameState.ContainsKey(PlayerModel.Id))
        {
            localMatchScore = matchData.gameState[PlayerModel.Id].score;
            if (ScoreFeedbackUIManager.Instance != null)
            {
                ScoreFeedbackUIManager.Instance.SetScore(localMatchScore);
            }
            Debug.Log($"[GameManager] Match recovered. Syncing local score to: {localMatchScore}");
        }
        else
        {
            localMatchScore = 0;
            if (ScoreFeedbackUIManager.Instance != null) ScoreFeedbackUIManager.Instance.ResetScore();
        }
        
        Debug.Log($"[GameManager] Game configured from Lobby: {config.lobbyName}. Participants: {matchData.playerAvatars.Count}");
        
        // Clear network caches for a fresh game session
        cachedNetworkNames.Clear();
        cachedNetworkAvatars.Clear();
        cachedNetworkAvatarIndices.Clear();
        Debug.Log("[GameManager] Network profile caches cleared for new session.");
    }
    
    /// <summary>
    /// Awards prizes based on current lobby configuration and player rankings
    /// </summary>
    private void AwardPrizes(List<(PlayerType playerType, int cpuIndex, int count, double time, string userId)> rankings)
    {
        if (prizesAwardedForThisMatch) return;
        prizesAwardedForThisMatch = true;

        Debug.Log("[GameManager] Awarding prizes (visual only, server handles data).");
    }
    
    /// <summary>
    /// Refreshes lobby buttons after a delay (to allow animation to complete)
    /// </summary>
    private System.Collections.IEnumerator RefreshLobbyButtonsDelayed()
    {
        // Wait for animation to complete (particles + count-up)
        yield return new WaitForSeconds(3f); // Adjust based on your animation duration
        
        HorizontalLobbyManager lobbyManager = FindFirstObjectByType<HorizontalLobbyManager>();
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
    
    public string GetCurrentRoomId()
    {
        return currentMatchData?.roomId;
    }

    public StartGameData GetCurrentMatchData()
    {
        return currentMatchData;
    }

    /// <summary>
    /// Creates a fallback StartGameData payload for offline or CPU matches
    /// </summary>
    public static StartGameData CreateFallbackMatchData(int totalPlayers = 3, float stake = 100f)
    {
        StartGameData fallback = new StartGameData();
        fallback.roomId = $"room_{totalPlayers}p_{Random.Range(1000, 9999)}";
        fallback.duration = 60;
        fallback.stake = stake;
        fallback.winConditionTarget = 50;
        fallback.playerAvatars = new Dictionary<string, PlayerAvatarInfo>();

        string localId = !string.IsNullOrEmpty(PlayerModel.Id) ? PlayerModel.Id : "local_player";
        string localName = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "You";
        fallback.playerAvatars[localId] = new PlayerAvatarInfo
        {
            name = localName,
            avatar = PlayerModel.SelectedAvatarIndex.ToString()
        };

        string[] cpuNames = new string[] {
            "Omega Shenron", "Muhammad Usama", "Noman Ali", "Dice Master",
            "Speedy Roll", "Lucky Strike", "Golden Dice", "Shadow Player", "Victory Ace"
        };

        for (int i = 1; i < totalPlayers; i++)
        {
            string cpuId = $"cpu_{i}";
            string cpuName = (i - 1 < cpuNames.Length) ? cpuNames[i - 1] : $"CPU {i}";
            fallback.playerAvatars[cpuId] = new PlayerAvatarInfo
            {
                name = cpuName,
                avatar = ((i - 1) % 6).ToString()
            };
        }

        return fallback;
    }

    private Coroutine serverGameOverTimeoutCoroutine = null;

    private void StartServerGameOverTimeout()
    {
        if (serverGameOverTimeoutCoroutine != null)
        {
            StopCoroutine(serverGameOverTimeoutCoroutine);
        }
        serverGameOverTimeoutCoroutine = StartCoroutine(WaitForServerGameOverTimeout());
    }

    private System.Collections.IEnumerator WaitForServerGameOverTimeout()
    {
        bool isOnlineSocket = SocketManager.Instance != null && 
                             SocketManager.Instance.Socket != null && 
                             SocketManager.Instance.Socket.IsOpen &&
                             (currentMatchData != null && !string.IsNullOrEmpty(currentMatchData.roomId));

        float waitTime = isOnlineSocket ? 3.0f : 0.5f;

        yield return new WaitForSeconds(waitTime);

        if (isWaitingForServer && !winnerScreenShown)
        {
            Debug.LogWarning($"[GameManager] Server gameOver event did not arrive after {waitTime}s. Triggering fallback results.");
            GameOverData fallbackData = GenerateFallbackGameOverData();
            HandleServerGameOver(fallbackData);
        }

        serverGameOverTimeoutCoroutine = null;
    }

    public GameOverData GenerateFallbackGameOverData()
    {
        GameOverData data = new GameOverData();
        data.reason = "game_completed";
        data.scores = new Dictionary<string, int>();
        data.earnings = new Dictionary<string, double>();
        data.coinsEarnings = new Dictionary<string, double>();

        string localId = !string.IsNullOrEmpty(PlayerModel.Id) ? PlayerModel.Id : "local_player";
        data.scores[localId] = localMatchScore;

        int highestScore = localMatchScore;
        string winnerId = localId;

        if (networkPlayerScores != null)
        {
            foreach (var kvp in networkPlayerScores)
            {
                data.scores[kvp.Key] = kvp.Value;
                if (kvp.Value > highestScore)
                {
                    highestScore = kvp.Value;
                    winnerId = kvp.Key;
                }
            }
        }

        data.winnerId = winnerId;

        // Populate players dictionary
        data.players = currentMatchData != null && currentMatchData.playerAvatars != null 
            ? new Dictionary<string, PlayerAvatarInfo>(currentMatchData.playerAvatars) 
            : new Dictionary<string, PlayerAvatarInfo>();

        if (!data.players.ContainsKey(localId))
        {
            data.players[localId] = new PlayerAvatarInfo
            {
                name = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : "You",
                avatar = PlayerModel.SelectedAvatarIndex.ToString()
            };
        }

        // Calculate earnings for winner
        float stake = currentLobbyConfig != null ? (currentLobbyConfig.entryFeeCash > 0 ? currentLobbyConfig.entryFeeCash : currentLobbyConfig.entryFeeChips) : 100f;
        int totalPlayers = data.players.Count > 0 ? data.players.Count : 3;
        double prize = stake * totalPlayers;

        data.earnings[winnerId] = prize;

        return data;
    }
    
    /// <summary>
    /// Creates individual NumberGrid instances for player and each opponent
    /// </summary>
    private void CreateIndividualNumberGrids(StartGameData matchData)
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
            playerNumberGrid.OnWrongBoxSelected += OnWrongBoxSelected;
            
            // Set the owner ID for Socket.io communication
            playerNumberGrid.SetOwner(PlayerModel.Id);
            
            // Ensure player grid is active and visible
            playerNumberGrid.gameObject.SetActive(true);
            
            Debug.Log($"[GameManager] Created player's NumberGrid with owner ID: {PlayerModel.Id}");
        }
        
        // Create Opponent NumberGrids
        int opponentIdx = 0;
        foreach (var pair in matchData.playerAvatars)
        {
            if (pair.Key == PlayerModel.Id) continue; // Skip player

            NumberGrid oppGrid = CreateNumberGridInstance($"OpponentGrid_{pair.Key}", parentTransform);
            if (oppGrid != null)
            {
                oppGrid.SetOwner(pair.Key);
                opponentNumberGrids[opponentIdx] = oppGrid;
                allNumberGrids.Add(oppGrid);
                
                // Subscribe to events
                oppGrid.OnAllBoxesSelected += OnAllBoxesSelected;
                oppGrid.OnBoxSelected += OnBoxSelected;
                
                oppGrid.gameObject.SetActive(false); // Hidden from main view
                Debug.Log($"[GameManager] Created grid for opponent: {pair.Key}");
                opponentIdx++;
            }
        }
        
        // Assign grids to DicesController
        AssignGridsToControllers();
        
        // If we have a matrix from the backend, apply it to all grids so they match exactly
        if (currentMatchData != null && currentMatchData.matrix != null && currentMatchData.matrix.Length == 25)
        {
            foreach (NumberGrid grid in allNumberGrids)
            {
                if (grid != null)
                {
                    grid.GenerateGridFromMatrix(currentMatchData.matrix);
                }
            }
        }

        // Handle Match Recovery (gameState)
        if (matchData.gameState != null)
        {
            foreach (var playerState in matchData.gameState)
            {
                string userId = playerState.Key;
                int[] lockedIndices = playerState.Value.lockedIndices;
                int score = playerState.Value.score;

                // Find the grid for this player
                NumberGrid grid = allNumberGrids.Find(g => g.GetOwnerId() == userId);
                if (grid != null && lockedIndices != null)
                {
                    grid.RestoreLockedIndices(lockedIndices, score);
                }
            }
        }
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
        // Clear caches when destroying grids
        cachedNetworkNames.Clear();
        cachedNetworkAvatars.Clear();

        // Unsubscribe from events before destroying
        foreach (NumberGrid grid in allNumberGrids)
        {
            if (grid != null)
            {
                grid.OnAllBoxesSelected -= OnAllBoxesSelected;
                grid.OnBoxSelected -= OnBoxSelected;
                grid.OnWrongBoxSelected -= OnWrongBoxSelected; // Unsubscribe
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
        opponentNumberGrids.Clear();
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
        
    }
    
    public Color GetPlayerOutlineColor() => playerOutlineColor;
    
    public Color GetOpponentOutlineColor(int index)
    {
        if (opponentOutlineColors != null && index >= 0 && index < opponentOutlineColors.Length)
            return opponentOutlineColors[index];
        return Color.white;
    }

    public void SetProfileSlot(Image spriteSlot, Image customSlot, Sprite sprite, bool isCustom)
    {
        if (spriteSlot != null) spriteSlot.gameObject.SetActive(false);
        if (customSlot != null) customSlot.gameObject.SetActive(false);

        if (isCustom)
        {
            if (customSlot != null)
            {
                customSlot.sprite = sprite;
                customSlot.rectTransform.localScale = Vector3.one * UIConstants.CustomAvatarScale;
                customSlot.gameObject.SetActive(true);
            }
        }
        else
        {
            if (spriteSlot != null)
            {
                spriteSlot.sprite = sprite;
                spriteSlot.rectTransform.localScale = Vector3.one;
                spriteSlot.gameObject.SetActive(true);
            }
        }
    }
}

/// <summary>
/// Serializable data for capturing a snapshot of the podium for disconnected players
/// </summary>
[System.Serializable]
public class OfflinePodiumSnapshot {
    public System.Collections.Generic.List<OfflinePlayerEntry> topThree = new System.Collections.Generic.List<OfflinePlayerEntry>();
}

[System.Serializable]
public class OfflinePlayerEntry {
    public string name;
    public string pfId;
    public PlayerType type;
    public int opponentIdx;
    public float cash;
    public int chips;
    public int avatarIdx;
    public string customImg;
}
