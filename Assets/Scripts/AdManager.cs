using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.LevelPlay;
using UnityEngine;

/// <summary>
/// Manages all ad functionality directly via the LevelPlay SDK.
/// Simplified for Dice-Rush: Only Rewarded Ads included.
/// </summary>
public class AdManager : MonoBehaviour
{
    #region Constants

    private const string REMOVE_ADS_KEY = "RemoveAds";
    private const string GDPR_KEY       = "UserConsent";

#if UNITY_IOS
    private const string AppKey           = "25fb1382d";
    private const string RewardedAdUnitId = "zz0xye8bos3fdhao";
#else
    private const string AppKey           = "25fb0fea5";
    private const string RewardedAdUnitId = "zhnzswxatn5j0bz8";
#endif

    private const float ReloadDelay      = 30f;
    private const int   MaxRetryCount    = 10;

    #endregion

    #region Properties

    /// <summary>Singleton accessor.</summary>
    public static AdManager Instance { get; private set; }

    /// <summary>Fired when a rewarded ad finishes loading and is ready to show.</summary>
    public static event Action OnRewardedAdLoaded;

    /// <summary>
    /// Fired with true if the user watched the full rewarded ad, false if skipped.
    /// </summary>
    public static event Action<bool> RewardedAdComplete;

    [SerializeField]
    private bool DebugDoNotShowAds = false;

    // LevelPlay ad instances
    private LevelPlayRewardedAd     _rewardedAd;

    // State
    private bool    _isInitialized;
    private bool    _rewardedWatched;

    private readonly List<string> EU_Timezones = new List<string>
    {
        "Europe/Vienna", "Europe/Brussels", "Europe/Sofia", "Europe/Zagreb",
        "Asia/Famagusta", "Asia/Nicosia", "Europe/Prague", "Europe/Copenhagen",
        "Europe/Tallinn", "Europe/Helsinki", "Europe/Paris", "Europe/Berlin",
        "Europe/Busingen", "Europe/Athens", "Europe/Budapest", "Europe/Dublin",
        "Europe/Rome", "Europe/Riga", "Europe/Vilnius", "Europe/Luxembourg",
        "Europe/Malta", "Europe/Amsterdam", "Europe/Warsaw", "Atlantic/Azores",
        "Atlantic/Madeira", "Europe/Lisbon", "Europe/Bucharest", "Europe/Bratislava",
        "Europe/Ljubljana", "Africa/Ceuta", "Atlantic/Canary", "Europe/Madrid",
        "Europe/Stockholm"
    };

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
#if UNITY_EDITOR
        DebugDoNotShowAds = true;
#endif
        // Subscribe so we initialize as soon as a userId is available (login / auto-login).
        PlayerModel.OnDataUpdated += OnPlayerDataReady;

