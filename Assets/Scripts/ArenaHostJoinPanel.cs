using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles the UI flows for Hosting and Joining a Private Arena.
/// Attach this script to a dedicated UI Panel for Arenas.
/// </summary>
public class ArenaHostJoinPanel : MonoBehaviour
{
    public static ArenaHostJoinPanel Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject mainActionPanel; // Shows Host/Join options
    public GameObject hostPanel;       // Shows the generated code & waiting state
    public GameObject joinPanel;       // Shows input field to enter code

    [Header("Host UI")]
    public TextMeshProUGUI generatedCodeText;
    public Button copyCodeButton;
    public Button startArenaButton; // Bypasses wait
    public Button cancelHostButton;

    [Header("Join UI")]
    public TMP_InputField joinCodeInput;
    public Button submitJoinButton;
    public Button cancelJoinButton;
    public TextMeshProUGUI joinErrorText;

    private string currentArenaCode = "";
    private string currentLobbyId = "";
    private float currentStake = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Wire up Host buttons
        if (copyCodeButton) copyCodeButton.onClick.AddListener(OnCopyCodeClicked);
        if (startArenaButton) startArenaButton.onClick.AddListener(OnStartArenaClicked);
        if (cancelHostButton) cancelHostButton.onClick.AddListener(OnCancelHostClicked);

        // Wire up Join buttons
        if (submitJoinButton) submitJoinButton.onClick.AddListener(OnSubmitJoinClicked);
        if (cancelJoinButton) cancelJoinButton.onClick.AddListener(OnCancelJoinClicked);

        ShowMainActionPanel();
    }

    private void OnEnable()
    {
        MatchmakingModel.OnArenaHostedEvent += OnArenaHosted;
        MatchmakingModel.OnPrivateArenaCreatedEvent += OnPrivateArenaCreated;
        MatchmakingModel.OnMatchmakingFailed += OnMatchmakingFailed;
        MatchmakingModel.OnMatchFound += OnMatchFound;
    }

    private void OnDisable()
    {
        MatchmakingModel.OnArenaHostedEvent -= OnArenaHosted;
        MatchmakingModel.OnPrivateArenaCreatedEvent -= OnPrivateArenaCreated;
        MatchmakingModel.OnMatchmakingFailed -= OnMatchmakingFailed;
        MatchmakingModel.OnMatchFound -= OnMatchFound;
    }

    public void ShowMainActionPanel()
    {
        if (mainActionPanel) mainActionPanel.SetActive(true);
        if (hostPanel) hostPanel.SetActive(false);
        if (joinPanel) joinPanel.SetActive(false);
        
        currentArenaCode = "";
        if (generatedCodeText) generatedCodeText.text = "----";
        if (joinCodeInput) joinCodeInput.text = "";
        if (joinErrorText) joinErrorText.text = "";
    }

    // Called by HorizontalLobbyManager when clicking Play on an Arena preset
    public void OpenPanel(string lobbyId, float stake)
    {
        currentLobbyId = lobbyId;
        currentStake = stake;
        gameObject.SetActive(true);
        ShowMainActionPanel();
    }

    // Called by a UI Button when user selects "Host Arena"
    public void StartHostFlow()
    {
        if (mainActionPanel) mainActionPanel.SetActive(false);
        if (hostPanel) hostPanel.SetActive(true);
        
        if (generatedCodeText) generatedCodeText.text = "Generating...";
        
        string hostName = PlayerModel.Name ?? "Player";
        string tableName = $"{hostName}'s Arena";
        MatchmakingModel.HostArena(currentLobbyId, tableName, currentStake);
    }

    // Called by a UI Button when user selects "Host Private Arena"
    public void StartPrivateHostFlow()
    {
        if (mainActionPanel) mainActionPanel.SetActive(false);
        if (hostPanel) hostPanel.SetActive(true);
        
        if (generatedCodeText) generatedCodeText.text = "Generating Private Code...";
        
        MatchmakingModel.CreatePrivateArena(currentLobbyId, currentStake); // Assuming currentLobbyId maps to mode
    }

    // Called by a UI Button when user selects "Join Arena"
    public void StartJoinFlow()
    {
        if (mainActionPanel) mainActionPanel.SetActive(false);
        if (joinPanel) joinPanel.SetActive(true);
        
        if (joinCodeInput) joinCodeInput.text = "";
        if (joinErrorText) joinErrorText.text = "";
    }

    private void OnArenaHosted(ArenaHostedData data)
    {
        currentArenaCode = data.arenaCode;
        if (generatedCodeText) generatedCodeText.text = data.arenaCode;
        Debug.Log($"[ArenaHostJoinPanel] Arena Hosted successfully. Code: {data.arenaCode}");
    }

    private void OnPrivateArenaCreated(PrivateArenaCreatedData data)
    {
        currentArenaCode = data.code;
        if (generatedCodeText) generatedCodeText.text = data.code;
        Debug.Log($"[ArenaHostJoinPanel] Private Arena Created successfully. Code: {data.code}");
    }

    private void OnCopyCodeClicked()
    {
        if (!string.IsNullOrEmpty(currentArenaCode))
        {
            GUIUtility.systemCopyBuffer = currentArenaCode;
            Debug.Log("[ArenaHostJoinPanel] Copied arena code to clipboard: " + currentArenaCode);
            // Optionally show a toast here
        }
    }

    private void OnStartArenaClicked()
    {
        // For the host to force start the match before max players join
        if (!string.IsNullOrEmpty(currentArenaCode))
        {
            // We reuse StartNow which bypasses the wait
            MatchmakingModel.StartNow(currentArenaCode);
        }
    }

    private void OnCancelHostClicked()
    {
        if (!string.IsNullOrEmpty(currentArenaCode))
        {
            MatchmakingModel.CancelArena(currentArenaCode);
        }
        ShowMainActionPanel();
    }

    private void OnSubmitJoinClicked()
    {
        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(code))
        {
            if (joinErrorText) joinErrorText.text = "Please enter a valid code.";
            return;
        }

        if (joinErrorText) joinErrorText.text = "Joining...";
        MatchmakingModel.JoinArena(code);
    }

    private void OnCancelJoinClicked()
    {
        ShowMainActionPanel();
    }

    private void OnMatchmakingFailed(string error)
    {
        if (joinPanel != null && joinPanel.activeSelf && joinErrorText != null)
        {
            joinErrorText.text = error;
        }
    }

    private void OnMatchFound(StartGameData data)
    {
        // Server accepted the match, panel can be hidden
        gameObject.SetActive(false);
    }
}
