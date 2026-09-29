using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A centralized helper to automatically apply profile avatars and dynamic frames
/// across the entire game UI without needing to manually add frame Image components to every prefab.
/// </summary>
public static class UIProfileHelper
{
    private const string FRAME_OVERLAY_NAME = "ProfileFrameOverlay_Auto";

    /// <summary>
    /// Applies a frame over an existing avatar image by dynamically creating a child Image.
    /// Uses PlayerModel/PlayerPrefs to get the current frame if frameIndex is -1.
    /// </summary>
    public static void ApplyFrameToImage(Image avatarImage, int frameIndex = -1)
    {
        if (avatarImage == null) return;

        // If no specific index provided, fetch the current local player's frame
        if (frameIndex == -1)
        {
            frameIndex = int.TryParse(PlayerModel.ProfileFrame, out int parsedFrame)
                ? parsedFrame
                : PlayerPrefs.GetInt("SelectedProfileFrameIndex", 0);
        }

        // Find or create the overlay Image
        Transform frameOverlay = avatarImage.transform.Find(FRAME_OVERLAY_NAME);
        Image frameImage = null;

        if (frameOverlay == null)
        {
            // Dynamically create the frame object
            GameObject go = new GameObject(FRAME_OVERLAY_NAME, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(avatarImage.transform, false);
            
            RectTransform rt = go.GetComponent<RectTransform>();
            // Make it stretch fully to match the avatar image size exactly
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            
            // Expand slightly if frames need to be larger than the avatar bounds. 
            // Most frames are designed to overlap perfectly, so slightly scaling up looks natural.
            rt.offsetMin = new Vector2(-15f, -15f);
            rt.offsetMax = new Vector2(15f, 15f);
            
            frameImage = go.GetComponent<Image>();
            frameImage.raycastTarget = false; // Don't block clicks
            frameImage.preserveAspect = true;
        }
        else
        {
            frameImage = frameOverlay.GetComponent<Image>();
        }

        // Apply the sprite from AvatarSelector
        if (AvatarSelector.Instance != null && frameIndex >= 0)
        {
            Sprite frameSprite = AvatarSelector.Instance.GetBackgroundSpriteByIndex(frameIndex);
            if (frameSprite != null)
            {
                frameImage.sprite = frameSprite;
                frameImage.gameObject.SetActive(true);
                return;
            }
        }

        // Hide if no frame found or index invalid
        if (frameImage != null)
        {
            frameImage.gameObject.SetActive(false);
        }
    }
}
