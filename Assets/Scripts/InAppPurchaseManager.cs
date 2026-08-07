using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles shop purchases via Stripe Checkout (backend v2.7+).
/// Unity IAP (UnityEngine.Purchasing) is no longer used.
///
/// Flow:
///   Buy tap → POST /api/shop/checkout { itemId } → checkoutUrl
///   → Application.OpenURL(checkoutUrl)  [Chrome / in-app browser opens]
///   → Stripe hosted page → user pays
///   → Stripe redirects to blingodice://payment-success  (or /payment-cancel)
///   → Application.deepLinkActivated fires
///   → Wait 2s for webhook to credit wallet
///   → GET /api/wallet/balance → update PlayerModel → fire OnPurchaseSuccess
/// </summary>
public class InAppPurchaseManager : MonoBehaviour
{
    public static InAppPurchaseManager Instance { get; private set; }

    [Serializable]
    public class PurchasePackage
    {
        public float  price;
        public int    chipsAmount;
        public float  cashAmount;
        public float  cashBonus;
        public string productId; // itemId sent to POST /api/shop/checkout
    }

    // itemId constants — must match GET /api/shop/items values exactly
    public const string PRODUCT_ID_PACKAGE_1      = "package_1";
    public const string PRODUCT_ID_PACKAGE_2      = "package_2";
    public const string PRODUCT_ID_PACKAGE_3      = "package_3";
    public const string PRODUCT_ID_PACKAGE_4      = "package_4";
    public const string PRODUCT_ID_PACKAGE_5      = "package_5";
    public const string PRODUCT_ID_PACKAGE_6      = "package_6";
    public const string PRODUCT_ID_PREMIUM_VALUE  = "offer_premium_value";
    public const string PRODUCT_ID_BEST_OFFERS    = "offer_best_offers";
    public const string PRODUCT_ID_QUICK_BOOST    = "offer_quick_boost";
    public const string PRODUCT_ID_MEGA_BUNDLE    = "offer_mega_bundle";
    public const string PRODUCT_ID_POWER_DEAL     = "offer_power_deal";
    public const string PRODUCT_ID_BONUS_PLUS     = "offer_bonus_plus";
    public const string PRODUCT_ID_SMART_STARTER  = "offer_smart_starter";

    [Header("Purchase Packages (13 total: 6 lucky deals + 7 offers)")]
    [SerializeField] private PurchasePackage[] purchasePackages = new PurchasePackage[13]
    {
        // Lucky Deals (indices 0-5) — matches /api/shop/items type:lucky_deal
        new PurchasePackage { price = 3f,  chipsAmount = 50,   cashAmount = 3f,  cashBonus = 0.2f, productId = PRODUCT_ID_PACKAGE_1 },
        new PurchasePackage { price = 10f, chipsAmount = 200,  cashAmount = 10f, cashBonus = 2f,   productId = PRODUCT_ID_PACKAGE_2 },
        new PurchasePackage { price = 15f, chipsAmount = 350,  cashAmount = 15f, cashBonus = 2f,   productId = PRODUCT_ID_PACKAGE_3 },
        new PurchasePackage { price = 30f, chipsAmount = 400,  cashAmount = 30f, cashBonus = 6f,   productId = PRODUCT_ID_PACKAGE_4 },
        new PurchasePackage { price = 50f, chipsAmount = 1000, cashAmount = 50f, cashBonus = 10f,  productId = PRODUCT_ID_PACKAGE_5 },
        new PurchasePackage { price = 75f, chipsAmount = 1200, cashAmount = 75f, cashBonus = 25f,  productId = PRODUCT_ID_PACKAGE_6 },
        // Offers (indices 6-12) — matches /api/shop/items type:offer
        new PurchasePackage { price = 17f, chipsAmount = 220,  cashAmount = 17f, cashBonus = 4.2f, productId = PRODUCT_ID_PREMIUM_VALUE },
        new PurchasePackage { price = 6f,  chipsAmount = 140,  cashAmount = 3f,  cashBonus = 1.2f, productId = PRODUCT_ID_BEST_OFFERS  },
        new PurchasePackage { price = 3f,  chipsAmount = 120,  cashAmount = 6f,  cashBonus = 1.2f, productId = PRODUCT_ID_QUICK_BOOST  },
        new PurchasePackage { price = 8f,  chipsAmount = 80,   cashAmount = 8f,  cashBonus = 3.2f, productId = PRODUCT_ID_MEGA_BUNDLE  },
        new PurchasePackage { price = 5f,  chipsAmount = 70,   cashAmount = 5f,  cashBonus = 2.5f, productId = PRODUCT_ID_POWER_DEAL   },
        new PurchasePackage { price = 4f,  chipsAmount = 52,   cashAmount = 4f,  cashBonus = 1.2f, productId = PRODUCT_ID_BONUS_PLUS   },
        new PurchasePackage { price = 12f, chipsAmount = 160,  cashAmount = 12f, cashBonus = 2.4f, productId = PRODUCT_ID_SMART_STARTER },
    };

