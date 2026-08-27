using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitingForPlayerPanelManager : MonoBehaviour
{
    public static WaitingForPlayerPanelManager Instance { get; private set; }

    public bool IsPanelActive => waitingForPlayerPanel != null ? waitingForPlayerPanel.activeSelf : gameObject.activeSelf;

    [Header("Panel References")]
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
    [SerializeField] private bool enableBotSimulation = true;
    [SerializeField] private float botJoinInterval = 1.2f;
    [SerializeField] private bool autoStartWhenFull = false;

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

    public void OpenWaitingPanel(int maxPlayers, float stake = 100f, string lobbyId = "combo-dice")
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

        // 1. Instantiate joined players
        for (int i = 0; i < players.Count; i++)
        {
            if (i >= targetPlayerCount) break; // Safety check

            GameObject slot = Instantiate(playerSlotPrefab, slotsContainer);
            slot.SetActive(true); // Fix: Ensure it's active even if prefab is disabled
            var ui = slot.GetComponent<WaitingPlayerSlotUI>();
            if (ui != null)
                ui.SetJoinedPlayer(players[i].name, players[i].avatar, players[i].isReady);

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

        // Fix: Removed autoStartWhenFull to prevent game from starting on its own
        // User must manually press the Start Game button
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

        while (players.Count < targetPlayerCount)
        {
            players.Add(new PlayerSlotData("Bot_" + Random.Range(100, 999), true, true));
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

            GameManager.Instance.ConfigureGameFromLobby(config);
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
}