using UnityEngine;

[CreateAssetMenu(fileName = "BackendConfig", menuName = "Dice Rush/Backend Config")]
public class BackendConfig : ScriptableObject
{
    public enum AppEnvironment { Development, Production }

    [Header("Active Environment")]
    public AppEnvironment activeEnvironment = AppEnvironment.Development;

    [Header("Development URLs")]
    public string devApiUrl = "http://localhost:3000";
    public string devSocketUrl = "http://localhost:3000";

    [Header("Production URLs")]
    public string prodApiUrl = "https://api.yourdomain.com";
    public string prodSocketUrl = "https://api.yourdomain.com";

    public string ApiUrl => activeEnvironment == AppEnvironment.Development ? devApiUrl : prodApiUrl;
    public string SocketUrl => activeEnvironment == AppEnvironment.Development ? devSocketUrl : prodSocketUrl;

    private static BackendConfig _instance;

    // Auto-loads from Resources/BackendConfig.asset when not injected via Inspector
    public static BackendConfig Load()
    {
        if (_instance == null)
            _instance = Resources.Load<BackendConfig>("BackendConfig");
        if (_instance == null)
            Debug.LogError("[BackendConfig] BackendConfig.asset not found in Resources folder!");
        return _instance;
    }

#if UNITY_EDITOR
    [ContextMenu("Log Active URLs")]
    private void LogActiveUrls()
    {
        Debug.Log($"[BackendConfig] Environment: {activeEnvironment}");
        Debug.Log($"[BackendConfig] API URL: {ApiUrl}");
        Debug.Log($"[BackendConfig] Socket URL: {SocketUrl}");
    }
#endif
}
