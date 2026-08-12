using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Winner Screen UI.
/// Displays dynamic player ranking list (up to 10 players) when a match completes,
/// and handles navigation back to lobby or sharing.
/// </summary>
public class WinnerScreen : MonoBehaviour
{
    [Header("Dynamic Player Ranking List (Up to 10 Players)")]
    [Tooltip("Parent Content transform inside ScrollView where player rows are spawned")]
    [SerializeField] private Transform playerRowsContainer;

    [Tooltip("Prefab for PlayerRow (containing bg, Rank Image, Avatar, Name Text, Coin Icon, Score Text)")]
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Action Buttons")]
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button shareButton;

    private GameManager gameManager;
    private List<GameObject> spawnedRowObjects = new List<GameObject>();

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (lobbyButton != null)
        {
            lobbyButton.onClick.AddListener(OnLobbyButtonClicked);
        }
        if (shareButton != null)
        {
            shareButton.onClick.AddListener(OnShareButtonClicked);
        }
    }

    private void OnEnable()
    {
        HideDiceIfPresent();
    }

    /// <summary>
    /// Ensures 3D gameplay dice are hidden when Winner screen is active.
    /// </summary>
    public void HideDiceIfPresent()
    {
        DicesController dicesController = FindFirstObjectByType<DicesController>();
        if (dicesController != null)
        {
            dicesController.HideDices();
        }

        DiceVisibilityManager dvm = FindFirstObjectByType<DiceVisibilityManager>();
        if (dvm != null)
        {
            dvm.ForceHideDices();
        }
    }

    /// <summary>
    /// Sets up the winner results for up to 10 players dynamically using data from the server.
    /// Called by GameManager when match is over.
    /// </summary>
    public void SetupWinnerResults(List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)> rankings, GameOverData serverData)
    {
        if (rankings == null || rankings.Count == 0) return;

        ClearSpawnedRows();

        for (int i = 0; i < rankings.Count && i < 10; i++)
        {
            var r = rankings[i];
            int place = i + 1;

            // Prize data — server is authoritative for both currencies.
            float cash = 0f;
            int chips = 0;

            if (serverData?.earnings != null && serverData.earnings.TryGetValue(r.userId, out double earning))
            {
                cash = (float)earning;
            }
            if (serverData?.coinsEarnings != null && serverData.coinsEarnings.TryGetValue(r.userId, out double coinEarning))
            {
                chips = (int)coinEarning;
            }

            // Name and Avatar
            string displayName = (r.playerType == PlayerType.Player) ? "YOU" : "Opponent";
            Sprite avatarSprite = null;

            if (serverData != null && serverData.players != null && serverData.players.TryGetValue(r.userId, out PlayerAvatarInfo info))
            {
                if (r.playerType != PlayerType.Player)
                {
                    displayName = info.name;
                }
            }

            // Instantiate Dynamic PlayerRow inside playerRowsContainer (up to 10 players)
            if (playerRowsContainer != null && playerRowPrefab != null)
            {
                GameObject rowObj = Instantiate(playerRowPrefab, playerRowsContainer);
                spawnedRowObjects.Add(rowObj);

                WinnerPlayerRowUI rowUI = rowObj.GetComponent<WinnerPlayerRowUI>();
                if (rowUI != null)
                {
                    rowUI.SetupRow(place, displayName, avatarSprite, chips, cash, r.count);
                }
            }
        }
    }

    /// <summary>
    /// Resets the Winner Screen UI by clearing any spawned player rows.
    /// Called by GameManager when resetting state for next game.
    /// </summary>
    public void ResetUI()
    {
        ClearSpawnedRows();
    }

    /// <summary>
    /// Compatibility helper method called by GameManager.
    /// </summary>
    public void PlayAnimationIfPlayerInTop3(List<(PlayerType playerType, int opponentIndex, int count, double time, string userId)> rankings, LobbyConfig config)
    {
        // New UI directly displays results in the ranking list.
    }

    private void ClearSpawnedRows()
    {
        foreach (var row in spawnedRowObjects)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }
        spawnedRowObjects.Clear();
    }

    private void OnLobbyButtonClicked()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.OnWinnerLobbyButtonClicked();
        }
    }

    private void OnShareButtonClicked()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        Debug.Log("[WinnerScreen] Share button clicked.");
    }
}
