using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitingForPlayerPanelManager : MonoBehaviour
{
    public static WaitingForPlayerPanelManager Instance { get; private set; }

    // ========== YE PROPERTY ZAROORI HAI ==========
    public bool IsPanelActive => waitingForPlayerPanel != null ? waitingForPlayerPanel.activeSelf : gameObject.activeSelf;
    // =============================================

    [Header("Panel References")]
    [SerializeField] private GameObject waitingForPlayerPanel;
    [SerializeField] private GameObject selectBoardPanel;      // Back button ke liye (wapas jaana)
    [SerializeField] private GameObject gameplayScreenPanel;   // Start Game ke baad yeh open hoga

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI lookingForPlayersTitle;

    [Header("Slots")]
    [SerializeField] private Transform slotsContainer;          // Content
    [SerializeField] private GameObject playerSlotPrefab;       // ProfilePlayer prefab
    [SerializeField] private GameObject waitingSlotPrefab;      // Waitingtojoin prefab

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Button backButton;

    [Header("Settings")]
    [SerializeField] private bool enableBotSimulation = true;
    [SerializeField] private float botJoinInterval = 1.2f;
    [SerializeField] private bool autoStartWhenFull = false;

    // Runtime
    private int targetPlayerCount = 5;
    private int currentJoinedCount = 0;
    private bool isLocalPlayerReady = true;
    private bool isHost = true;

    private List<PlayerSlotData> players = new List<PlayerSlotData>();
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private Coroutine botCoroutine;

    [System.Serializable]
    public class PlayerSlotData
    {
        public string name;
        public Sprite avatar;
        public bool isReady;
        public bool isBot;

        public PlayerSlotData(string name, bool isReady = true, bool isBot = false, Sprite avatar = null)
        {
            this.name = name;
            this.isReady = isReady;
            this.isBot = isBot;
            this.avatar = avatar;
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

    public void OpenWaitingPanel(int maxPlayers, int stake = 100, string lobbyId = "combo-dice")
    {
        targetPlayerCount = Mathf.Clamp(maxPlayers, 2, 10);
        isLocalPlayerReady = true;
        isHost = true;

        players.Clear();
        string localName = PlayerModel.Name ?? ("Player " + Random.Range(100, 999));
        players.Add(new PlayerSlotData(localName, true, false));

        currentJoinedCount = 1;

        waitingForPlayerPanel.SetActive(true);
        RebuildSlots();
        UpdateUI();

        if (botCoroutine != null) StopCoroutine(botCoroutine);
        if (enableBotSimulation)
            botCoroutine = StartCoroutine(SimulateBots());
    }

    public void CloseWaitingPanel()
    {
        if (botCoroutine != null) StopCoroutine(botCoroutine);
        ClearSlots();
        waitingForPlayerPanel.SetActive(false);
    }

    private void RebuildSlots()
    {
        ClearSlots();

        // Only spawn slots for players who have actually joined.
        // No extra empty "Waiting..." slots — count grows as players join.
        for (int i = 0; i < players.Count; i++)
        {
            GameObject slot = Instantiate(playerSlotPrefab, slotsContainer);
            var ui = slot.GetComponent<WaitingPlayerSlotUI>();
            if (ui != null)
                ui.SetJoinedPlayer(players[i].name, players[i].avatar, players[i].isReady);

            spawnedSlots.Add(slot);
        }

        if (slotsContainer is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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

        // Baki empty slots ko bots se fill karo
        while (players.Count < targetPlayerCount)
        {
            players.Add(new PlayerSlotData("Bot_" + Random.Range(100, 999), true, true));
        }

        CloseWaitingPanel();
        Debug.Log("🚀 Starting Game with " + targetPlayerCount + " players");

        // ── Option 1: GameManager ke zariye proper game start karo ──
        if (GameManager.Instance != null)
        {
            // targetPlayerCount ke liye ek minimal LobbyConfig banao
            LobbyConfig config = new LobbyConfig(
                lobbyId: -1,
                numberOfPlayers: targetPlayerCount,
                entryFeeCash: 0,
                entryFeeChips: 0,
                prizePool1stCash: 0f,
                prizePool1stChips: 0,
                prizePool2ndCash: 0f,
                prizePool2ndChips: 0,
                prizePool3rdCash: 0f,
                prizePool3rdChips: 0
            );
            GameManager.Instance.ConfigureGameFromLobby(config);
            return;
        }

        // ── Option 2: Directly gameplay screen activate karo ──
        if (gameplayScreenPanel != null)
        {
            gameplayScreenPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[WaitingForPlayerPanelManager] gameplayScreenPanel assign nahi hai! Inspector mein assign karo.");
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
}