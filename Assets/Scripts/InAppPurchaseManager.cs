using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

/// <summary>
/// Handles Unity IAP setup and purchases for Dice Rush.
/// Manages 13 purchase packages: 6 regular packages + 7 offer panel packages.
/// Each package gives chips (coins) and cash rewards (with optional cash bonus).
/// Updates UI displays in main menu, gameplay, and winner screen.
/// </summary>
#if UNITY_PURCHASING
public class InAppPurchaseManager : MonoBehaviour, IDetailedStoreListener
#else
public class InAppPurchaseManager : MonoBehaviour
#endif
{
    public static InAppPurchaseManager Instance { get; private set; }

    [System.Serializable]
    public class PurchasePackage
    {
        public int chipsAmount; // Chips (coins) to give
        public float cashAmount; // Cash to give
        public float cashBonus; // Cash bonus to give (added to cashAmount)
        public string productId; // Product ID for IAP - MUST match Unity IAP Catalog
    }

    // Product IDs - Regular Packages (MUST match the Unity IAP Catalog)
    public const string PRODUCT_ID_PACKAGE_1 = "package_1";
    public const string PRODUCT_ID_PACKAGE_2 = "package_2";
    public const string PRODUCT_ID_PACKAGE_3 = "package_3";
    public const string PRODUCT_ID_PACKAGE_4 = "package_4";
    public const string PRODUCT_ID_PACKAGE_5 = "package_5";
    public const string PRODUCT_ID_PACKAGE_6 = "package_6";

    // Product IDs - Offer Panel Packages (MUST match the Unity IAP Catalog)
    public const string PRODUCT_ID_PREMIUM_VALUE = "offer_premium_value";
    public const string PRODUCT_ID_BEST_OFFERS = "offer_best_offers";
    public const string PRODUCT_ID_QUICK_BOOST = "offer_quick_boost";
    public const string PRODUCT_ID_MEGA_BUNDLE = "offer_mega_bundle";
    public const string PRODUCT_ID_POWER_DEAL = "offer_power_deal";
    public const string PRODUCT_ID_BONUS_PLUS = "offer_bonus_plus";
    public const string PRODUCT_ID_SMART_STARTER = "offer_smart_starter";

    [Header("Purchase Packages (13 total: 6 regular + 7 offer panels)")]
    [SerializeField] private PurchasePackage[] purchasePackages = new PurchasePackage[13]
    {
        // Regular Packages (0-5)
        new PurchasePackage { chipsAmount = 50, cashAmount = 0.2f, cashBonus = 0f, productId = PRODUCT_ID_PACKAGE_1 },
        new PurchasePackage { chipsAmount = 200, cashAmount = 2f, cashBonus = 0f, productId = PRODUCT_ID_PACKAGE_2 },
        new PurchasePackage { chipsAmount = 350, cashAmount = 2f, cashBonus = 0f, productId = PRODUCT_ID_PACKAGE_3 },
        new PurchasePackage { chipsAmount = 400, cashAmount = 6f, cashBonus = 0f, productId = PRODUCT_ID_PACKAGE_4 },
        new PurchasePackage { chipsAmount = 1000, cashAmount = 10f, cashBonus = 0f, productId = PRODUCT_ID_PACKAGE_5 },
        new PurchasePackage { chipsAmount = 1200, cashAmount = 25f, cashBonus = 0f, productId = PRODUCT_ID_PACKAGE_6 },
        
        // Offer Panel Packages (6-12)
        new PurchasePackage { chipsAmount = 220, cashAmount = 17f, cashBonus = 4.2f, productId = PRODUCT_ID_PREMIUM_VALUE },      // Premium value: $17
        new PurchasePackage { chipsAmount = 140, cashAmount = 3f, cashBonus = 1.2f, productId = PRODUCT_ID_BEST_OFFERS },         // Best offers: $3
        new PurchasePackage { chipsAmount = 120, cashAmount = 6f, cashBonus = 1.2f, productId = PRODUCT_ID_QUICK_BOOST },         // Quick Boost: $6
        new PurchasePackage { chipsAmount = 80, cashAmount = 8f, cashBonus = 3.2f, productId = PRODUCT_ID_MEGA_BUNDLE },         // Mega Bundle: $8
        new PurchasePackage { chipsAmount = 70, cashAmount = 5f, cashBonus = 2.5f, productId = PRODUCT_ID_POWER_DEAL },          // Power deal: $5
        new PurchasePackage { chipsAmount = 52, cashAmount = 4f, cashBonus = 1.2f, productId = PRODUCT_ID_BONUS_PLUS },         // Bonus Plus: $4
        new PurchasePackage { chipsAmount = 160, cashAmount = 12f, cashBonus = 2.4f, productId = PRODUCT_ID_SMART_STARTER }      // Smart Starter: $12
    };

    [Header("Purchase Buttons (13 total: 6 regular + 7 offer panels)")]
    [SerializeField] private Button[] purchaseButtons = new Button[13]; // 13 purchase buttons

    [Header("UI Manager Reference")]
    [SerializeField] private PlayerProfileUI playerProfileUI; // Reference to PlayerProfileUI for updating all currency displays

    [Header("Purchase Panel")]
    [SerializeField] private GameObject purchasePanel; // Optional: Panel that contains purchase buttons

    // Events
    public event Action<int, float, string> OnPurchaseSuccess; // chips, cash, productId
    public event Action<string> PurchaseFailed;
    public event Action OnPurchaseInitialized;

#if UNITY_PURCHASING
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    private bool isInitialized = false;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_PURCHASING
        InitializePurchasing();
#endif
    }

    private void Start()
    {
        // Setup button listeners
        SetupPurchaseButtons();
        
        // Subscribe to PlayerModel data updates to refresh UI
        PlayerModel.OnDataUpdated += OnPlayerDataUpdated;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        PlayerModel.OnDataUpdated -= OnPlayerDataUpdated;
    }

    /// <summary>
    /// Sets up purchase button listeners
    /// </summary>
    private void SetupPurchaseButtons()
    {
        if (purchaseButtons == null || purchaseButtons.Length != 13)
        {
            Debug.LogWarning("[InAppPurchaseManager] Purchase buttons array is not properly set up! Expected 13 buttons.");
            return;
        }

        for (int i = 0; i < purchaseButtons.Length && i < purchasePackages.Length; i++)
        {
            int packageIndex = i; // Capture index for closure
            if (purchaseButtons[i] != null)
            {
                purchaseButtons[i].onClick.RemoveAllListeners();
                purchaseButtons[i].onClick.AddListener(() => OnPurchaseButtonClicked(packageIndex));
                float totalCash = purchasePackages[packageIndex].cashAmount + purchasePackages[packageIndex].cashBonus;
                Debug.Log($"[InAppPurchaseManager] Setup purchase button {i + 1}: {purchasePackages[packageIndex].chipsAmount} chips + {totalCash} cash (base: {purchasePackages[packageIndex].cashAmount} + bonus: {purchasePackages[packageIndex].cashBonus})");
            }
            else
            {
                Debug.LogWarning($"[InAppPurchaseManager] Purchase button {i + 1} is null!");
            }
        }
    }

    /// <summary>
    /// Called when a purchase button is clicked
    /// </summary>
    private void OnPurchaseButtonClicked(int packageIndex)
    {
        if (packageIndex < 0 || packageIndex >= purchasePackages.Length)
        {
            Debug.LogError($"[InAppPurchaseManager] Invalid package index: {packageIndex}");
            return;
        }

        PurchasePackage package = purchasePackages[packageIndex];
        Debug.Log($"[InAppPurchaseManager] Purchase button {packageIndex + 1} clicked! Package: {package.chipsAmount} chips + {package.cashAmount} cash");

#if UNITY_PURCHASING
        // Use Unity IAP if initialized
        if (IsInitialized())
        {
            BuyProductID(package.productId);
        }
        else
        {
            Debug.LogError("[InAppPurchaseManager] Unity IAP not initialized. Cannot purchase.");
            PurchaseFailed?.Invoke("IAP not initialized");
        }
#else
        // Unity IAP not available - use direct purchase for testing
        Debug.LogWarning("[InAppPurchaseManager] Unity IAP not available. Using direct purchase mode.");
        ProcessPurchaseDirectly(package);
#endif
    }

    // Public methods for direct purchase calls - Regular Packages
    public void BuyPackage1() => OnPurchaseButtonClicked(0);
    public void BuyPackage2() => OnPurchaseButtonClicked(1);
    public void BuyPackage3() => OnPurchaseButtonClicked(2);
    public void BuyPackage4() => OnPurchaseButtonClicked(3);
    public void BuyPackage5() => OnPurchaseButtonClicked(4);
    public void BuyPackage6() => OnPurchaseButtonClicked(5);

    // Public methods for direct purchase calls - Offer Panel Packages
    public void BuyPremiumValue() => OnPurchaseButtonClicked(6);      // Premium value
    public void BuyBestOffers() => OnPurchaseButtonClicked(7);         // Best offers
    public void BuyQuickBoost() => OnPurchaseButtonClicked(8);         // Quick Boost
    public void BuyMegaBundle() => OnPurchaseButtonClicked(9);         // Mega Bundle
    public void BuyPowerDeal() => OnPurchaseButtonClicked(10);         // Power deal
    public void BuyBonusPlus() => OnPurchaseButtonClicked(11);         // Bonus Plus
    public void BuySmartStarter() => OnPurchaseButtonClicked(12);      // Smart Starter

