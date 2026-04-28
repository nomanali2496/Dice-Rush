using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class BackendAuthService : MonoBehaviour
{
    public static BackendAuthService Instance { get; private set; }

    [Header("Server Settings")]
    [Tooltip("Base URL for the Node.js backend API")]
    [SerializeField] private string baseUrl = "https://unclog-goggles-drastic.ngrok-free.dev";

    private const string JWT_PREF_KEY = "CustomBackendJWT";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static string GetStoredJWT()
    {
        return PlayerPrefs.GetString(JWT_PREF_KEY, string.Empty);
    }

    public static bool HasStoredJWT()
    {
        return !string.IsNullOrEmpty(GetStoredJWT());
    }

    public static void ClearJWT()
    {
        PlayerPrefs.DeleteKey(JWT_PREF_KEY);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Sends a Registration request to the custom Node.js server.
    /// </summary>
    public void Register(string username, string email, string password, Action<bool, string> onComplete)
    {
        StartCoroutine(RegisterCoroutine(username, email, password, onComplete));
    }

    private IEnumerator RegisterCoroutine(string username, string email, string password, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/register"; // Assumes your endpoint is /api/auth/register
        
        // Build JSON payload manually or use JsonUtility
        string jsonData = $"{{\"name\":\"{username}\", \"email\":\"{email}\", \"password\":\"{password}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, "Registration successful!");
            }
            else
            {
                Debug.LogError($"[BackendAuthService] Register Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Sends a Login request to the custom Node.js server and stores the JWT.
    /// </summary>
    public void Login(string email, string password, Action<bool, string> onComplete)
    {
        StartCoroutine(LoginCoroutine(email, password, onComplete));
    }

    private IEnumerator LoginCoroutine(string email, string password, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/login"; // Assumes your endpoint is /api/auth/login
        
        string jsonData = $"{{\"email\":\"{email}\", \"password\":\"{password}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Attempt to parse the token from the backend's JSON response
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    
                    if (!string.IsNullOrEmpty(response.token))
                    {
                        // Save the token locally
                        PlayerPrefs.SetString(JWT_PREF_KEY, response.token);
                        PlayerPrefs.Save();
                        onComplete?.Invoke(true, "Login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Login succeeded, but no token was returned.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] JSON Parse Error: {e.Message}");
                    onComplete?.Invoke(false, "Failed to parse server response.");
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] Login Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Helper class to parse the JSON response from your Node.js server.
    /// Ensure your server returns a JSON object like: { "token": "your_jwt_here" }
    /// </summary>
    [Serializable]
    private class AuthResponse
    {
        public string token;
        public string message;
        // Add any other fields your server returns here (e.g., userId, username)
    }
}
