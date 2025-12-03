using UnityEngine;

/// <summary>
/// Simple debug script to verify PlayFab data is loading correctly
/// Attach to any GameObject to see player data in Console
/// </summary>
public class PlayerDataDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private float updateInterval = 2f;
    
    private float timer = 0f;
    
    private void Start()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnDataLoaded;
        }
    }
    
    private void Update()
    {
        if (!showDebugLogs) return;
        
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            LogPlayerData();
        }
    }
    
    private void OnDataLoaded(PlayerData data)
    {
        Debug.Log("=== PLAYER DATA LOADED ===");
        Debug.Log($"Name: {data.playerName}");
        Debug.Log($"Email: {data.email}");
        Debug.Log($"Cash: {data.cash}");
        Debug.Log($"Coins: {data.coins}");
        Debug.Log($"Image URL: {data.imageUrl}");
        Debug.Log("=========================");
    }
    
    private void LogPlayerData()
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("PlayFabManager not found!");
            return;
        }
        
        if (!PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.Log("Player not logged in yet...");
            return;
        }
        
        PlayerData data = PlayFabManager.Instance.currentPlayerData;
        if (data != null)
        {
            Debug.Log($"[Player Data] Name: {data.playerName}, Cash: {data.cash}, Coins: {data.coins}");
        }
        else
        {
            Debug.LogWarning("Player data is null!");
        }
    }
    
    private void OnDestroy()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnDataLoaded;
        }
    }
}

