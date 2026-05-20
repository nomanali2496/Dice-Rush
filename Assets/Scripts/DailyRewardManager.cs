using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages daily rewards system using custom backend API.
/// Independent script that monitors the Main Menu to show the panel when appropriate.
/// </summary>
public class DailyRewardManager : MonoBehaviour
{
    [Header("Daily Rewards UI")]
    [SerializeField] private Image[] dayBackgrounds = new Image[7];
    [SerializeField] private Sprite[] selectedBackgroundSprites = new Sprite[7];
    [SerializeField] private Sprite[] normalBackgroundSprites = new Sprite[7];
    
    [Header("Panel & Screens")]
    [SerializeField] private GameObject dailyRewardPanel;
    [SerializeField] private GameObject mainMenuScreen; // Monitor this to know when to show
    
    [Header("Collect Button")]
    [SerializeField] private Button collectButton;
    [SerializeField] private Button closeButton;
    
    [Header("Settings")]
    [SerializeField] private float autoCloseDelay = 0.5f;
    [SerializeField] private float checkInterval = 0.5f; // How often to check for main menu
    
    [Header("Animation - Target Positions")]
    [SerializeField] private RectTransform[] sourceRewardPositions = new RectTransform[7];
    [SerializeField] private RectTransform targetCashPosition;
    [SerializeField] private RectTransform targetChipsPosition;
    
    [Header("Animation - Sprites")]
    [SerializeField] private Sprite cashSprite;
    [SerializeField] private Sprite chipsSprite;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private int baseParticles = 3;
    [SerializeField] private float particlesPerCash = 0.5f;
    [SerializeField] private float particlesPerChip = 0.1f;
    [SerializeField] private int maxParticles = 20;
    [SerializeField] private float delayBetweenParticles = 0.1f;
    
    [Header("Animation - Prefab")]
    [SerializeField] private GameObject animatedSpritePrefab;
    
    [Header("Debug Info")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private bool canClaim = false;
    public bool CanClaim => canClaim;
    [SerializeField] private bool hasTriggeredThisSession = false;
    private bool isStatusLoaded = false;
    public bool IsStatusLoaded => isStatusLoaded;

    private List<GameObject> activeAnimations = new List<GameObject>();
    private PlayerProfileUI playerProfileUI;
    
    // Values for count-up animation
    private float currentAnimatedCash = 0f;
    private int currentAnimatedChips = 0;
    
    // Reward data from server
    private List<BackendAuthService.RewardItem> serverRewards = new List<BackendAuthService.RewardItem>();

    public Action<int, int, float> OnRewardCollected;
    public Action OnDailyRewardPanelClosed;

    private void Start()
    {
        SetupCloseButton();
        playerProfileUI = FindObjectOfType<PlayerProfileUI>();
        
        if (collectButton != null)
        {
            collectButton.onClick.AddListener(CollectCurrentDayReward);
        }

        // Subscribe to login events
        AuthenticationManager authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess += RefreshStatusFromServer;
        }

        // Check if already logged in (for app restarts)
        if (BackendAuthService.HasStoredJWT())
        {
            RefreshStatusFromServer();
        }

        // Start independent monitoring of the Main Menu
        StartCoroutine(MonitorMainMenuActivation());
    }

    private void OnDestroy()
    {
        AuthenticationManager authManager = FindObjectOfType<AuthenticationManager>();
        if (authManager != null)
        {
            authManager.OnAuthenticationSuccess -= RefreshStatusFromServer;
        }
    }

