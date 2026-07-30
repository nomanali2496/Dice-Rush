using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the Select Player Panel (Dice Blitz / Dice Rush UI)
/// Manages player count selection (3, 5, 8, 10 players), panel visibility, and back navigation.
/// </summary>
public class SelectPlayerPanelController : MonoBehaviour
{
    [Header("Main Panel GameObject")]
    [Tooltip("The main container panel GameObject to show or hide")]
    [SerializeField] private GameObject selectPlayerPanel;

    [Header("Navigation")]
    [Tooltip("Back button to close/hide this panel")]
    [SerializeField] private Button backButton;

    [Header("Player Select Buttons")]
    [SerializeField] private Button button3Players;
    [SerializeField] private Button button5Players;
    [SerializeField] private Button button8Players;
    [SerializeField] private Button button10Players;

    [Header("Matchmaking Settings")]
    [Tooltip("Lobby ID to join when a mode is selected")]
    [SerializeField] private string lobbyId = "combo-dice";
    [Tooltip("Default entry stake amount")]
    [SerializeField] private int defaultStake = 100;
    [Tooltip("Optional reference to the waiting/matchmaking panel to trigger after selection")]
    [SerializeField] private GameObject waitingPanel;

    private void Awake()
    {
        if (selectPlayerPanel == null)
        {
            selectPlayerPanel = gameObject;
        }

        // Setup button listeners
        if (button3Players != null) 
            button3Players.onClick.AddListener(() => SelectPlayerCount(3));

        if (button5Players != null) 
            button5Players.onClick.AddListener(() => SelectPlayerCount(5));

        if (button8Players != null) 
            button8Players.onClick.AddListener(() => SelectPlayerCount(8));

        if (button10Players != null) 
            button10Players.onClick.AddListener(() => SelectPlayerCount(10));

        if (backButton != null) 
            backButton.onClick.AddListener(OnBackButtonClicked);
    }

    /// <summary>
    /// Called when player clicks any player count button (3, 5, 8, 10)
    /// </summary>
    /// <param name="playerCount">Number of players (e.g. 3, 5, 8, 10)</param>
    public void SelectPlayerCount(int playerCount)
    {
        Debug.Log($"<color=#00FF00>🎯 [SelectPlayerPanel] Player selected mode: {playerCount} Players</color>");

        // Play tap audio feedback
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        // Hide select player panel
        HidePanel();

        // Show waiting/searching panel if assigned
        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);
        }

        // Initiate Matchmaking request via Socket
        string mode = $"{playerCount}-players";
        MatchmakingModel.FindMatch(mode, defaultStake, lobbyId);
    }

    /// <summary>
    /// Open / Show the Select Player Panel
    /// </summary>
    public void ShowPanel()
    {
        if (selectPlayerPanel != null)
        {
            selectPlayerPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Close / Hide the Select Player Panel
    /// </summary>
    public void HidePanel()
    {
        if (selectPlayerPanel != null)
        {
            selectPlayerPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Back button click handler
    /// </summary>
    private void OnBackButtonClicked()
    {
        Debug.Log("🔙 [SelectPlayerPanel] Back button pressed");

        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        HidePanel();
    }
}
