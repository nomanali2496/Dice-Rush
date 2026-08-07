using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the unified "Mighty Tasks" popup panel.
/// Manages tab switching between the DAILY (7-Day Login Rewards) tab and the WEEKLY (Tasks) tab,
/// handles tab active/inactive visuals, sub-panel visibility, close button, and reset countdown timer.
/// </summary>
public class MightyTasksPanelManager : MonoBehaviour
{
    public enum TaskTab
    {
        Daily,
        Weekly
    }

    [Header("Panel Root & Navigation")]
    [Tooltip("Main popup panel root GameObject (defaults to this GameObject if unassigned)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Tab Buttons")]
    [Tooltip("Button for the DAILY tab")]
    [SerializeField] private Button dailyTabButton;

    [Tooltip("Button for the WEEKLY tab")]
    [SerializeField] private Button weeklyTabButton;

    [Header("Tab Visual Customization (Optional)")]
    [SerializeField] private Image dailyTabImage;
    [SerializeField] private Image weeklyTabImage;
    [SerializeField] private Sprite activeTabSprite;
    [SerializeField] private Sprite inactiveTabSprite;
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Sub Panels")]
    [Tooltip("Sub-panel containing the 7-Day Daily Login Rewards grid")]
    [SerializeField] private GameObject dailyContentPanel;

    [Tooltip("Sub-panel containing the Weekly Tasks list")]
    [SerializeField] private GameObject weeklyContentPanel;

    [Header("Reset Timer Display")]
    [Tooltip("Text component showing reset countdown (e.g. 'Daily Task Reset in 04:12:30')")]
    [SerializeField] private TextMeshProUGUI resetTimerText;
    [SerializeField] private string resetTimerPrefix = "Daily Task Reset in ";

    [Header("Tab Settings")]
    [SerializeField] private TaskTab defaultTab = TaskTab.Daily;
    private TaskTab currentTab = TaskTab.Daily;

    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        // Setup Tab Button listeners
        if (dailyTabButton != null)
        {
            dailyTabButton.onClick.AddListener(SelectDailyTab);
        }
        if (weeklyTabButton != null)
        {
            weeklyTabButton.onClick.AddListener(SelectWeeklyTab);
        }

        // Setup Close Button listener
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnEnable()
    {
        // Switch to default or active tab when panel opens
        SwitchTab(currentTab);

        // Start reset countdown timer coroutine
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(UpdateResetTimerRoutine());
    }

    private void OnDisable()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    /// <summary>
    /// Opens the panel and shows the specified tab (or default tab).
    /// </summary>
    public void OpenPanel(TaskTab tab = TaskTab.Daily)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        SwitchTab(tab);
    }

    /// <summary>
    /// Closes the panel popup.
    /// </summary>
    public void ClosePanel()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Callback when Daily Tab button is clicked.
    /// </summary>
    public void SelectDailyTab()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
        SwitchTab(TaskTab.Daily);
    }

    /// <summary>
    /// Callback when Weekly Tab button is clicked.
    /// </summary>
    public void SelectWeeklyTab()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
        SwitchTab(TaskTab.Weekly);
    }

    /// <summary>
    /// Switches sub-panel visibility and updates tab button visuals.
    /// </summary>
    public void SwitchTab(TaskTab tab)
    {
        currentTab = tab;

        bool isDaily = (tab == TaskTab.Daily);

        // 1. Toggle Sub Panels Visibility
        if (dailyContentPanel != null)
        {
            dailyContentPanel.SetActive(isDaily);
        }

        if (weeklyContentPanel != null)
        {
            weeklyContentPanel.SetActive(!isDaily);
        }

        // 2. Update Daily Tab Visuals
        if (dailyTabImage != null)
        {
            if (activeTabSprite != null && inactiveTabSprite != null)
            {
                dailyTabImage.sprite = isDaily ? activeTabSprite : inactiveTabSprite;
            }
            dailyTabImage.color = isDaily ? activeTabColor : inactiveTabColor;
        }

        // 3. Update Weekly Tab Visuals
        if (weeklyTabImage != null)
        {
            if (activeTabSprite != null && inactiveTabSprite != null)
            {
                weeklyTabImage.sprite = !isDaily ? activeTabSprite : inactiveTabSprite;
            }
            weeklyTabImage.color = !isDaily ? activeTabColor : inactiveTabColor;
        }

        // 4. Subtle scale feedback on active tab button
        if (dailyTabButton != null)
        {
            dailyTabButton.transform.localScale = isDaily ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);
        }
        if (weeklyTabButton != null)
        {
            weeklyTabButton.transform.localScale = !isDaily ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);
        }
    }

    /// <summary>
    /// Coroutine that continuously updates the reset countdown timer until midnight (UTC / Local).
    /// </summary>
    private IEnumerator UpdateResetTimerRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);

        while (true)
        {
            if (resetTimerText != null)
            {
                DateTime now = DateTime.UtcNow;
                DateTime tomorrow = now.Date.AddDays(1);
                TimeSpan timeRemaining = tomorrow - now;

                resetTimerText.text = $"{resetTimerPrefix}{timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }

            yield return wait;
        }
    }
}
