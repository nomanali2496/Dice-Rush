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
    [SerializeField] private Image playerProfileImageCustom; // Player's profile image (custom/zoomed)
    [SerializeField] private Image playerOutlineImage; // Player's outline image (for color assignment)
    [SerializeField] private MainMenuAvatarDisplay mainMenuAvatarDisplay; // Reference to main menu avatar display (to get user's selected avatar)
    
    [Header("Opponent Profiles")]
    [SerializeField] private Image[] opponentProfileImages; // Array of profile image displays for opponents
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
    [SerializeField] private float gameTimer = 30f; // Game timer in seconds (30 seconds)
    
    private float currentTimer; // Current timer value
    private Coroutine timerCoroutine; // Reference to the timer coroutine
    private bool isGameActive = false; // Flag to track if game is active
    private bool timerStarted = false; // Flag to prevent multiple timer starts
    private float initialGameTimer; // Original game timer value to reset back to

    public const string GAME_EXTENSION_KEY = "GameExtensionCount"; 
    
    // Current lobby configuration
    private LobbyConfig currentLobbyConfig; // Current lobby configuration being played
    private StartGameData currentMatchData; // Current match data from socket
    
    // Win conditions
    private int currentWinConditionTarget = 50; // Target score or boxes to win

    [Header("Winner Screen")]
    [SerializeField] private GameObject winnerScreen; // Winner screen GameObject (set active when game ends)
    [SerializeField] private WinnerScreen winnerScreenScript; // Winner screen script for animations
    [SerializeField] private Button winnerScreenLobbyButton; // Lobby button on winner screen
    [SerializeField] private GameObject calculateLoadingScreen; // Loading screen shown while calculating results
    
    private float lastAwardedCash = 0f;
    private int lastAwardedChips = 0;
    private bool prizesAwardedForThisMatch = false; // Prevents infinite loop/duplicate prizes
    private bool winnerScreenShown = false; // Track if we've already displayed the results
    private bool isWaitingForServer = false; // Track if we are waiting for server gameOver
    private int localMatchScore = 0; // The persistent running score for the local player match
    
    public (float cash, int chips) GetLastAwardedPrize() => (lastAwardedCash, lastAwardedChips);
    
    [Header("Countdown")]
    [SerializeField] private TMPro.TextMeshProUGUI countdownText; // Countdown text display ("3", "2", "1")
    [SerializeField] private TMPro.TextMeshProUGUI countdownTextShadow; // Countdown text shadow display ("3", "2", "1")
    [SerializeField] private float countdownDuration = 1f; // Duration for each countdown number (in seconds)
    
    [Header("1st Place")]
    [SerializeField] private Image winnerProfileImage; // Winner's profile image display
    [SerializeField] private Image winnerProfileImageCustom; // Winner's profile image (custom/zoomed)
    [SerializeField] private Image winnerOutlineImage; // Winner's outline image display
    [SerializeField] private Image winnerFrameImage; // Winner's frame image display
    [SerializeField] private TMPro.TextMeshProUGUI winnerNameText; // Winner's name text (TextMeshProUGUI)
    [SerializeField] private Text winnerNameTextLegacy; // Winner's name text (Legacy Text - use if not using TextMeshPro)
    
    [Header("2nd Place")]
    [SerializeField] private Image secondPlaceProfileImage; // 2nd place profile image display
    [SerializeField] private Image secondPlaceProfileImageCustom; // 2nd place profile image (custom/zoomed)
    [SerializeField] private Image secondPlaceOutlineImage; // 2nd place outline image display
    [SerializeField] private Image secondPlaceFrameImage; // 2nd place frame image display
    [SerializeField] private TMPro.TextMeshProUGUI secondPlaceNameText; // 2nd place name text (TextMeshProUGUI)
    [SerializeField] private Text secondPlaceNameTextLegacy; // 2nd place name text (Legacy Text - use if not using TextMeshPro)
    
    [Header("3rd Place")]
    [SerializeField] private Image thirdPlaceProfileImage; // 3rd place profile image display
    [SerializeField] private Image thirdPlaceProfileImageCustom; // 3rd place profile image (custom/zoomed)
    [SerializeField] private Image thirdPlaceOutlineImage; // 3rd place outline image display
    [SerializeField] private Image thirdPlaceFrameImage; // 3rd place frame image display
    [SerializeField] private TMPro.TextMeshProUGUI thirdPlaceNameText; // 3rd place name text (TextMeshProUGUI)
    [SerializeField] private Text thirdPlaceNameTextLegacy; // 3rd place name text (Legacy Text - use if not using TextMeshPro)
    
    private Sprite playerSelectedAvatar; // Store player's selected avatar sprite
    private List<Sprite> availableOpponentAvatars; // List of available avatars for opponents
    private Color playerOutlineColor; // Store player's outline color
    private Color[] opponentOutlineColors; // Store opponent outline colors
    
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

        // PlayFab subscriptions removed as we are using custom backend
        
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
    }

    private void HandleServerGameOver(GameOverData data)
    {
        if (data == null) return;

        Debug.Log($"<color=#00FFFF>⬇️ [GameManager] Server GameOver received. Winner: {data.winnerId}, Reason: {data.reason}</color>");
        
        // Hide the "Calculating Results" screen if it was shown
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);

        // If we already showed the screen (e.g. in Solo mode or duplicate packet), ignore
        if (winnerScreenShown) 
        {
            Debug.Log("[GameManager] Winner screen already shown. Skipping server gameOver trigger.");
            return;
        }

        isGameActive = false;
        timerStarted = false;
        isWaitingForServer = false;

        // Stop local timer coroutine
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        // Convert server rankings to the format expected by ShowWinnerScreen
        // List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)>
        var rankings = new List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)>();

        // Set the official prizes from server data
        lastAwardedCash = (float)data.earnings.GetValueOrDefault(PlayerModel.Id, 0);
        lastAwardedChips = 0;

        if (data.scores != null)
        {
            int oppIdx = 0;
            foreach (var kvp in data.scores)
            {
                string userId = kvp.Key;
                int score = kvp.Value;
                
                PlayerType type = (userId == PlayerModel.Id) ? PlayerType.Player : PlayerType.Network;
                double time = (userId == data.winnerId) ? 0 : 999.0;

                int index = (type == PlayerType.Player) ? -1 : oppIdx++;
                rankings.Add((type, index, score, time, userId));
            }
        }

        // Sort rankings by score (descending)
        rankings.Sort((a, b) => b.count.CompareTo(a.count));

        // Show the screen
        ShowWinnerScreen(rankings);
    }

    private void HandleServerNumberLocked(NumberLockedData data)
    {
        if (data != null && data.userId != PlayerModel.Id)
        {
            networkPlayerScores[data.userId] = data.score;
            networkPlayerTimes[data.userId] = GetEstimatedServerTime();
            Debug.Log($"[GameManager] Sync: Player {data.userId} updated score to: {data.score}");
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

        // 2. Identify the place from message
        int playerPlace = 1;
        if (message.Contains("1st")) playerPlace = 1;
        else if (message.Contains("2nd")) playerPlace = 3; // Correct 2nd vs 3rd if needed
        else if (message.Contains("3rd")) playerPlace = 3;

        // 3. Setup Podium
        ResetWinnerScreenUI();
        
        // Try parsing full rankings snapshot
        OfflinePodiumSnapshot snapshot = null;
        if (!string.IsNullOrEmpty(rankingsJson))
        {
            try { snapshot = JsonUtility.FromJson<OfflinePodiumSnapshot>(rankingsJson); } 
            catch { Debug.LogWarning("[GameManager] Failed to parse podium snapshot JSON."); }
        }

        if (snapshot != null && snapshot.topThree.Count > 0)
        {
            Debug.Log($"[GameManager] 🥇 Displaying FULL PODIUM for offline win ({snapshot.topThree.Count} entries).");
            
            for (int i = 0; i < snapshot.topThree.Count; i++)
            {
                var entry = snapshot.topThree[i];
                int place = i + 1;
                
                Image pIcon = null;
                TMPro.TextMeshProUGUI pText = null;
                Image pIconSmall = null;
                
                switch (place) {
                    case 1: pIcon = ws.GetFirstPlacePrizeIcon(); pText = ws.GetFirstPlacePrizeText(); break;
                    case 2: pIcon = ws.GetSecondPlacePrizeIcon(); pText = ws.GetSecondPlacePrizeText(); break;
                    case 3: pIcon = ws.GetThirdPlacePrizeIcon(); pText = ws.GetThirdPlacePrizeText(); break;
                }

                // FIX: If this entry in the snapshot is the LOCAL PLAYER watching the screen, always show them as "YOU"
                if (!string.IsNullOrEmpty(entry.pfId) && entry.pfId == PlayerModel.Id)
                {
                    // opponentIdx = -1 forces DisplayPlace to use local player logic/high-quality avatar
                    DisplayPlace(place, PlayerType.Player, -1, 0,
                                 GetProfileImageForPod(place), GetProfileImageCustomForPod(place), GetOutlineImageForPod(place), GetFrameImageForPod(place), GetNameTextForPod(place), null,
                                 pIcon, pText, null,
                                 entry.cash, entry.chips);
                }
                else 
                {
                    // Use the snapshot's saved name and avatar index
                    DisplayPlace(place, entry.type, entry.opponentIdx, 0,
                                 GetProfileImageForPod(place), GetProfileImageCustomForPod(place), GetOutlineImageForPod(place), GetFrameImageForPod(place), GetNameTextForPod(place), null,
                                 pIcon, pText, null,
                                 entry.cash, entry.chips, entry.name, entry.avatarIdx, entry.customImg);
                }
            }

            // Hide pods that were not in snapshot
            if (snapshot.topThree.Count < 2) HidePlace(secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy, null, null, null);
            if (snapshot.topThree.Count < 3) HidePlace(thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy, null, null, null);
        }
        else
        {
            Debug.Log($"[GameManager] 🥈 No podium snapshot found. Displaying only the local player on Rank {playerPlace}.");
            DisplayPlace(playerPlace, PlayerType.Player, -1, 0, GetProfileImageForPod(playerPlace), GetProfileImageCustomForPod(playerPlace), GetOutlineImageForPod(playerPlace), GetFrameImageForPod(playerPlace), GetNameTextForPod(playerPlace), null, null, null, null, cash, chips);
        }

        // 4. (Profile load moved to start)

        // 5. Activate visuals
        if (winnerScreen != null) winnerScreen.SetActive(true);

        // 6. Trigger animation
        if (ws != null) {
            List<(PlayerType, int, int)> uiRankings = new List<(PlayerType, int, int)>();
            
            // Explicitly build a 3-person list (1st, 2nd, 3rd) and place YOU in your exact rank.
            // This ensures the animation starting position is always 100% accurate to the podium.
            for (int i = 1; i <= 3; i++)
            {
                if (i == playerPlace) {
                    uiRankings.Add((PlayerType.Player, -1, 0)); // Add local player at their real rank
                } else {
                    uiRankings.Add((PlayerType.CPU, -2, 0)); // Add placeholder for other ranks
                }
            }
            
            ws.PlayAnimationIfPlayerInTop3(uiRankings, currentLobbyConfig);
        }
    }

    // Helpers for pod access
    private Image GetProfileImageForPod(int place) { return place == 1 ? winnerProfileImage : place == 2 ? secondPlaceProfileImage : thirdPlaceProfileImage; }
    private Image GetProfileImageCustomForPod(int place) { return place == 1 ? winnerProfileImageCustom : place == 2 ? secondPlaceProfileImageCustom : thirdPlaceProfileImageCustom; }
    private Image GetOutlineImageForPod(int place) { return place == 1 ? winnerOutlineImage : place == 2 ? secondPlaceOutlineImage : thirdPlaceOutlineImage; }
    private Image GetFrameImageForPod(int place) { return place == 1 ? winnerFrameImage : place == 2 ? secondPlaceFrameImage : thirdPlaceFrameImage; }
    private TMPro.TextMeshProUGUI GetNameTextForPod(int place) { return place == 1 ? winnerNameText : place == 2 ? secondPlaceNameText : thirdPlaceNameText; }


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
        localMatchScore = 0; 
        
        // Reset score UI
        if (ScoreFeedbackUIManager.Instance != null)
        {
            ScoreFeedbackUIManager.Instance.ResetScore();
        }
        
        timerCoroutine = StartCoroutine(CountdownBeforeStart());
        Debug.Log("[GameManager] Starting match. Score reset to 0.");
    }
    
    /// <summary>
    /// Coroutine for "3, 2, 1" countdown before game starts
    /// </summary>
    private System.Collections.IEnumerator CountdownBeforeStart(string customMessage = null)
    {
        // Make sure countdown text exists
        if (countdownText == null)
        {
            Debug.LogWarning("[GameManager] Countdown text not assigned! Skipping countdown.");
            currentTimer = gameTimer;
            isGameActive = true;
            timerCoroutine = StartCoroutine(TimerCountdown());
            yield break;
        }
        
        // Hide timer text during countdown
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (timerTextLegacy != null) timerTextLegacy.gameObject.SetActive(false);
        
        // Disable roll button during countdown
        if (playerDicesController != null)
        {
            playerDicesController.SetRollButtonInteractable(false);
        }

        // Show countdown text
        countdownText.gameObject.SetActive(true);
        countdownTextShadow.gameObject.SetActive(true);

        // Countdown from 3 to 1
        for (int i = 3; i >= 1; i--)
        {
            if (!string.IsNullOrEmpty(customMessage) && i < 3) break;

            if (countdownText != null) countdownText.text = string.IsNullOrEmpty(customMessage) ? i.ToString() : customMessage;
            if (countdownTextShadow != null) countdownTextShadow.text = string.IsNullOrEmpty(customMessage) ? i.ToString() : customMessage;
            
            if (SoundManager.instance != null) SoundManager.instance.PlayCountdown();
            
            countdownText.transform.localScale = Vector3.zero;
            countdownText.alpha = 0f;
            countdownTextShadow.transform.localScale = Vector3.zero;
            countdownTextShadow.alpha = 0f;
            
            countdownText.transform.DOScale(Vector3.one * 1.5f, countdownDuration * 0.3f).SetEase(DG.Tweening.Ease.OutBack);
            countdownText.DOFade(1f, countdownDuration * 0.3f);
            countdownTextShadow.transform.DOScale(Vector3.one * 1.5f, countdownDuration * 0.3f).SetEase(DG.Tweening.Ease.OutBack);
            countdownTextShadow.DOFade(1f, countdownDuration * 0.3f);
            
            yield return new WaitForSeconds(countdownDuration * 0.3f);
            
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
        
        // Start gameplay music
        if (SettingManager.instance != null) SettingManager.instance.OnCountdownComplete();
        
        OnGameStarted?.Invoke();
        StartAllPowerupTimers();
        
        if (playerDicesController != null) playerDicesController.SetRollButtonInteractable(true);
        
        currentTimer = gameTimer;
        isGameActive = true;
        timerCoroutine = StartCoroutine(TimerCountdown());
        Debug.Log($"[GameManager] Countdown complete! Game timer started: {gameTimer} seconds");
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
            // For network sync, we update more frequently (every 0.5s) to ensure 
            // the UI stays snapped to global truth
            yield return new WaitForSeconds(0.5f);
            
            // FALLBACK: If we are offline or haven't received the network 'finish line' yet,
            // we manually decrement the timer locally so it doesn't appear frozen.
            // As soon as UpdateTimerDisplay find the network key, it will OVERWRITE this local value.
            currentTimer -= 0.5f;
            
            // Update display (this also resyncs currentTimer from Photon if in a room)
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
    }

    /// <summary>
    /// Called when the lobby/finish button on the winner screen is clicked
    /// Leaves the room and cleans up game state
    /// </summary>
    private void OnWinnerLobbyButtonClicked()
    {
        Debug.Log("[GameManager] Winner lobby button clicked.");
        
        // Return to lobby by disabling gameplay screen
        if (gameplayScreen != null)
        {
            gameplayScreen.SetActive(false);
        }
        
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(false);
        }
        
        ResetGameState();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from Socket events
        MatchmakingModel.OnNumberLockedEvent -= HandleServerNumberLocked;
        MatchmakingModel.OnGameOverEvent -= HandleServerGameOver;

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
            // In your current architecture, human opponents use scores from Photon properties instead of grids.
        }

        // 2. Record the time of this score achievement for tie-breakers
        // FIX: Use estimated server time so offline selections use the SAME scale as online ones.
        double currentTotalTime = GetEstimatedServerTime();
        playerLastScoreTime[ownerKey] = currentTotalTime;

        // 3. Update local score for local player
        if (ownerKey == -1) 
        {
            // NEW SCORING: 
            // - Selecting correct number: +100
            // - Selecting icon (if number is -1): +200
            if (number == -1) {
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
    }
    
    /// <summary>
    /// Called when the quit button is clicked - player goes to lobby screen
    /// No prizes awarded, no winner screen shown to quitting player
    /// If only 2 players total: CPU wins instantly but player doesn't see winner screen
    /// If more than 2 players: CPUs continue playing
    /// </summary>
    private void OnQuitButtonClicked()
    {
        // 0. If we are currently searching for a match, cancel it immediately
        if (MatchmakingModel.IsSearching)
        {
            MatchmakingModel.CancelSearch();
            
            // Also ensure the waiting panel is hidden if we quit during matchmaking
            HorizontalLobbyManager lobbyManager = FindObjectOfType<HorizontalLobbyManager>();
            if (lobbyManager != null)
            {
                // We'll add a public method to HorizontalLobbyManager to hide the panel
                lobbyManager.HideWaitingPanel();
            }
        }

        if (!isGameActive)
        {
            return; // Game already ended
        }
        
        Debug.Log("[GameManager] Player quit! Returning to lobby...");

        // Notify server about forfeit if in a room
        if (currentMatchData != null && !string.IsNullOrEmpty(currentMatchData.roomId))
        {
            if (SocketManager.Instance != null && SocketManager.Instance.Socket != null)
            {
                Debug.Log($"<color=#FFA500>⬆️ [Socket EMIT] 'forfeitMatch' for Room: {currentMatchData.roomId}</color>");
                SocketManager.Instance.Socket.Emit("forfeitMatch", new ForfeitPayload { roomId = currentMatchData.roomId });
            }
        }

        // Hide gameplay screen and quit panel
        if (gameplayScreen != null)
        {
            gameplayScreen.SetActive(false);
            Debug.Log("[GameManager] Gameplay screen hidden - player returned to lobby");
        }
        
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
        
        ResetGameState();
    }


    /// <summary>
    /// Shows the winner screen with 1st, 2nd, and 3rd place information
    /// </summary>
    private void ShowWinnerScreen(List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)> rankings)
    {
        if (rankings == null || rankings.Count == 0) return;
        
        winnerScreenShown = true;
        
        // Reset winner screen UI first
        ResetWinnerScreenUI();
        
        // Display top 3
        if (rankings.Count >= 1)
        {
            var firstPlace = rankings[0];
            float firstCash = lastAwardedCash; // For now, we only show the local player's prize
            int firstChips = lastAwardedChips;
            
            DisplayPlace(1, firstPlace.playerType, firstPlace.opponentIndex, firstPlace.count,
                         winnerProfileImage, winnerProfileImageCustom, winnerOutlineImage, winnerFrameImage, winnerNameText, winnerNameTextLegacy,
                         winnerScreenScript != null ? winnerScreenScript.GetFirstPlacePrizeIcon() : null,
                         winnerScreenScript != null ? winnerScreenScript.GetFirstPlacePrizeText() : null,
                         null, firstCash, firstChips,
                         (firstPlace.playerType == PlayerType.Network) ? "Opponent" : "");
        }

        // Hide 2nd and 3rd if not present
        if (rankings.Count < 2) HidePlace(secondPlaceProfileImage, secondPlaceOutlineImage, secondPlaceFrameImage, secondPlaceNameText, secondPlaceNameTextLegacy, null, null, null);
        if (rankings.Count < 3) HidePlace(thirdPlaceProfileImage, thirdPlaceOutlineImage, thirdPlaceFrameImage, thirdPlaceNameText, thirdPlaceNameTextLegacy, null, null, null);

        // Activate visuals
        if (winnerScreen != null) winnerScreen.SetActive(true);

        // Trigger animation
        if (winnerScreenScript != null)
        {
            List<(PlayerType, int, int)> uiRankings = new List<(PlayerType, int, int)>();
            foreach (var r in rankings) uiRankings.Add((r.playerType, r.opponentIndex, r.count));
            winnerScreenScript.PlayAnimationIfPlayerInTop3(uiRankings, currentLobbyConfig);
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
    private void DisplayPlace(int placeNumber, PlayerType playerType, int opponentIndex, int selectionCount,
                             Image profileImage, Image profileImageCustom, Image outlineImage, Image frameImage, TMPro.TextMeshProUGUI nameText, Text nameTextLegacy,
                             Image prizeIcon, TMPro.TextMeshProUGUI prizeText, Text prizeTextLegacy,
                             float cashPrize, int chipsPrize, string overrideName = "", int overrideAvatarIdx = -1, string overrideCustomImg = "")
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
        if (profileImage != null || profileImageCustom != null)
        {
            if (playerType == PlayerType.Player)
            {
                bool customProfileMatched = false;

                // Priority 1: Use Offline Snapshot Custom Image (Match Recovery)
                // We check this FIRST so match history overrides generic shortcuts
                if (!string.IsNullOrEmpty(overrideCustomImg))
                {
                    try
                    {
                        byte[] bytes = System.Convert.FromBase64String(overrideCustomImg);
                        Texture2D tex = new Texture2D(2, 2);
                        if (tex.LoadImage(bytes))
                        {
                            Sprite customSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            SetProfileSlot(profileImage, profileImageCustom, customSprite, true);
                            customProfileMatched = true;
                            Debug.Log($"[GameManager] Winner podium: used SAVED snapshot picture for place {placeNumber}");
                        }
                    }
                    catch (System.Exception ex) { Debug.LogWarning($"[GameManager] Failed to decode saved photo: {ex.Message}"); }
                }

                // Priority 2: Use Offline Snapshot Avatar Index (Match Recovery Fallback)
                if (!customProfileMatched && overrideAvatarIdx >= 0)
                {
                    if (availableProfileSprites != null && overrideAvatarIdx < availableProfileSprites.Length)
                    {
                        SetProfileSlot(profileImage, profileImageCustom, availableProfileSprites[overrideAvatarIdx], false);
                        customProfileMatched = true;
                        Debug.Log($"[GameManager] Winner podium: used SAVED avatar index {overrideAvatarIdx} for place {placeNumber}");
                    }
                }

                // Priority 3: Live Logic (Only if snapshot was empty)
                if (!customProfileMatched)
                {
                    if (opponentIndex == -1) // Local Player
                    {
                        // Use standard player avatar from profile
                        bool isCustomLocally = (playerProfileImageCustom != null && playerProfileImageCustom.gameObject.activeSelf) 
                                            || (mainMenuAvatarDisplay != null && mainMenuAvatarDisplay.IsShowingCustomAvatar());

                        if (playerSelectedAvatar != null)
                        {
                            SetProfileSlot(profileImage, profileImageCustom, playerSelectedAvatar, isCustomLocally);
                            customProfileMatched = true;
                            Debug.Log($"[GameManager] Winner podium: local player avatar assigned.");
                        }
                    }
                    else if (opponentIndex <= -100) // Network Human
                    {
                        int actorNumber = Mathf.Abs(opponentIndex + 100);

                        // Priority A: Use Cached High-Quality Custom Avatar
                        if (cachedNetworkAvatars.TryGetValue(actorNumber, out Sprite cachedSprite))
                        {
                            SetProfileSlot(profileImage, profileImageCustom, cachedSprite, true);
                            customProfileMatched = true;
                        }
                        else if (cachedNetworkAvatarIndices.TryGetValue(actorNumber, out int cachedIdx))
                        {
                            // Priority B: Use Cached Basic Avatar Index
                            if (availableProfileSprites != null && cachedIdx < availableProfileSprites.Length)
                            {
                                SetProfileSlot(profileImage, profileImageCustom, availableProfileSprites[cachedIdx], false);
                                customProfileMatched = true;
                            }
                        }
                    }
                }

                // Final Fallback: Default icon if all else failed
                if (!customProfileMatched && availableProfileSprites != null && availableProfileSprites.Length > 0)
                {
                    SetProfileSlot(profileImage, profileImageCustom, availableProfileSprites[0], false);
                }
            }
            else // Network Opponent (Human or Bot)
            {
                if (opponentProfileImages != null && opponentIndex >= 0 && opponentIndex < opponentProfileImages.Length)
                {
                    if (opponentProfileImages[opponentIndex] != null && opponentProfileImages[opponentIndex].sprite != null)
                    {
                        SetProfileSlot(profileImage, profileImageCustom, opponentProfileImages[opponentIndex].sprite, false);
                    }
                }
            }
        }
        
        // Set outline color
        if (outlineImage != null)
        {
            if (playerType == PlayerType.Player)
            {
                if (opponentIndex == -1) // Local Player
                {
                    outlineImage.color = playerOutlineColor;
                }
                else // Network Human
                {
                    // Use a distinct color for network humans if available, or just white/default
                    outlineImage.color = Color.white;
                }
            }
            else // Opponent
            {
                if (opponentOutlineColors != null && opponentIndex >= 0 && opponentIndex < opponentOutlineColors.Length)
                {
                    outlineImage.color = opponentOutlineColors[opponentIndex];
                }
            }
        }
        
        // Set name (use override if provided for offline recovery)
        string playerName = !string.IsNullOrEmpty(overrideName) ? overrideName : GetWinnerName(playerType, opponentIndex);
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
                prizeText.text = "$" + CurrencyDisplayUI.FormatCash(cashPrize);
                prizeText.gameObject.SetActive(true);
                Debug.Log($"[GameManager] Set prize text to ${CurrencyDisplayUI.FormatCash(cashPrize)} for place {placeNumber}");
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
                prizeText.text = CurrencyDisplayUI.FormatCoins(chipsPrize);
                prizeText.gameObject.SetActive(true);
                Debug.Log($"[GameManager] Set prize text to {CurrencyDisplayUI.FormatCoins(chipsPrize)} chips for place {placeNumber}");
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
    /// Gets the display name 
    private string GetWinnerName(PlayerType playerType, int opponentIndex)
    {
        if (playerType == PlayerType.Player)
        {
            return "YOU";
        }
        else // Network Opponent
        {
            return "Opponent";
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
            return;
        }
        
        // Try to get avatar from MainMenuAvatarDisplay
        if (mainMenuAvatarDisplay != null)
        {
            // Use the public method to get the current avatar sprite
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
                        playerProfileImageCustom.rectTransform.localScale = Vector3.one * 2f;
                        playerProfileImageCustom.gameObject.SetActive(true);
                    }
                    if (playerProfileImage != null) playerProfileImage.gameObject.SetActive(false);
                    Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay (Custom/Zoomed)");
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
                    Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay (Standard)");
                }
                return;
            }
        }
        
        // Try to get from AvatarSelector
        AvatarSelector avatarSelector = FindObjectOfType<AvatarSelector>();
        if (avatarSelector != null)
        {
            // Try to get current selected avatar
            // Get selected avatar index from PlayerModel
            int selectedIndex = PlayerModel.SelectedAvatarIndex;
            
            Sprite avatarSprite = avatarSelector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                if (playerProfileImage != null) playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log($"[GameManager] Player profile image loaded from AvatarSelector (index: {selectedIndex})");
                return;
            }
        }
        
        Debug.LogWarning("[GameManager] Could not load player profile image from main menu. Using default or assigned sprite.");
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
                if (opponentProfileImages[opponentCount] != null)
                {
                    opponentProfileImages[opponentCount].gameObject.SetActive(true);
                    // Use a helper to load the avatar (assuming index or name mapping exists)
                    opponentProfileImages[opponentCount].sprite = GetAvatarSprite(info.avatar);
                }
                
                opponentCount++;
            }
        }
        
        // Hide remaining slots
        for (int i = opponentCount; i < opponentProfileImages.Length; i++)
        {
            if (opponentProfileImages[i] != null) opponentProfileImages[i].gameObject.SetActive(false);
            if (opponentOutlineImages[i] != null) opponentOutlineImages[i].gameObject.SetActive(false);
        }
    }

    private Sprite GetAvatarSprite(string avatarKey)
    {
        // Try to parse index if it's a number, or use a default
        if (int.TryParse(avatarKey, out int index) && availableProfileSprites != null && index >= 0 && index < availableProfileSprites.Length)
            return availableProfileSprites[index];
        
        // Fallback to random or boy/girl default
        return (availableProfileSprites != null && availableProfileSprites.Length > 0) ? availableProfileSprites[0] : null;
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
            Debug.LogWarning("[GameManager] Opponent outline images array is empty or not assigned!");
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
        gameTimer = initialGameTimer; // Reset timer back to its original value after match ends
        currentTimer = gameTimer; // Reset to default timer value
        
        // Clear current lobby config (will be set when new game starts)
        currentLobbyConfig = null;
        currentMatchData = null;
        
        // Reset outline fill amounts
        ResetOutlineFillAmounts();
        
        if (calculateLoadingScreen != null) calculateLoadingScreen.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);
        
        // Reset score UI
        if (ScoreFeedbackUIManager.Instance != null)
        {
            ScoreFeedbackUIManager.Instance.ResetScore();
        }

        Debug.Log("[GameManager] Game state reset");
    }
    
    /// <summary>
    /// Configures the game from a lobby configuration
    /// Sets number of CPUs, handles entry fee, and stores prize information
    /// </summary>
    public void ConfigureGameFromLobby(LobbyConfig config, int overrideCPUs = -1, StartGameData matchData = null)
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
        // gameTimer will be updated below from currentLobbyConfig
        
        // Reset winner screen UI to ensure clean state
        ResetWinnerScreenUI();
        
        // Reset outline fill amounts for new game
        ResetOutlineFillAmounts();
        
        Debug.Log("[GameManager] Board reset for new match.");
        
        // Set new lobby configuration
        currentLobbyConfig = config;
        currentMatchData = matchData;
        
        if (matchData == null)
        {
            Debug.LogError("❌ [GameManager] CRITICAL ERROR: Attempted to start game WITHOUT matchData. Aborting.");
            return;
        }

        Debug.Log($"[GameManager] ✅ Server match initialized. Room: {matchData.roomId}, Duration: {matchData.duration}s");

        // Update game duration from server data
        if (matchData.duration > 0)
        {
            this.gameTimer = matchData.duration;
        }
        else
        {
            this.gameTimer = config.gameTimer;
        }
        
        this.currentTimer = this.gameTimer;
        
        // Setup Opponents and Grids
        SetUpOpponentsFromMatch(matchData);
        CreateIndividualNumberGrids(matchData);
        
        Debug.Log($"[GameManager] Game configured from Lobby: {config.lobbyName}. Participants: {matchData.playerAvatars.Count}");
        
        // Clear network caches for a fresh game session
        cachedNetworkNames.Clear();
        cachedNetworkAvatars.Clear();
        cachedNetworkAvatarIndices.Clear();
        Debug.Log("[GameManager] Network profile caches cleared for new session.");
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
    /// Awards prizes based on current lobby configuration and player rankings
    /// </summary>
    private void AwardPrizes(List<(PlayerType playerType, int cpuIndex, int count, double time, string userId)> rankings)
    {
        if (prizesAwardedForThisMatch) return;
        prizesAwardedForThisMatch = true;

        Debug.Log("[GameManager] Awarding prizes (visual only, server handles data).");
        
        // Show winner screen
        ShowWinnerScreen(rankings);
    }
    
    /// <summary>
    /// Refreshes lobby buttons after a delay (to allow animation to complete)
    /// </summary>
    private System.Collections.IEnumerator RefreshLobbyButtonsDelayed()
    {
        // Wait for animation to complete (particles + count-up)
        yield return new WaitForSeconds(3f); // Adjust based on your animation duration
        
        HorizontalLobbyManager lobbyManager = FindObjectOfType<HorizontalLobbyManager>();
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

    private void SetProfileSlot(Image spriteSlot, Image customSlot, Sprite sprite, bool isCustom)
    {
        if (spriteSlot != null) spriteSlot.gameObject.SetActive(false);
        if (customSlot != null) customSlot.gameObject.SetActive(false);

        if (isCustom)
        {
            if (customSlot != null)
            {
                customSlot.sprite = sprite;
                customSlot.rectTransform.localScale = Vector3.one * 2f;
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

[System.Serializable]
public class ForfeitPayload
{
    public string roomId;
}
