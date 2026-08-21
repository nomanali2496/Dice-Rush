using System;
using System.Text.RegularExpressions;
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
/// Automatically fills progress bar (Slider, Image Fill, and RectTransform Width) according to current/target progress.
/// Safeguards Title & Description text visibility against misconfigured Completed Badge assignments in Inspector.
/// </summary>
[ExecuteAlways]
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

    [Tooltip("RectTransform fill for progress bar (anchor scaling)")]
    [SerializeField] private RectTransform progressBarFillRectTransform;

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

    // [Tooltip("Badge/text shown ONLY after task reward has been claimed (Do NOT assign Title text here!)")]
    // [SerializeField] private GameObject completedBadge;

    private TaskData currentTaskData;
    private Action<TaskData> onClaimCallback;
    private Action<TaskData> onGoCallback;

    private void Awake()
    {
        AutoDetectReferences();

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(OnClaimClicked);
            claimButton.onClick.AddListener(OnClaimClicked);
        }
        if (goButton != null)
        {
            goButton.onClick.RemoveListener(OnGoClicked);
            goButton.onClick.AddListener(OnGoClicked);
        }
    }

    private void Start()
    {
        AutoDetectReferences();
        RefreshProgressBarFromText();
    }

    private void OnEnable()
    {
        AutoDetectReferences();
        RefreshProgressBarFromText();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoDetectReferences();
        RefreshProgressBarFromText();
    }
