using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Component attached to each Profile Row prefab in the dropdown.
    /// Manages avatar, username, score/coins, and optional rank position text.
    /// </summary>
    public class ProfileDropdownRowUI : MonoBehaviour
    {
        [Header("UI Component Bindings")]
        [Tooltip("Optional rank text component (e.g., #1, #2, 1st)")]
        [SerializeField] private TextMeshProUGUI rankText;

        [Tooltip("Image component for player avatar")]
        [SerializeField] private Image avatarImage;
        
        [Tooltip("Image component for the profile frame")]
        [SerializeField] private Image frameImage;

        [Tooltip("TextMeshProUGUI component for player name")]
        [SerializeField] private TextMeshProUGUI usernameText;

        [Tooltip("TextMeshProUGUI component for score/coins")]
        [SerializeField] private TextMeshProUGUI scoreText;

        /// <summary>
        /// Populates row UI with given profile data including optional rank
        /// </summary>
        public void SetData(string username, int score, Sprite avatar = null, int rank = 0, int frameIndex = -1)
        {
            if (rankText != null)
            {
                rankText.gameObject.SetActive(rank > 0);
                rankText.text = rank > 0 ? $"#{rank}" : "";
            }

            if (usernameText != null)
            {
                usernameText.text = username;
            }

            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0"); // Formats numbers with commas (e.g. 1,000)
            }

            if (avatarImage != null && avatar != null)
            {
                avatarImage.sprite = avatar;
            }
            
            // Handle Frame
            if (frameIndex == -1)
            {
                frameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
                    ? parsedFrame
                    : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
            }
            
            if (frameImage != null && AvatarSelector.Instance != null)
            {
                Sprite fSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex);
                if (fSprite != null)
                {
                    frameImage.sprite = fSprite;
                    frameImage.gameObject.SetActive(true);
                }
                else
                {
                    frameImage.gameObject.SetActive(false);
                }
            }
            else if (avatarImage != null && frameImage == null)
            {
                // Fallback to dynamic overlay if frameImage reference is missing but avatar is present
                UIProfileHelper.ApplyFrameToImage(avatarImage, frameIndex);
            }
        }
    }
}