#if UNITY_PURCHASING
    /// <summary>
    /// Initialize Unity IAP
    /// </summary>
    private void InitializePurchasing()
    {
        if (IsInitialized())
        {
            OnPurchaseInitialized?.Invoke();
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Add all products to the builder
        for (int i = 0; i < purchasePackages.Length; i++)
        {
            if (!string.IsNullOrEmpty(purchasePackages[i].productId))
            {
                builder.AddProduct(purchasePackages[i].productId, ProductType.Consumable);
                Debug.Log($"[InAppPurchaseManager] Added product to IAP: {purchasePackages[i].productId}");
            }
        }

        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[InAppPurchaseManager] Initializing Unity IAP...");
    }

    /// <summary>
    /// Check if Unity IAP is initialized
    /// </summary>
    private bool IsInitialized()
    {
        return isInitialized && storeController != null && extensionProvider != null;
    }

    /// <summary>
    /// Buy a product by ID
    /// </summary>
    private void BuyProductID(string productId)
    {
        if (!IsInitialized())
        {
            Debug.LogError("[InAppPurchaseManager] Store not initialized. Cannot purchase.");
            PurchaseFailed?.Invoke("Store not initialized");
            return;
        }

        Product product = storeController.products.WithID(productId);
        if (product != null && product.availableToPurchase)
        {
            Debug.Log($"[InAppPurchaseManager] Purchasing product: {productId}");
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogError($"[InAppPurchaseManager] Product {productId} not available for purchase");
            PurchaseFailed?.Invoke($"Product {productId} not available");
        }
    }

    /// <summary>
    /// Restore previous purchases (for iOS/Apple and Android devices)
    /// </summary>
    public void RestorePurchases()
    {
        if (!IsInitialized())
        {
            Debug.LogError("[InAppPurchaseManager] Store not initialized. Cannot restore purchases.");
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("[InAppPurchaseManager] Restoring purchases...");
            extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestoreResult);
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("[InAppPurchaseManager] Restoring purchases...");
            extensionProvider.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(OnRestoreResult);
        }
        else
        {
            Debug.LogWarning("[InAppPurchaseManager] Restore not supported on this platform");
        }
    }

    private void OnRestoreResult(bool success, string message)
    {
        if (success)
        {
            Debug.Log($"[InAppPurchaseManager] Restore purchases successful: {message}");
        }
        else
        {
            Debug.LogWarning($"[InAppPurchaseManager] Restore purchases failed: {message}");
        }
    }

    // IDetailedStoreListener implementation
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("[InAppPurchaseManager] IAP initialized successfully");
        storeController = controller;
        extensionProvider = extensions;
        isInitialized = true;
        OnPurchaseInitialized?.Invoke();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[InAppPurchaseManager] IAP initialization failed: {error}");
        PurchaseFailed?.Invoke($"Initialization failed: {error}");
        isInitialized = false;
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[InAppPurchaseManager] IAP initialization failed: {error} - {message}");
        PurchaseFailed?.Invoke($"Initialization failed: {error} - {message}");
        isInitialized = false;
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[InAppPurchaseManager] Purchase successful: {productId}");

        // Find the package that matches this product ID
        PurchasePackage package = null;
        for (int i = 0; i < purchasePackages.Length; i++)
        {
            if (purchasePackages[i].productId == productId)
            {
                package = purchasePackages[i];
                break;
            }
        }

        if (package == null)
        {
            Debug.LogError($"[InAppPurchaseManager] Unknown product ID: {productId}");
            PurchaseFailed?.Invoke($"Unknown product: {productId}");
            return PurchaseProcessingResult.Complete;
        }

        // Grant rewards
        GrantPurchaseRewards(package);
        float totalCash = package.cashAmount + package.cashBonus;
        OnPurchaseSuccess?.Invoke(package.chipsAmount, totalCash, productId);
        
        return PurchaseProcessingResult.Complete;
    }

    // IStoreListener implementation
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"[InAppPurchaseManager] Purchase failed: {product.definition.id} - {failureReason}");
        PurchaseFailed?.Invoke(failureReason.ToString());
    }

    // IDetailedStoreListener implementation
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogError($"[InAppPurchaseManager] Purchase failed: {product.definition.id} - {failureDescription.reason} - {failureDescription.message}");
        PurchaseFailed?.Invoke(failureDescription.message);
    }

    /// <summary>
    /// Get product price by product ID
    /// </summary>
    public string GetProductPrice(string productId)
    {
        if (IsInitialized())
        {
            Product product = storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                return product.metadata.localizedPriceString;
            }
        }

        return "$0.00";
    }

    /// <summary>
    /// Check if a product is available for purchase
    /// </summary>
    public bool IsProductAvailable(string productId)
    {
        if (!IsInitialized())
            return false;

        Product product = storeController.products.WithID(productId);
        return product != null && product.availableToPurchase;
    }