    [Header("Purchase Buttons (13 + 7 carousel duplicates = 20 total)")]
    [SerializeField] private Button[] purchaseButtons = new Button[20];

    [Header("UI")]
    [SerializeField] private PlayerProfileUI        playerProfileUI;
    [SerializeField] private GameObject             purchasePanel;
    [SerializeField] private PurchaseLoadingOverlay loadingOverlay;
    [SerializeField] private CurrencyFlyAnimator    currencyAnimator;

    // Events
    public event Action<int, float, string> OnPurchaseSuccess; // (chips, cash, productId)
    public event Action<string>             PurchaseFailed;
    public event Action                     OnPurchaseInitialized;

    private int   lastPurchasedPackageIndex  = -1;
    private bool  purchaseInProgress         = false;
    private bool  checkoutUrlOpened          = false;
    private bool  purchaseFromShopPanel      = false; // captured at tap time, not at completion
    private int   coinsBeforePurchase        = 0;
    private float cashBeforePurchase         = 0f;
    private Coroutine resumeCheckCoroutine   = null;

    private const string DEEP_LINK_SUCCESS     = "blingodice://open/payment-success";
    private const string DEEP_LINK_CANCEL      = "blingodice://open/payment-cancel";
    private const float  WEBHOOK_SETTLE_DELAY  = 2f;
    // How long to wait after app focus before concluding no deep link is coming
    private const float  RESUME_DEEP_LINK_GRACE = 1.5f;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetupPurchaseButtons();
        PlayerModel.OnDataUpdated += OnPlayerDataUpdated;

        Application.deepLinkActivated += OnDeepLinkActivated;
        // Handle cold-start deep link (app launched via blingodice:// URL while not running)
        if (!string.IsNullOrEmpty(Application.absoluteURL))
            OnDeepLinkActivated(Application.absoluteURL);

