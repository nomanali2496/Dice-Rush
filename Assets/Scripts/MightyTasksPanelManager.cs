using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the unified "Mighty Tasks" popup panel.
/// Manages tab switching between the DAILY (7-Day Login Rewards) tab and the WEEKLY (Tasks) tab,
/// handles dynamic 5-task progression tracking, auto-fill progress bars, claims, and reset timer.
/// </summary>
public class MightyTasksPanelManager : MonoBehaviour
{
    public enum TaskTab
    {
        Daily,
        Weekly
    }

    public static MightyTasksPanelManager Instance { get; private set; }

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
    [SerializeField] private bool changeTabSprites = true;
    [SerializeField] private bool changeTabColors = false;
    [SerializeField] private bool changeTabScale = false;

    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Sub Panels")]
    [Tooltip("Sub-panel containing the 7-Day Daily Login Rewards grid")]
    [SerializeField] private GameObject dailyContentPanel;

    [Tooltip("Sub-panel containing the Weekly Tasks list")]
    [SerializeField] private GameObject weeklyContentPanel;

    [Header("Weekly Task Cards Container")]
    [Tooltip("Content container holding the 5 task items in ScrollView (auto-detected if null)")]
    [SerializeField] private Transform weeklyTasksContainer;

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

    // 5 Default Weekly Tasks Data definitions
    private List<TaskData> defaultWeeklyTasks = new List<TaskData>()
    {
        new TaskData { taskId = "task_roll_50_dice", title = "Roll 50 Dice", description = "Roll 50 Total Dices in Gameplay", currentProgress = 50, targetProgress = 50, rewardChips = 500, rewardCash = 0f },
        new TaskData { taskId = "task_win_3_matches", title = "Win 3 Matches", description = "Win 3 Matches in Any Mode", currentProgress = 3, targetProgress = 3, rewardChips = 750, rewardCash = 0f },
        new TaskData { taskId = "task_play_5_games", title = "Play 5 Games", description = "Complete 5 Total Matches", currentProgress = 5, targetProgress = 5, rewardChips = 1000, rewardCash = 0f },
        new TaskData { taskId = "task_earn_2000_coins", title = "Earn 2,000 Coins", description = "Earn 2000 Coins from Matches", currentProgress = 2000, targetProgress = 2000, rewardChips = 1500, rewardCash = 0f },
        new TaskData { taskId = "task_score_100_pts", title = "Score 100 Points", description = "Score 100 Total Points in Grid", currentProgress = 100, targetProgress = 100, rewardChips = 0, rewardCash = 1.00f }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

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
        SwitchTab(currentTab);
        PopulateWeeklyTasks();

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
    /// Configures all 5 weekly task cards in the UI hierarchy with progress tracking and claim listeners
    /// </summary>
    public void PopulateWeeklyTasks()
    {
        MightyTaskItemUI[] taskCards = null;

        if (weeklyTasksContainer != null)
        {
            taskCards = weeklyTasksContainer.GetComponentsInChildren<MightyTaskItemUI>(true);
        }
        else if (weeklyContentPanel != null)
        {
            taskCards = weeklyContentPanel.GetComponentsInChildren<MightyTaskItemUI>(true);
        }
        else
        {
            taskCards = GetComponentsInChildren<MightyTaskItemUI>(true);
        }

        if (taskCards == null || taskCards.Length == 0) return;

        for (int i = 0; i < taskCards.Length; i++)
        {
            if (taskCards[i] == null) continue;

            int taskDataIndex = i % defaultWeeklyTasks.Count;
            TaskData baseData = defaultWeeklyTasks[taskDataIndex];

            // Load saved task state from PlayerPrefs
            TaskData taskInstance = new TaskData()
            {
                taskId = baseData.taskId,
                title = baseData.title,
                description = baseData.description,
                targetProgress = baseData.targetProgress,
                rewardChips = baseData.rewardChips,
                rewardCash = baseData.rewardCash,
                taskIcon = baseData.taskIcon,
                currentProgress = PlayerPrefs.GetInt($"MightyTask_Prog_{baseData.taskId}", baseData.targetProgress),
                isClaimed = PlayerPrefs.GetInt($"MightyTask_Claimed_{baseData.taskId}", 0) == 1
            };

            taskCards[i].SetupTask(taskInstance, OnClaimTaskClicked, OnGoTaskClicked);
        }
    }

    private void OnClaimTaskClicked(TaskData task)
    {
        if (task == null) return;

        Debug.Log($"<color=#00FF00>🎁 [MightyTasks] Task Claimed: {task.title} (+{task.rewardChips} Chips, +${task.rewardCash:F2})</color>");
        PlayerPrefs.SetInt($"MightyTask_Claimed_{task.taskId}", 1);
        PlayerPrefs.Save();

        // Refresh UI
        PopulateWeeklyTasks();
    }

    private void OnGoTaskClicked(TaskData task)
    {
        Debug.Log($"[MightyTasks] GO button clicked for task: {task?.title}");
        ClosePanel();

        // Navigate to ChooseYourMode or Lobby
        ChooseYourModeManager chooseModeMgr = ChooseYourModeManager.Instance != null ? ChooseYourModeManager.Instance : FindFirstObjectByType<ChooseYourModeManager>(FindObjectsInactive.Include);
        if (chooseModeMgr != null)
        {
            chooseModeMgr.OpenPanel();
        }
        else
        {
            HorizontalLobbyManager lobbyMgr = HorizontalLobbyManager.Instance != null ? HorizontalLobbyManager.Instance : FindFirstObjectByType<HorizontalLobbyManager>(FindObjectsInactive.Include);
            if (lobbyMgr != null)
            {
                lobbyMgr.OnLobbyOpened();
            }
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

    public void OpenDailyTasksPanel()
    {
        OpenPanel(TaskTab.Daily);
    }

    public void OpenWeeklyTasksPanel()
    {
        OpenPanel(TaskTab.Weekly);
    }

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

    public void SelectDailyTab()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
        SwitchTab(TaskTab.Daily);
    }

    public void SelectWeeklyTab()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
        SwitchTab(TaskTab.Weekly);
    }

    public void SwitchTab(TaskTab tab)
    {
        currentTab = tab;
        bool isDaily = (tab == TaskTab.Daily);

        if (dailyContentPanel != null)
        {
            dailyContentPanel.SetActive(isDaily);
        }

        if (weeklyContentPanel != null)
        {
            weeklyContentPanel.SetActive(!isDaily);
        }

        if (collectButton != null)
        {
            collectButton.SetActive(isDaily);
        }

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

        if (!isDaily)
        {
            PopulateWeeklyTasks();
        }
    }

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
