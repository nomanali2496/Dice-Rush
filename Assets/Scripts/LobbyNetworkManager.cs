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

    public override void OnJoinedRoom()
    {
        Debug.Log($"[LobbyNetworkManager] Joined Room: {PhotonNetwork.CurrentRoom.Name}");
        if (waitingPanel != null) waitingPanel.SetActive(true);
        UpdateWaitingStatus();
        CheckIfRoomFull();
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

    private void UpdateWaitingStatus()
    {
        if (PhotonNetwork.InRoom && statusText != null)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            statusText.text = $"Waiting for players... {current}/{max}";
        }
    }

    private void CheckIfRoomFull()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            Debug.Log("[LobbyNetworkManager] Room is full! Starting game...");
            StartMultiplayerGame();
        }
    }

    private void StartMultiplayerGame()
    {
        if (waitingPanel != null) waitingPanel.SetActive(false);
        
        LobbyManager lobbyManager = FindObjectOfType<LobbyManager>();
        if (lobbyManager != null && currentSelectedConfig != null)
        {
            // Call the original gameplay start logic in LobbyManager 
            // but now it's synchronized by Photon
            lobbyManager.StartGameFromNetwork(currentSelectedConfig.lobbyIndex);
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