        OnPurchaseInitialized?.Invoke();
    }

    private void OnDestroy()
    {
        PlayerModel.OnDataUpdated     -= OnPlayerDataUpdated;
        Application.deepLinkActivated -= OnDeepLinkActivated;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !purchaseInProgress || !checkoutUrlOpened) return;
        TriggerResumeCheck();
    }

    private void OnApplicationPause(bool isPaused)
    {
        // OnApplicationFocus is unreliable on some Android devices/launchers.
        // OnApplicationPause(false) fires reliably when returning from Chrome.
        if (isPaused || !purchaseInProgress || !checkoutUrlOpened) return;
        TriggerResumeCheck();
    }

    private void TriggerResumeCheck()
    {
        // Give the deep link handler a short grace period to fire first.
        // If it fires, CancelResumeCheck() stops this coroutine.
        if (resumeCheckCoroutine != null) StopCoroutine(resumeCheckCoroutine);
        resumeCheckCoroutine = StartCoroutine(ResumeCheckCoroutine());
    }

    private IEnumerator ResumeCheckCoroutine()
    {
        yield return new WaitForSeconds(RESUME_DEEP_LINK_GRACE);
        resumeCheckCoroutine = null;

        if (!purchaseInProgress) yield break; // deep link already handled it

        // No deep link arrived — user closed Chrome (paid but deep link failed, or just dismissed).
        // Restore buttons and do a silent wallet refresh to catch any payment that went through.
        Debug.Log("[InAppPurchaseManager] App resumed with no payment deep link. Restoring buttons and refreshing wallet.");
        lastPurchasedPackageIndex = -1;
        purchaseInProgress        = false;
        checkoutUrlOpened         = false;
        loadingOverlay?.Hide();
        RestoreButtons();

        if (BackendAuthService.Instance != null)
        {
            BackendAuthService.Instance.GetWalletBalance((success, data) =>
            {
                if (success && data != null)
                {
                    PlayerModel.UpdateData(
                        PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
                        PlayerModel.Wins, PlayerModel.Losses, PlayerModel.Balance,
                        (int)data.cashBalance, (int)data.bonusBalance, (int)data.winningBalance,
                        data.coins, PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level,
                        PlayerModel.Role, PlayerModel.SelectedAvatarIndex,
                        PlayerModel.ActiveMatchId, PlayerModel.ActiveRoomId);
                    Debug.Log("<color=#00FF00>[InAppPurchaseManager] Wallet silently refreshed on resume.</color>");
                }
            });
        }
    }

    private void CancelResumeCheck()
    {
        if (resumeCheckCoroutine == null) return;
        StopCoroutine(resumeCheckCoroutine);
        resumeCheckCoroutine = null;
    }

    // ── Deep link handler ──────────────────────────────────────────────────────

    private void OnDeepLinkActivated(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        // A real deep link arrived — cancel the focus-based fallback timer
        CancelResumeCheck();

        if (url.Contains("open/payment-success"))
        {
            Debug.Log("<color=#00FF00>[InAppPurchaseManager] Deep link: payment-success</color>");
            loadingOverlay?.Show("Verifying payment...");
            StartCoroutine(HandlePaymentSuccessCoroutine());
        }
        else if (url.Contains("open/payment-cancel"))
        {
            Debug.Log("[InAppPurchaseManager] Deep link: payment-cancel");
            loadingOverlay?.Hide();
            purchaseInProgress        = false;
            lastPurchasedPackageIndex = -1;
            RestoreButtons();
            PurchaseFailed?.Invoke("Payment cancelled");
        }
    }

    private IEnumerator HandlePaymentSuccessCoroutine()
    {
        // Snapshot balance BEFORE the refresh so the animation can count up from the old value
        coinsBeforePurchase = PlayerModel.Coins;
        cashBeforePurchase  = PlayerModel.CashBalance;

        yield return new WaitForSeconds(WEBHOOK_SETTLE_DELAY);

        if (BackendAuthService.Instance == null)
        {
            FinalisePurchase();
            yield break;
        }

        BackendAuthService.Instance.GetWalletBalance((success, data) =>
        {
            if (success && data != null)
            {
                PlayerModel.UpdateData(
                    PlayerModel.Id, PlayerModel.Name, PlayerModel.Email,
                    PlayerModel.Wins, PlayerModel.Losses, PlayerModel.Balance,
                    (int)data.cashBalance, (int)data.bonusBalance, (int)data.winningBalance,
                    data.coins, PlayerModel.Avatar, PlayerModel.Xp, PlayerModel.Level,
                    PlayerModel.Role, PlayerModel.SelectedAvatarIndex,
                    PlayerModel.ActiveMatchId, PlayerModel.ActiveRoomId);

                Debug.Log("<color=#00FF00>[InAppPurchaseManager] Wallet refreshed after purchase.</color>");
            }
            else
            {
                BackendAuthService.Instance?.GetProfile(null);
            }

            FinalisePurchase();
        });
    }

    private void FinalisePurchase()
    {
        int  completedIndex       = lastPurchasedPackageIndex;
        bool wasShopPurchase      = purchaseFromShopPanel;
        lastPurchasedPackageIndex = -1;
        purchaseInProgress        = false;
        checkoutUrlOpened         = false;
        purchaseFromShopPanel     = false;
        loadingOverlay?.Hide();
        RestoreButtons();

        // Use the target icons that were active when the user tapped — not the current state.
        if (currencyAnimator != null && completedIndex >= 0 && completedIndex < purchaseButtons.Length)
        {
            RectTransform source = purchaseButtons[completedIndex]?.GetComponent<RectTransform>();

            int   coinsAdded = PlayerModel.Coins       - coinsBeforePurchase;
            float cashAdded  = PlayerModel.CashBalance - cashBeforePurchase;

            RectTransform coinsTarget = wasShopPurchase ? currencyAnimator.shopTargetCoinsPosition : null;
            RectTransform cashTarget  = wasShopPurchase ? currencyAnimator.shopTargetCashPosition  : null;

            if (source != null)
            {
                if (coinsAdded > 0) currencyAnimator.PlayCoins(source, coinsAdded, coinsBeforePurchase, coinsTarget);
                if (cashAdded  > 0) currencyAnimator.PlayCash (source, cashAdded,  cashBeforePurchase,  cashTarget);
            }
        }

        if (completedIndex >= 0 && completedIndex < purchasePackages.Length)
        {
            PurchasePackage pkg = purchasePackages[completedIndex];
            OnPurchaseSuccess?.Invoke(pkg.chipsAmount, pkg.cashAmount + pkg.cashBonus, pkg.productId);
        }
        else
        {
            OnPurchaseSuccess?.Invoke(0, 0f, string.Empty);
        }
    }

    // ── Purchase flow ──────────────────────────────────────────────────────────

    private void OnPurchaseButtonClicked(int packageIndex)
    {
        if (purchaseInProgress)
        {
            Debug.LogWarning("[InAppPurchaseManager] Purchase already in progress — ignoring tap.");
            return;
        }

        if (packageIndex < 0 || packageIndex >= purchasePackages.Length)
        {
            Debug.LogError($"[InAppPurchaseManager] Invalid package index: {packageIndex}");
            return;
        }

        if (BackendAuthService.Instance == null)
        {
            PurchaseFailed?.Invoke("Backend unavailable");
            return;
        }

        PurchasePackage pkg = purchasePackages[packageIndex];
        Debug.Log($"<color=#FFA500>[InAppPurchaseManager] Checkout tapped: {pkg.productId} (${pkg.price})</color>");

        purchaseInProgress        = true;
        lastPurchasedPackageIndex = packageIndex;
        purchaseFromShopPanel     = purchasePanel != null && purchasePanel.activeSelf;
        DisableButtons();
        loadingOverlay?.Show("Initialising transaction...");

        BackendAuthService.Instance.ShopCheckout(pkg.productId, (success, response) =>
        {
            if (success && response != null && !string.IsNullOrEmpty(response.checkoutUrl))
            {
                loadingOverlay?.UpdateStatus("Opening payment page...");
#if UNITY_EDITOR
                // In the editor, deep links never fire — log the URL so you can copy it,
                // then use the ContextMenu test methods below to simulate the result.
                Debug.Log($"<color=#FFD700>🔗 [IAP EDITOR] Stripe URL (copy into browser to pay):\n{response.checkoutUrl}\n\nThen right-click InAppPurchaseManager → Test: Simulate Payment Success\n(deep link: blingodice://open/payment-success)</color>");
                UnityEditor.EditorGUIUtility.systemCopyBuffer = response.checkoutUrl;
#else
                Application.OpenURL(response.checkoutUrl);
#endif
                checkoutUrlOpened = true;
                loadingOverlay?.Hide(); // Chrome is now open — hand off to Stripe
            }
            else
            {
                Debug.LogError($"[InAppPurchaseManager] ShopCheckout failed for {pkg.productId}: {response?.message}");
                loadingOverlay?.Hide();
                purchaseInProgress        = false;
                lastPurchasedPackageIndex = -1;
                RestoreButtons();
                PurchaseFailed?.Invoke("Could not start checkout. Please try again.");
            }
        });
    }

    // ── Button helpers ─────────────────────────────────────────────────────────

    private void SetupPurchaseButtons()
    {
        if (purchaseButtons == null) return;

        if (purchaseButtons.Length != 13 && purchaseButtons.Length != 20)
            Debug.LogWarning($"[InAppPurchaseManager] Expected 13 or 20 buttons, got {purchaseButtons.Length}.");

        // Auto-wire carousel duplicate buttons
        OffersCarouselManager carousel = FindFirstObjectByType<OffersCarouselManager>();
        if (carousel != null)
        {
            GameObject[] items = carousel.GetOfferItems();
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    int idx = (purchaseButtons.Length == 20) ? (13 + i) : (6 + i);
                    if (idx < purchaseButtons.Length && purchaseButtons[idx] == null && items[i] != null)
                    {
                        Button btn = items[i].GetComponentInChildren<Button>();
                        if (btn != null) purchaseButtons[idx] = btn;
                    }
                }
            }
        }

        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            if (purchaseButtons[i] == null) continue;
            int packageIndex = (i >= 13 && i < 20) ? (i - 7) : i;
            if (packageIndex < 0 || packageIndex >= purchasePackages.Length) continue;

            int captured = packageIndex;
            purchaseButtons[i].onClick.RemoveAllListeners();
            purchaseButtons[i].onClick.AddListener(() => OnPurchaseButtonClicked(captured));
        }
    }

    private void DisableButtons()
    {
        foreach (var btn in purchaseButtons)
            if (btn != null) btn.interactable = false;
    }

    private void RestoreButtons()
    {
        foreach (var btn in purchaseButtons)
            if (btn != null) btn.interactable = true;
    }

    // ── Public shorthand helpers (Inspector OnClick wiring) ───────────────────

    public void BuyPackage1()     => OnPurchaseButtonClicked(0);
    public void BuyPackage2()     => OnPurchaseButtonClicked(1);
    public void BuyPackage3()     => OnPurchaseButtonClicked(2);
    public void BuyPackage4()     => OnPurchaseButtonClicked(3);
    public void BuyPackage5()     => OnPurchaseButtonClicked(4);
    public void BuyPackage6()     => OnPurchaseButtonClicked(5);
    public void BuyPremiumValue() => OnPurchaseButtonClicked(6);
    public void BuyBestOffers()   => OnPurchaseButtonClicked(7);
    public void BuyQuickBoost()   => OnPurchaseButtonClicked(8);
    public void BuyMegaBundle()   => OnPurchaseButtonClicked(9);
    public void BuyPowerDeal()    => OnPurchaseButtonClicked(10);
    public void BuyBonusPlus()    => OnPurchaseButtonClicked(11);
    public void BuySmartStarter() => OnPurchaseButtonClicked(12);

    // ── UI helpers ─────────────────────────────────────────────────────────────

    private void OnPlayerDataUpdated() => UpdateAllUIDisplays();

    private void UpdateAllUIDisplays()
    {
        if (playerProfileUI != null)
            CurrencyDisplayUI.TriggerVisualAnimationTick(PlayerModel.Coins, PlayerModel.CashBalance);
    }

    public void RefreshAllUI()                        => UpdateAllUIDisplays();
    public void SetPlayerProfileUI(PlayerProfileUI ui) => playerProfileUI = ui;
    public void SetPurchasePanelActive(bool active)
    {
        if (purchasePanel != null) purchasePanel.SetActive(active);
    }

    public PurchasePackage GetPurchasePackage(int index) =>
        (index >= 0 && index < purchasePackages.Length) ? purchasePackages[index] : null;

