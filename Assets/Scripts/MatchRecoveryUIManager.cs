using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MatchRecoveryUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject recoveryPromptPanel;
    [SerializeField] private GameObject mainMenuScreen; // Monitor this to show at the right time
    [SerializeField] private GameObject loadingPanel; // Show this when rejoining
    [SerializeField] private Button btnRejoinMatch;
    [SerializeField] private Button btnCancelMatch;

    private bool isReconnecting = false;
    private bool hasCheckedForRecovery = false;

    private void OnEnable()
    {
        if (btnRejoinMatch != null) btnRejoinMatch.onClick.AddListener(OnRejoinClicked);
        if (btnCancelMatch != null) btnCancelMatch.onClick.AddListener(OnCancelRecoveryClicked);
        if (recoveryPromptPanel != null) recoveryPromptPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);

        // Listen for when player data finishes loading
        PlayerModel.OnDataUpdated += CheckForActiveMatch;
        
        // Listen for match found to hide loading panel
        MatchmakingModel.OnMatchFound += HandleMatchFound;
        
        // Start monitoring for Main Menu activation
        StartCoroutine(MonitorMainMenuActivation());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (btnRejoinMatch != null) btnRejoinMatch.onClick.RemoveListener(OnRejoinClicked);
        if (btnCancelMatch != null) btnCancelMatch.onClick.RemoveListener(OnCancelRecoveryClicked);
        PlayerModel.OnDataUpdated -= CheckForActiveMatch;
        MatchmakingModel.OnMatchFound -= HandleMatchFound;
    }

    private void HandleMatchFound(StartGameData data)
    {
        // Hide loading panel once the game starts
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Coroutine that waits for the Main Menu to be active before checking for match recovery.
    /// This prevents the prompt from appearing on splash or loading screens.
    /// </summary>
    private IEnumerator MonitorMainMenuActivation()
    {
        while (!hasCheckedForRecovery)
        {
            if (mainMenuScreen != null && mainMenuScreen.activeSelf)
            {
                // Only check if we haven't already shown it and data is likely loaded
                if (!string.IsNullOrEmpty(PlayerModel.ActiveMatchId))
                {
                    CheckForActiveMatch();
                    hasCheckedForRecovery = true;
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void CheckForActiveMatch()
    {
        if (isReconnecting) return;
        
        // Safeguard: Ensure Main Menu is active before showing the prompt
        if (mainMenuScreen != null && !mainMenuScreen.activeSelf) return;
        
        // Don't show the recovery prompt if we are actively playing a game!
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && gm.IsGameplayScreenActive) return;
        
        if (!string.IsNullOrEmpty(PlayerModel.ActiveMatchId) && !string.IsNullOrEmpty(PlayerModel.ActiveRoomId))
        {
            Debug.Log($"[MatchRecoveryUI] Active match found locally: {PlayerModel.ActiveMatchId}. Showing Rejoin Prompt.");
            if (recoveryPromptPanel != null)
            {
                recoveryPromptPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[MatchRecoveryUI] recoveryPromptPanel is not assigned! Defaulting to auto-rejoin.");
                OnRejoinClicked();
            }
        }
    }

    private void OnRejoinClicked()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();
        
        isReconnecting = true; // Lock the popup from reappearing
        
        if (recoveryPromptPanel != null) recoveryPromptPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);
        
        // Emit reconnectMatch to tell the server we want the state.
        // The server will reply with 'matchRecovered', which MatchmakingModel catches to start the game.
        MatchmakingModel.ReconnectMatch(PlayerModel.ActiveMatchId, PlayerModel.ActiveRoomId);
    }

    private void OnCancelRecoveryClicked()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();
        
        if (recoveryPromptPanel != null) recoveryPromptPanel.SetActive(false);
        
        if (SocketManager.Instance?.Socket != null)
        {
            Debug.Log($"[MatchRecoveryUI] Player canceled recovery. Emitting forfeitMatch.");
            SocketManager.Instance.Socket.Emit("forfeitMatch", new ForfeitMatchPayload { roomId = PlayerModel.ActiveRoomId });
        }
        
        // Clear local active match IDs so it stops prompting
        PlayerModel.UpdateData(
            PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
            PlayerModel.Wins, PlayerModel.Losses, PlayerModel.Balance,
            PlayerModel.CashBalance, PlayerModel.BonusBalance, PlayerModel.WinningBalance,
            PlayerModel.Coins, PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level, PlayerModel.Role,
            PlayerModel.SelectedAvatarIndex, null, null
        );
    }
}