        // Returning user: PlayerModel.Id may already be populated if auto-login ran first.
        TryInitializeSdk();
    }


    void OnDestroy()
    {
        PlayerModel.OnDataUpdated -= OnPlayerDataReady;
        LevelPlay.OnInitSuccess -= LevelPlay_OnInitSuccess;
        LevelPlay.OnInitFailed  -= LevelPlay_OnInitFailed;

        if (_rewardedAd != null)
        {
            _rewardedAd.OnAdLoaded        -= OnRewardedLoaded;
            _rewardedAd.OnAdLoadFailed    -= OnRewardedLoadFailed;
            _rewardedAd.OnAdClicked       -= OnRewardedClicked;
            _rewardedAd.OnAdClosed        -= OnRewardedClosed;
            _rewardedAd.OnAdRewarded      -= OnRewardedRewarded;
            _rewardedAd.OnAdDisplayFailed -= OnRewardedDisplayFailed;
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        // LevelPlay v9+ handles app pause automatically
    }

    #endregion

    #region Initialization

    // Called from Start() and from OnPlayerDataReady() — guards against double-init.
    private void TryInitializeSdk()
    {
        if (_isInitialized) return;
        if (!CanShowAds() || DebugDoNotShowAds)
        {
            Debug.LogWarning($"AdManager: Ads skipped. CanShowAds={CanShowAds()}, DebugMode={DebugDoNotShowAds}");
            return;
        }

        string userId = PlayerModel.Id;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.Log("AdManager: No userId yet — deferring SDK init until login completes.");
            return;
        }

        InitializeSdk(userId);
    }

    // Fired by PlayerModel.OnDataUpdated after every login / profile fetch.
    private void OnPlayerDataReady()
    {
        TryInitializeSdk();
    }

    private void InitializeSdk(string userId)
    {
        Debug.Log($"AdManager: Initializing LevelPlay SDK for user '{userId}'...");
        try
        {
            LevelPlay.SetMetaData("is_test_suite", "enable");

            // Must be set BEFORE LevelPlay.Init so the ad network attributes
            // impressions and rewards to the correct user (MongoDB _id).
            IronSource.Agent.setUserId(userId);
            Debug.Log($"AdManager: userId set → {userId}");

            // Unsubscribe first to prevent double-subscription on re-init
            LevelPlay.OnInitSuccess -= LevelPlay_OnInitSuccess;
            LevelPlay.OnInitFailed  -= LevelPlay_OnInitFailed;
            LevelPlay.OnInitSuccess += LevelPlay_OnInitSuccess;
            LevelPlay.OnInitFailed  += LevelPlay_OnInitFailed;

            LevelPlay.Init(AppKey);
            Debug.Log("AdManager: LevelPlay.Init called");
        }
        catch (Exception e)
        {
            Debug.LogError($"AdManager: Exception during initialization: {e.Message}\n{e.StackTrace}");
        }
    }

    private void LevelPlay_OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log($"AdManager: LevelPlay initialized. Config: {config}");
        _isInitialized = true;

        // Create rewarded ad instance
        _rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
        _rewardedAd.OnAdLoaded        += OnRewardedLoaded;
        _rewardedAd.OnAdLoadFailed    += OnRewardedLoadFailed;
        _rewardedAd.OnAdClicked       += OnRewardedClicked;
        _rewardedAd.OnAdClosed        += OnRewardedClosed;
        _rewardedAd.OnAdRewarded      += OnRewardedRewarded;
        _rewardedAd.OnAdDisplayFailed += OnRewardedDisplayFailed;
        _rewardedAd.LoadAd();

        // GDPR consent check
        if (!GDPRConsentWasSet())
        {
            string timezone = TimeZoneInfo.Local.Id;
            Debug.Log($"AdManager: User timezone: {timezone}");
            if (EU_Timezones.Contains(timezone))
            {
                Debug.Log("AdManager: EU user — consent must be obtained before showing ads");
                // No built-in LevelPlay consent popup; implement custom UI if needed
            }
            else
            {
                Debug.Log("AdManager: Non-EU user, setting consent to true");
                SetGDPRConsent(true);
            }
        }
    }

    private void LevelPlay_OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"AdManager: LevelPlay init failed. Error: {error}");
    }

    #endregion

    #region Public API — Ads State

    /// <summary>Returns true if ads are allowed (user has not purchased Remove Ads).</summary>
    public bool CanShowAds()
    {
        if (!PlayerPrefs.HasKey(REMOVE_ADS_KEY)) return true;
        return PlayerPrefs.GetInt(REMOVE_ADS_KEY) == 0;
    }

    /// <summary>Returns true if the LevelPlay SDK has finished initializing.</summary>
    public bool IsInitialized() => _isInitialized;

    /// <summary>Returns true if a rewarded ad is loaded and ready to show.</summary>
    public bool IsRewardedVideoAvailable() => _rewardedAd != null && _rewardedAd.IsAdReady();

    /// <summary>Enables or disables ads permanently via PlayerPrefs.</summary>
    public void RemoveAds(bool shouldRemove)
    {
        PlayerPrefs.SetInt(REMOVE_ADS_KEY, shouldRemove ? 1 : 0);
        Debug.Log($"AdManager: RemoveAds set to {shouldRemove}");

        if (!shouldRemove && !_isInitialized && !DebugDoNotShowAds)
        {
            // SDK was never initialized (Start() ran while ads were disabled).
            // TryInitializeSdk guards against missing userId.
            Debug.Log("AdManager: Ads re-enabled — attempting SDK init now");
            TryInitializeSdk();
        }
    }

    #endregion

    #region Rewarded Ads

    public void ShowRewardedAd()
    {
#if UNITY_EDITOR
        RewardedVideoCompleted(true);
        return;
#endif
        if (CanShowAds() && !DebugDoNotShowAds)
        {
            if (IsRewardedVideoAvailable())
            {
                _rewardedAd.ShowAd(RewardedAdUnitId);
            }
            else
            {
                Debug.Log("AdManager: Rewarded ad not ready, skipping");
            }
        }
        else
        {
            Debug.Log("AdManager: Ads not allowed.");
            if (DebugDoNotShowAds)
                RewardedVideoCompleted(true);
        }
    }

    public void ShowRewardedAdNow()
    {
#if UNITY_EDITOR
        RewardedVideoCompleted(true);
        return;
#endif
        if (CanShowAds() && !DebugDoNotShowAds)
        {
            if (IsRewardedVideoAvailable())
                _rewardedAd.ShowAd(RewardedAdUnitId);
        }
        else
        {
            Debug.Log("AdManager: Ads not allowed.");
            if (DebugDoNotShowAds)
                RewardedVideoCompleted(true);
        }
    }

    public bool CanShowRewardedAd()
    {
#if UNITY_EDITOR
        return true;
#endif
        bool canShow    = CanShowAds() && !DebugDoNotShowAds;
        bool isAvailable = IsRewardedVideoAvailable();
        Debug.Log($"AdManager: CanShowRewardedAd — CanShow: {canShow}, IsAvailable: {isAvailable}");
        return canShow && isAvailable;
    }

    private void RewardedVideoCompleted(bool shouldAwardUser)
    {
        if (RewardedAdComplete != null)
            RewardedAdComplete.Invoke(shouldAwardUser);
        else
            Debug.LogWarning("AdManager: RewardedAdComplete has no subscribers.");

        Debug.Log("AdManager: Rewarded ad completed, reloading next ad");
    }

    #endregion

    #region LevelPlay — Rewarded Event Handlers

    private void OnRewardedLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdManager: Rewarded ad loaded");
        OnRewardedAdLoaded?.Invoke();
    }

    private void OnRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError($"AdManager: Rewarded failed to load. {error}");
        StartCoroutine(ReloadRewardedDelayed());
    }

    private void OnRewardedClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdManager: Rewarded ad clicked");
    }

    private void OnRewardedRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"AdManager: Rewarded — {reward.Name} x{reward.Amount}");
        _rewardedWatched = true;
    }

    private void OnRewardedClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("AdManager: Rewarded ad closed");
        RewardedVideoCompleted(_rewardedWatched);
        _rewardedWatched = false;
        _rewardedAd.LoadAd();
    }

    private void OnRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"AdManager: Rewarded display failed. {error}");
        StartCoroutine(ReloadRewardedDelayed());
    }

    private IEnumerator ReloadRewardedDelayed()
    {
        yield return new WaitForSeconds(ReloadDelay);
        _rewardedAd?.LoadAd();
    }

    #endregion

    #region Consent

    private bool GDPRConsentWasSet() => PlayerPrefs.HasKey(GDPR_KEY);

    private void SetGDPRConsent(bool accept)
    {
        PlayerPrefs.SetInt(GDPR_KEY, accept ? 1 : 0);
#pragma warning disable CS0618
        LevelPlay.SetConsent(accept);
#pragma warning restore CS0618
    }

    #endregion
}
