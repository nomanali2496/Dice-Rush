using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Handles rewarded ad flow per the backend contract (v2.6):
///   1. Snapshot coins before showing the ad.
///   2. After SDK fires onRewardedVideoAdRewarded, poll GET /api/wallet/balance
///      every 2s for up to 15s until coins increase.
///   3. If the socket is connected the server also emits adRewardGranted — use
///      that as a fast path to stop polling immediately.
public class WatchVideoRewardController : MonoBehaviour
{
    [Tooltip("The button used to trigger the rewarded ad. Defaults to the Button on this GameObject.")]
    public Button watchVideoButton;

    private const float PollInterval = 2f;
    private const float PollTimeout  = 15f;

    private int  _coinsBeforeAd;
    private bool _pollActive;
    private Coroutine _pollCoroutine;

    private void Awake()
    {
        if (watchVideoButton == null)
            watchVideoButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        AdManager.RewardedAdComplete          += OnRewardedAdCompleted;
        SocketManager.OnAdRewardGranted       += OnSocketAdReward;
        if (watchVideoButton != null)
            watchVideoButton.onClick.AddListener(OnWatchVideoButtonClicked);
    }

    private void OnDisable()
    {
        AdManager.RewardedAdComplete          -= OnRewardedAdCompleted;
        SocketManager.OnAdRewardGranted       -= OnSocketAdReward;
        if (watchVideoButton != null)
            watchVideoButton.onClick.RemoveListener(OnWatchVideoButtonClicked);
        StopPoll();
    }

    private void OnWatchVideoButtonClicked()
    {
        if (AdManager.Instance != null && AdManager.Instance.CanShowRewardedAd())
        {
            // Snapshot BEFORE showing so we can detect the delta afterwards.
            _coinsBeforeAd = PlayerModel.Coins;
            AdManager.Instance.ShowRewardedAd();
        }
        else
        {
            Debug.Log("[WatchVideoReward] Ad not ready or ads disabled.");
        }
    }

    // SDK fires this when the user watched the full ad.
    private void OnRewardedAdCompleted(bool successfullyWatched)
    {
        if (!successfullyWatched)
        {
            Debug.Log("[WatchVideoReward] Ad skipped — no reward.");
            return;
        }

        Debug.Log($"[WatchVideoReward] Ad completed. Coins before: {_coinsBeforeAd}. Starting reward poll...");
        ShowClaimingSpinner(true);
        _pollActive    = true;
        _pollCoroutine = StartCoroutine(PollForAdReward(_coinsBeforeAd));
    }

    // Socket fast path — arrives if socket is already connected when S2S callback hits the server.
    private void OnSocketAdReward(int newTotal, int coinsAwarded)
    {
        if (!_pollActive) return;
        Debug.Log($"[WatchVideoReward] adRewardGranted socket event — stopping poll early (+{coinsAwarded} coins).");
        StopPoll();
        ShowClaimingSpinner(false);
        ShowRewardAnimation(coinsAwarded, newTotal);
    }

    private IEnumerator PollForAdReward(int coinsBeforeAd)
    {
        float elapsed = 0f;

        while (elapsed < PollTimeout)
        {
            yield return new WaitForSeconds(PollInterval);
            elapsed += PollInterval;

            if (!_pollActive) yield break; // Socket fast path already handled it.

            bool done = false;
            BackendAuthService.Instance.GetWalletBalance((success, data) =>
            {
                if (!success || data == null) return;

                if (data.coins > coinsBeforeAd)
                {
                    int earned = data.coins - coinsBeforeAd;
                    // Update PlayerModel with the authoritative balance snapshot.
                    PlayerModel.UpdateData(
                        PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
                        PlayerModel.Wins, PlayerModel.Losses,
                        (int)data.balance, (int)data.cashBalance, (int)data.bonusBalance, (int)data.winningBalance, data.coins,
                        PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level, PlayerModel.Role,
                        PlayerModel.SelectedAvatarIndex, PlayerModel.ActiveMatchId, PlayerModel.ActiveRoomId);

                    StopPoll();
                    ShowClaimingSpinner(false);
                    ShowRewardAnimation(earned, data.coins);
                    done = true;
                }
            });

            if (done) yield break;
        }

        // Timed out — S2S callback may still arrive (LevelPlay retries for ~24h).
        StopPoll();
        ShowClaimingSpinner(false);
        ShowTimeoutToast();
    }

    private void StopPoll()
    {
        _pollActive = false;
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }
    }

    // ── UI hooks — wire these up or override in a subclass ──────────────────

    protected virtual void ShowClaimingSpinner(bool visible)
    {
        Debug.Log($"[WatchVideoReward] Spinner: {visible}");
        // TODO: show/hide your loading spinner UI here
    }

    protected virtual void ShowRewardAnimation(int earned, int newTotal)
    {
        Debug.Log($"[WatchVideoReward] +{earned} coins! New total: {newTotal}");
        // TODO: play coin animation, update coins display
    }

    protected virtual void ShowTimeoutToast()
    {
        Debug.Log("[WatchVideoReward] Reward is on its way!");
        // TODO: show toast — "Reward is on its way!" per contract spec
    }
}
