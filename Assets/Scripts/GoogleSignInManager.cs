#pragma warning disable 0414
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
    
    private bool isConfigured = false; // Track if Google Sign-In has been configured
    
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
    
    /// <summary>
    /// Attempts to silently sign in with Google (if user is already signed in)
    /// This can be used for automatic restoration of Google login
    /// </summary>
    public void AttemptSilentSignIn()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            AttemptSilentSignInAndroid();
        #elif UNITY_IOS && !UNITY_EDITOR
            // iOS silent sign-in would go here
            OnGoogleSignInFailure?.Invoke("Silent sign-in not implemented for iOS");
        #else
            OnGoogleSignInFailure?.Invoke("Silent sign-in not supported in editor");
        #endif
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Ensures Google Sign-In is configured. Only configures once to avoid "already created" errors.
    /// </summary>
    private void EnsureGoogleSignInConfigured()
    {
        if (isConfigured)
        {
            // Already configured, skip
            return;
        }
        
        if (string.IsNullOrEmpty(webClientId))
        {
            throw new Exception("Web Client ID is not set! Please set it in the inspector or call SetWebClientId()");
        }
        
        try
        {
            // Configure Google Sign-In only once
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestAuthCode = true, // gives us ServerAuthCode
                RequestEmail = true,
                RequestProfile = true
            };
            
            isConfigured = true;
            Debug.Log("[GoogleSignInManager] Google Sign-In configured successfully");
        }
        catch (Exception ex)
        {
            // Configuration might already be set - check if DefaultInstance exists
            // If it does, mark as configured anyway
            try
            {
                var instance = GoogleSignIn.DefaultInstance;
                if (instance != null)
                {
                    isConfigured = true;
                    Debug.Log("[GoogleSignInManager] Google Sign-In already configured, reusing existing configuration");
                }
                else
                {
                    throw ex; // Re-throw if instance is null
                }
            }
            catch
            {
                throw ex; // Re-throw original exception if we can't access instance
            }
        }
    }
    
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
            // Ensure configuration is set (only sets if not already configured)
            EnsureGoogleSignInConfigured();
            
            // Start the sign-in process
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith((Task<GoogleSignInUser> task) =>
            {
                HandleSignInResult(task, false);
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GoogleSignInManager] Exception during Google Sign-In: {ex.Message}");
            OnGoogleSignInFailure?.Invoke(ex.Message);
        }
    }
    
    private void AttemptSilentSignInAndroid()
    {
        if (string.IsNullOrEmpty(webClientId))
        {
            Debug.Log("[GoogleSignInManager] Web Client ID not set, cannot attempt silent sign-in");
            OnGoogleSignInFailure?.Invoke("Web Client ID not configured");
            return;
        }
        
        Debug.Log("[GoogleSignInManager] Attempting silent Google Sign-In...");
        
        try
        {
            // Ensure configuration is set (only sets if not already configured)
            EnsureGoogleSignInConfigured();
            
            // Try to sign in silently (without user interaction)
            GoogleSignIn.DefaultInstance.SignInSilently().ContinueWith((Task<GoogleSignInUser> task) =>
            {
                HandleSignInResult(task, true);
            });
        }
        catch (Exception ex)
        {
            Debug.Log($"[GoogleSignInManager] Silent sign-in failed: {ex.Message}");
            OnGoogleSignInFailure?.Invoke(ex.Message);
        }
    }
    
    private void HandleSignInResult(Task<GoogleSignInUser> task, bool isSilent)
    {
        if (task.IsFaulted)
        {
            string error = task.Exception?.GetBaseException()?.Message ?? "Unknown error";
            string logMessage = isSilent ? 
                $"[GoogleSignInManager] Silent Google Sign-In failed: {error}" : 
                $"[GoogleSignInManager] Google Sign-In failed: {error}";
            Debug.Log(logMessage);
            OnGoogleSignInFailure?.Invoke(error);
        }
        else if (task.IsCanceled)
        {
            string logMessage = isSilent ? 
                "[GoogleSignInManager] Silent sign-in was cancelled" : 
                "[GoogleSignInManager] Google Sign-In was cancelled by user";
            Debug.Log(logMessage);
            OnGoogleSignInFailure?.Invoke(isSilent ? "Silent sign-in cancelled" : "Sign-in cancelled by user");
        }
        else
        {
            var signInResult = task.Result;
            string logMessage = isSilent ? 
                $"[GoogleSignInManager] Silent Google Sign-In successful! Email: {signInResult.Email}" : 
                $"[GoogleSignInManager] Google Sign-In successful! Email: {signInResult.Email}, DisplayName: {signInResult.DisplayName}";
            Debug.Log(logMessage);
            
            // Get the auth code (this is the ServerAuthCode)
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

