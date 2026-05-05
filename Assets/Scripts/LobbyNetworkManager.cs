using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
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
    private Coroutine pendingJoinCoroutine;
    private double lastJoinTimeLocal;

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

        // Defensive: Ensure we always have a temporary name from the start
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Guest_" + Random.Range(1000, 9999);
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
        currentSelectedConfig = config;

        // If not connected yet, show waiting panel and wait for connection
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[LobbyNetworkManager] Not connected to Photon yet! Waiting for connection...");
            
            if (waitingPanel != null) waitingPanel.SetActive(true);
            if (statusText != null) statusText.text = "Connecting to Server...";
            
            if (pendingJoinCoroutine != null) StopCoroutine(pendingJoinCoroutine);
            pendingJoinCoroutine = StartCoroutine(WaitAndJoinRoom(config));
            return;
        }

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

    /// <summary>
    /// Coroutine that waits for connection before joined the requested lobby.
    /// </summary>
    private System.Collections.IEnumerator WaitAndJoinRoom(LobbyConfig config)
    {
        while (!PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;
        }

        Debug.Log("[LobbyNetworkManager] Connection established! Proceeding to join room.");
        JoinLobbyRoom(config);
        pendingJoinCoroutine = null;
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
        lastJoinTimeLocal = Time.timeAsDouble; // Store local join time to prevent ghost timers

        // NOTE: Matchmaking countdown removed - game only starts when room is full of real human players.

        // CRITICAL: Immediately sync PlayFabId as a tiny standalone property the moment we enter
        // the room. This runs BEFORE the full profile sync so that other players (and the Master
        // Client building the Participants list) always see our PlayFabId even if our full profile
        // data is still loading from PlayFab.
        SyncPlayFabIdToPhoton();

        // Try to sync full profile (might be empty if still loading data)
        SyncCurrentProfileToPhoton();

        // RECONNECT SCORE SYNC: If a game is already running when we (re)join this room,
        // immediately push our current local score to Photon so other players see the real
        // value and not the stale pre-disconnect score. This prevents wrong winner decisions.
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.ReSyncScoreToPhoton();

        UpdateWaitingStatus();
        CheckIfRoomFull();
    }

    /// <summary>
    /// Sends only the PlayFabId to Photon immediately. This is a tiny packet that is guaranteed
    /// to arrive well before any larger profile sync. Called straight from OnJoinedRoom.
    /// </summary>
    private void SyncPlayFabIdToPhoton()
    {
        if (!PhotonNetwork.InRoom) return;
        if (PlayFabManager.Instance == null || PlayFabManager.Instance.currentPlayerData == null) return;

        string pfId = PlayFabManager.Instance.currentPlayerData.playFabId;
        if (string.IsNullOrEmpty(pfId)) return;

        ExitGames.Client.Photon.Hashtable idProp = new ExitGames.Client.Photon.Hashtable();
        idProp.Add(PLAYER_ID_KEY, pfId);
        PhotonNetwork.LocalPlayer.SetCustomProperties(idProp);
        Debug.Log($"[LobbyNetworkManager] ✅ PlayFabId sent immediately on room join: {pfId}");
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

            // 1. Update NickName immediately (this works even before joining a room)
            if (PhotonNetwork.NickName != data.playerName)
            {
                PhotonNetwork.NickName = data.playerName;
                Debug.Log($"[LobbyNetworkManager] Updated Local NickName to: {data.playerName}");
            }
            
            // Stop here if we aren't in a room yet - custom properties require a room
            if (!PhotonNetwork.InRoom) return;

            // 2. SYNC NAME AND AVATAR INDEX (Tiny, instant packet — priority 1)
            ExitGames.Client.Photon.Hashtable basicProps = new ExitGames.Client.Photon.Hashtable();
            basicProps.Add(PLAYER_NAME_KEY, data.playerName);
            basicProps.Add(PLAYER_AVATAR_KEY, data.selectedAvatarIndex);
            basicProps.Add(PLAYER_ID_KEY, data.playFabId);
            
            // Send the basic info first so it's guaranteed to reach others even on slow networks
            PhotonNetwork.LocalPlayer.SetCustomProperties(basicProps);
            Debug.Log($"[LobbyNetworkManager] ✅ Priority 1 (Name/Index) sent to Photon for {data.playerName}");
            
            // 3. SYNC CUSTOM IMAGE (Larger packet — priority 2)
            // We send this separately to avoid the "All or Nothing" failure on mobile devices
            if (!string.IsNullOrEmpty(data.imageUrl) && data.imageUrl.StartsWith("data:image"))
            {
                try
                {
                    string base64Data = data.imageUrl.Substring(data.imageUrl.IndexOf(",") + 1);
                    byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                    
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(imageBytes))
                    {
                        // Use 128x128 for even better mobile reliability
                        byte[] compressedImage = CompressAvatarTexture(tex);
                        if (compressedImage != null)
                        {
                            ExitGames.Client.Photon.Hashtable imageProps = new ExitGames.Client.Photon.Hashtable();
                            imageProps.Add(PLAYER_CUSTOM_IMAGE_KEY, compressedImage);
                            PhotonNetwork.LocalPlayer.SetCustomProperties(imageProps);
                            Debug.Log($"[LobbyNetworkManager] ✅ Priority 2 (Custom Image: {compressedImage.Length} bytes) sent separately for {data.playerName}");
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
                ExitGames.Client.Photon.Hashtable clearProps = new ExitGames.Client.Photon.Hashtable();
                clearProps.Add(PLAYER_CUSTOM_IMAGE_KEY, null);
                PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);
            }

            // Only consider it "fully synced" if the name is not empty
            if (!string.IsNullOrEmpty(data.playerName) && data.playerName != "Guest") {
                hasSyncedProfile = true;
            }
        }
    }

    public const string PLAYER_SCORE_KEY = "Score";
    public const string PLAYER_SCORE_TIME_KEY = "ScoreTime";
    public const string PLAYER_ID_KEY = "PlayFabId";
    public const string PLAYER_NAME_KEY = "PlayerName";
    public const string PLAYER_AVATAR_KEY = "AvatarIndex";
    public const string PLAYER_CUSTOM_IMAGE_KEY = "CustomImage";
    public const string GAME_OVER_KEY = "GameOver";
    public const string GAME_START_TIME_KEY = "GameStartTime";
    public const string GAME_END_TIME_KEY = "GameEndTime";

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
            // Aggressive mobile optimization: 128x128 at 60% quality ensures the packet stays small
            int size = 128;
            RenderTexture rt = RenderTexture.GetTemporary(size, size);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D smallTexture = new Texture2D(size, size);
            smallTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            smallTexture.Apply();
            
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = smallTexture.EncodeToJPG(60); 
            Destroy(smallTexture);
            return bytes;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LobbyNetworkManager] Compression failed: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates the local player's score in Photon custom properties.
    /// This allows other players to see your progress.
    /// </summary>
    public void UpdateLocalPlayerScore(int score, double time = 0)
    {
        if (PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add(PLAYER_SCORE_KEY, score);
            if (time > 0)
            {
                props.Add(PLAYER_SCORE_TIME_KEY, time);
            }
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
            }
        }
    }

    // Matchmaking timer removed - game now only starts when full.
    private void CheckMatchmakingTimer() { }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string currentName = !string.IsNullOrEmpty(newPlayer.NickName) ? newPlayer.NickName : "A Player";
        Debug.Log($"[LobbyNetworkManager] Player entered: {currentName} (Actor: {newPlayer.ActorNumber})");
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

        // Handle game timer extension (triggered when nobody has selected any boxes)
        if (propertiesThatChanged.ContainsKey(GameManager.GAME_EXTENSION_KEY))
        {
            float newTimerValue = 30f; // Safe fallback
            if (propertiesThatChanged.TryGetValue("gameTimerVal", out object val))
            {
                newTimerValue = (float)val;
            }

            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.OnGameExtendedByNetwork(newTimerValue);
            }
        }
    }

    /// <summary>
    /// Important override for mobile: Detects when the player's name/profile actually arrives 
    /// from the server if it was a few frames late (the "Blank Name" fix).
    /// </summary>
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PLAYER_NAME_KEY))
        {
            string newName = (string)changedProps[PLAYER_NAME_KEY];
            Debug.Log($"[LobbyNetworkManager] Sync: Player {targetPlayer.ActorNumber} updated name to: {newName}");
            UpdateWaitingStatus();
        }
        
        // Also refresh UI when someone finishes syncing their gallery image
        if (changedProps.ContainsKey(PLAYER_CUSTOM_IMAGE_KEY))
        {
            Debug.Log($"[LobbyNetworkManager] Sync: Player {targetPlayer.ActorNumber} ({targetPlayer.NickName}) successfully synced custom photo!");
            UpdateWaitingStatus();
        }
    }

    private void UpdateWaitingStatus()
    {
        if (PhotonNetwork.InRoom && statusText != null)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            
            // No timer - showing count only
            statusText.text = $"Waiting for players...\nPlayers: {current}/{max}";
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
        
        HorizontalLobbyManager horizontalLobbyManager = FindObjectOfType<HorizontalLobbyManager>();
        
        if (horizontalLobbyManager != null && currentSelectedConfig != null)
        {
            // No CPUs functionality: only real humans.
            int humanPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int neededCPUs = 0; // Forced to zero per user request

            Debug.Log($"[LobbyNetworkManager] Starting game with {humanPlayers} humans. No CPUs allowed.");

            // Set the official game start time in room properties (only Master Client)
            // This ensures all clients start their match timers from the exact same network timestamp.
            if (PhotonNetwork.IsMasterClient)
            {
                ExitGames.Client.Photon.Hashtable startTimeProp = new ExitGames.Client.Photon.Hashtable();
                double serverTime = PhotonNetwork.Time;
                startTimeProp.Add(GAME_START_TIME_KEY, serverTime);
                
                // Calculate absolute end time: Start + Countdown + Match Duration
                // Use 1.0s countdown for short games (<=15s) and 3.0s for regular matches
                double matchDuration = currentSelectedConfig.gameTimer; 
                double countdown = (matchDuration <= 15.1f) ? 1.0 : 3.0;
                double absoluteEndTime = serverTime + countdown + matchDuration;
                startTimeProp.Add(GAME_END_TIME_KEY, absoluteEndTime);

                PhotonNetwork.CurrentRoom.SetCustomProperties(startTimeProp);
                Debug.Log($"[LobbyNetworkManager] Master Client set Start: {serverTime} | End: {absoluteEndTime}");
            }
            
            // Pass the number of CPUs needed to the LobbyManager first (start the game visually)
            horizontalLobbyManager.StartGameFromNetwork(currentSelectedConfig.lobbyIndex, null, neededCPUs);

            // NEW: Save the lobby prizes (rules) to PlayFab Match Shared Data
            // This ensures that even if you disconnect, the server knows exactly what the prizes were.
            if (PhotonNetwork.IsMasterClient)
            {
                SyncLobbyPrizesToPlayFabMatch(currentSelectedConfig);
            }

            // Delay writing the Participants list so every player has had time to push their
            // PlayFabId to Photon. Without this delay the Master Client may record NOID placeholders
            // for players whose PlayFab data was still loading, making disconnect recovery impossible.
            StartCoroutine(WriteParticipantsDelayed());
        }
    }

    /// <summary>
    /// Saves the current match's prize rules to PlayFab Match Shared Data.
    /// This allows the server (CloudScript) to know the exact payouts for 1st, 2nd, 3rd place
    /// even if the Master Client disconnects later.
    /// </summary>
    private void SyncLobbyPrizesToPlayFabMatch(LobbyConfig config)
    {
        if (config == null || PlayFabManager.Instance == null) return;

        // Create a simple JSON object string with the prize data
        // We use a flat dictionary for speed and reliability.
        var prizeRules = new Dictionary<string, string>
        {
            { "MatchId", PhotonNetwork.CurrentRoom.Name },
            { "LobbyName", config.lobbyName },
            { "1st_Cash", config.firstPlaceCash.ToString() },
            { "1st_Chips", config.firstPlaceChips.ToString() },
            { "2nd_Cash", config.secondPlaceCash.ToString() },
            { "2nd_Chips", config.secondPlaceChips.ToString() },
            { "3rd_Cash", config.thirdPlaceCash.ToString() },
            { "3rd_Chips", config.thirdPlaceChips.ToString() }
        };

        // We save this to the Master Client's PlayFab data under a shared MatchRules key.
        // We use a specific prefix so the reward recovery logic doesn't mistake it for a cash reward.
        PlayFab.PlayFabClientAPI.UpdateUserData(new PlayFab.ClientModels.UpdateUserDataRequest
        {
             Data = new Dictionary<string, string> { { "LobbyMatchRules_START", Newtonsoft.Json.JsonConvert.SerializeObject(prizeRules) } },
             Permission = PlayFab.ClientModels.UserDataPermission.Public
        }, 
        res => Debug.Log($"[LobbyNetworkManager] ✅ Official Match Setup saved to PlayFab: {config.lobbyName} | Key: LobbyMatchRules_START"),
        err => Debug.LogWarning($"[LobbyNetworkManager] ⚠️ Failed to backup match rules to PlayFab: {err.GenerateErrorReport()}")
        );
    }

    /// <summary>
    /// Writes the Participants room property after a short delay to ensure all players'
    /// PlayFabIds have propagated through Photon before the list is saved.
    /// </summary>
    private System.Collections.IEnumerator WriteParticipantsDelayed()
    {
        // Wait 1.5 seconds: enough for PlayFabId properties to arrive on the Master Client
        // from all connected players even on slow mobile connections.
        yield return new WaitForSeconds(1.5f);

        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient) yield break;

            
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();

        // Store ActorNumber:PlayFabId mapping for ALL players present at start.
        // Thanks to SyncPlayFabIdToPhoton() + the 1.5s delay, most players will have
        // their real PlayFabId here. NOID is the last-resort fallback.
        List<string> mappings = new List<string>();
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(PLAYER_ID_KEY, out object pfId) && pfId != null && !string.IsNullOrEmpty(pfId.ToString()))
            {
                mappings.Add($"{p.ActorNumber}:{pfId}");
                Debug.Log($"[LobbyNetworkManager] Participants: Actor {p.ActorNumber} ({p.NickName}) → PlayFabId {pfId}");
            }
            else
            {
                mappings.Add($"{p.ActorNumber}:NOID_{p.ActorNumber}");
                Debug.LogWarning($"[LobbyNetworkManager] Participants: Actor {p.ActorNumber} ({p.NickName}) has no PlayFabId after delay — using NOID placeholder.");
            }
        }
        props.Add("Participants", string.Join(",", mappings));
        // NOTE: Do NOT re-write GAME_START_TIME_KEY here — it was already written at game start
        // and overwriting it 1.5s later would desync the timer on all clients.

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        Debug.Log($"[LobbyNetworkManager] Participants list written after delay: {string.Join(" | ", mappings)}");
    }

    private void OnCancelWaiting()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        if (pendingJoinCoroutine != null)
        {
            StopCoroutine(pendingJoinCoroutine);
            pendingJoinCoroutine = null;
        }

        if (waitingPanel != null) waitingPanel.SetActive(false);
    }

    public override void OnLeftRoom()
    {
        if (waitingPanel != null) waitingPanel.SetActive(false);
    }
}
