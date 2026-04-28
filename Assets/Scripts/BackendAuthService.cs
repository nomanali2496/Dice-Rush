using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
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
        string url = baseUrl.TrimEnd('/') + "/api/auth/register"; 
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
                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    if (response != null && response.user != null && !string.IsNullOrEmpty(response.user.id))
                    {
                        PlayerModel.UpdateData(
                            response.user.id, response.user.name, response.user.email,
                            response.user.wins, response.user.losses, response.user.balance,
                            response.user.cashBalance, response.user.bonusBalance, response.user.winningBalance,
                            response.user.coins, response.user.avatar, response.user.xp,
                            response.user.level, response.user.role
                        );
                    }
                    if (response != null && !string.IsNullOrEmpty(response.token))
                    {
                        PlayerPrefs.SetString(JWT_PREF_KEY, response.token);
                        PlayerPrefs.Save();
                    }
                }
                catch { }

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
        string url = baseUrl.TrimEnd('/') + "/api/auth/login"; 
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
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    
                    if (!string.IsNullOrEmpty(response.token))
                    {
                        if (response.user != null && !string.IsNullOrEmpty(response.user.id))
                        {
                            PlayerModel.UpdateData(
                                response.user.id, response.user.name, response.user.email,
                                response.user.wins, response.user.losses, response.user.balance,
                                response.user.cashBalance, response.user.bonusBalance, response.user.winningBalance,
                                response.user.coins, response.user.avatar, response.user.xp,
                                response.user.level, response.user.role
                            );
                        }

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
    /// Sends a Guest Login request to the custom Node.js server using the device ID.
    /// </summary>
    public void GuestLogin(string deviceId, Action<bool, string> onComplete)
    {
        StartCoroutine(GuestLoginCoroutine(deviceId, onComplete));
    }

    private IEnumerator GuestLoginCoroutine(string deviceId, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/guest-login"; 
        string jsonData = $"{{\"guestId\":\"{deviceId}\"}}";

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
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    
                    if (!string.IsNullOrEmpty(response.token))
                    {
                        if (response.user != null && !string.IsNullOrEmpty(response.user.id))
                        {
                            PlayerModel.UpdateData(
                                response.user.id, response.user.name, response.user.email,
                                response.user.wins, response.user.losses, response.user.balance,
                                response.user.cashBalance, response.user.bonusBalance, response.user.winningBalance,
                                response.user.coins, response.user.avatar, response.user.xp,
                                response.user.level, response.user.role
                            );
                        }

                        PlayerPrefs.SetString(JWT_PREF_KEY, response.token);
                        PlayerPrefs.Save();
                        onComplete?.Invoke(true, "Guest Login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Guest Login succeeded, but no token was returned.");
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
                Debug.LogError($"[BackendAuthService] Guest Login Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Updates the user's profile (e.g., name) on the backend.
    /// </summary>
    public void UpdateProfile(string newName, Action<bool, string> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "No stored JWT found.");
            return;
        }
        StartCoroutine(UpdateProfileCoroutine(newName, onComplete));
    }

    private IEnumerator UpdateProfileCoroutine(string newName, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/profile"; 
        string token = GetStoredJWT();
        
        string jsonData = $"{{\"name\":\"{newName}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    if (response != null && response.user != null)
                    {
                        PlayerModel.UpdateData(
                            response.user.id, response.user.name, response.user.email,
                            response.user.wins, response.user.losses, response.user.balance,
                            response.user.cashBalance, response.user.bonusBalance, response.user.winningBalance,
                            response.user.coins, response.user.avatar, response.user.xp,
                            response.user.level, response.user.role
                        );
                        onComplete?.Invoke(true, "Profile updated successfully!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Failed to parse updated user data.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] UpdateProfile Parse Error: {e.Message}");
                    onComplete?.Invoke(false, "JSON parsing failed.");
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] UpdateProfile Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Fetches the user's latest profile data from the backend using the stored JWT.
    /// </summary>
    public void GetProfile(Action<bool, string> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "No stored JWT found.");
            return;
        }
        StartCoroutine(GetProfileCoroutine(onComplete));
    }

    private IEnumerator GetProfileCoroutine(Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/me"; 
        string token = GetStoredJWT();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    if (response != null && response.user != null)
                    {
                        PlayerModel.UpdateData(
                            response.user.id, response.user.name, response.user.email,
                            response.user.wins, response.user.losses, response.user.balance,
                            response.user.cashBalance, response.user.bonusBalance, response.user.winningBalance,
                            response.user.coins, response.user.avatar, response.user.xp,
                            response.user.level, response.user.role
                        );
                        onComplete?.Invoke(true, "Profile loaded successfully!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Failed to parse user data.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] GetProfile Parse Error: {e.Message}");
                    onComplete?.Invoke(false, "JSON parsing failed.");
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetProfile Error: {request.error}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Fetches the daily reward status from the backend.
    /// </summary>
    public void GetDailyRewardStatus(Action<bool, DailyRewardStatusResponse> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(GetDailyRewardStatusCoroutine(onComplete));
    }

    private IEnumerator GetDailyRewardStatusCoroutine(Action<bool, DailyRewardStatusResponse> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/rewards/status";
        string token = GetStoredJWT();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                DailyRewardStatusResponse response = JsonUtility.FromJson<DailyRewardStatusResponse>(request.downloadHandler.text);
                onComplete?.Invoke(true, response);
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetDailyRewardStatus Error: {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// Claims the current daily reward from the backend.
    /// </summary>
    public void ClaimDailyReward(Action<bool, DailyRewardStatusResponse> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(ClaimDailyRewardCoroutine(onComplete));
    }

    private IEnumerator ClaimDailyRewardCoroutine(Action<bool, DailyRewardStatusResponse> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/rewards/claim";
        string token = GetStoredJWT();

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                DailyRewardStatusResponse response = JsonUtility.FromJson<DailyRewardStatusResponse>(request.downloadHandler.text);
                onComplete?.Invoke(true, response);
            }
            else
            {
                Debug.LogError($"[BackendAuthService] ClaimDailyReward Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    [Serializable]
    public class DailyRewardStatusResponse
    {
        public bool success;
        public DailyRewardDataPayload data;
        public BackendUser user; 
    }

    [Serializable]
    public class DailyRewardDataPayload
    {
        public bool canClaim;
        public int currentStreak;
        public int nextDay;
        public List<RewardItem> rewards;
        public string lastClaimed;
    }

    [Serializable]
    public class RewardItem
    {
        public int day;
        public int coins;
        public float cash;
    }

    [Serializable]
    public class AuthResponse
    {
        public bool success;
        public string token;
        public string message;
        public BackendUser user;
    }

    [Serializable]
    public class BackendUser
    {
        public string id;
        public string name;
        public string email;
        public int wins;
        public int losses;
        public int balance;
        public int cashBalance;
        public int bonusBalance;
        public int winningBalance;
        public int coins;
        public string avatar;
        public int xp;
        public int level;
        public string role;
    }
}
