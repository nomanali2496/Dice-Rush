using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls a single row in the "My Tickets" list inside HelpAndSupportManager.
///
/// Unity Inspector Setup:
///   - Attach this to the TicketRow prefab.
///   - Wire all [SerializeField] references for the labels/panels.
///   - Optionally wire the expand button to toggle the details panel.
/// </summary>
public class TicketRowUI : MonoBehaviour
{
    [Header("Summary Row")]
    [SerializeField] private TextMeshProUGUI subjectText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Image           statusBadge;        // Optional: change colour by status

    [Header("Status Badge Colours")]
    [SerializeField] private Color openColour        = new Color(0.18f, 0.56f, 1f);     // blue
    [SerializeField] private Color inProgressColour  = new Color(1f,   0.65f, 0f);      // orange
    [SerializeField] private Color resolvedColour    = new Color(0.18f, 0.75f, 0.35f);  // green
    [SerializeField] private Color closedColour      = new Color(0.5f,  0.5f,  0.5f);   // grey

    [Header("Expand / Collapse")]
    [SerializeField] private Button    expandButton;    // Tapping the row header toggles details
    [SerializeField] private GameObject detailsPanel;   // Expanded area with description + replies

    [Header("Details")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Transform       repliesContainer;   // Parent for reply rows
    [SerializeField] private GameObject      replyRowPrefab;     // Simple prefab: avatar icon + text
    [SerializeField] private TextMeshProUGUI noRepliesText;

    // ─── State ───────────────────────────────────────────────────────────────────
    private bool _isExpanded = false;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (expandButton != null)
            expandButton.onClick.AddListener(ToggleDetails);

        if (detailsPanel != null)
            detailsPanel.SetActive(false);
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates this row with data received from the backend.
    /// Called by HelpAndSupportManager after instantiating the prefab.
    /// </summary>
    public void Populate(BackendAuthService.SupportTicket ticket)
    {
        if (ticket == null) return;

        // Subject
        if (subjectText != null)
            subjectText.text = ticket.subject;

        // Category — capitalise first letter for display
        if (categoryText != null)
            categoryText.text = CapitaliseFirst(ticket.category);

        // Status
        string displayStatus = FormatStatus(ticket.status);
        if (statusText != null)
            statusText.text = displayStatus;

        // Status badge colour
        if (statusBadge != null)
            statusBadge.color = GetStatusColour(ticket.status);

        // Date — show only the date part (YYYY-MM-DD) from the ISO string
        if (dateText != null)
            dateText.text = FormatDate(ticket.createdAt);

        // Description
        if (descriptionText != null)
            descriptionText.text = ticket.description;

        // Replies
        BuildReplies(ticket.replies);
    }

    // ─── Toggle ──────────────────────────────────────────────────────────────────

    private void ToggleDetails()
    {
        _isExpanded = !_isExpanded;
        if (detailsPanel != null)
            detailsPanel.SetActive(_isExpanded);
    }

    // ─── Replies ─────────────────────────────────────────────────────────────────

    private void BuildReplies(BackendAuthService.SupportTicketReply[] replies)
    {
        if (repliesContainer == null) return;

        // Clear old reply rows
        foreach (Transform child in repliesContainer)
            Destroy(child.gameObject);

        bool hasReplies = replies != null && replies.Length > 0;

        if (noRepliesText != null)
            noRepliesText.gameObject.SetActive(!hasReplies);

        if (!hasReplies) return;

        foreach (var reply in replies)
        {
            if (replyRowPrefab != null)
            {
                GameObject row = Instantiate(replyRowPrefab, repliesContainer);
                TicketReplyRowUI replyRowUI = row.GetComponent<TicketReplyRowUI>();
                if (replyRowUI != null)
                    replyRowUI.Populate(reply);
            }
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private Color GetStatusColour(string status)
    {
        switch (status)
        {
            case "open":        return openColour;
            case "in_progress": return inProgressColour;
            case "resolved":    return resolvedColour;
            case "closed":      return closedColour;
            default:            return closedColour;
        }
    }

    private string FormatStatus(string status)
    {
        switch (status)
        {
            case "open":        return "Open";
            case "in_progress": return "In Progress";
            case "resolved":    return "Resolved";
            case "closed":      return "Closed";
            default:            return CapitaliseFirst(status);
        }
    }

    /// <summary>Trims the ISO 8601 timestamp to YYYY-MM-DD for display.</summary>
    private string FormatDate(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "";
        int tIndex = iso.IndexOf('T');
        return tIndex > 0 ? iso.Substring(0, tIndex) : iso;
    }

    private string CapitaliseFirst(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
