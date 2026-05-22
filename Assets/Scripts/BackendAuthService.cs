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
        string fullUrl;
        if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
        {
            fullUrl = imagePath;
        }
        else
        {
            string relativePath = imagePath.StartsWith("/") ? imagePath : "/" + imagePath;
            fullUrl = baseUrl.TrimEnd('/') + relativePath;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    
                    if (!_spriteCache.ContainsKey(imagePath))
                    {
                        _spriteCache.Add(imagePath, sprite);
                    }
                    
                    onComplete?.Invoke(sprite);
                }
                else
                {
                    onComplete?.Invoke(null);
                }
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
        PlayerModel.UpdateData(null, null, null, 0, 0, 0, 0, 0, 0, 0, null, 0, 0, null, 0, null, null);
    }

    /// <summary>
    /// Sends a Logout request to the custom Node.js server.
    /// Clears active match state server-side and local JWT.
    /// </summary>
    public void Logout(Action<bool, string> onComplete = null)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "No stored JWT found.");
            return;
        }
        Debug.Log("<color=#00ffff>[BackendAuthService] POST Request: /api/auth/logout</color>");
        StartCoroutine(LogoutCoroutine(onComplete));
    }

    private IEnumerator LogoutCoroutine(Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/auth/logout";
        string token = GetStoredJWT();

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            // Always clear local data even if the request fails (to allow logout in offline mode)
            ClearJWT();
            
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.Disconnect();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Logout Result: {request.downloadHandler.text}</color>");
                onComplete?.Invoke(true, "Logout successful!");
            }
            else
            {
                Debug.LogWarning($"<color=#ff0000>[BackendAuthService] Logout Server Error: {request.error}. Local state cleared anyway.</color>");
                onComplete?.Invoke(false, request.error);
            }
        }
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
                            response.user.level, response.user.role, 0,
                            response.user.activeMatchId, response.user.activeRoomId
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
                                response.user.level, response.user.role, 0,
                                response.user.activeMatchId, response.user.activeRoomId
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
                                response.user.level, response.user.role, 0,
                                response.user.activeMatchId, response.user.activeRoomId
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

    /// <summary>
    /// Sends an Apple Login request to the custom Node.js server.
    /// </summary>
    public void AppleLogin(string identityToken, string displayName, Action<bool, string> onComplete)
    {
        Debug.Log($"<color=#00ffff>[BackendAuthService] POST Request: /api/auth/apple | Token provided</color>");
        
        string firstName = "";
        string lastName = "";
        
        if (!string.IsNullOrEmpty(displayName))
        {
            string[] names = displayName.Split(' ');
            firstName = names[0];
            if (names.Length > 1)
            {
                lastName = displayName.Substring(firstName.Length).Trim();
            }
        }
        
        string jsonData = $"{{\"idToken\":\"{identityToken}\", \"user\": {{\"name\": {{\"firstName\": \"{firstName}\", \"lastName\": \"{lastName}\"}}}}}}";
        
        StartCoroutine(CustomPayloadLoginCoroutine("/api/auth/apple", jsonData, onComplete));
    }

    private IEnumerator SocialLoginCoroutine(string endpoint, string token, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + endpoint; 
        string jsonData = $"{{\"token\":\"{token}\", \"idToken\":\"{token}\"}}";
        
        Debug.Log($"[BackendAuthService] DIAGNOSTIC - Sending payload to {endpoint}: {jsonData}");
        
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
                                response.user.level, response.user.role, 0,
                                response.user.activeMatchId, response.user.activeRoomId
                            );
                        }

                        PlayerPrefs.SetString(JWT_PREF_KEY, response.token);
                        PlayerPrefs.Save();
                        
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

    private IEnumerator CustomPayloadLoginCoroutine(string endpoint, string jsonData, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + endpoint; 
        
        Debug.Log($"[BackendAuthService] DIAGNOSTIC - Sending payload to {endpoint}: {jsonData}");
        
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
                                response.user.level, response.user.role, 0,
                                response.user.activeMatchId, response.user.activeRoomId
                            );
                        }

                        PlayerPrefs.SetString(JWT_PREF_KEY, response.token);
                        PlayerPrefs.Save();
                        
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
                            response.user.level, response.user.role, 0,
                            response.user.activeMatchId, response.user.activeRoomId
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
                            response.user.level, response.user.role, 0,
                            response.user.activeMatchId, response.user.activeRoomId
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
                        int finalCoins = response.user.coins > 0 ? response.user.coins : coins;
                        int finalCash = response.user.cashBalance > 0 ? response.user.cashBalance : cashBalance;

                        PlayerModel.UpdateData(
                            response.user.id, response.user.name, response.user.email,
                            response.user.wins, response.user.losses, response.user.balance,
                            finalCash, response.user.bonusBalance, response.user.winningBalance,
                            finalCoins, response.user.avatar, response.user.xp,
                            response.user.level, response.user.role, 0,
                            response.user.activeMatchId, response.user.activeRoomId
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
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Profile Result: {request.downloadHandler.text}</color>");
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
                            response.user.level, response.user.role, 0,
                            response.user.activeMatchId, response.user.activeRoomId
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
    public void GetImage(string url, Action<Texture2D> onComplete, bool forceRefresh = false)
    {
        if (string.IsNullOrEmpty(url)) return;

        // 1. Check if already cached (skip if forcing refresh)
        if (!forceRefresh && _imageCache.TryGetValue(url, out Texture2D cachedTexture) && cachedTexture != null)
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
        Debug.Log($"<color=#00ffff>[BackendAuthService] GET Request (Image) [ForceRefresh: {forceRefresh}]: {url}</color>");
        StartCoroutine(GetImageCoroutine(url));
    }

    private IEnumerator GetImageCoroutine(string url)
    {
        // Use UnityWebRequestTexture instead of DownloadHandlerBuffer + LoadImage().
        // UnityWebRequestTexture uses the OS-level image decoder which supports
        // PNG, JPG AND WebP natively on Android & iOS — LoadImage() only supports PNG/JPG.
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            Texture2D texture = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Guard against the server returning an HTML error page
                string contentType = request.GetResponseHeader("Content-Type");
                bool isHtml = !string.IsNullOrEmpty(contentType) && contentType.ToLower().Contains("text/html");

                if (isHtml)
                {
                    Debug.LogWarning($"[BackendAuthService] Image not found (server returned HTML): {url}");
                }
                else
                {
                    texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Image Downloaded: {url}</color>");
                        _imageCache[url] = texture;
                    }
                    else
                    {
                        Debug.LogError($"<color=#ff0000>[BackendAuthService] ERROR: Texture decode failed (format unsupported?): {url}</color>");
                    }
                }
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
    /// Fetches the global leaderboard from the backend.
    /// </summary>
    public void GetLeaderboard(int limit, Action<bool, LeaderboardResponse> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(GetLeaderboardCoroutine(limit, onComplete));
    }

    private IEnumerator GetLeaderboardCoroutine(int limit, Action<bool, LeaderboardResponse> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/leaderboard?limit=" + limit;
        string token = GetStoredJWT();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Leaderboard Result: {request.downloadHandler.text}</color>");
                try
                {
                    LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        onComplete?.Invoke(true, response);
                    }
                    else
                    {
                        onComplete?.Invoke(false, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] GetLeaderboard Parse Error: {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetLeaderboard Error: {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// Submits the inviter ID (from Branch deep link) to the backend.
    /// </summary>
    public void SubmitInviter(string inviterId, Action<bool> onComplete)
    {
        if (!HasStoredJWT() || string.IsNullOrEmpty(inviterId))
        {
            onComplete?.Invoke(false);
            return;
        }
        StartCoroutine(SubmitInviterCoroutine(inviterId, onComplete));
    }

    private IEnumerator SubmitInviterCoroutine(string inviterId, Action<bool> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/invites/invitedby";
        string token = GetStoredJWT();
        string jsonPayload = "{\"inviterId\":\"" + inviterId + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Submit Inviter Result: {request.downloadHandler.text}</color>");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"[BackendAuthService] SubmitInviter Error: {request.error}");
                onComplete?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// Sends a cash withdrawal request to the backend.
    /// </summary>
    public void Withdraw(float amount, Action<bool, string> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "Unauthorized: No stored JWT");
            return;
        }
        StartCoroutine(WithdrawCoroutine(amount, onComplete));
    }

    private IEnumerator WithdrawCoroutine(float amount, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/wallet/withdraw";
        string token = GetStoredJWT();
        string jsonPayload = "{\"amount\":" + amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Withdrawal Result: {request.downloadHandler.text}</color>");
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[BackendAuthService] Withdraw Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Fetches the invite history from the backend.
    /// </summary>
    public void GetInviteHistory(Action<bool, List<InviteHistoryItem>> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(GetInviteHistoryCoroutine(onComplete));
    }

    private IEnumerator GetInviteHistoryCoroutine(Action<bool, List<InviteHistoryItem>> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/invites/history";
        string token = GetStoredJWT();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Invite History Result: {request.downloadHandler.text}</color>");
                try
                {
                    InviteHistoryResponse response = JsonUtility.FromJson<InviteHistoryResponse>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        onComplete?.Invoke(true, response.data);
                    }
                    else
                    {
                        onComplete?.Invoke(false, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] GetInviteHistory Parse Error: {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetInviteHistory Error: {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// Fetches the transaction history from the backend.
    /// </summary>
    public void GetTransactionHistory(Action<bool, List<TransactionHistoryItem>> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(GetTransactionHistoryCoroutine(onComplete));
    }

    private IEnumerator GetTransactionHistoryCoroutine(Action<bool, List<TransactionHistoryItem>> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/wallet/history";
        string token = GetStoredJWT();

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Transaction History Result: {request.downloadHandler.text}</color>");
                try
                {
                    TransactionHistoryResponse response = JsonUtility.FromJson<TransactionHistoryResponse>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        onComplete?.Invoke(true, response.data);
                    }
                    else
                    {
                        onComplete?.Invoke(false, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] GetTransactionHistory Parse Error: {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] GetTransactionHistory Error: {request.error}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    [Serializable]
    public class DepositResponse
    {
        public bool success;
        public string paymentIntentId;
        public string clientSecret;
        public string message;
    }

    /// <summary>
    /// Initiates a deposit request to obtain a payment intent.
    /// </summary>
    public void Deposit(float amount, string productId, Action<bool, DepositResponse> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, null);
            return;
        }
        StartCoroutine(DepositCoroutine(amount, productId, onComplete));
    }

    private IEnumerator DepositCoroutine(float amount, string productId, Action<bool, DepositResponse> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/wallet/deposit";
        string token = GetStoredJWT();
        string jsonPayload = $"{{\"amount\":{amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},\"productId\":\"{productId}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Deposit Result: {request.downloadHandler.text}</color>");
                try
                {
                    DepositResponse response = JsonUtility.FromJson<DepositResponse>(request.downloadHandler.text);
                    onComplete?.Invoke(true, response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] Deposit Parse Error: {e.Message}");
                    onComplete?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] Deposit Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// Synchronizes the completed payment intent with the server.
    /// </summary>
    public void SyncDeposit(string paymentIntentId, Action<bool, string> onComplete)
    {
        if (!HasStoredJWT())
        {
            onComplete?.Invoke(false, "Unauthorized: No stored JWT");
            return;
        }
        StartCoroutine(SyncDepositCoroutine(paymentIntentId, onComplete));
    }

    private IEnumerator SyncDepositCoroutine(string paymentIntentId, Action<bool, string> onComplete)
    {
        string url = baseUrl.TrimEnd('/') + "/api/wallet/sync-deposit/" + paymentIntentId;
        string token = GetStoredJWT();

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=#00ff00>[BackendAuthService] SUCCESS: Sync Deposit Result: {request.downloadHandler.text}</color>");
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
                            response.user.level, response.user.role, 0,
                            response.user.activeMatchId, response.user.activeRoomId
                        );
                        onComplete?.Invoke(true, "Deposit synced successfully!");
                    }
                    else
                    {
                        if (response != null && response.success)
                        {
                            Debug.LogWarning("[BackendAuthService] Deposit sync succeeded but no user wallet data was returned. Keeping current local balances.");
                            onComplete?.Invoke(true, "Deposit synced successfully (local balance unchanged).");
                        }
                        else
                        {
                            onComplete?.Invoke(false, "Failed to parse updated user data.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BackendAuthService] Sync Deposit Parse Error: {e.Message}");
                    onComplete?.Invoke(false, "JSON parsing failed.");
                }
            }
            else
            {
                Debug.LogError($"[BackendAuthService] Sync Deposit Error: {request.error} | {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
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
        public string activeMatchId;
        public string activeRoomId;
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

    [Serializable]
    public class TransactionHistoryResponse
    {
        public bool success;
        public List<TransactionHistoryItem> data;
    }

    [Serializable]
    public class TransactionHistoryItem
    {
        public string id;
        public float amount;
        public string type;
        public string status;
        public string method;
        public string createdAt;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public bool success;
        public LeaderboardMe me;
        public List<LeaderboardItem> leaderboard;
    }

    [Serializable]
    public class LeaderboardMe
    {
        public int rank;
        public string userId;
        public string name;
        public string avatar;
        public int wins;
        public int losses;
    }

    [Serializable]
    public class LeaderboardItem
    {
        public int rank;
        public string userId;
        public string name;
        public string avatar;
        public int wins;
        public int losses;
    }

    [Serializable]
    public class InvitedByResponse
    {
        public bool success;
        public bool alreadyAssigned;
    }

    [Serializable]
    public class InviteHistoryResponse
    {
        public bool success;
        public int count;
        public List<InviteHistoryItem> data;
    }

    [Serializable]
    public class InviteHistoryItem
    {
        public string userId;
        public string name;
        public string avatar;
        public int wins;
        public int losses;
        public bool isOnline;
        public string joinedAt;
    }
}
