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

    [Tooltip("Main Menu Task Button that opens this panel (optional - assign in Inspector)")]
    [SerializeField] private Button mainMenuTaskButton;

    [Header("Tab Buttons")]
    [Tooltip("Button for the DAILY tab")]
    [SerializeField] private Button dailyTabButton;

    [Tooltip("Button for the WEEKLY tab")]
    [SerializeField] private Button weeklyTabButton;

    [Header("Tab Visual Customization")]
    [SerializeField] private Image dailyTabImage;
    [SerializeField] private Image weeklyTabImage;

    [Header("Daily Tab Sprites")]
    [Tooltip("Sprite for Daily Tab when selected/active")]
    [SerializeField] private Sprite dailyActiveSprite;
    [Tooltip("Sprite for Daily Tab when unselected/inactive")]
    [SerializeField] private Sprite dailyInactiveSprite;

    [Header("Weekly Tab Sprites")]
    [Tooltip("Sprite for Weekly Tab when selected/active")]
    [SerializeField] private Sprite weeklyActiveSprite;
    [Tooltip("Sprite for Weekly Tab when unselected/inactive")]
    [SerializeField] private Sprite weeklyInactiveSprite;

    [Header("Shared Tab Sprites (Fallback)")]
    [SerializeField] private Sprite activeTabSprite;
    [SerializeField] private Sprite inactiveTabSprite;

    [Header("Visual Customization Options")]
    [Tooltip("If true, swaps tab sprites at runtime (only when sprite reference is assigned)")]
    [SerializeField] private bool changeTabSprites = true;

    [Tooltip("If true, changes tab image color tint on tab switch")]
    [SerializeField] private bool changeTabColors = false;

    [Tooltip("If true, applies scale feedback (0.95x) on inactive tab button. Leave false to preserve exact RectTransform layout.")]
    [SerializeField] private bool changeTabScale = false;

    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Sub Panels")]
    [Tooltip("Sub-panel containing the 7-Day Daily Login Rewards grid")]
    [SerializeField] private GameObject dailyContentPanel;

    [Tooltip("Sub-panel containing the Weekly Tasks list")]
    [SerializeField] private GameObject weeklyContentPanel;

    [Header("Collect Button")]
    [Tooltip("Global collect button for Daily Login Rewards (only shown when DAILY tab is active)")]
    [SerializeField] private GameObject collectButton;

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
        EnsureListenersWired();
    }

    /// <summary>
    /// Ensures button listeners are attached even if panel starts inactive in scene hierarchy.
    /// </summary>
    public void EnsureListenersWired()
    {
        currentTab = defaultTab;

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        // Auto-find task button in scene if unassigned
        if (mainMenuTaskButton == null)
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var btn in allButtons)
            {
                if (btn != null && (btn.name.Equals("Tasks", StringComparison.OrdinalIgnoreCase) || 
                                    btn.name.Equals("Task", StringComparison.OrdinalIgnoreCase) || 
                                    btn.name.Equals("TaskButton", StringComparison.OrdinalIgnoreCase) ||
                                    btn.name.Equals("TasksButton", StringComparison.OrdinalIgnoreCase) ||
                                    btn.name.Equals("MightyTasks", StringComparison.OrdinalIgnoreCase) ||
                                    btn.name.Equals("MightyTasksButton", StringComparison.OrdinalIgnoreCase)))
                {
                    mainMenuTaskButton = btn;
                    break;
                }
            }
        }

        // Auto-detect Tab Images if unassigned
        if (dailyTabImage == null && dailyTabButton != null)
        {
            dailyTabImage = dailyTabButton.GetComponent<Image>();
            if (dailyTabImage == null) dailyTabImage = dailyTabButton.GetComponentInChildren<Image>();
        }
        if (weeklyTabImage == null && weeklyTabButton != null)
        {
            weeklyTabImage = weeklyTabButton.GetComponent<Image>();
            if (weeklyTabImage == null) weeklyTabImage = weeklyTabButton.GetComponentInChildren<Image>();
        }

        // Setup Tab Button listeners safely
        if (dailyTabButton != null)
        {
            dailyTabButton.onClick.RemoveListener(SelectDailyTab);
            dailyTabButton.onClick.AddListener(SelectDailyTab);
        }
        if (weeklyTabButton != null)
        {
            weeklyTabButton.onClick.RemoveListener(SelectWeeklyTab);
            weeklyTabButton.onClick.AddListener(SelectWeeklyTab);
        }

        // Setup Close Button listener safely
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }

        // Setup Main Menu Task Button listener safely (opens Weekly Tasks tab)
        if (mainMenuTaskButton != null)
        {
            mainMenuTaskButton.onClick.RemoveListener(OpenDailyTasksPanel);
            mainMenuTaskButton.onClick.RemoveListener(OpenWeeklyTasksPanel);
            mainMenuTaskButton.onClick.AddListener(OpenWeeklyTasksPanel);
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
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }
        else
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
        SwitchTab(tab);
    }

    /// <summary>
    /// Public trigger for Unity UI Button OnClick event in Inspector (opens Daily tab).
    /// </summary>
    public void OpenDailyTasksPanel()
    {
        OpenPanel(TaskTab.Daily);
    }

    /// <summary>
    /// Public trigger for Unity UI Button OnClick event in Inspector (opens Weekly tab).
    /// </summary>
    public void OpenWeeklyTasksPanel()
    {
        OpenPanel(TaskTab.Weekly);
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

        // 2. Toggle Collect Button Visibility (Daily tab only)
        if (collectButton != null)
        {
            collectButton.SetActive(isDaily);
        }

        // 3. Update Daily Tab Visuals
        if (dailyTabImage != null)
        {
            if (changeTabSprites)
            {
                Sprite targetDailySprite = isDaily 
                    ? (dailyActiveSprite != null ? dailyActiveSprite : activeTabSprite)
                    : (dailyInactiveSprite != null ? dailyInactiveSprite : inactiveTabSprite);

                if (targetDailySprite != null)
                {
                    dailyTabImage.sprite = targetDailySprite;
                }
            }

            if (changeTabColors)
            {
                dailyTabImage.color = isDaily ? activeTabColor : inactiveTabColor;
            }
        }

        // 4. Update Weekly Tab Visuals
        if (weeklyTabImage != null)
        {
            if (changeTabSprites)
            {
                Sprite targetWeeklySprite = !isDaily 
                    ? (weeklyActiveSprite != null ? weeklyActiveSprite : activeTabSprite)
                    : (weeklyInactiveSprite != null ? weeklyInactiveSprite : inactiveTabSprite);

                if (targetWeeklySprite != null)
                {
                    weeklyTabImage.sprite = targetWeeklySprite;
                }
            }

            if (changeTabColors)
            {
                weeklyTabImage.color = !isDaily ? activeTabColor : inactiveTabColor;
            }
        }

        // 5. Scale feedback on active tab button (Optional, disabled by default to preserve exact RectTransform layout)
        if (changeTabScale)
        {
            if (dailyTabButton != null)
            {
                dailyTabButton.transform.localScale = isDaily ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);
            }
            if (weeklyTabButton != null)
            {
                weeklyTabButton.transform.localScale = !isDaily ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);
            }
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
