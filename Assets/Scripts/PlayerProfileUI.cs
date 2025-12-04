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
    
    private void Start()
    {
        // Add button listeners
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
    }
    
    private void OnEnable()
    {
        Debug.Log("[PlayerProfileUI] ===== PANEL ENABLED =====");
        
        // Wait a tiny bit to ensure PlayFabManager is fully initialized
        StartCoroutine(InitializeUIAfterDelay());
    }
    
    private System.Collections.IEnumerator InitializeUIAfterDelay()
    {
        // Wait one frame to ensure everything is set up
        yield return null;
        
        Debug.Log("[PlayerProfileUI] Initializing UI after delay...");
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] PlayFabManager not found!");
            UpdateUIWithDefaultData();
            yield break;
        }
        
        // Subscribe to future events
        PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
        
        Debug.Log($"[PlayerProfileUI] IsLoggedIn: {PlayFabManager.Instance.IsLoggedIn()}");
        
        // Check if player is logged in and data exists
        if (PlayFabManager.Instance.IsLoggedIn())
        {
            PlayerData data = PlayFabManager.Instance.currentPlayerData;
            
            Debug.Log($"[PlayerProfileUI] Checking data - playerName: '{data?.playerName}', cash: {data?.cash}, coins: {data?.coins}");
            
            if (data != null && !string.IsNullOrEmpty(data.playerName))
            {
                // Data was already loaded while this panel was inactive!
                Debug.Log("[PlayerProfileUI] ✓✓✓ DATA FOUND! Displaying immediately! ✓✓✓");
                Debug.Log($"[PlayerProfileUI] Updating UI with: Name={data.playerName}, Email={data.email}, Cash={data.cash}, Coins={data.coins}");
                UpdateUI(data);
            }
            else
            {
                // Data not loaded yet, request it
                Debug.Log("[PlayerProfileUI] No data yet, requesting from PlayFab...");
                UpdateUIWithDefaultData();
                
                // Wait a bit then check again (data might be loading)
                yield return new WaitForSeconds(0.5f);
                
                // Check again after waiting
                data = PlayFabManager.Instance.currentPlayerData;
                if (data != null && !string.IsNullOrEmpty(data.playerName))
                {
                    Debug.Log("[PlayerProfileUI] ✓ Data arrived! Displaying now!");
                    UpdateUI(data);
                }
                else
                {
                    Debug.Log("[PlayerProfileUI] Still no data, loading from PlayFab...");
                    PlayFabManager.Instance.LoadPlayerData();
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] Player not logged in!");
            UpdateUIWithDefaultData();
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events when disabled
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved -= OnPlayerDataSaved;
        }
    }
    
    private void RefreshPlayerData()
    {
        Debug.Log("[PlayerProfileUI] Manual RefreshPlayerData called");
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[PlayerProfileUI] PlayFabManager not found!");
            UpdateUIWithDefaultData();
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.LogWarning("[PlayerProfileUI] Player not logged in!");
            UpdateUIWithDefaultData();
            return;
        }
        
        // Request fresh data from PlayFab
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
    /// Manually force UI update with current PlayFab data
    /// Call this if UI is not updating automatically
    /// </summary>
    public void ForceUpdateUI()
    {
        Debug.Log("[PlayerProfileUI] ForceUpdateUI called manually");
        
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            Debug.Log("[PlayerProfileUI] Forcing UI update with current data");
            UpdateUI(PlayFabManager.Instance.currentPlayerData);
        }
        else
        {
            Debug.LogWarning("[PlayerProfileUI] Cannot force update - no data available");
            RefreshPlayerData();
        }
    }
    
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

