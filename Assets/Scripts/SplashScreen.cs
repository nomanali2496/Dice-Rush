using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Splash screen that shows AFTER login/registration
/// Displays loading animation then transitions to game
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Header("Slider Settings")]
    [Tooltip("The slider that will be filled. If not assigned, will try to find one on this GameObject.")]
    [SerializeField] private Slider progressSlider;

    [Header("Loading Settings")]
    [Tooltip("Duration in seconds for the loading animation to complete.")]
    [SerializeField] private float loadingDuration = 3f;

    [Tooltip("Minimum loading time before completion (prevents instant completion).")]
    [SerializeField] private float minLoadingTime = 1f;

    [Header("Animation Settings")]
    [Tooltip("Smoothing speed for the slider fill animation.")]
    [SerializeField] private float fillSpeed = 2f;

    [Tooltip("Use smooth interpolation instead of linear fill.")]
    [SerializeField] private bool useSmoothFill = true;

    [Header("Next Panel")]
    [Tooltip("The game panel to show after loading completes")]
    [SerializeField] private GameObject gamePanel;

    public Action OnLoadingCompleteEvent;

    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float elapsedTime = 0f;
    private bool isLoading = false;

    private void OnEnable()
    {
        InitializeSlider();
        StartLoading();
    }

    private void InitializeSlider()
    {
        // If slider is not assigned, try to find it on this GameObject
        if (progressSlider == null)
        {
            progressSlider = GetComponent<Slider>();
        }

        // If still not found, try to find it in children
        if (progressSlider == null)
        {
            progressSlider = GetComponentInChildren<Slider>();
        }

        // Initialize slider
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
        }
        else
        {
            Debug.LogError("SplashScreen: No Slider component found! Please assign a Slider component.");
        }
    }

    private void StartLoading()
    {
        if (progressSlider == null) return;

        isLoading = true;
        currentProgress = 0f;
        targetProgress = 0f;
        elapsedTime = 0f;
        progressSlider.value = 0f;
    }

    private void Update()
    {
        if (!isLoading || progressSlider == null) return;

        // Update elapsed time
        elapsedTime += Time.deltaTime;

        // Calculate target progress based on elapsed time
        float timeProgress = Mathf.Clamp01(elapsedTime / loadingDuration);
        targetProgress = timeProgress;

        // Update current progress with smoothing
        if (useSmoothFill)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * fillSpeed);
        }
        else
        {
            currentProgress = targetProgress;
        }

        // Ensure minimum loading time
        if (elapsedTime < minLoadingTime)
        {
            currentProgress = Mathf.Min(currentProgress, elapsedTime / minLoadingTime * 0.9f);
        }

        // Update slider value
        progressSlider.value = Mathf.Clamp01(currentProgress);

        // Check if loading is complete
        if (elapsedTime >= loadingDuration && currentProgress >= 0.99f)
        {
            OnLoadingComplete();
        }
    }

    private void OnLoadingComplete()
    {
        isLoading = false;
        progressSlider.value = 1f;
        
        Debug.Log("Splash Screen: Loading Complete!");
        
        // Hide splash panel and show game panel
        gameObject.SetActive(false);
        
        if (gamePanel != null)
        {
            gamePanel.SetActive(true);
        }
        
        // Notify listeners
        OnLoadingCompleteEvent?.Invoke();
    }

    /// <summary>
    /// Manually set the progress (0 to 1).
    /// </summary>
    public void SetProgress(float progress)
    {
        if (progressSlider == null) return;
        
        targetProgress = Mathf.Clamp01(progress);
        if (!useSmoothFill)
        {
            currentProgress = targetProgress;
            progressSlider.value = currentProgress;
        }
    }

    /// <summary>
    /// Get the current progress value (0 to 1).
    /// </summary>
    public float GetProgress()
    {
        return progressSlider != null ? progressSlider.value : 0f;
    }

    /// <summary>
    /// Reset the splash screen and start loading again.
    /// </summary>
    public void Reset()
    {
        StartLoading();
    }

    /// <summary>
    /// Set the loading duration.
    /// </summary>
    public void SetLoadingDuration(float duration)
    {
        loadingDuration = Mathf.Max(0.1f, duration);
    }
}