#if UNITY_EDITOR
    // ── Editor-only test helpers ───────────────────────────────────────────────
    // Right-click the InAppPurchaseManager component in the Inspector to use these.
    //
    // Typical test flow:
    //   1. Enter Play mode.
    //   2. Right-click → "Test: Start Checkout (Package 1)" — calls real API, URL is
    //      copied to clipboard automatically and printed in Console.
    //   3. Paste URL in a browser to complete (or skip) the Stripe payment page.
    //   4. Right-click → "Test: Simulate Payment Success" (or Cancel) to trigger the
    //      deep link callback that would normally come from blingodice://.

    [ContextMenu("Test: Start Checkout (Package 1 — $3)")]
    private void EditorTestCheckoutPackage1() => OnPurchaseButtonClicked(0);

    [ContextMenu("Test: Start Checkout (Quick Boost — $3)")]
    private void EditorTestCheckoutQuickBoost() => OnPurchaseButtonClicked(8);

    [ContextMenu("Test: Simulate Payment Success")]
    private void EditorSimulateSuccess() => OnDeepLinkActivated(DEEP_LINK_SUCCESS);

    [ContextMenu("Test: Simulate Payment Cancel")]
    private void EditorSimulateCancel() => OnDeepLinkActivated(DEEP_LINK_CANCEL);

    [ContextMenu("Test: Simulate Currency Animation (50 coins + $5 cash)")]
    private void EditorTestAnimation()
    {
        if (currencyAnimator == null)
        {
            Debug.LogWarning("[IAP] currencyAnimator is not assigned — cannot test animation.");
            return;
        }

        RectTransform source = null;
        foreach (var btn in purchaseButtons)
        {
            if (btn != null) { source = btn.GetComponent<RectTransform>(); break; }
        }

        if (source == null)
        {
            Debug.LogWarning("[IAP] No purchase button found for source position — animation skipped.");
            return;
        }

        // Mirror the real purchase logic: use shop targets if the shop panel is active
        bool fromShop         = purchasePanel != null && purchasePanel.activeSelf;
        RectTransform coinsTgt = fromShop ? currencyAnimator.shopTargetCoinsPosition : null;
        RectTransform cashTgt  = fromShop ? currencyAnimator.shopTargetCashPosition  : null;

        int   fakeCoins = 50;
        float fakeCash  = 5f;
        Debug.Log($"<color=#FFD700>[IAP EDITOR] Animation: +{fakeCoins} coins +${fakeCash} from '{source.name}' → {(fromShop ? "SHOP targets" : "DEFAULT targets")}</color>");
        Debug.Log($"<color=#FFD700>[IAP EDITOR] coinsTarget={coinsTgt?.name ?? "default"} cashTarget={cashTgt?.name ?? "default"}</color>");

        currencyAnimator.PlayCoins(source, fakeCoins, PlayerModel.Coins, coinsTgt);
        currencyAnimator.PlayCash (source, fakeCash,  PlayerModel.CashBalance, cashTgt);
    }

    [ContextMenu("Test: Print Current Purchase State")]
    private void EditorPrintState()
    {
        Debug.Log($"[IAP State] inProgress={purchaseInProgress} | lastPackageIdx={lastPurchasedPackageIndex} | " +
                  $"coins={PlayerModel.Coins} | cash={PlayerModel.CashBalance}");
    }
#endif
}
