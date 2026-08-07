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
        
        // Check PlayerModel
        Debug.Log($"✓ PlayerModel Info:");
        Debug.Log($"  - Id: {PlayerModel.Id}");
        Debug.Log($"  - Name: '{PlayerModel.Name}'");
        Debug.Log($"  - Email: '{PlayerModel.Email}'");
        Debug.Log($"  - CashBalance: {PlayerModel.CashBalance}");
        Debug.Log($"  - Coins: {PlayerModel.Coins}");
        
        // Check PlayerProfileUI
        PlayerProfileUI[] profileUIs = FindObjectsByType<PlayerProfileUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

