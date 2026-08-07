using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Data model representing a single task in the Mighty Tasks / Weekly Tasks list.
/// </summary>
[System.Serializable]
public class TaskData
{
    public string taskId;
    public string title;
    public string description;
    public int currentProgress;
    public int targetProgress;
    public int rewardChips;
    public float rewardCash;
    public Sprite taskIcon;
    public bool isClaimed;

    public bool IsCompleted => currentProgress >= targetProgress;
}

/// <summary>
/// Controls an individual task card in the Mighty Tasks list.
/// Handles title, description, progress bar (e.g. 50/50), reward text (+500 Chips),
/// and dynamic button states (Green CLAIM when ready, Blue GO when in-progress, or Claimed state).
/// </summary>
public class MightyTaskItemUI : MonoBehaviour
{
    [Header("UI Text & Visual References")]
    [Tooltip("Icon representing the task")]
    [SerializeField] private Image iconImage;

    [Tooltip("Title text (e.g. 'Roll 50 Dice')")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Description text (e.g. 'Roll 50 Total Dices')")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Progress Bar")]
    [Tooltip("Slider for progress bar fill")]
    [SerializeField] private Slider progressBarSlider;

    [Tooltip("Image fill for progress bar (alternative to Slider)")]
    [SerializeField] private Image progressBarFillImage;

    [Tooltip("Text showing progress fraction (e.g. '50/50')")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Reward Displays")]
    [Tooltip("Chips/Coins reward icon")]
    [SerializeField] private Image rewardIconImage;

    [Tooltip("Reward amount text (e.g. '+500 Chips')")]
    [SerializeField] private TextMeshProUGUI rewardAmountText;

    [Header("Action Button States")]
    [Tooltip("Green CLAIM button (shown when task target progress is reached)")]
    [SerializeField] private Button claimButton;

    [Tooltip("Blue GO button (shown when task is in-progress)")]
    [SerializeField] private Button goButton;

    [Tooltip("Badge/text shown after task reward has been claimed")]
    [SerializeField] private GameObject completedBadge;

    private TaskData currentTaskData;
    private Action<TaskData> onClaimCallback;
    private Action<TaskData> onGoCallback;

    private void Awake()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimClicked);
        }
        if (goButton != null)
        {
            goButton.onClick.AddListener(OnGoClicked);
        }
    }

    /// <summary>
    /// Configures the task card with data and button click handlers.
    /// </summary>
    public void SetupTask(TaskData task, Action<TaskData> onClaim, Action<TaskData> onGo)
    {
        currentTaskData = task;
        onClaimCallback = onClaim;
        onGoCallback = onGo;

        if (task == null) return;

        // 1. Setup Title & Description
        if (titleText != null)
        {
            titleText.text = task.title;
        }
        if (descriptionText != null)
        {
            descriptionText.text = task.description;
        }

        // 2. Setup Icon
        if (iconImage != null && task.taskIcon != null)
        {
            iconImage.sprite = task.taskIcon;
        }

        // 3. Setup Progress Bar & Progress Text
        float progressRatio = (task.targetProgress > 0) ? Mathf.Clamp01((float)task.currentProgress / task.targetProgress) : 0f;

        if (progressBarSlider != null)
        {
            progressBarSlider.minValue = 0f;
            progressBarSlider.maxValue = 1f;
            progressBarSlider.value = progressRatio;
        }
        if (progressBarFillImage != null)
        {
            progressBarFillImage.fillAmount = progressRatio;
        }
        if (progressText != null)
        {
            progressText.text = $"{task.currentProgress}/{task.targetProgress}";
        }

        // 4. Setup Reward Text
        if (rewardAmountText != null)
        {
            if (task.rewardChips > 0)
            {
                rewardAmountText.text = $"+{task.rewardChips} Chips";
            }
            else if (task.rewardCash > 0f)
            {
                rewardAmountText.text = $"+${task.rewardCash:F2}";
            }
            else
            {
                rewardAmountText.text = "+0";
            }
        }

        // 5. Update Action Button States (CLAIM vs GO vs Completed)
        UpdateButtonStates(task);
    }

    /// <summary>
    /// Updates button visibility based on task state (Claimed, Ready to Claim, or In Progress).
    /// </summary>
    private void UpdateButtonStates(TaskData task)
    {
        if (task.isClaimed)
        {
            // Already claimed -> show completed badge, hide both buttons
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (goButton != null) goButton.gameObject.SetActive(false);
            if (completedBadge != null) completedBadge.SetActive(true);
        }
        else if (task.IsCompleted)
        {
            // Target progress reached -> show Green CLAIM button, hide GO button
            if (claimButton != null) claimButton.gameObject.SetActive(true);
            if (goButton != null) goButton.gameObject.SetActive(false);
            if (completedBadge != null) completedBadge.SetActive(false);
        }
        else
        {
            // Still in progress -> show Blue GO button, hide CLAIM button
            if (claimButton != null) claimButton.gameObject.SetActive(false);
            if (goButton != null) goButton.gameObject.SetActive(true);
            if (completedBadge != null) completedBadge.SetActive(false);
        }
    }

    private void OnClaimClicked()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();

        if (currentTaskData != null && currentTaskData.IsCompleted && !currentTaskData.isClaimed)
        {
            currentTaskData.isClaimed = true;
            UpdateButtonStates(currentTaskData);
            onClaimCallback?.Invoke(currentTaskData);
        }
    }

    private void OnGoClicked()
    {
        if (SoundManager.instance != null) SoundManager.instance.PlayTap();
        onGoCallback?.Invoke(currentTaskData);
    }
}
