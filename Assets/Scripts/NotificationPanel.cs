using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A reusable, general-purpose notification panel/popup.
/// </summary>
public class NotificationPanel : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private Action onPanelClosed;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        
        // Hide by default on awake
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Displays the notification panel with a specific message and callback.
    /// </summary>
    /// <param name="message">The message to display in the text field.</param>
    /// <param name="onClose">Optional callback to execute when the close button is clicked.</param>
    public void Show(string message, Action onClose = null)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        onPanelClosed = onClose;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Closes the notification panel manually.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        onPanelClosed = null;
    }

    private void OnCloseButtonClicked()
    {
        // Cache callback and clear before execution to prevent recursion issues
        Action callback = onPanelClosed;
        Hide();
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }
}