#endif

    /// <summary>
    /// Processes the purchase directly (for testing or when IAP is not available)
    /// </summary>
    private void ProcessPurchaseDirectly(PurchasePackage package)
    {
        GrantPurchaseRewards(package);
        float totalCash = package.cashAmount + package.cashBonus;
        OnPurchaseSuccess?.Invoke(package.chipsAmount, totalCash, package.productId);
    }

    /// <summary>
    /// Grants the rewards for a purchase (chips and cash)
    /// </summary>
    private void GrantPurchaseRewards(PurchasePackage package)
    {
        // In clean architecture, we notify the backend or update the local model
        // For now, we'll log it and refresh UI. The server should ideally be notified via API.
        
        float totalCash = package.cashAmount + package.cashBonus;
        Debug.Log($"[InAppPurchaseManager] Granting Rewards: {package.chipsAmount} chips + {totalCash} cash");

        // Update all UI displays using PlayerProfileUI
        UpdateAllUIDisplays();
    }

    /// <summary>
    /// Updates all UI displays (main menu, gameplay, winner screen) using PlayerProfileUI
    /// </summary>
    private void UpdateAllUIDisplays()
    {
        int chips = PlayerModel.Coins; 
        float cash = PlayerModel.CashBalance; 

        // Update all UI displays using PlayerProfileUI
        if (playerProfileUI != null)
        {
            CurrencyDisplayUI.TriggerVisualAnimationTick(chips, cash);
        }
        else
        {
            Debug.LogWarning("[InAppPurchaseManager] PlayerProfileUI reference is null! Cannot update UI displays. Assign it in Inspector.");
        }
    }

    /// <summary>
    /// Called when player data is updated
    /// </summary>
    private void OnPlayerDataUpdated()
    {
        UpdateAllUIDisplays();
    }

    /// <summary>
    /// Manually refresh all UI displays
    /// Call this if UI is not updating automatically
    /// </summary>
    public void RefreshAllUI()
    {
        UpdateAllUIDisplays();
    }
    
    /// <summary>
    /// Set the PlayerProfileUI reference
    /// </summary>
    public void SetPlayerProfileUI(PlayerProfileUI ui)
    {
        playerProfileUI = ui;
    }

    /// <summary>
    /// Get purchase package info by index
    /// </summary>
    public PurchasePackage GetPurchasePackage(int index)
    {
        if (index >= 0 && index < purchasePackages.Length)
        {
            return purchasePackages[index];
        }
        return null;
    }

    /// <summary>
    /// Enable or disable purchase panel
    /// </summary>
    public void SetPurchasePanelActive(bool active)
    {
        if (purchasePanel != null)
        {
            purchasePanel.SetActive(active);
        }
    }
}
