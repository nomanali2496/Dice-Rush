using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

/// <summary>
/// Handles Photon networking for the lobby, including joining rooms and waiting for players.
/// </summary>
public class LobbyNetworkManager : MonoBehaviourPunCallbacks
{
    public static LobbyNetworkManager Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject waitingPanel; // The panel shown when waiting for players
    [SerializeField] private TextMeshProUGUI statusText; // Text showing "Waiting for players 1/4"
    [SerializeField] private Button cancelButton; // Button to cancel and leave the room

    private LobbyConfig currentSelectedConfig;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelWaiting);

        // Connect to Photon if not already connected
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[LobbyNetworkManager] Connecting to Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }

        // Listen for data loading to trigger a resync if we are already in a room
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += SyncCurrentProfileToPhoton;
        }
    }

    private void OnDestroy()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= SyncCurrentProfileToPhoton;
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[LobbyNetworkManager] Connected to Master Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[LobbyNetworkManager] Joined Lobby! Ready to join rooms.");
    }

    /// <summary>
    /// Joins or creates a room based on the selected lobby configuration.
    /// </summary>
    public void JoinLobbyRoom(LobbyConfig config)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[LobbyNetworkManager] Not connected to Photon yet!");
            return;
        }

        currentSelectedConfig = config;
        Debug.Log($"[LobbyNetworkManager] Seeking room for Lobby: {config.lobbyName}");

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)config.numberOfPlayers;
        
        // Use custom properties to ensure players join the correct lobby type
        Hashtable customProps = new Hashtable();
        customProps.Add("LobbyIndex", config.lobbyIndex);
        roomOptions.CustomRoomProperties = customProps;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "LobbyIndex" };

        // We use a specific room name pattern or join random with filters
        // For simplicity, let's try joining a random room with the same LobbyIndex
        OpJoinRandomRoomParams joinParams = new OpJoinRandomRoomParams();
        joinParams.ExpectedCustomRoomProperties = customProps;

        PhotonNetwork.JoinRandomOrCreateRoom(
            expectedCustomRoomProperties: customProps,
            expectedMaxPlayers: (byte)config.numberOfPlayers,
            roomOptions: roomOptions
        );
    }

    private bool isStarting = false;
    private const string START_TIME_KEY = "StartTime";
    private const float MATCHMAKING_TIMEOUT = 30f;

    public override void OnJoinedRoom()
    {
        Debug.Log($"[LobbyNetworkManager] Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        if (waitingPanel != null) waitingPanel.SetActive(true);
        isStarting = false;
        hasSyncedProfile = false; // Reset sync flag for new room session

        // If we are the Master Client, set the start time for the countdown
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add(START_TIME_KEY, PhotonNetwork.Time);
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        // Try to sync initially (might be empty if still loading data)
        SyncCurrentProfileToPhoton();

        UpdateWaitingStatus();
        CheckIfRoomFull();
    }

    private bool hasSyncedProfile = false;

    /// <summary>
    /// Synchronizes the local player's Name, Avatar Index, and Custom Photo to Photon.
    /// This is called when joining a room AND periodically until successful (to handle slow loading).
    /// </summary>
    public void SyncCurrentProfileToPhoton(PlayerData info = null)
    {
        // Ensure PlayFab data is actually present before trying to sync
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            var data = PlayFabManager.Instance.currentPlayerData;
            
            // Critical: Don't sync if playerName is empty
            if (string.IsNullOrEmpty(data.playerName)) return;

            // Always update NickName immediately (this works even before joining a room)
            if (PhotonNetwork.NickName != data.playerName)
            {
                PhotonNetwork.NickName = data.playerName;
                Debug.Log($"[LobbyNetworkManager] Updated Local NickName to: {data.playerName}");
            }
            
            // Stop here if we aren't in a room yet - custom properties require a room
            if (!PhotonNetwork.InRoom) return;

            ExitGames.Client.Photon.Hashtable profileProps = new ExitGames.Client.Photon.Hashtable();
            profileProps.Add(PLAYER_NAME_KEY, data.playerName);
            profileProps.Add(PLAYER_AVATAR_KEY, data.selectedAvatarIndex);
            
            // Sync custom photo from PlayFab data if present (scene-independent)
            if (!string.IsNullOrEmpty(data.imageUrl) && data.imageUrl.StartsWith("data:image"))
            {
                try
                {
                    // Extract base64 part
                    string base64Data = data.imageUrl.Substring(data.imageUrl.IndexOf(",") + 1);
                    byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                    
                    // Decode to check validity and then re-compress/resize for network
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(imageBytes))
                    {
                        byte[] compressedImage = CompressAvatarTexture(tex);
                        if (compressedImage != null)
                        {
                            profileProps.Add(PLAYER_CUSTOM_IMAGE_KEY, compressedImage);
                            Debug.Log($"[LobbyNetworkManager] Syncing custom photo from PlayFab data ({compressedImage.Length} bytes) for {data.playerName}");
                        }
                        Destroy(tex);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LobbyNetworkManager] Failed to process custom image for sync: {e.Message}");
                }
            }
            else
            {
                // Clear stale custom image if switched back to preset
                profileProps.Add(PLAYER_CUSTOM_IMAGE_KEY, null);
            }

            PhotonNetwork.LocalPlayer.SetCustomProperties(profileProps);
            
            // Only consider it "fully synced" if the name is not empty
            // We allow re-syncing if the name was "Guest" but now it's something else
            if (!string.IsNullOrEmpty(data.playerName) && data.playerName != "Guest") {
                hasSyncedProfile = true;
            }
            
            Debug.Log($"[LobbyNetworkManager] Successfully synced profile properties to Photon.");
        }
    }

    public const string PLAYER_SCORE_KEY = "Score";
    public const string PLAYER_NAME_KEY = "PlayerName";
    public const string PLAYER_AVATAR_KEY = "AvatarIndex";
    public const string PLAYER_CUSTOM_IMAGE_KEY = "CustomImage";
    public const string GAME_OVER_KEY = "GameOver";

    /// <summary>
    /// Signals to the room that the game is over.
    /// Used when someone finishes their board or the timer expires.
    /// </summary>
    public void MarkGameAsFinished()
    {
        if (PhotonNetwork.InRoom)
        {
            // Lock the room so nobody can re-join this finished game session
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add(GAME_OVER_KEY, true);
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log("[LobbyNetworkManager] Room marked as GAME OVER and LOCKED from new joins.");
        }
    }

    /// <summary>
    /// Resizes and compresses texture to byte array for network sync
    /// </summary>
    private byte[] CompressAvatarTexture(Texture2D source)
    {
        try
        {
            // Reduced resolution to 160x160 to ensure we stay under Photon's property size limits on mobile
            // while still maintaining enough clarity for the winner podium.
            int size = 160; 
            RenderTexture rt = RenderTexture.GetTemporary(size, size);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D readableText = new Texture2D(size, size);
            readableText.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            readableText.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            
            // JPG Quality 75 provides a great balance of size (usually 5-10KB) and clarity.
            // High quality (90) was too large for reliable network property syncing on devices.
            byte[] bytes = readableText.EncodeToJPG(75);
            Destroy(readableText);
            return bytes;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LobbyNetworkManager] Failed to compress avatar: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates the local player's score in Photon custom properties.
    /// This allows other players to see your progress.
    /// </summary>
    public void UpdateLocalPlayerScore(int score)
    {
        if (PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add(PLAYER_SCORE_KEY, score);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            // If data is ready but hasn't been synced to this room yet, do it now
            if (!hasSyncedProfile)
            {
                SyncCurrentProfileToPhoton();
            }

            if (!isStarting)
            {
                UpdateWaitingStatus();
                CheckMatchmakingTimer();
            }
        }
    }

    private void CheckMatchmakingTimer()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(START_TIME_KEY, out object startTimeObj))
        {
            double startTime = (double)startTimeObj;
            double elapsedTime = PhotonNetwork.Time - startTime;
            double remainingTime = MATCHMAKING_TIMEOUT - elapsedTime;

            if (remainingTime <= 0)
            {
                Debug.Log("[LobbyNetworkManager] Matchmaking timeout reached! Starting with current players.");
                StartMultiplayerGame();
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[LobbyNetworkManager] Player entered: {newPlayer.NickName}");
        UpdateWaitingStatus();
        CheckIfRoomFull();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[LobbyNetworkManager] Player left: {otherPlayer.NickName}");
        UpdateWaitingStatus();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(START_TIME_KEY))
        {
            UpdateWaitingStatus();
        }

        if (propertiesThatChanged.ContainsKey(GAME_OVER_KEY))
        {
            bool isGameOver = (bool)propertiesThatChanged[GAME_OVER_KEY];
            if (isGameOver)
            {
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    gm.OnGameFinishedByOtherPlayer();
                }
            }
        }
    }

    private void UpdateWaitingStatus()
    {
        if (PhotonNetwork.InRoom && statusText != null)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            
            string timerPrefix = "";
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(START_TIME_KEY, out object startTimeObj))
            {
                double startTime = (double)startTimeObj;
                double remainingTime = Mathf.Max(0, (float)(MATCHMAKING_TIMEOUT - (PhotonNetwork.Time - startTime)));
                timerPrefix = $"Starting in {Mathf.CeilToInt((float)remainingTime)}s...\n";
            }

            statusText.text = $"{timerPrefix}Players: {current}/{max}";
        }
    }

    private void CheckIfRoomFull()
    {
        if (PhotonNetwork.InRoom && !isStarting && PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            Debug.Log("[LobbyNetworkManager] Room is full! Starting game immediately...");
            StartMultiplayerGame();
        }
    }

    private void StartMultiplayerGame()
    {
        if (isStarting) return;
        isStarting = true;

        if (waitingPanel != null) waitingPanel.SetActive(false);
        
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null && currentSelectedConfig != null)
        {
            // Determine how many CPUs we need to fill the remaining slots
            int humanPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int totalSlots = currentSelectedConfig.numberOfPlayers;
            int neededCPUs = Mathf.Max(0, totalSlots - humanPlayers);

            Debug.Log($"[LobbyNetworkManager] Starting game with {humanPlayers} humans and {neededCPUs} CPUs");
            
            // Pass the number of CPUs needed to the LobbyManager
            lobbyManager.StartGameFromNetwork(currentSelectedConfig.lobbyIndex, neededCPUs);
        }
    }

    private void OnCancelWaiting()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        if (waitingPanel != null) waitingPanel.SetActive(false);
    }

    public override void OnLeftRoom()
    {
        if (waitingPanel != null) waitingPanel.SetActive(false);
    }
}
