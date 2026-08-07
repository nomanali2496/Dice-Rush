using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Waiting For Players Panel.
/// Handles dynamic slot creation based on selected player count (3, 5, 8, 10 players),
/// updates player count text ('X/N Players'), handles bot/socket joining, and triggers game start.
/// </summary>
public class WaitingForPlayerPanelManager : MonoBehaviour
{
    public static WaitingForPlayerPanelManager Instance { get; private set; }

    [Header("UI Panel Reference")]
    [Tooltip("The main container GameObject for the Waiting For Players Panel")]
    [SerializeField] private GameObject waitingForPlayerPanel;

    [Tooltip("Reference to Select Board / Lobby Panel to show when back button is pressed")]
    [SerializeField] private GameObject selectBoardPanel;

    [Header("Text References")]
    [Tooltip("Text component to display current joined count vs target, e.g. '3/5 Players'")]
    [SerializeField] private TextMeshProUGUI playerCountText;

    [Tooltip("Title text component, e.g. 'Looking For Players'")]
    [SerializeField] private TextMeshProUGUI lookingForPlayersTitle;

    [Header("Slots Container & Prefabs")]
    [Tooltip("Parent Transform (Grid / Horizontal Layout) where player slots are spawned")]
    [SerializeField] private Transform slotsContainer;

    [Tooltip("Prefab for a Joined & Ready player slot")]
    [SerializeField] private GameObject playerSlotPrefab;

    [Tooltip("Prefab for an Empty / Waiting slot (optional, if unassigned uses playerSlotPrefab in waiting state)")]
    [SerializeField] private GameObject waitingSlotPrefab;

    [Header("Action Buttons")]
    [Tooltip("Button to manually start the game")]
    [SerializeField] private Button startGameButton;

    [Tooltip("Back / Cancel matchmaking button")]
    [SerializeField] private Button backButton;

    [Header("Matchmaking / Simulation Settings")]
    [Tooltip("If true, automatically simulates players joining when offline or waiting for real players")]
    [SerializeField] private bool enableBotSimulation = true;

    [Tooltip("Time interval in seconds between simulated player joins")]
    [SerializeField] private float botJoinIntervalSeconds = 1.5f;

    [Tooltip("If true, automatically starts the game when all required player slots are full")]
    [SerializeField] private bool autoStartWhenFull = true;

    // Runtime state
    private int targetPlayerCount = 5;
    private int currentJoinedCount = 1;
    private int currentStake = 100;
    private string currentLobbyId = "combo-dice";

    private List<GameObject> spawnedSlotObjects = new List<GameObject>();
    private List<PlayerSlotData> joinedPlayersList = new List<PlayerSlotData>();
    private Coroutine botSimulationCoroutine;

    [System.Serializable]
    public class PlayerSlotData
    {
        public string playerName;
        public Sprite avatarSprite;
        public bool isReady;

