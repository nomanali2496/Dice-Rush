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
        // if (refreshButton != null)
        //     refreshButton.onClick.AddListener(RefreshPlayerData);
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
        
        // Subscribe to PlayFab events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
            PlayFabManager.Instance.OnPlayerDataSaved += OnPlayerDataSaved;
        }
        
        // Load initial data
        RefreshPlayerData();
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
        if (PlayFabManager.Instance != null)
        {
            if (PlayFabManager.Instance.IsLoggedIn())
            {
                PlayFabManager.Instance.LoadPlayerData();
            }
            else
            {
                Debug.LogWarning("Player not logged in!");
                UpdateUIWithDefaultData();
            }
        }
    }
    
    private void OnPlayerDataLoaded(PlayerData data)
    {
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
        if (data == null)
        {
            UpdateUIWithDefaultData();
            return;
        }
        
        // Update name
        if (playerNameText != null)
        {
            playerNameText.text = !string.IsNullOrEmpty(data.playerName) ? data.playerName : "Guest";
        }
        
        // Update email
        if (emailText != null)
        {
            emailText.text = !string.IsNullOrEmpty(data.email) ? data.email : "No email";
        }
        
        // Update cash
        if (cashText != null)
        {
            cashText.text = cashPrefix + data.cash.ToString("N0");
        }
        
        // Update coins
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + data.coins.ToString("N0");
        }
        
        // Update profile image
        if (profileImage != null && !string.IsNullOrEmpty(data.imageUrl))
        {
            // If you want to load the image from URL, you'll need to implement that
            // For now, we'll just log it
            Debug.Log($"Profile image URL: {data.imageUrl}");
            // StartCoroutine(LoadImageFromUrl(data.imageUrl));
        }
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
            
            // Optionally reload login scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
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

