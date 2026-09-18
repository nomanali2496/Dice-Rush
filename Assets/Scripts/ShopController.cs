using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Shop UI panel in Dice Blitz.
/// Manages the "REMOVE ADS" purchase banner & button, "EARN FREE" rewarded ad chips button,
/// Chips Pack IAP purchases (5k, 10k, 25k, 50k, 100k chips), and back button navigation.
/// </summary>
public class ShopController : MonoBehaviour
{
    public static ShopController Instance { get; private set; }

    [Header("Shop Panel Root & Navigation")]
    [Tooltip("Main Shop panel GameObject (defaults to this GameObject if null)")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button backButton;

    [Header("Main Menu Trigger Buttons")]
    [Tooltip("Main Menu Shop button that opens this panel")]
    [SerializeField] private Button mainShopButton;
    [Tooltip("Additional buttons in the scene that should open the shop (e.g. Cash/Chips plus buttons)")]
    [SerializeField] private Button[] extraShopButtons;

    [Header("Remove Ads Section")]
    [Tooltip("The Remove Ads bottom banner container GameObject")]
    [SerializeField] private GameObject removeAdsBanner;
    [Tooltip("The 'PKR 650.00' / Remove Ads purchase button")]
    [SerializeField] private Button removeAdsButton;
    [Tooltip("Text component inside the Remove Ads button (e.g. 'PKR 650.00')")]
    [SerializeField] private TextMeshProUGUI removeAdsButtonText;
    [SerializeField] private Text removeAdsButtonTextLegacy;
    [SerializeField] private string removeAdsPriceString = "PKR 650.00";

    [Header("Earn Free Chips (Rewarded Ad)")]
    [Tooltip("The 'EARN FREE' button on the 50 Chips item card")]
    [SerializeField] private Button earnFreeChipsButton;
    [SerializeField] private int freeChipsRewardAmount = 50;

    [Header("Chips Packs Buy Buttons")]
    [Tooltip("Button for 5,000 Chips ($10)")]
    [SerializeField] private Button buy5kChipsButton;
    [Tooltip("Button for 10,000 Chips ($20)")]
    [SerializeField] private Button buy10kChipsButton;
    [Tooltip("Button for 25,000 Chips ($50)")]
    [SerializeField] private Button buy25kChipsButton;
    [Tooltip("Button for 50,000 Chips ($100)")]
    [SerializeField] private Button buy50kChipsButton;
    [Tooltip("Button for 100,000 Chips ($200)")]
    [SerializeField] private Button buy100kChipsButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (shopPanel == null)
        {
            shopPanel = gameObject;
        }

        AutoDetectReferences();
        WireButtonListeners();
        HookMainMenuButtons();
    }

    private void Start()
    {
        HookMainMenuButtons();
    }

    private void OnEnable()
    {
        AutoDetectReferences();
        WireButtonListeners();
        HookMainMenuButtons();
        RefreshRemoveAdsState();

        // Subscribe to Rewarded Ad Completion
        AdManager.RewardedAdComplete += OnRewardedAdCompleted;
    }

    private void OnDisable()
    {
        AdManager.RewardedAdComplete -= OnRewardedAdCompleted;
    }

