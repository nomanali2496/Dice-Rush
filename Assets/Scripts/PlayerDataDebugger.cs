using UnityEngine;

/// <summary>
/// Simple debug script to verify PlayerModel data is loading correctly
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
        PlayerModel.OnDataUpdated += OnDataLoaded;
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
    
    private void OnDataLoaded()
    {
        Debug.Log("=== PLAYER DATA UPDATED ===");
        Debug.Log($"Name: {PlayerModel.Name}");
        Debug.Log($"Cash: {PlayerModel.CashBalance}");
        Debug.Log($"Coins: {PlayerModel.Coins}");
        Debug.Log($"Level: {PlayerModel.Level}");
        Debug.Log("=========================");
    }
    
    private void LogPlayerData()
    {
        if (string.IsNullOrEmpty(PlayerModel.Id))
        {
            Debug.Log("Player not logged in yet (No ID)...");
            return;
        }
        
        Debug.Log($"[Player Data] Name: {PlayerModel.Name}, Cash: {PlayerModel.CashBalance}, Coins: {PlayerModel.Coins}");
    }
    
    private void OnDestroy()
    {
        PlayerModel.OnDataUpdated -= OnDataLoaded;
    }
}
