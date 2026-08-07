using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls an individual player slot UI in the Waiting For Players Panel.
/// Supports both Ready State (joined player) and Waiting State (empty slot).
/// Attach this script to your Player Slot Prefab and/or Waiting Slot Prefab.
/// </summary>
public class WaitingPlayerSlotUI : MonoBehaviour
{
    [Header("Player Details")]
    [Tooltip("Image component to display player avatar")]
    [SerializeField] private Image avatarImage;

    [Tooltip("Text component to display player name")]
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Status Badges / Indicators")]
    [Tooltip("GameObject container for Ready badge (e.g. green button/tag with '✓ Ready')")]
    [SerializeField] private GameObject readyBadge;

    [Tooltip("GameObject container for Waiting badge (e.g. blue tag with 'Waiting...')")]
    [SerializeField] private GameObject waitingBadge;

    [Tooltip("Text component inside status badge for '✓ Ready' or 'Waiting...' status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Default Assets")]
    [Tooltip("Default sprite to display when slot is waiting or avatar is missing")]
    [SerializeField] private Sprite defaultAvatarSprite;

    /// <summary>
    /// Configures this slot as a Joined & Ready player.
    /// </summary>
    /// <param name="playerName">Display name of the player (e.g., 'Player 123')</param>
    /// <param name="avatarSprite">Optional profile picture sprite</param>
    public void SetReadyPlayer(string playerName, Sprite avatarSprite = null)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
            playerNameText.gameObject.SetActive(true);
        }

        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(true);
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
            else if (defaultAvatarSprite != null)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
        }

        if (readyBadge != null)
        {
            readyBadge.SetActive(true);
        }

        if (waitingBadge != null)
        {
            waitingBadge.SetActive(false);
        }

        if (statusText != null)
        {
            statusText.text = "✓ Ready";
        }
    }

    /// <summary>
    /// Configures this slot as an Empty / Waiting slot.
    /// </summary>
    /// <param name="waitingMessage">Text to display (e.g., 'Waiting...')</param>
    public void SetWaitingSlot(string waitingMessage = "Waiting...")
    {
        if (playerNameText != null)
        {
            playerNameText.text = waitingMessage;
        }

        if (avatarImage != null)
        {
            if (defaultAvatarSprite != null)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
        }

        if (readyBadge != null)
        {
            readyBadge.SetActive(false);
        }

        if (waitingBadge != null)
        {
            waitingBadge.SetActive(true);
        }

        if (statusText != null)
        {
            statusText.text = waitingMessage;
        }
    }
}