    private void OnEnable()
    {
        if (BackendAuthService.HasStoredJWT())
        {
            RefreshStatusFromServer();
        }
    }

    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    /// <summary>
    /// Monitors the main menu. When it becomes active, we show the reward panel if canClaim is true.
    /// This makes the script independent of the OfferPanelManager for opening.
    /// </summary>
    private IEnumerator MonitorMainMenuActivation()
    {
        while (true)
        {
            // If Main Menu is active, and we haven't shown yet this session, and we can claim
            if (mainMenuScreen != null && mainMenuScreen.activeSelf && !hasTriggeredThisSession && canClaim)
            {
                Debug.Log("[DailyRewardManager] Main Menu active and can claim. Showing Reward Panel independently.");
                ShowPanel();
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    public void ShowPanel()
    {
        if (dailyRewardPanel != null)
        {
            hasTriggeredThisSession = true; // Mark as shown so we don't spam it
            dailyRewardPanel.SetActive(true);
            UpdateUI(); // Ensure UI is updated with latest data
        }
    }

    public void RefreshStatusFromServer()
    {
        if (BackendAuthService.Instance == null) return;

        BackendAuthService.Instance.GetDailyRewardStatus((success, response) => 
        {
            isStatusLoaded = true;
            if (success && response != null && response.data != null)
            {
                serverRewards = response.data.rewards;
                currentDay = response.data.nextDay;
                canClaim = response.data.canClaim;
                UpdateUI();
            }
        });
    }

    private void UpdateUI()
    {
        for (int i = 0; i < 7; i++)
        {
            if (dayBackgrounds[i] == null) continue;
            
            int dayNumber = i + 1;
            
            // Highlight the day only if it's the current claimable day
            if (dayNumber == currentDay && canClaim)
            {
                if (selectedBackgroundSprites != null && i < selectedBackgroundSprites.Length)
                    dayBackgrounds[i].sprite = selectedBackgroundSprites[i];
            }
            else
            {
                if (normalBackgroundSprites != null && i < normalBackgroundSprites.Length)
                    dayBackgrounds[i].sprite = normalBackgroundSprites[i];
            }
        }
        
        if (collectButton != null)
        {
            collectButton.interactable = canClaim;
        }
    }

    public void CollectCurrentDayReward()
    {
        if (!canClaim || BackendAuthService.Instance == null) return;

        collectButton.interactable = false;

        // Capture values BEFORE update
        int coinsBefore = PlayerModel.Coins;
        float cashBefore = PlayerModel.CashBalance;

        BackendAuthService.Instance.ClaimDailyReward((success, response) => 
        {
            if (success && response != null)
            {
                int dayToAnimate = currentDay;
                int coinsReward = 0;
                float cashReward = 0;

                var reward = serverRewards.Find(r => r.day == dayToAnimate);
                if (reward != null)
                {
                    coinsReward = reward.coins;
                    cashReward = reward.cash;
                }

                StartCoroutine(PlayRewardAnimationAndFinish(coinsReward, cashReward, dayToAnimate, coinsBefore, cashBefore, response));
            }
            else
            {
                collectButton.interactable = true;
            }
        });
    }

    private IEnumerator PlayRewardAnimationAndFinish(int coins, float cash, int day, int coinsBefore, float cashBefore, BackendAuthService.DailyRewardStatusResponse response)
    {
        currentAnimatedCash = cashBefore;
        currentAnimatedChips = coinsBefore;

        if (coins > 0) PlayRewardAnimation(coins, false, day);
        if (cash > 0) PlayRewardAnimation(cash, true, day);

        yield return new WaitForSeconds(animationDuration + (delayBetweenParticles * maxParticles) + 0.2f);

        // Explicitly push the updated currency values to the backend profile endpoint
        int newTotalCoins = coinsBefore + coins;
        int newTotalCash = Mathf.RoundToInt(cashBefore + cash);

        if (response != null && response.user != null)
        {
            int finalCoins = response.user.coins > 0 ? response.user.coins : newTotalCoins;
            int finalCash = response.user.cashBalance > 0 ? response.user.cashBalance : newTotalCash;

            PlayerModel.UpdateData(
                response.user.id, response.user.name, response.user.email,
                response.user.wins, response.user.losses, response.user.balance,
                finalCash, response.user.bonusBalance, response.user.winningBalance,
                finalCoins, response.user.avatar, response.user.xp,
                response.user.level, response.user.role
            );
        }

        BackendAuthService.Instance.UpdateCurrency(newTotalCash, newTotalCoins, (profileSuccess, message) => 
        {
            if (profileSuccess)
            {
                Debug.Log("[DailyRewardManager] Currency successfully synced to backend profile.");
            }
            else
            {
                Debug.LogError("[DailyRewardManager] Failed to sync currency to backend profile: " + message);
            }
        });

        OnRewardCollected?.Invoke(day, coins, cash);
        canClaim = false;
        UpdateUI();

        yield return new WaitForSeconds(autoCloseDelay);
        ClosePanel();
    }

    private void PlayRewardAnimation(float amount, bool isCash, int day)
    {
        RectTransform source = GetSourceRewardPosition(day);
        RectTransform target = isCash ? targetCashPosition : targetChipsPosition;
        Sprite sprite = isCash ? cashSprite : chipsSprite;

        if (source == null || target == null || sprite == null) return;

        StartCoroutine(AnimateParticles(source, target, sprite, amount, isCash));
    }

    private RectTransform GetSourceRewardPosition(int day)
    {
        int index = day - 1;
        if (index >= 0 && index < sourceRewardPositions.Length)
            return sourceRewardPositions[index];
        return null;
    }

    private IEnumerator AnimateParticles(RectTransform source, RectTransform target, Sprite sprite, float amount, bool isCash)
    {
        int particleCount = isCash ? 
            Mathf.RoundToInt(baseParticles + (amount * particlesPerCash)) : 
            Mathf.RoundToInt(baseParticles + (amount * particlesPerChip));
        
        particleCount = Mathf.Clamp(particleCount, baseParticles, maxParticles);
        float increment = amount / particleCount;
        Vector3 sourcePos = source.position;

        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = CreateAnimatedSprite(sourcePos, sprite);
            if (particle != null)
            {
                activeAnimations.Add(particle);
                StartCoroutine(AnimateSingleParticleWithCountUp(particle.transform, target.position, animationDuration, increment, isCash));
            }
            yield return new WaitForSeconds(delayBetweenParticles);
        }
    }

    private GameObject CreateAnimatedSprite(Vector3 position, Sprite sprite)
    {
        Canvas canvas = dailyRewardPanel.GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;
        
        GameObject obj = animatedSpritePrefab != null ? Instantiate(animatedSpritePrefab, parent) : new GameObject("AnimatedSprite");
        if (animatedSpritePrefab == null)
        {
            obj.transform.SetParent(parent);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            obj.AddComponent<Image>().sprite = sprite;
        }
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null) 
        {
            rect.position = position;
            rect.localScale = Vector3.one;
        }
        else
        {
            obj.transform.localScale = Vector3.one;
        }
        
        Image img = obj.GetComponent<Image>();
        if (img != null) img.sprite = sprite;
        
        return obj;
    }

    private IEnumerator AnimateSingleParticleWithCountUp(Transform particle, Vector3 target, float duration, float increment, bool isCash)
    {
        Vector3 start = particle.position;
        float elapsed = 0f;
        bool applied = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            particle.position = Vector3.Lerp(start, target, movementCurve.Evaluate(t));
            
            if (!applied && t >= 0.8f)
            {
                if (isCash) currentAnimatedCash += increment;
                else currentAnimatedChips += Mathf.RoundToInt(increment);
                
                CurrencyDisplayUI.TriggerVisualAnimationTick(currentAnimatedChips, currentAnimatedCash);
                applied = true;
                if (SoundManager.instance != null)
                {
                    if (isCash) SoundManager.instance.PlayCash();
                    else SoundManager.instance.PlayChips();
                }
            }
            yield return null;
        }

        activeAnimations.Remove(particle.gameObject);
        Destroy(particle.gameObject);
    }

    public void ClosePanel()
    {
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(false);
            OnDailyRewardPanelClosed?.Invoke();
        }
    }
}