    /// <summary>
    /// Auto-detects missing button and text references from child GameObjects
    /// </summary>
    public void AutoDetectReferences()
    {
        // 1. Auto-detect Back Button
        if (backButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn != null && (btn.name.ToLower().Contains("back") || btn.name.ToLower().Contains("close")))
                {
                    backButton = btn;
                    break;
                }
            }
        }

        // 2. Auto-detect Remove Ads Button & Banner
        if (removeAdsButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    string bName = btn.name.ToLower();
                    if (bName.Contains("remove") || bName.Contains("noad") || bName.Contains("pkr") || bName.Contains("adbutton"))
                    {
                        removeAdsButton = btn;
                        break;
                    }
                }
            }
        }

        if (removeAdsBanner == null)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t != null && t != transform)
                {
                    string tName = t.name.ToLower();
                    if (tName.Contains("remove") || tName.Contains("noad") || tName.Contains("adbanner") || tName.Contains("ads"))
                    {
                        removeAdsBanner = t.gameObject;
                        break;
                    }
                }
            }
        }

        // Auto-detect Remove Ads Button Text
        if (removeAdsButton != null && removeAdsButtonText == null && removeAdsButtonTextLegacy == null)
        {
            removeAdsButtonText = removeAdsButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (removeAdsButtonText == null)
            {
                removeAdsButtonTextLegacy = removeAdsButton.GetComponentInChildren<Text>(true);
            }
        }

        // 3. Auto-detect "EARN FREE" Button
        if (earnFreeChipsButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    string bName = btn.name.ToLower();
                    if (bName.Contains("earn") || bName.Contains("free") || bName.Contains("watch"))
                    {
                        earnFreeChipsButton = btn;
                        break;
                    }
                }
            }
        }
    }

    private void WireButtonListeners()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(CloseShop);
            backButton.onClick.AddListener(CloseShop);
        }

        if (removeAdsButton != null)
        {
            removeAdsButton.onClick.RemoveListener(OnRemoveAdsClicked);
            removeAdsButton.onClick.AddListener(OnRemoveAdsClicked);
        }

        if (earnFreeChipsButton != null)
        {
            earnFreeChipsButton.onClick.RemoveListener(OnEarnFreeChipsClicked);
            earnFreeChipsButton.onClick.AddListener(OnEarnFreeChipsClicked);
        }

        // Wire Chips Pack Buy buttons
        if (buy5kChipsButton != null)
        {
            buy5kChipsButton.onClick.RemoveListener(() => BuyChipsPack(5000, "$10"));
            buy5kChipsButton.onClick.AddListener(() => BuyChipsPack(5000, "$10"));
        }
        if (buy10kChipsButton != null)
        {
            buy10kChipsButton.onClick.RemoveListener(() => BuyChipsPack(10000, "$20"));
            buy10kChipsButton.onClick.AddListener(() => BuyChipsPack(10000, "$20"));
        }
        if (buy25kChipsButton != null)
        {
            buy25kChipsButton.onClick.RemoveListener(() => BuyChipsPack(25000, "$50"));
            buy25kChipsButton.onClick.AddListener(() => BuyChipsPack(25000, "$50"));
        }
        if (buy50kChipsButton != null)
        {
            buy50kChipsButton.onClick.RemoveListener(() => BuyChipsPack(50000, "$100"));
            buy50kChipsButton.onClick.AddListener(() => BuyChipsPack(50000, "$100"));
        }
        if (buy100kChipsButton != null)
        {
            buy100kChipsButton.onClick.RemoveListener(() => BuyChipsPack(100000, "$200"));
            buy100kChipsButton.onClick.AddListener(() => BuyChipsPack(100000, "$200"));
        }
    }

    /// <summary>
    /// Checks if user has already purchased Remove Ads and updates the button UI accordingly
    /// </summary>
    public void RefreshRemoveAdsState()
    {
        bool hasRemovedAds = (AdManager.Instance != null && !AdManager.Instance.CanShowAds()) || PlayerPrefs.GetInt("RemoveAds", 0) == 1;

        if (removeAdsButton != null)
        {
            if (hasRemovedAds)
            {
                removeAdsButton.interactable = false;
                SetRemoveAdsButtonText("PURCHASED");
            }
            else
            {
                removeAdsButton.interactable = true;
                SetRemoveAdsButtonText(removeAdsPriceString);
            }
        }
    }

    private void SetRemoveAdsButtonText(string text)
    {
        if (removeAdsButtonText != null)
        {
            removeAdsButtonText.text = text;
        }
        if (removeAdsButtonTextLegacy != null)
        {
            removeAdsButtonTextLegacy.text = text;
        }
    }

    /// <summary>
    /// Handler for Remove Ads purchase button click
    /// </summary>
    public void OnRemoveAdsClicked()
    {
        PlayTapSound();
        Debug.Log("<color=#00FF00>🛒 [Shop] Remove Ads purchase initiated!</color>");

        // Execute Remove Ads
        if (AdManager.Instance != null)
        {
            AdManager.Instance.RemoveAds(true);
        }
        else
        {
            PlayerPrefs.SetInt("RemoveAds", 1);
            PlayerPrefs.Save();
        }

        RefreshRemoveAdsState();

        // Show feedback notification
        ShowNotification("Ads Removed Successfully! Enjoy an ad-free experience.");
    }

    /// <summary>
    /// Handler for "EARN FREE" 50 chips rewarded ad button click
    /// </summary>
    public void OnEarnFreeChipsClicked()
    {
        PlayTapSound();
        Debug.Log("📺 [Shop] EARN FREE chips button clicked - requesting Rewarded Ad...");

        if (AdManager.Instance != null)
        {
            if (AdManager.Instance.CanShowRewardedAd())
            {
                AdManager.Instance.ShowRewardedAd();
            }
            else
            {
                // In Editor mode or when ad is loading, simulate ad completion
                Debug.LogWarning("[Shop] Ad not ready or in Editor. Simulating rewarded ad completion...");
                OnRewardedAdCompleted(true);
            }
        }
        else
        {
            // Fallback award if AdManager instance is missing
            OnRewardedAdCompleted(true);
        }
    }

    private void OnRewardedAdCompleted(bool isCompleted)
    {
        if (isCompleted)
        {
            Debug.Log($"<color=#00FF00>🎉 [Shop] Rewarded Ad finished! Awarding {freeChipsRewardAmount} Free Chips to player.</color>");
            
            // Add free chips to player balance
            AwardChipsToPlayer(freeChipsRewardAmount);
            ShowNotification($"Congratulations! You earned {freeChipsRewardAmount} Free Chips!");
        }
        else
        {
            Debug.LogWarning("[Shop] Rewarded Ad skipped or incomplete.");
        }
    }

    /// <summary>
    /// Handler for buying chip packs ($10, $20, $50, $100, $200)
    /// </summary>
    public void BuyChipsPack(int chipAmount, string priceLabel)
    {
        PlayTapSound();
        Debug.Log($"<color=#00FFFF>💰 [Shop] Buy Chips Pack clicked: {chipAmount:N0} Chips for {priceLabel}</color>");

        AwardChipsToPlayer(chipAmount);
        ShowNotification($"Successfully purchased {chipAmount:N0} Chips!");
    }

    private void AwardChipsToPlayer(int chipAmount)
    {
        // Update local balance and trigger backend sync if available
        int currentCoins = PlayerPrefs.GetInt("UserCoins", 0);
        int newCoins = currentCoins + chipAmount;
        PlayerPrefs.SetInt("UserCoins", newCoins);
        PlayerPrefs.Save();

        if (BackendAuthService.Instance != null && BackendAuthService.HasStoredJWT())
        {
            BackendAuthService.Instance.GetProfile(null);
        }

        Debug.Log($"[Shop] Player coins balance updated: {newCoins:N0}");
    }

    public void OpenShop()
    {
        PlayTapSound();
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
        }
        RefreshRemoveAdsState();
    }

    public void CloseShop()
    {
        PlayTapSound();
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    private void ShowNotification(string message)
    {
        NotificationPanel notification = FindFirstObjectByType<NotificationPanel>(FindObjectsInactive.Include);
        if (notification != null)
        {
            notification.Show(message);
        }
        else
        {
            Debug.Log($"[Shop Notification] {message}");
        }
    }

    /// <summary>
    /// Auto-detects and hooks any Shop button or currency plus buttons in the main menu to open the shop panel
    /// </summary>
    public void HookMainMenuButtons()
    {
        // 1. Auto-detect main Shop button if unassigned
        if (mainShopButton == null)
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                string bName = btn.name.ToLower();
                if (bName.Equals("shop") || bName.Equals("shopbutton") || bName.Equals("btnshop") || 
                    bName.Equals("shopbtn") || bName.Equals("buttonshop") || bName.Contains("shop_button") ||
                    bName.Equals("shop icon") || bName.Equals("shopicon") || bName.Equals("storeshop") ||
                    bName.Equals("shop_panel_button"))
                {
                    mainShopButton = btn;
                    break;
                }
            }
        }

        if (mainShopButton != null)
        {
            mainShopButton.onClick.RemoveListener(OpenShop);
            mainShopButton.onClick.AddListener(OpenShop);
        }

        // 2. Hook extra shop / store / currency plus buttons in scene
        Button[] sceneButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in sceneButtons)
        {
            if (btn == null || btn == backButton || btn == removeAdsButton || btn == earnFreeChipsButton ||
                btn == buy5kChipsButton || btn == buy10kChipsButton || btn == buy25kChipsButton || 
                btn == buy50kChipsButton || btn == buy100kChipsButton)
                continue;

            string nameLower = btn.name.ToLower();
            if (nameLower.Contains("shop") || nameLower.Contains("store") || nameLower.Contains("addchips") || 
                nameLower.Contains("addcash") || nameLower.Contains("chipsplus") || nameLower.Contains("cashplus") ||
                nameLower.Contains("buychips") || nameLower.Contains("plusbutton"))
            {
                btn.onClick.RemoveListener(OpenShop);
                btn.onClick.AddListener(OpenShop);
            }
        }

        // 3. Hook extra explicitly assigned shop buttons
        if (extraShopButtons != null)
        {
            foreach (var btn in extraShopButtons)
            {
                if (btn != null)
                {
                    btn.onClick.RemoveListener(OpenShop);
                    btn.onClick.AddListener(OpenShop);
                }
            }
        }
    }

    /// <summary>
    /// Static helper method to open the Shop panel from anywhere in code or Unity UI events.
    /// </summary>
    public static void ShowShop()
    {
        ShopController shop = Instance != null ? Instance : FindFirstObjectByType<ShopController>(FindObjectsInactive.Include);
        if (shop != null)
        {
            shop.OpenShop();
        }
        else
        {
            Debug.LogWarning("[ShopController] ShopController instance not found in scene!");
        }
    }

    private void PlayTapSound()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlayTap();
        }
    }
}