#endif

    /// <summary>
    /// Auto-detects missing UI references from child GameObjects if unassigned in Inspector
    /// and fixes misconfigured Completed Badge assignments.
    /// </summary>
    public void AutoDetectReferences()
    {
        // 1. Auto-detect Title Text
        if (titleText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt == null) continue;
                string tName = txt.name.ToLower();
                if (tName.Contains("title") && !tName.Contains("chip") && !tName.Contains("reward"))
                {
                    titleText = txt;
                    break;
                }
            }
            if (titleText == null && texts.Length > 0)
            {
                titleText = texts[0];
            }
        }

        // 2. Auto-detect Description Text
        if (descriptionText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt == null || txt == titleText) continue;
                string tName = txt.name.ToLower();
                if (tName.Contains("desc") || tName.Contains("sub"))
                {
                    descriptionText = txt;
                    break;
                }
            }
        }

        // 3. Auto-detect Progress Text
        if (progressText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt == null || txt == titleText || txt == descriptionText) continue;

                if (txt.text.Contains("/") || txt.name.ToLower().Contains("progress"))
                {
                    progressText = txt;
                    break;
                }
            }
        }

        // 4. Auto-detect Reward Text
        if (rewardAmountText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt == null || txt == titleText || txt == descriptionText || txt == progressText) continue;

                string tName = txt.name.ToLower();
                if (tName.Contains("chip") || tName.Contains("reward") || tName.Contains("coin") || tName.Contains("cash"))
                {
                    rewardAmountText = txt;
                    break;
                }
            }
        }

        // 5. Auto-detect Slider
        if (progressBarSlider == null)
        {
            progressBarSlider = GetComponentInChildren<Slider>(true);
        }

        // 6. Auto-detect Progress Fill Image
        if (progressBarFillImage == null)
        {
            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (var img in allImages)
            {
                if (img == null || img == iconImage || img == rewardIconImage) continue;

                string imgName = img.name.ToLower();
                if (imgName.Contains("fill") || imgName.Contains("bar") || imgName.Contains("progress") || imgName.Contains("rectangle"))
                {
                    progressBarFillImage = img;
                    break;
                }
            }
        }

        // 7. Sanity check: if Completed Badge points to titleText or descriptionText, clear it!
        // if (completedBadge != null && (completedBadge == (titleText != null ? titleText.gameObject : null) || 
        //                                completedBadge == (descriptionText != null ? descriptionText.gameObject : null)))
        // {
        //     Debug.LogWarning($"[MightyTaskItemUI] Fixed misconfigured Completed Badge on {gameObject.name} (Title Text was assigned to Completed Badge). Clearing reference.");
        //     completedBadge = null;
        // }
    }

    /// <summary>
    /// Reads progress fraction from progressText (e.g. '25/50') and updates fill bar visuals
    /// </summary>
    public void RefreshProgressBarFromText()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
        }
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
        }

        if (progressText != null && !string.IsNullOrEmpty(progressText.text))
        {
            Match match = Regex.Match(progressText.text, @"(\d+)\s*/\s*(\d+)");
            if (match.Success)
            {
                if (int.TryParse(match.Groups[1].Value, out int current) &&
                    int.TryParse(match.Groups[2].Value, out int target) && target > 0)
                {
                    UpdateProgressBarVisual(current, target);
                    return;
                }
            }
        }

        if (currentTaskData != null)
        {
            UpdateProgressBarVisual(currentTaskData.currentProgress, currentTaskData.targetProgress);
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

        AutoDetectReferences();

        // 1. Setup Title & Description
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = task.title;
        }
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.text = task.description;
        }

        // 2. Setup Icon
        if (iconImage != null && task.taskIcon != null)
        {
            iconImage.sprite = task.taskIcon;
        }

        // 3. Setup Progress Bar & Text
        UpdateProgressBarVisual(task.currentProgress, task.targetProgress);

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
    /// Updates all progress bar fill representations (Slider, Filled Image, Anchor Width)
    /// </summary>
    public void UpdateProgressBarVisual(int currentProgress, int targetProgress)
    {
        float ratio = (targetProgress > 0) ? Mathf.Clamp01((float)currentProgress / targetProgress) : 0f;

        // A. Update Slider
        if (progressBarSlider != null)
        {
            progressBarSlider.minValue = 0f;
            progressBarSlider.maxValue = 1f;
            progressBarSlider.value = ratio;
        }

        // B. Update Filled Image
        if (progressBarFillImage != null)
        {
            progressBarFillImage.type = Image.Type.Filled;
            progressBarFillImage.fillMethod = Image.FillMethod.Horizontal;
            progressBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressBarFillImage.fillAmount = ratio;

            // Anchor Fill fallback for Sliced/Simple images
            RectTransform imgRect = progressBarFillImage.rectTransform;
            if (imgRect != null && imgRect.parent != null && imgRect.anchorMin.x == 0f)
            {
                imgRect.anchorMax = new Vector2(ratio, imgRect.anchorMax.y);
                imgRect.offsetMax = new Vector2(0f, imgRect.offsetMax.y);
            }
        }

        // C. Update RectTransform Width Anchor
        if (progressBarFillRectTransform != null && progressBarFillRectTransform.parent != null)
        {
            progressBarFillRectTransform.anchorMin = new Vector2(0f, progressBarFillRectTransform.anchorMin.y);
            progressBarFillRectTransform.anchorMax = new Vector2(ratio, progressBarFillRectTransform.anchorMax.y);
            progressBarFillRectTransform.offsetMin = new Vector2(0f, progressBarFillRectTransform.offsetMin.y);
            progressBarFillRectTransform.offsetMax = new Vector2(0f, progressBarFillRectTransform.offsetMax.y);
        }

        // D. Update Progress Text
        if (progressText != null)
        {
            progressText.text = $"{currentProgress}/{targetProgress}";
        }
    }

    /// <summary>
    /// Updates button visibility based on task state (Claimed, Ready to Claim, or In Progress).
    /// Prevents hiding Title or Description texts.
    /// </summary>
    private void UpdateButtonStates(TaskData task)
    {
        if (task == null) return;

        // Ensure Title & Description are always active
        if (titleText != null && !titleText.gameObject.activeSelf)
        {
            titleText.gameObject.SetActive(true);
        }
        if (descriptionText != null && !descriptionText.gameObject.activeSelf)
        {
            descriptionText.gameObject.SetActive(true);
        }

        bool isCompleted = task.IsCompleted;
        bool isClaimed = task.isClaimed;

        if (claimButton != null) claimButton.gameObject.SetActive(!isClaimed && isCompleted);
        if (goButton != null) goButton.gameObject.SetActive(!isClaimed && !isCompleted);

        // if (completedBadge != null && completedBadge != (titleText != null ? titleText.gameObject : null) &&
        //     completedBadge != (descriptionText != null ? descriptionText.gameObject : null))
        // {
        //     completedBadge.SetActive(isClaimed);
        // }
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
