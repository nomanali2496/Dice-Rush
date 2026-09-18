using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrivateTablePanel : MonoBehaviour
{
    [Header("UI Elements")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI statusText;

    public string defaultLobbyId = "arena-custom-table"; // Backend preset ID for custom arena

    private void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);
            
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);
            
        MatchmakingModel.OnArenaHostedEvent += HandleArenaHosted;
    }

    private void OnDestroy()
    {
        MatchmakingModel.OnArenaHostedEvent -= HandleArenaHosted;
    }

    private void OnHostClicked()
    {
        Debug.Log("[PrivateTablePanel] Host Button Clicked!");
        if (statusText != null)
        {
            statusText.text = "Generating code...";
        }
        
        string hostName = PlayerModel.Name ?? "Player";
        string tableName = $"{hostName}'s Arena";
        
        // Pass the default lobbyId, tableName, and null stake (for presets it will use its own fee)
        MatchmakingModel.HostArena(defaultLobbyId, tableName, null);
    }
    
    private void HandleArenaHosted(ArenaHostedData data)
    {
        Debug.Log($"[PrivateTablePanel] Arena Hosted! Code: {data.arenaCode}");
        if (statusText != null)
        {
            statusText.text = $"Hosted! Share Code: {data.arenaCode}";
        }
    }

    private void OnJoinClicked()
    {
        string code = joinCodeInput != null ? joinCodeInput.text : "";
        if (string.IsNullOrEmpty(code)) return;
        
        Debug.Log($"[PrivateTablePanel] Join Button Clicked! Joining code: {code}");
        if (statusText != null)
        {
            statusText.text = $"Joining {code}...";
        }
        
        MatchmakingModel.JoinArena(code);
    }
}
