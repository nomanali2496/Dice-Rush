using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Reflection;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google;
#endif

/// <summary>
/// Manages Google Sign-In authentication
/// </summary>
public class GoogleSignInManager : MonoBehaviour
{
    public static GoogleSignInManager Instance { get; private set; }
    
    [Header("Google Sign-In Settings")]
    [SerializeField] private string webClientId = ""; // Set this in the inspector or via code
    
    public Action<string, string, string, string, string> OnGoogleSignInSuccess; // Server Auth Code, ID Token, Email, DisplayName, PhotoUrl
    public Action<string> OnGoogleSignInFailure;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GoogleSignInManager] Instance created");
        }
        else
        {
            Debug.Log("[GoogleSignInManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Sets the Web Client ID (required for Google Sign-In)
    /// </summary>
    public void SetWebClientId(string clientId)
    {
        webClientId = clientId;
        Debug.Log($"[GoogleSignInManager] Web Client ID set: {clientId}");
    }
    
    /// <summary>
    /// Initiates Google Sign-In flow
    /// </summary>
    public void SignInWithGoogle()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            SignInWithGoogleAndroid();
        #elif UNITY_IOS && !UNITY_EDITOR
            SignInWithGoogleIOS();
        #else
            // Editor/unsupported platform
            Debug.LogWarning("[GoogleSignInManager] Google Sign-In is only supported on Android/iOS devices. Not available in editor.");
            OnGoogleSignInFailure?.Invoke("Google Sign-In not supported in editor. Please test on a device.");
        #endif
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private void SignInWithGoogleAndroid()
    {
        if (string.IsNullOrEmpty(webClientId))
        {
            Debug.LogError("[GoogleSignInManager] Web Client ID is not set! Please set it in the inspector or call SetWebClientId()");
            OnGoogleSignInFailure?.Invoke("Web Client ID not configured");
            return;
        }
        
        Debug.Log("[GoogleSignInManager] Starting Google Sign-In for Android...");
        
        try
        {
            // Configure Google Sign-In
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestAuthCode = true, // Required for PlayFab (this gives us ServerAuthCode)
                RequestEmail = true,
                RequestProfile = true
            };
            
            // Start the sign-in process
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith((Task<GoogleSignInUser> task) =>
            {
                if (task.IsFaulted)
                {
                    string error = task.Exception?.GetBaseException()?.Message ?? "Unknown error";
                    Debug.LogError($"[GoogleSignInManager] Google Sign-In failed: {error}");
                    OnGoogleSignInFailure?.Invoke(error);
                }
                else if (task.IsCanceled)
                {
                    Debug.LogWarning("[GoogleSignInManager] Google Sign-In was cancelled by user");
                    OnGoogleSignInFailure?.Invoke("Sign-in cancelled by user");
                }
                else
                {
                    var signInResult = task.Result;
                    Debug.Log($"[GoogleSignInManager] Google Sign-In successful! Email: {signInResult.Email}, DisplayName: {signInResult.DisplayName}");
                    
                    // Get the auth code (this is the ServerAuthCode for PlayFab)
                    string authCode = signInResult.AuthCode;
                    string idToken = signInResult.IdToken;
                    string email = signInResult.Email ?? "";
                    string displayName = signInResult.DisplayName ?? "";
                    
                    // Try to get photo URL - property may not be available in all SDK versions
                    string photoUrl = "";
                    try
                    {
                        // Use reflection to safely access PhotoUrl property if it exists
                        var photoUrlProperty = typeof(GoogleSignInUser).GetProperty("PhotoUrl");
                        if (photoUrlProperty != null)
                        {
                            var photoValue = photoUrlProperty.GetValue(signInResult);
                            if (photoValue != null)
                            {
                                photoUrl = photoValue.ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // PhotoUrl not available in this SDK version, continue without it
                        Debug.Log($"[GoogleSignInManager] PhotoUrl not available: {ex.Message}");
                    }
                    
                    if (string.IsNullOrEmpty(authCode))
                    {
                        Debug.LogError("[GoogleSignInManager] Auth Code is empty! Make sure RequestAuthCode is set to true in configuration.");
                        OnGoogleSignInFailure?.Invoke("Auth code not received from Google Sign-In");
                        return;
                    }
                    
                    // Notify success with auth code and profile data
                    // Parameters: serverAuthCode, idToken, email, displayName, photoUrl
                    OnGoogleSignInSuccess?.Invoke(authCode, idToken ?? "", email, displayName, photoUrl);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GoogleSignInManager] Exception during Google Sign-In: {ex.Message}");
            OnGoogleSignInFailure?.Invoke(ex.Message);
        }
    }
    #endif
    
    #if UNITY_IOS && !UNITY_EDITOR
    private void SignInWithGoogleIOS()
    {
        // iOS implementation would go here
        // This requires native iOS Google Sign-In SDK integration
        Debug.LogWarning("[GoogleSignInManager] iOS Google Sign-In not yet implemented");
        OnGoogleSignInFailure?.Invoke("iOS Google Sign-In not yet implemented");
    }
    #endif
    
    /// <summary>
    /// Signs out from Google account
    /// </summary>
    public void SignOut()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            GoogleSignIn.DefaultInstance.SignOut();
            Debug.Log("[GoogleSignInManager] Signed out from Google");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GoogleSignInManager] Error signing out: {ex.Message}");
        }
        #endif
    }
}

