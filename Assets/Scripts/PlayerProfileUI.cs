using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays and manages player profile information
/// </summary>
public class PlayerProfileUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI emailText;
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private Image profileImage;
    
    [Header("Buttons")]
    // [SerializeField] private Button refreshButton;
    [SerializeField] private Button logoutButton;
    
    [Header("Currency Formatting")]
    [SerializeField] private string cashPrefix = "Cash: $";
    [SerializeField] private string coinsPrefix = "Coins: ";
    
    private void Awake()
    {
        // Subscribe to PlayFab events as early as possible
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
        }
    }
    
    private void Start()
    {
        // Add button listeners
        // if (refreshButton != null)
        //     refreshButton.onClick.AddListener(RefreshPlayerData);
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
        
        // Try to subscribe again in case PlayFabManager was created after Awake
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded; // Prevent double subscription
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
            PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
        }
        
        // Load data immediately if already logged in
        RefreshPlayerData();
    }
    
    private void OnEnable()
    {
        // Refresh data every time this panel becomes active
        Debug.Log("[PlayerProfileUI] Panel enabled, refreshing data...");
        
        // Subscribe to events if not already
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
        
        // Wait a moment then refresh
        Invoke(nameof(RefreshPlayerData), 0.3f);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        }
    }
    
    private void RefreshPlayerData()
    {
        Debug.Log("[PlayerProfileUI] RefreshPlayerData called");
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] PlayFabManager not found!");
            UpdateUIWithDefaultData();
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[PlayerProfileUI] Player not logged in! Showing default data.");
            UpdateUIWithDefaultData();
            return;
        }
        
        // Player is logged in
        Debug.Log("[PlayerProfileUI] Player is logged in, loading data...");
        
        // First, try to display current data immediately if available
        if (PlayFabManager.Instance.currentPlayerData != null)
        {
            Debug.Log("[PlayerProfileUI] Found cached data, displaying it immediately");
            UpdateUI(PlayFabManager.Instance.currentPlayerData);
        }
        else
        {
            Debug.Log("[PlayerProfileUI] No cached data, showing defaults while loading...");
            UpdateUIWithDefaultData();
        }
        
        // Always load fresh data from PlayFab (will trigger OnPlayerDataLoaded when complete)
        Debug.Log("[PlayerProfileUI] Requesting fresh data from PlayFab...");
        PlayFabManager.Instance.LoadPlayerData();
    }
    
    private void OnPlayerDataLoaded(PlayerData data)
    {
        Debug.Log("[PlayerProfileUI] OnPlayerDataLoaded called!");
        if (data != null)
        {
            Debug.Log($"[PlayerProfileUI] Received data - Name: {data.playerName}, Cash: {data.cash}, Coins: {data.coins}");
        }
        UpdateUI(data);
    }
    
    private void OnPlayerDataSaved(bool success)
    {
        if (success)
        {
            Debug.Log("Player data saved successfully!");
            // Optionally show a notification
        }
        else
        {
            Debug.LogWarning("Failed to save player data!");
        }
    }
    
    private void UpdateUI(PlayerData data)
    {
        Debug.Log("[PlayerProfileUI] UpdateUI called");
        
        if (data == null)
        {
            Debug.LogWarning("[PlayerProfileUI] Data is null, showing defaults");
            UpdateUIWithDefaultData();
            return;
        }
        
        Debug.Log($"[PlayerProfileUI] Updating UI with: Name={data.playerName}, Email={data.email}, Cash={data.cash}, Coins={data.coins}");
        
        // Update name
        if (playerNameText != null)
        {
            playerNameText.text = !string.IsNullOrEmpty(data.playerName) ? data.playerName : "Guest";
            Debug.Log($"[PlayerProfileUI] Set playerNameText to: {playerNameText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] playerNameText is null!");
        }
        
        // Update email
        if (emailText != null)
        {
            emailText.text = !string.IsNullOrEmpty(data.email) ? data.email : "No email";
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] emailText is null!");
        }
        
        // Update cash
        if (cashText != null)
        {
            cashText.text = cashPrefix + data.cash.ToString("N0");
            Debug.Log($"[PlayerProfileUI] Set cashText to: {cashText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] cashText is null!");
        }
        
        // Update coins
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + data.coins.ToString("N0");
            Debug.Log($"[PlayerProfileUI] Set coinsText to: {coinsText.text}");
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] coinsText is null!");
        }
        
        // Update profile image
        if (profileImage != null && !string.IsNullOrEmpty(data.imageUrl))
        {
            // If you want to load the image from URL, you'll need to implement that
            // For now, we'll just log it
            Debug.Log($"[PlayerProfileUI] Profile image URL: {data.imageUrl}");
            // StartCoroutine(LoadImageFromUrl(data.imageUrl));
        }
        
        Debug.Log("[PlayerProfileUI] UI Update Complete!");
    }
    
    private void UpdateUIWithDefaultData()
    {
        if (playerNameText != null)
            playerNameText.text = "Guest";
        
        if (emailText != null)
            emailText.text = "Not logged in";
        
        if (cashText != null)
            cashText.text = cashPrefix + "0";
        
        if (coinsText != null)
            coinsText.text = coinsPrefix + "0";
    }
    
    private void OnLogoutClicked()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.Logout();
            Debug.Log("Player logged out");
            
            // Since we're using single scene with panels, reload the entire scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
    
    // Optional: Load image from URL
    /*
    private System.Collections.IEnumerator LoadImageFromUrl(string url)
    {
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        
        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
            if (profileImage != null)
            {
                profileImage.sprite = Sprite.Create(texture, 
                    new Rect(0, 0, texture.width, texture.height), 
                    new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            Debug.LogError("Failed to load profile image: " + request.error);
        }
    }
    */
    
    #region Public Helper Methods
    
    /// <summary>
    /// Update displayed cash value
    /// </summary>
    public void UpdateCashDisplay(int amount)
    {
        if (cashText != null)
        {
            cashText.text = cashPrefix + amount.ToString("N0");
        }
    }
    
    /// <summary>
    /// Update displayed coins value
    /// </summary>
    public void UpdateCoinsDisplay(int amount)
    {
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + amount.ToString("N0");
        }
    }
    
    #endregion
}