        public PlayerSlotData(string name, Sprite avatar = null, bool ready = true)
        {
            playerName = name;
            avatarSprite = avatar;
            isReady = ready;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (waitingForPlayerPanel == null)
        {
            waitingForPlayerPanel = gameObject;
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnEnable()
    {
        // Subscribe to socket matchmaking events if available
        MatchmakingModel.OnMatchmakingUpdated += HandleMatchmakingUpdated;
        MatchmakingModel.OnMatchFound += HandleMatchFound;
        MatchmakingModel.OnMatchmakingFailed += HandleMatchmakingFailed;
    }

    private void OnDisable()
    {
        MatchmakingModel.OnMatchmakingUpdated -= HandleMatchmakingUpdated;
        MatchmakingModel.OnMatchFound -= HandleMatchFound;
        MatchmakingModel.OnMatchmakingFailed -= HandleMatchmakingFailed;
    }

    /// <summary>
    /// Opens the Waiting Panel and initializes slots according to selected player count (3, 5, 8, 10).
    /// </summary>
    /// <param name="totalPlayers">Target player count selected by user (e.g. 3, 5, 8, 10)</param>
    /// <param name="stake">Entry fee / stake amount</param>
    /// <param name="lobbyId">Selected lobby ID</param>
    public void OpenWaitingPanel(int totalPlayers, int stake = 100, string lobbyId = "combo-dice")
    {
        targetPlayerCount = Mathf.Max(2, totalPlayers);
        currentStake = stake;
        currentLobbyId = lobbyId;

        Debug.Log($"<color=#00FFFF>⌛ [WaitingPanel] Opening Waiting Panel for {targetPlayerCount} Players (Stake: {currentStake})</color>");

        if (waitingForPlayerPanel != null)
        {
            waitingForPlayerPanel.SetActive(true);
        }

        // Add local player as 1st joined player
        joinedPlayersList.Clear();
        string localName = !string.IsNullOrEmpty(PlayerModel.Name) ? PlayerModel.Name : ("Player " + UnityEngine.Random.Range(100, 999));
        joinedPlayersList.Add(new PlayerSlotData(localName, null, true));
        currentJoinedCount = 1;

        UpdatePlayerCountDisplay();
        RebuildAllSlots();

        // Enable / Disable start game button initial state
        if (startGameButton != null)
        {
            startGameButton.interactable = (currentJoinedCount >= targetPlayerCount);
        }

        // Start socket search or bot simulation
        if (SocketManager.Instance != null && SocketManager.Instance.Socket != null && BackendAuthService.HasStoredJWT())
        {
            string mode = $"{targetPlayerCount}-players";
            MatchmakingModel.FindMatch(mode, currentStake, currentLobbyId);
        }
        else if (enableBotSimulation)
        {
            if (botSimulationCoroutine != null)
            {
                StopCoroutine(botSimulationCoroutine);
            }
            botSimulationCoroutine = StartCoroutine(SimulateBotPlayersJoining());
        }
    }

    /// <summary>
    /// Closes the Waiting For Players panel.
    /// </summary>
    public void CloseWaitingPanel()
    {
        if (botSimulationCoroutine != null)
        {
            StopCoroutine(botSimulationCoroutine);
            botSimulationCoroutine = null;
        }

        ClearSpawnedSlots();

        if (waitingForPlayerPanel != null)
        {
            waitingForPlayerPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Adds a newly joined player to the waiting slots list.
    /// </summary>
    public void AddJoinedPlayer(string playerName, Sprite avatarSprite = null)
    {
        if (currentJoinedCount >= targetPlayerCount) return;

        joinedPlayersList.Add(new PlayerSlotData(playerName, avatarSprite, true));
        currentJoinedCount = joinedPlayersList.Count;

        Debug.Log($"<color=#00FF00>👤 [WaitingPanel] Player Joined: {playerName} ({currentJoinedCount}/{targetPlayerCount})</color>");

        UpdatePlayerCountDisplay();
        RebuildAllSlots();

        if (startGameButton != null)
        {
            startGameButton.interactable = (currentJoinedCount >= targetPlayerCount);
        }

        if (currentJoinedCount >= targetPlayerCount && autoStartWhenFull)
        {
            StartCoroutine(AutoStartGameDelay());
        }
    }

    /// <summary>
    /// Updates the text display, e.g. "3/5 Players".
    /// </summary>
    private void UpdatePlayerCountDisplay()
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"<color=#FFCC00>{currentJoinedCount}/{targetPlayerCount}</color> Players";
        }
    }

    /// <summary>
    /// Re-instantiates or updates all slot GameObjects inside slotsContainer.
    /// </summary>
    private void RebuildAllSlots()
    {
        ClearSpawnedSlots();

        if (slotsContainer == null)
        {
            Debug.LogWarning("[WaitingPanelManager] 'slotsContainer' is not assigned in the Inspector!");
            return;
        }

        for (int i = 0; i < targetPlayerCount; i++)
        {
            GameObject slotInstance = null;

            if (i < joinedPlayersList.Count)
            {
                // Ready player slot
                GameObject prefabToUse = playerSlotPrefab != null ? playerSlotPrefab : waitingSlotPrefab;
                if (prefabToUse != null)
                {
                    slotInstance = Instantiate(prefabToUse, slotsContainer);
                    WaitingPlayerSlotUI slotUI = slotInstance.GetComponent<WaitingPlayerSlotUI>();
                    if (slotUI != null)
                    {
                        var data = joinedPlayersList[i];
                        slotUI.SetReadyPlayer(data.playerName, data.avatarSprite);
                    }
                }
            }
            else
            {
                // Waiting empty slot
                GameObject prefabToUse = waitingSlotPrefab != null ? waitingSlotPrefab : playerSlotPrefab;
                if (prefabToUse != null)
                {
                    slotInstance = Instantiate(prefabToUse, slotsContainer);
                    WaitingPlayerSlotUI slotUI = slotInstance.GetComponent<WaitingPlayerSlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.SetWaitingSlot("Waiting...");
                    }
                }
            }

            if (slotInstance != null)
            {
                spawnedSlotObjects.Add(slotInstance);
            }
        }
    }

    private void ClearSpawnedSlots()
    {
        foreach (var obj in spawnedSlotObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedSlotObjects.Clear();
    }

    /// <summary>
    /// Coroutine to simulate bot players joining step by step when playing offline or waiting.
    /// </summary>
    private IEnumerator SimulateBotPlayersJoining()
    {
        yield return new WaitForSeconds(1.0f);

        string[] botNames = new string[] {
            "Player 234", "DiceMaster", "Alex_Rush", "Shadow99", "ProGamer",
            "LuckyRoll", "DiceKing", "Speedy88", "Titan_X", "Viper"
        };

        int nameIndex = 0;

        while (currentJoinedCount < targetPlayerCount)
        {
            yield return new WaitForSeconds(botJoinIntervalSeconds);

            string bName = botNames[nameIndex % botNames.Length];
            nameIndex++;

            AddJoinedPlayer(bName);
        }
    }

    private IEnumerator AutoStartGameDelay()
    {
        yield return new WaitForSeconds(1.2f);
        OnStartGameClicked();
    }

    /// <summary>
    /// Called when Start Game button is pressed or when auto-start triggers.
    /// </summary>
    public void OnStartGameClicked()
    {
        Debug.Log("<color=#00FF00>🚀 [WaitingPanel] Starting Game!</color>");

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        CloseWaitingPanel();

        // Configure fallback / match data for GameManager
        LobbyConfig config = new LobbyConfig(-1, targetPlayerCount, currentStake, 0, 0f, 0, 0f, 0, 0f, 0);
        config.lobbyId = currentLobbyId;
        config.mode = $"{targetPlayerCount}p";

        GameManager gm = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.ConfigureGameFromLobby(config, targetPlayerCount - 1, GameManager.CreateFallbackMatchData(targetPlayerCount, currentStake));
        }

        // Open gameplay container if referenced
        HorizontalLobbyManager lobbyMgr = HorizontalLobbyManager.Instance != null ? HorizontalLobbyManager.Instance : FindFirstObjectByType<HorizontalLobbyManager>();
        if (lobbyMgr != null && lobbyMgr.GameplayObject != null)
        {
            lobbyMgr.GameplayObject.SetActive(true);
        }
    }

    /// <summary>
    /// Called when Back button is pressed.
    /// </summary>
    public void OnBackButtonClicked()
    {
        Debug.Log("🔙 [WaitingPanel] Back button pressed. Cancelling matchmaking.");

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        MatchmakingModel.CancelSearch();
        CloseWaitingPanel();

        if (selectBoardPanel != null)
        {
            selectBoardPanel.SetActive(true);
        }
        else
        {
            HorizontalLobbyManager lobbyMgr = HorizontalLobbyManager.Instance != null ? HorizontalLobbyManager.Instance : FindFirstObjectByType<HorizontalLobbyManager>();
            if (lobbyMgr != null)
            {
                lobbyMgr.OnLobbyOpened();
            }
        }
    }

    #region Socket Event Handlers
    private void HandleMatchmakingUpdated(int current, int total)
    {
        targetPlayerCount = total;
        UpdatePlayerCountDisplay();
    }

    private void HandleMatchFound(StartGameData gameData)
    {
        Debug.Log("<color=#00FF00>🎮 [WaitingPanel] Real Match Found via Socket!</color>");
        CloseWaitingPanel();
    }

    private void HandleMatchmakingFailed(string error)
    {
        Debug.LogWarning($"[WaitingPanel] Matchmaking failed: {error}");
    }
    #endregion
}
