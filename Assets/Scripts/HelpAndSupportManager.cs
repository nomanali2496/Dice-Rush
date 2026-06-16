using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Help & Support panel.
/// Handles two sub-panels:
///   1. Submit Ticket — user fills email, category, subject, description and submits.
///   2. My Tickets   — shows a scrollable list of the user's past tickets and replies.
///
/// Unity Inspector Setup:
///   - Assign this script to a GameObject (e.g. "HelpAndSupportManager").
///   - Wire all [SerializeField] references in the Inspector.
///   - The ticket row prefab should have a TicketRowUI component attached.
/// </summary>
public class HelpAndSupportManager : MonoBehaviour
{
    // ─── Main Panel ─────────────────────────────────────────────────────────────

    [Header("Main Panel")]
    [SerializeField] private GameObject helpAndSupportPanel;

    // ─── Tab Buttons ─────────────────────────────────────────────────────────────

    [Header("Tab Buttons")]
    [SerializeField] private Button submitTicketTabButton;
    [SerializeField] private Button myTicketsTabButton;

    // ─── Submit Ticket Panel ─────────────────────────────────────────────────────

    [Header("Submit Ticket Panel")]
    [SerializeField] private GameObject submitTicketPanel;
    // [SerializeField] private TMP_InputField subjectInput; // No longer used; subject is hardcoded to "Help & Support"
    [SerializeField] private TMP_InputField descriptionInput;
    [SerializeField] private TMP_Dropdown   categoryDropdown;  // options: technical, payment, account, complaint, other
    [SerializeField] private Button         submitTicketButton;

    // ─── My Tickets Panel ────────────────────────────────────────────────────────

    [Header("My Tickets Panel")]
    [SerializeField] private GameObject    myTicketsPanel;
    [SerializeField] private Transform     ticketsListContainer; // ScrollView Content
    [SerializeField] private GameObject    ticketRowPrefab;      // Prefab with TicketRowUI
    [SerializeField] private Button        refreshTicketsButton;
    [SerializeField] private TextMeshProUGUI noTicketsText;

    // ─── Shared ──────────────────────────────────────────────────────────────────

    [Header("Shared")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject      loadingOverlay;   // optional spinner

    // ─── Category values (must match backend exactly) ────────────────────────────

    private readonly string[] _categoryValues = { "technical", "payment", "account", "complaint", "other" };

    // ─── Unity Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
        // Tab buttons
        if (submitTicketTabButton != null)
            submitTicketTabButton.onClick.AddListener(ShowSubmitTicketPanel);

        if (myTicketsTabButton != null)
            myTicketsTabButton.onClick.AddListener(OnMyTicketsTabClicked);

        // Submit
        if (submitTicketButton != null)
            submitTicketButton.onClick.AddListener(OnSubmitTicketClicked);

        // Refresh
        if (refreshTicketsButton != null)
            refreshTicketsButton.onClick.AddListener(LoadMyTickets);

        // Show submit panel by default
        ShowSubmitTicketPanel();
        HideStatus();
    }

    // ─── Panel Management ────────────────────────────────────────────────────────

    /// <summary>Opens the Help & Support panel from outside (e.g., Settings button).</summary>
    public void OpenPanel()
    {
        if (helpAndSupportPanel != null)
            helpAndSupportPanel.SetActive(true);

        ShowSubmitTicketPanel();
        HideStatus();
    }

    /// <summary>Closes the Help & Support panel.</summary>
    public void ClosePanel()
    {
        if (helpAndSupportPanel != null)
            helpAndSupportPanel.SetActive(false);
    }

    private void ShowSubmitTicketPanel()
    {
        if (submitTicketPanel != null) submitTicketPanel.SetActive(true);
        if (myTicketsPanel    != null) myTicketsPanel.SetActive(false);
        HideStatus();
    }

    private void ShowMyTicketsPanel()
    {
        if (submitTicketPanel != null) submitTicketPanel.SetActive(false);
        if (myTicketsPanel    != null) myTicketsPanel.SetActive(true);
    }

    private void OnMyTicketsTabClicked()
    {
        ShowMyTicketsPanel();
        LoadMyTickets();
    }

    // ─── Submit Ticket ───────────────────────────────────────────────────────────

    private void OnSubmitTicketClicked()
    {
        // Email is automatically taken from the logged-in user's profile
        string email       = PlayerModel.Email;
        string subject     = "Help & Support";
        string description = descriptionInput != null ? descriptionInput.text.Trim() : "";
        string category    = GetSelectedCategory();

        // Validation
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowStatus("Could not read your account email. Please try logging out and back in.");
            return;
        }
        if (string.IsNullOrEmpty(description))
        {
            ShowStatus("Please describe your issue.");
            return;
        }
        if (description.Length < 10)
        {
            ShowStatus("Description must be at least 10 characters.");
            return;
        }

        SetLoading(true);
        HideStatus();

        BackendAuthService.Instance.CreateSupportTicket(email, subject, description, category, (success, message) =>
        {
            SetLoading(false);
            ShowStatus(message);

            if (success)
            {
                // Clear form fields on success
                if (descriptionInput != null) descriptionInput.text = "";
                if (categoryDropdown != null) categoryDropdown.value = 0;
            }
        });
    }

    // ─── My Tickets ──────────────────────────────────────────────────────────────

    private void LoadMyTickets()
    {
        if (BackendAuthService.Instance == null)
        {
            ShowStatus("Service not available.");
            return;
        }

        SetLoading(true);
        HideStatus();
        ClearTicketList();

        BackendAuthService.Instance.GetMyTickets((success, message, tickets) =>
        {
            SetLoading(false);

            if (!success)
            {
                ShowStatus(message);
                if (noTicketsText != null)
                {
                    noTicketsText.text = "Could not load tickets.";
                    noTicketsText.gameObject.SetActive(true);
                }
                return;
            }

            if (tickets == null || tickets.Length == 0)
            {
                if (noTicketsText != null)
                {
                    noTicketsText.text = "You have no support tickets yet.";
                    noTicketsText.gameObject.SetActive(true);
                }
                return;
            }

            if (noTicketsText != null) noTicketsText.gameObject.SetActive(false);

            // Populate the list
            foreach (var ticket in tickets)
                SpawnTicketRow(ticket);
        });
    }

    private void SpawnTicketRow(BackendAuthService.SupportTicket ticket)
    {
        if (ticketRowPrefab == null || ticketsListContainer == null) return;

        GameObject row = Instantiate(ticketRowPrefab, ticketsListContainer);
        TicketRowUI rowUI = row.GetComponent<TicketRowUI>();
        if (rowUI != null)
            rowUI.Populate(ticket);
    }

    private void ClearTicketList()
    {
        if (ticketsListContainer == null) return;
        foreach (Transform child in ticketsListContainer)
            Destroy(child.gameObject);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private string GetSelectedCategory()
    {
        if (categoryDropdown == null) return "other";
        int index = categoryDropdown.value;
        if (index >= 0 && index < _categoryValues.Length)
            return _categoryValues[index];
        return "other";
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }

    private void ShowStatus(string message)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.gameObject.SetActive(true);
    }

    private void HideStatus()
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(false);
    }

    private void SetLoading(bool isLoading)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(isLoading);

        if (submitTicketButton != null)
            submitTicketButton.interactable = !isLoading;

        if (refreshTicketsButton != null)
            refreshTicketsButton.interactable = !isLoading;
    }
}
