using UnityEngine;

/// <summary>
/// Diagnostic helper to debug why PlayerProfileUI doesn't update on fresh game start
/// Attach this to any GameObject in the scene
/// </summary>
public class DiagnosticHelper : MonoBehaviour
{
    [Header("Check every X seconds")]
    [SerializeField] private float checkInterval = 1f;
    
    private float timer = 0f;
    
    private void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckEverything();
        }
    }
    
    private void CheckEverything()
    {
        Debug.Log("========== DIAGNOSTIC CHECK ==========");
        
        // Check PlayFabManager
        if (PlayFabManager.Instance != null)
        {
            Debug.Log($"✓ PlayFabManager exists");
            Debug.Log($"  - IsLoggedIn: {PlayFabManager.Instance.IsLoggedIn()}");
            
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                var data = PlayFabManager.Instance.currentPlayerData;
                Debug.Log($"  - PlayerData exists:");
                Debug.Log($"    Name: '{data.playerName}'");
                Debug.Log($"    Email: '{data.email}'");
                Debug.Log($"    Cash: {data.cash}");
                Debug.Log($"    Coins: {data.coins}");
            }
            else
            {
                Debug.LogWarning("  - PlayerData is NULL!");
            }
        }
        else
        {
            Debug.LogError("✗ PlayFabManager NOT FOUND!");
        }
        
        // Check PlayerProfileUI
        PlayerProfileUI[] profileUIs = FindObjectsOfType<PlayerProfileUI>(true);
        Debug.Log($"Found {profileUIs.Length} PlayerProfileUI instances");
        
        foreach (var ui in profileUIs)
        {
            Debug.Log($"  - PlayerProfileUI on: {ui.gameObject.name}");
            Debug.Log($"    GameObject active: {ui.gameObject.activeSelf}");
            Debug.Log($"    GameObject activeInHierarchy: {ui.gameObject.activeInHierarchy}");
            Debug.Log($"    Script enabled: {ui.enabled}");
        }
        
        Debug.Log("=====================================");
    }
    
    [ContextMenu("Force Check Now")]
    public void ForceCheckNow()
    {
        CheckEverything();
    }
}

