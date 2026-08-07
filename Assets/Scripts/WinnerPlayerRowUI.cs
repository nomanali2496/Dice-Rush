using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls an individual player row in the Winner Screen ranking list.
/// Handles 1st, 2nd, 3rd place crown icons & background bars,
/// as well as 4th-10th place entries, player avatar, name, and chip rewards (+200).
/// </summary>
public class WinnerPlayerRowUI : MonoBehaviour
{
    [Header("UI Component References")]
    [Tooltip("Row background bar image")]
    [SerializeField] private Image bgImage;

    [Tooltip("Crown / Rank icon image")]
    [SerializeField] private Image rankImage;

    [Tooltip("Player profile avatar image")]
    [SerializeField] private Image avatarImage;

    [Tooltip("Player display name text")]
    [SerializeField] private TextMeshProUGUI nameText;

    [Tooltip("Coin / Chip icon image")]
    [SerializeField] private Image coinIcon;

    [Tooltip("Reward / Score text display (e.g. '+200')")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("1st, 2nd, 3rd Rank Sprites (Assigned in Inspector)")]
    [SerializeField] private Sprite crown1st;
    [SerializeField] private Sprite crown2nd;
    [SerializeField] private Sprite crown3rd;

    [Header("1st, 2nd, 3rd Row Background Bars (Assigned in Inspector)")]
    [SerializeField] private Sprite bg1st;
    [SerializeField] private Sprite bg2nd;
    [SerializeField] private Sprite bg3rd;
    [SerializeField] private Sprite bgDefault;

    [Header("Default Avatar Fallback")]
    [SerializeField] private Sprite defaultAvatarSprite;

    /// <summary>
    /// Configures the player row for a specific rank (1 to 10).
    /// </summary>
    /// <param name="rank">Rank position (1-indexed: 1 = 1st place, 2 = 2nd place, etc.)</param>
    /// <param name="playerName">Name of the player</param>
    /// <param name="avatarSprite">Player profile picture sprite</param>
    /// <param name="chipsReward">Chips reward awarded (+200)</param>
    /// <param name="cashReward">Cash reward awarded if cash game</param>
    /// <param name="score">Score achieved in game</param>
    public void SetupRow(int rank, string playerName, Sprite avatarSprite = null, int chipsReward = 0, float cashReward = 0f, int score = 0)
    {
        // 1. Setup Player Name
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(playerName) ? $"Player {rank}" : playerName;
        }

        // 2. Setup Player Avatar
        if (avatarImage != null)
        {
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
            else if (defaultAvatarSprite != null)
            {
                avatarImage.sprite = defaultAvatarSprite;
            }
        }

        // 3. Setup Rank Crown & Row Background Bar based on Rank (1st, 2nd, 3rd, 4th+)
        switch (rank)
        {
            case 1:
                if (rankImage != null && crown1st != null)
                {
                    rankImage.sprite = crown1st;
                    rankImage.gameObject.SetActive(true);
                }
                if (bgImage != null && bg1st != null)
                {
                    bgImage.sprite = bg1st;
                }
                break;

            case 2:
                if (rankImage != null && crown2nd != null)
                {
                    rankImage.sprite = crown2nd;
                    rankImage.gameObject.SetActive(true);
                }
                if (bgImage != null && bg2nd != null)
                {
                    bgImage.sprite = bg2nd;
                }
                break;

            case 3:
                if (rankImage != null && crown3rd != null)
                {
                    rankImage.sprite = crown3rd;
                    rankImage.gameObject.SetActive(true);
                }
                if (bgImage != null && bg3rd != null)
                {
                    bgImage.sprite = bg3rd;
                }
                break;

            default:
                // 4th to 10th Place
                if (rankImage != null)
                {
                    // Hide crown icon or use default rank icon for 4th+
                    rankImage.gameObject.SetActive(false);
                }
                if (bgImage != null && bgDefault != null)
                {
                    bgImage.sprite = bgDefault;
                }
                break;
        }

        // 4. Setup Reward / Score Text
        if (scoreText != null)
        {
            if (chipsReward > 0)
            {
                scoreText.text = $"+{chipsReward}";
            }
            else if (cashReward > 0f)
            {
                scoreText.text = $"+${cashReward:F2}";
            }
            else if (score > 0)
            {
                scoreText.text = $"+{score}";
            }
            else
            {
                scoreText.text = "+0";
            }
        }
    }
}
