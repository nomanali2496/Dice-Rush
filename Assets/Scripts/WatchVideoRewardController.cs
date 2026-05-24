using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to the GameObject that has your "Watch Video" Button component.
/// It will connect to AdManager to show the ad, and BackendAuthService to add the 10 chips reward.
/// </summary>
public class WatchVideoRewardController : MonoBehaviour
{
    [Tooltip("The button used to trigger the rewarded ad. If left blank, it will try to get the Button component on this GameObject.")]
    public Button watchVideoButton;

    private void Awake()
    {
        if (watchVideoButton == null)
        {
            watchVideoButton = GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        // Subscribe to the AdManager's completion event when this button is active
        AdManager.RewardedAdComplete += OnRewardedAdCompleted;
        
        if (watchVideoButton != null)
        {
            watchVideoButton.onClick.AddListener(OnWatchVideoButtonClicked);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        AdManager.RewardedAdComplete -= OnRewardedAdCompleted;
        
        if (watchVideoButton != null)
        {
            watchVideoButton.onClick.RemoveListener(OnWatchVideoButtonClicked);
        }
    }

    private void OnWatchVideoButtonClicked()
    {
        if (AdManager.Instance != null && AdManager.Instance.CanShowRewardedAd())
        {
            AdManager.Instance.ShowRewardedAd();
        }
        else
        {
            Debug.Log("Ad is not ready yet or ads are disabled.");
        }
    }

    private void OnRewardedAdCompleted(bool successfullyWatched)
    {
        if (!successfullyWatched)
        {
            Debug.Log("WatchVideoReward: User skipped the ad — no reward.");
            return;
        }

        Debug.Log("WatchVideoReward: Ad completed. Requesting server-side reward...");

        if (BackendAuthService.Instance == null)
        {
            Debug.LogError("WatchVideoReward: BackendAuthService instance not found!");
            return;
        }

        // Server awards the coins and returns the updated balance.
        // We just refresh the profile so PlayerModel stays in sync.
        BackendAuthService.Instance.ClaimVideoAdReward((success, message) =>
        {
            if (success)
                Debug.Log("WatchVideoReward: Server reward claimed. Profile refreshed.");
            else
                Debug.LogError($"WatchVideoReward: Server reward failed — {message}");
        });
    }
}
