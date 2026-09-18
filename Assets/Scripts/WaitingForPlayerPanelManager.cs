using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitingForPlayerPanelManager : MonoBehaviour
{
    public static WaitingForPlayerPanelManager Instance { get; private set; }

    public bool IsPanelActive => gameObject.activeInHierarchy;

    public void OnStartGameClicked()
    {
        if (!isHost) return;

        SoundManager.instance?.PlayTap();

        if (serverMatchData == null)
        {
            // If offline/bots, we can start immediately
            if (enableBotSimulation || (BackendAuthService.Instance != null && !BackendAuthService.HasStoredJWT()))
            {
                serverMatchData = GameManager.CreateFallbackMatchData(targetPlayerCount, 100f);
            }
            // If online but server hasn't replied yet, force start
            else if (isHost && !string.IsNullOrEmpty(currentLobbyId))
            {
                MatchmakingModel.StartNow(currentLobbyId);
                return; // Wait for server to reply with startGame payload
            }
        }

        CloseWaitingPanel();
        Debug.Log("🚀 Starting Game with " + targetPlayerCount + " players");

    [SerializeField] private GameObject waitingForPlayerPanel;
    [SerializeField] private GameObject selectBoardPanel;
    [SerializeField] private GameObject gameplayScreenPanel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI lookingForPlayersTitle;

    [Header("Slots")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private GameObject waitingSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Button backButton;

    [Header("Settings")]
    [SerializeField] private bool enableBotSimulation = false;
    [SerializeField] private float botJoinInterval = 1.2f;

    private int targetPlayerCount = 5;
    private int currentJoinedCount = 0;
    private bool isLocalPlayerReady = true;
    private bool isHost = true;
    private string currentLobbyId = "";

    private List<PlayerSlotData> players = new List<PlayerSlotData>();
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private Coroutine botCoroutine;
    private StartGameData serverMatchData;

    [System.Serializable]
    public class PlayerSlotData
    {
        public string name;
        public Sprite avatar;
        public string frame;
        public bool isReady;
        public bool isBot;

        public PlayerSlotData(string name, bool isReady = true, bool isBot = false, Sprite avatar = null, string frame = null)
        {
            this.name = name;
            this.isReady = isReady;
            this.isBot = isBot;
            this.avatar = avatar;
            this.frame = frame;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (waitingForPlayerPanel == null)
            waitingForPlayerPanel = gameObject;

        startGameButton?.onClick.AddListener(OnStartGameClicked);
        readyButton?.onClick.AddListener(OnReadyClicked);
        backButton?.onClick.AddListener(OnBackClicked);
    }

    public void OpenWaitingPanel(int maxPlayers, float stake = 100f, string lobbyId = "combo-dice", string mode = "3p")
    {
        targetPlayerCount = Mathf.Clamp(maxPlayers, 2, 10);
        isLocalPlayerReady = true;
        isHost = true;
        serverMatchData = null;
        currentLobbyId = lobbyId;

        players.Clear();
        string localName = PlayerModel.Name ?? ("Player " + Random.Range(100, 999));
        players.Add(new PlayerSlotData(localName, true, false));

        currentJoinedCount = 1;

        waitingForPlayerPanel.SetActive(true);
        RebuildSlots();
        UpdateUI();

        // ✅ FIX: Subscribe to real matchmaking events so server players show up
        MatchmakingModel.OnMatchmakingUpdated -= OnMatchmakingUpdated;
        MatchmakingModel.OnMatchmakingUpdated += OnMatchmakingUpdated;
        MatchmakingModel.OnMatchFound -= OnMatchFoundFromServer;
        MatchmakingModel.OnMatchFound += OnMatchFoundFromServer;

        // ✅ FIX: Trigger actual server matchmaking using exact backend mode
        MatchmakingModel.FindMatch(mode, stake, lobbyId);
        Debug.Log($"[WaitingForPlayerPanelManager] OpenWaitingPanel — FindMatch called for {maxPlayers} players (lobby: {lobbyId}, mode: {mode})");

        // Bot simulation only runs as fallback (e.g. offline/debug mode) if not an arena preset
        if (botCoroutine != null) StopCoroutine(botCoroutine);
        
        bool isArena = false;
        var config = LobbyModel.Configurations;
        if (config != null)
        {
            foreach(var c in config)
            {
                if (c.lobbyId == lobbyId && c.isArenaPreset)
                {
                    isArena = true;
                    break;
                }
            }
        }
        
        if (enableBotSimulation && !BackendAuthService.HasStoredJWT() && !isArena)
            botCoroutine = StartCoroutine(SimulateBots());
    }

    public void CloseWaitingPanel()
    {
        if (botCoroutine != null) StopCoroutine(botCoroutine);
        ClearSlots();
        waitingForPlayerPanel.SetActive(false);

        // ✅ FIX: Unsubscribe from matchmaking events to prevent memory leaks / stale calls
        MatchmakingModel.OnMatchmakingUpdated -= OnMatchmakingUpdated;
        MatchmakingModel.OnMatchFound -= OnMatchFoundFromServer;
    }

    private void RebuildSlots()
    {
        EnsureHorizontalSlotsLayout();
        ClearSlots();

        // 1. Instantiate joined players
        for (int i = 0; i < players.Count; i++)
        {
            if (i >= targetPlayerCount) break; // Safety check

            GameObject slot = Instantiate(playerSlotPrefab, slotsContainer);
            slot.SetActive(true); // Fix: Ensure it's active even if prefab is disabled
            var ui = slot.GetComponent<WaitingPlayerSlotUI>();
            if (ui != null)
                ui.SetJoinedPlayer(players[i].name, players[i].avatar, players[i].frame, players[i].isReady);

            spawnedSlots.Add(slot);
        }

        // 2. Instantiate "Waiting..." slots for the remaining empty spots
        int remainingSlots = targetPlayerCount - players.Count;
        for (int i = 0; i < remainingSlots; i++)
        {
            if (waitingSlotPrefab != null)
            {
                GameObject waitingSlot = Instantiate(waitingSlotPrefab, slotsContainer);
                waitingSlot.SetActive(true);
                spawnedSlots.Add(waitingSlot);
            }
        }

        // Force rebuild to ensure ScrollView detects the new size correctly
        if (slotsContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void EnsureHorizontalSlotsLayout()
    {
        if (slotsContainer == null) return;

        VerticalLayoutGroup verticalLayout = slotsContainer.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
        {
            DestroyImmediate(verticalLayout);
        }

        HorizontalLayoutGroup horizontalLayout = slotsContainer.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayout == null)
        {
            horizontalLayout = slotsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        horizontalLayout.enabled = true;
        horizontalLayout.spacing = 20f;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandWidth = true;
        horizontalLayout.childForceExpandHeight = true;

        ContentSizeFitter contentSizeFitter = slotsContainer.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = slotsContainer.gameObject.AddComponent<ContentSizeFitter>();
        }

        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ClearSlots()
    {
        foreach (var s in spawnedSlots)
            if (s != null) Destroy(s);
        spawnedSlots.Clear();
    }

    private IEnumerator SimulateBots()
    {
        string[] botNames = {
            "DiceMaster", "Alex_Rush", "Shadow99", "ProGamer", "LuckyRoll",
            "DiceKing", "Speedy88", "Titan_X", "Viper", "Nova"
        };

        int index = 0;
        yield return new WaitForSeconds(0.6f);

        while (players.Count < targetPlayerCount)
        {
            yield return new WaitForSeconds(botJoinInterval);

            players.Add(new PlayerSlotData(botNames[index % botNames.Length], true, true));
            currentJoinedCount = players.Count;

            RebuildSlots();
            UpdateUI();
            index++;
        }

        // User must manually press the Start Game button.
    }

    private void UpdateUI()
    {
        if (playerCountText != null)
            playerCountText.text = $"<color=#FFCC00>{currentJoinedCount}/{targetPlayerCount}</color> Players";

        if (lookingForPlayersTitle != null)
        {
            lookingForPlayersTitle.text = currentJoinedCount >= targetPlayerCount
                ? "All Players Joined! Press Start"
                : "Looking For Players...";
        }

        bool canStart = isHost && isLocalPlayerReady;
        if (startGameButton != null)
            startGameButton.interactable = canStart;

        if (readyButtonText != null)
            readyButtonText.text = isLocalPlayerReady ? "✓ Ready" : "Tap Ready";
    }

    public void OnReadyClicked()
    {
        isLocalPlayerReady = !isLocalPlayerReady;
        if (players.Count > 0)
            players[0].isReady = isLocalPlayerReady;

        RebuildSlots();
        UpdateUI();
        SoundManager.instance?.PlayTap();
    }

    public void OnStartGameClicked()
    {
        if (!isHost) return;

        SoundManager.instance?.PlayTap();

        if (serverMatchData == null)
        {
            // If no match data from server yet, this might be an Arena Start Now attempt
            if (isHost && !string.IsNullOrEmpty(currentLobbyId))
            {
                MatchmakingModel.StartNow(currentLobbyId);
                Debug.Log($"[WaitingForPlayerPanelManager] Emitted startNow for {currentLobbyId}");
                return; // Wait for server to reply with startGame payload
            }
            else
            {
                Debug.LogWarning("[WaitingForPlayerPanelManager] Cannot start without a valid server match.");
                return;
            }
        }

        CloseWaitingPanel();
        Debug.Log("🚀 Starting Game with " + targetPlayerCount + " players");

        if (GameManager.Instance != null)
        {
            LobbyConfig config = new LobbyConfig(
                -1,
                targetPlayerCount,
                0,
                0,
                0f,
                0,
                0f,
                0,
                0f,
                0
            );

            GameManager.Instance.ConfigureGameFromLobby(config, -1, serverMatchData);
            return;
        }

        if (gameplayScreenPanel != null)
        {
            gameplayScreenPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WaitingForPlayerPanelManager] gameplayScreenPanel assign nahi hai!");
        }
    }

    public void OnBackClicked()
    {
        SoundManager.instance?.PlayTap();
        MatchmakingModel.CancelSearch();
        CloseWaitingPanel();

        if (selectBoardPanel != null)
            selectBoardPanel.SetActive(true);
    }

    public void AddRealPlayer(string name, Sprite avatar = null)
    {
        if (players.Count >= targetPlayerCount) return;

        players.Add(new PlayerSlotData(name, true, false, avatar));
        currentJoinedCount = players.Count;
        RebuildSlots();
        UpdateUI();
    }

    public void SetHost(bool value)
    {
        isHost = value;
        UpdateUI();
    }

    // ✅ FIX: Called by MatchmakingModel when server sends "matchmakingUpdate" {current, target}
    // This is how REAL players joining appear in the panel slots.
    private void OnMatchmakingUpdated(int current, int target)
    {
        Debug.Log($"[WaitingForPlayerPanelManager] matchmakingUpdate received: {current}/{target}");

        // Fill up slots to match the server's reported current count
        // Start from 1 because index 0 is the local player (already added)
        while (players.Count < current && players.Count < targetPlayerCount)
        {
            players.Add(new PlayerSlotData("Player " + Random.Range(100, 999), true, false));
            currentJoinedCount = players.Count;
        }

        RebuildSlots();
        UpdateUI();
    }

    // ✅ FIX: Called when server confirms a real match is found ("startGame" event)
    // Stop bot coroutine since we now have real players, then let normal game flow proceed.
    private void OnMatchFoundFromServer(StartGameData data)
    {
        Debug.Log("[WaitingForPlayerPanelManager] Real match found from server! Stopping bot simulation.");

        if (data == null)
        {
            Debug.LogWarning("[WaitingForPlayerPanelManager] Server returned an empty match payload.");
            return;
        }

        serverMatchData = data;

        // Stop bot simulation — we have real players now
        if (botCoroutine != null)
        {
            StopCoroutine(botCoroutine);
            botCoroutine = null;
        }

        // Fill remaining slots with the confirmed player count from server
        if (data != null && data.playerAvatars != null)
        {
            players.Clear();
            string localName = PlayerModel.Name ?? "Me";
            players.Add(new PlayerSlotData(localName, true, false));

            foreach (var kvp in data.playerAvatars)
            {
                if (players.Count >= targetPlayerCount) break;
                // Avoid adding ourselves twice
                if (kvp.Value != null && kvp.Key != PlayerModel.Id)
                {
                    string pName = !string.IsNullOrEmpty(kvp.Value.name) ? kvp.Value.name : ("Player " + Random.Range(100, 999));
                    players.Add(new PlayerSlotData(pName, true, !kvp.Value.isBot == false, null, kvp.Value.frame));
                }
            }

            currentJoinedCount = players.Count;
            RebuildSlots();
            UpdateUI();
        }

        // NOTE: Do NOT close the panel or start the game here.
        // The user must press "Start Game" manually (or the host decides).
        // HorizontalLobbyManager.HandleMatchFound will not auto-start because IsPanelActive == true.
    }
}