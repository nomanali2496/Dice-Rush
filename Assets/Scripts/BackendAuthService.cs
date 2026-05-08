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

    private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

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

    /// <summary>
    /// Loads a sprite from a remote URL or returns it from cache if available.
    /// Follows clean architecture by acting as the low-level provider.
    /// </summary>
    public void LoadRemoteSprite(string imagePath, Action<Sprite> onComplete)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            onComplete?.Invoke(null);
            return;
        }

        // 1. Check cache first
        if (_spriteCache.ContainsKey(imagePath))
        {
            onComplete?.Invoke(_spriteCache[imagePath]);
            return;
        }

        // 2. Download if not in cache
        StartCoroutine(DownloadSpriteCoroutine(imagePath, onComplete));
    }

    private IEnumerator DownloadSpriteCoroutine(string imagePath, Action<Sprite> onComplete)
    {
        // 1. Construct and Encode URL (Handles spaces in filenames like "COMBO DICE.webp")
        string relativePath = imagePath.StartsWith("/") ? imagePath : "/" + imagePath;
        string fullUrl = baseUrl.TrimEnd('/') + relativePath;
        fullUrl = System.Uri.EscapeUriString(fullUrl);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                
                // Create a sprite from the texture
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                
                // Add to cache
                if (!_spriteCache.ContainsKey(imagePath))
                {
                    _spriteCache.Add(imagePath, sprite);
                }
                
                onComplete?.Invoke(sprite);
            }
            else
            {
                Debug.LogWarning($"[BackendAuthService] Image Download Failed: {fullUrl} | Error: {request.error}");
                onComplete?.Invoke(null);
            }
        }
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
        Debug.Log($"<color=#00ffff>[BackendAuthService] POST Request: /api/auth/register | Username: {username}, Email: {email}</color>");
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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Register Result: {request.downloadHandler.text}</color>");
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
                        
                        if (SocketManager.Instance != null)
                        {
                            SocketManager.Instance.ConnectToServer();
                        }
                    }
                }
                catch { }

                onComplete?.Invoke(true, "Registration successful!");
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] Register Error: {request.error} | {request.downloadHandler.text}</color>");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Sends a Login request to the custom Node.js server and stores the JWT.
    /// </summary>
    public void Login(string email, string password, Action<bool, string> onComplete)
    {
        Debug.Log($"<color=#00ffff>[BackendAuthService] POST Request: /api/auth/login | Email: {email}</color>");
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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Login Result: {request.downloadHandler.text}</color>");
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
                        
                        if (SocketManager.Instance != null)
                        {
                            SocketManager.Instance.ConnectToServer();
                        }
                        
                        onComplete?.Invoke(true, "Login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Login succeeded, but no token was returned.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"<color=#ff0000>[BackendAuthService] JSON Parse Error: {e.Message}</color>");
                    onComplete?.Invoke(false, "Failed to parse server response.");
                }
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] Login Error: {request.error} | {request.downloadHandler.text}</color>");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Sends a Guest Login request to the custom Node.js server using the device ID.
    /// </summary>
    public void GuestLogin(string deviceId, Action<bool, string> onComplete)
    {
        Debug.Log($"<color=#00ffff>[BackendAuthService] POST Request: /api/auth/guest-login | Device: {deviceId}</color>");
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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Guest Login Result: {request.downloadHandler.text}</color>");
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
                        
                        if (SocketManager.Instance != null)
                        {
                            SocketManager.Instance.ConnectToServer();
                        }
                        
                        onComplete?.Invoke(true, "Guest Login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Guest Login succeeded, but no token was returned.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"<color=#ff0000>[BackendAuthService] JSON Parse Error: {e.Message}</color>");
                    onComplete?.Invoke(false, "Failed to parse server response.");
                }
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] Guest Login Error: {request.error} | {request.downloadHandler.text}</color>");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Sends a Google Login request to the custom Node.js server.
    /// </summary>
    public void GoogleLogin(string idToken, Action<bool, string> onComplete)
    {
        Debug.Log($"<color=#00ffff>[BackendAuthService] POST Request: /api/auth/google | Token provided</color>");
        StartCoroutine(SocialLoginCoroutine("/api/auth/google", idToken, onComplete));
    }

    /// <summary>
    /// Sends a Facebook Login request to the custom Node.js server.
    /// </summary>
    public void FacebookLogin(string accessToken, Action<bool, string> onComplete)
    {
        Debug.Log($"<color=#00ffff>[BackendAuthService] POST Request: /api/auth/facebook | Token provided</color>");
        StartCoroutine(SocialLoginCoroutine("/api/auth/facebook", accessToken, onComplete));
    }

    private IEnumerator SocialLoginCoroutine(string endpoint, string token, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + endpoint; 
        string jsonData = $"{{\"token\":\"{token}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Social Login Result: {request.downloadHandler.text}</color>");
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
                        
                        if (SocketManager.Instance != null)
                        {
                            SocketManager.Instance.ConnectToServer();
                        }
                        
                        onComplete?.Invoke(true, "Social Login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Social Login succeeded, but no token was returned.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"<color=#ff0000>[BackendAuthService] JSON Parse Error: {e.Message}</color>");
                    onComplete?.Invoke(false, "Failed to parse server response.");
                }
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] Social Login Error: {request.error} | {request.downloadHandler.text}</color>");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Updates the user's profile (e.g., name or preset avatar) on the backend.
    /// </summary>
    public void UpdateProfile(string newName, string avatar = null, Action<bool, string> onComplete = null)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "No stored JWT found.");
            return;
        }
        Debug.Log($"<color=#00ffff>[BackendAuthService] PATCH Request: /api/auth/profile | Name: {newName}, Avatar: {avatar}</color>");
        StartCoroutine(UpdateProfileCoroutine(newName, avatar, onComplete));
    }

    private IEnumerator UpdateProfileCoroutine(string newName, string avatar, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/profile"; 
        string token = GetStoredJWT();
        
        string jsonData;
        if (string.IsNullOrEmpty(avatar))
        {
            jsonData = $"{{\"name\":\"{newName}\"}}";
        }
        else
        {
            jsonData = $"{{\"name\":\"{newName}\", \"avatar\":\"{avatar}\"}}";
        }

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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Profile Updated: {request.downloadHandler.text}</color>");
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
                    Debug.LogError($"<color=#ff0000>[BackendAuthService] UpdateProfile Parse Error: {e.Message}</color>");
                    onComplete?.Invoke(false, "JSON parsing failed.");
                }
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] UpdateProfile Error: {request.error} | {request.downloadHandler.text}</color>");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Uploads a custom avatar image to the backend.
    /// </summary>
    public void UploadAvatar(byte[] imageBytes, Action<bool, string> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "No stored JWT found.");
            return;
        }
        Debug.Log("<color=#00ffff>[BackendAuthService] POST Request: /api/auth/upload-avatar (Multipart Image Upload)</color>");
        StartCoroutine(UploadAvatarCoroutine(imageBytes, onComplete));
    }

    private IEnumerator UploadAvatarCoroutine(byte[] imageBytes, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/upload-avatar";
        string token = GetStoredJWT();

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        // Key "avatar" matches the expected field in most backend image upload configurations
        formData.Add(new MultipartFormFileSection("avatar", imageBytes, "avatar.jpg", "image/jpeg"));

        using (UnityWebRequest request = UnityWebRequest.Post(url, formData))
        {
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
                        onComplete?.Invoke(true, response.avatar);
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Failed to parse upload response.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] UploadAvatar Parse Error: {e.Message}");
                    onComplete?.Invoke(false, "JSON parsing failed.");
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] UploadAvatar Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Updates the user's currency (cash and coins) on the backend.
    /// </summary>
    public void UpdateCurrency(int cashBalance, int coins, Action<bool, string> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "No stored JWT found.");
            return;
        }
        StartCoroutine(UpdateCurrencyCoroutine(cashBalance, coins, onComplete));
    }

    private IEnumerator UpdateCurrencyCoroutine(int cashBalance, int coins, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/profile"; 
        string token = GetStoredJWT();
        
        string jsonData = $"{{\"cashBalance\":{cashBalance}, \"coins\":{coins}}}";

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
                        onComplete?.Invoke(true, "Currency updated successfully!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, "Failed to parse updated user data.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] UpdateCurrency Parse Error: {e.Message}");
                    onComplete?.Invoke(false, "JSON parsing failed.");
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] UpdateCurrency Error: {request.error} | {request.downloadHandler.text}");
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
                        
                        if (SocketManager.Instance != null)
                        {
                            SocketManager.Instance.ConnectToServer();
                        }
                        
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
        Debug.Log("<color=#00ffff>[BackendAuthService] GET Request: /api/rewards/status</color>");
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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: GetDailyRewardStatus Result: {request.downloadHandler.text}</color>");
                DailyRewardStatusResponse response = JsonUtility.FromJson<DailyRewardStatusResponse>(request.downloadHandler.text);
                onComplete?.Invoke(true, response);
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] GetDailyRewardStatus Error: {request.error} | {request.downloadHandler.text}</color>");
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
        Debug.Log("<color=#00ffff>[BackendAuthService] POST Request: /api/rewards/claim</color>");
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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: ClaimDailyReward Result: {request.downloadHandler.text}</color>");
                DailyRewardStatusResponse response = JsonUtility.FromJson<DailyRewardStatusResponse>(request.downloadHandler.text);
                onComplete?.Invoke(true, response);
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] ClaimDailyReward Error: {request.error} | {request.downloadHandler.text}</color>");
                onComplete?.Invoke(false, null);
            }
        }
    }

    private Dictionary<string, List<Action<Texture2D>>> _inFlightImageRequests = new Dictionary<string, List<Action<Texture2D>>>();
    private Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();

    /// <summary>
    /// Downloads an image from a URL. Centralizes networking logic away from UI scripts.
    /// Uses in-flight tracking and caching to prevent duplicate network requests.
    /// </summary>
    public void GetImage(string url, Action<Texture2D> onComplete)
    {
        if (string.IsNullOrEmpty(url)) return;

        // 1. Check if already cached
        if (_imageCache.TryGetValue(url, out Texture2D cachedTexture) && cachedTexture != null)
        {
            onComplete?.Invoke(cachedTexture);
            return;
        }

        // 2. Check if already downloading (in-flight)
        if (_inFlightImageRequests.ContainsKey(url))
        {
            _inFlightImageRequests[url].Add(onComplete);
            return; // Wait for the active download to finish
        }

        // 3. Start a new download
        _inFlightImageRequests[url] = new List<Action<Texture2D>> { onComplete };
        Debug.Log($"<color=#00ffff>[BackendAuthService] GET Request (Image): {url}</color>");
        StartCoroutine(GetImageCoroutine(url));
    }

    private IEnumerator GetImageCoroutine(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            Texture2D texture = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Image Downloaded: {url}</color>");
                texture = DownloadHandlerTexture.GetContent(request);
                _imageCache[url] = texture; // Cache the successful result
            }
            else
            {
                Debug.LogError($"<color=#ff0000>[BackendAuthService] ERROR: Image Download Failed: {url} | {request.error}</color>");
            }

            // Fire all waiting callbacks
            if (_inFlightImageRequests.TryGetValue(url, out var callbacks))
            {
                _inFlightImageRequests.Remove(url);
                foreach (var callback in callbacks)
                {
                    callback?.Invoke(texture);
                }
            }
        }
    }

    /// <summary>
    /// Fetches the daily offers from the backend.
    /// </summary>
    public void GetDailyOffers(Action<bool, DailyOffersResponse> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(GetDailyOffersCoroutine(onComplete));
    }

    private IEnumerator GetDailyOffersCoroutine(Action<bool, DailyOffersResponse> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/offers/daily";
        string token = GetStoredJWT();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Daily Offers Result: {request.downloadHandler.text}</color>");
                DailyOffersResponse response = JsonUtility.FromJson<DailyOffersResponse>(request.downloadHandler.text);
                onComplete?.Invoke(true, response);
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetDailyOffers Error: {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// Fetches the lobby configurations from the backend.
    /// </summary>
    public void GetLobbyConfigurations(Action<bool, LobbyApiResponse> onComplete)
    {
        StartCoroutine(GetLobbyConfigurationsCoroutine(onComplete));
    }

    private IEnumerator GetLobbyConfigurationsCoroutine(Action<bool, LobbyApiResponse> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/lobby";
        Debug.Log($"<color=#FFA500>⬆️ [GET] Lobby Configurations: {url}</color>");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (HasStoredJWT())
            {
                request.SetRequestHeader("Authorization", "Bearer " + GetStoredJWT());
            }
            
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Lobby Config Result: {request.downloadHandler.text}</color>");
                try
                {
                    LobbyApiResponse response = JsonUtility.FromJson<LobbyApiResponse>(request.downloadHandler.text);
                    onComplete?.Invoke(true, response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] GetLobbyConfigurations Parse Error: {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetLobbyConfigurations Error: {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    [Serializable]
    public class DailyOffersResponse
    {
        public bool success;
        public DailyOffersData data;
    }

    [Serializable]
    public class DailyOffersData
    {
        public List<string> offers;
        public string lastShown;
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
        public string avatar; // Explicitly added for upload response
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

    [Serializable]
    public class LobbyApiResponse
    {
        public bool success;
        public int count;
        public List<LobbyApiData> data;
    }

    [Serializable]
    public class LobbyApiData
    {
        public string id;
        public string name;
        public string image;
        public string mode;
        public int playerCount;
        public string description;
        public int lobbyIndex;
        public int gameTimer;
        public float entryFeeCash;
        public int entryFeeChips;
        public float firstPlaceCash;
        public int firstPlaceChips;
        public float secondPlaceCash;
        public int secondPlaceChips;
        public float thirdPlaceCash;
        public int thirdPlaceChips;
        public bool enabled;
    }
}
