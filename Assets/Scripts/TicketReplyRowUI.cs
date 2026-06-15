using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the display of a single reply inside an expanded ticket row.
///
/// Unity Inspector Setup:
///   - Attach this to the ReplyRow prefab used by TicketRowUI.
///   - Wire the labels and optionally the admin badge.
/// </summary>
public class TicketReplyRowUI : MonoBehaviour
{
    [Header("Reply UI")]
    [SerializeField] private TextMeshProUGUI senderNameText;   // "Support Admin" or user name
    [SerializeField] private TextMeshProUGUI messageText;       // The reply body
    [SerializeField] private TextMeshProUGUI timestampText;     // Reply time (date only)
    [SerializeField] private GameObject      adminBadge;        // Optional badge shown for admin replies
    [SerializeField] private Image           rowBackground;     // Optional: tint admin vs user replies

    [Header("Colours")]
    [SerializeField] private Color adminBgColour = new Color(0.18f, 0.56f, 1f, 0.12f);   // Subtle blue tint for admin
    [SerializeField] private Color userBgColour  = new Color(1f,   1f,   1f,  0.05f);    // Subtle white tint for user

    // ─── Public API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills the reply row with data from the backend response.
    /// </summary>
    public void Populate(BackendAuthService.SupportTicketReply reply)
    {
        if (reply == null) return;

        // Sender name
        if (senderNameText != null)
            senderNameText.text = reply.isAdmin ? reply.userName + " (Support)" : reply.userName;

        // Message body
        if (messageText != null)
            messageText.text = reply.message;

        // Timestamp — date only
        if (timestampText != null)
            timestampText.text = FormatDate(reply.createdAt);

        // Admin badge (only visible for admin replies)
        if (adminBadge != null)
            adminBadge.SetActive(reply.isAdmin);

        // Optional background tint to distinguish admin vs user
        if (rowBackground != null)
            rowBackground.color = reply.isAdmin ? adminBgColour : userBgColour;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private string FormatDate(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "";
        int tIndex = iso.IndexOf('T');
        return tIndex > 0 ? iso.Substring(0, tIndex) : iso;
    }
}
