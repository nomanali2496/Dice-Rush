using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using Facebook.Unity;
#endif

/// <summary>
/// Manages Facebook Sign-In authentication
/// </summary>
public class FacebookSignInManager : MonoBehaviour
{
    public static FacebookSignInManager Instance { get; private set; }
    
    public Action<string, string, string, string> OnFacebookSignInSuccess; // Access Token, Name, Email, ImageURL
    public Action<string> OnFacebookSignInFailure;
    
    private bool isInitialized = false;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[FacebookSignInManager] Instance created");
            
            // Initialize Facebook SDK if not already initialized
            InitializeFacebook();
        }
        else
        {
            Debug.Log("[FacebookSignInManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Initialize Facebook SDK
    /// </summary>
    private void InitializeFacebook()
    {
        #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        if (!FB.IsInitialized)
        {
            Debug.Log("[FacebookSignInManager] Initializing Facebook SDK...");
            FB.Init(OnFacebookInitComplete, OnHideUnity);
        }
        else
        {
            Debug.Log("[FacebookSignInManager] Facebook SDK already initialized");
            isInitialized = true;
        }
        #else
        Debug.LogWarning("[FacebookSignInManager] Facebook SDK not available on this platform");
        #endif
    }
    
    #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
    private void OnFacebookInitComplete()
    {
        Debug.Log("[FacebookSignInManager] Facebook SDK initialized successfully");
        isInitialized = true;
        
        // If user is already logged in, we can get the access token
        if (FB.IsLoggedIn && AccessToken.CurrentAccessToken != null)
        {
            Debug.Log("[FacebookSignInManager] User already logged in to Facebook");
        }
    }
    
    private void OnHideUnity(bool isGameShown)
    {
        Debug.Log($"[FacebookSignInManager] OnHideUnity called: {isGameShown}");
    }
    #endif
    
    /// <summary>
    /// Initiates Facebook Sign-In flow
    /// </summary>
    public void SignInWithFacebook()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR || UNITY_IOS && !UNITY_EDITOR
            SignInWithFacebookMobile();
        #else
            // Editor/unsupported platform
            Debug.LogWarning("[FacebookSignInManager] Facebook Sign-In is only supported on Android/iOS devices. Not available in editor.");
            OnFacebookSignInFailure?.Invoke("Facebook Sign-In not supported in editor. Please test on a device.");
        #endif
    }
    
    /// <summary>
    /// Attempts to silently sign in with Facebook (if user is already logged in)
    /// This can be used for automatic restoration of Facebook login
    /// </summary>
    public void AttemptSilentSignIn()
    {
        #if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_IOS && !UNITY_EDITOR)
            AttemptSilentSignInMobile();
        #else
            OnFacebookSignInFailure?.Invoke("Silent sign-in not supported on this platform");
        #endif
    }
    
    #if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_IOS && !UNITY_EDITOR)
    private void SignInWithFacebookMobile()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[FacebookSignInManager] Facebook SDK not initialized yet. Initializing now...");
            InitializeFacebook();
            
            // Wait a moment for initialization, then try again
            StartCoroutine(DelayedSignIn());
            return;
        }
        
        Debug.Log("[FacebookSignInManager] Starting Facebook Sign-In...");
        
        // Request permissions: public_profile and email (required for PlayFab)
        List<string> permissions = new List<string> { "public_profile", "email" };
        
        FB.LogInWithReadPermissions(permissions, OnFacebookLoginCallback);
    }
    
    private void AttemptSilentSignInMobile()
    {
        if (!isInitialized)
        {
            Debug.Log("[FacebookSignInManager] Facebook SDK not initialized yet, waiting for initialization...");
            // Wait for initialization
            StartCoroutine(DelayedSilentSignIn());
            return;
        }
        
        Debug.Log("[FacebookSignInManager] Attempting silent Facebook Sign-In...");
        
        // Check if user is already logged in to Facebook
        if (FB.IsLoggedIn && AccessToken.CurrentAccessToken != null)
        {
            string accessToken = AccessToken.CurrentAccessToken.TokenString;
            Debug.Log("[FacebookSignInManager] User already logged in to Facebook - fetching profile...");
            
            // Fetch profile data and notify success
            FetchFacebookProfile(accessToken);
        }
        else
        {
            Debug.Log("[FacebookSignInManager] User not logged in to Facebook - silent sign-in failed");
            OnFacebookSignInFailure?.Invoke("User not logged in to Facebook");
        }
    }
    
    private System.Collections.IEnumerator DelayedSilentSignIn()
    {
        // Wait for initialization (with timeout)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (!isInitialized && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (isInitialized)
        {
            // Try silent sign-in again
            AttemptSilentSignInMobile();
        }
        else
        {
            Debug.LogWarning("[FacebookSignInManager] Facebook SDK initialization timeout");
            OnFacebookSignInFailure?.Invoke("Facebook SDK initialization timeout");
        }
    }
    
    private System.Collections.IEnumerator DelayedSignIn()
    {
        // Wait for initialization
        while (!isInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Try sign in again
        SignInWithFacebookMobile();
    }
    
    private void OnFacebookLoginCallback(ILoginResult result)
    {
        if (result == null)
        {
            Debug.LogError("[FacebookSignInManager] Facebook login result is null");
            OnFacebookSignInFailure?.Invoke("Facebook login returned null result");
            return;
        }
        
        if (result.Error != null)
        {
            string errorMessage = $"Facebook login error: {result.Error}";
            Debug.LogError($"[FacebookSignInManager] {errorMessage}");
            OnFacebookSignInFailure?.Invoke(errorMessage);
            return;
        }
        
        if (result.Cancelled)
        {
            Debug.LogWarning("[FacebookSignInManager] Facebook login was cancelled by user");
            OnFacebookSignInFailure?.Invoke("Facebook login cancelled by user");
            return;
        }
        
        // Check if we have an access token
        if (AccessToken.CurrentAccessToken != null)
        {
            string accessToken = AccessToken.CurrentAccessToken.TokenString;
            Debug.Log($"[FacebookSignInManager] Facebook Sign-In successful! Access Token: {accessToken.Substring(0, Mathf.Min(20, accessToken.Length))}...");
            
            // Fetch Facebook profile data (name, email, image)
            FetchFacebookProfile(accessToken);
        }
        else
        {
            Debug.LogError("[FacebookSignInManager] Facebook login succeeded but access token is null");
            OnFacebookSignInFailure?.Invoke("Access token not received from Facebook");
        }
    }
    #endif
    
    /// <summary>
    /// Fetches Facebook profile data (name, email, image URL) using Graph API
    /// </summary>
    private void FetchFacebookProfile(string accessToken)
    {
        #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        try
        {
            Debug.Log("[FacebookSignInManager] Fetching Facebook profile data using Graph API...");
            
            // Use Graph API to fetch profile data (works on Android and iOS)
            // Request fields: name, email, picture (with width and height for better quality)
            string graphQuery = "/me?fields=name,email,picture.width(200).height(200)";
            
            FB.API(graphQuery, HttpMethod.GET, (IGraphResult result) =>
            {
                if (result.Error != null)
                {
                    Debug.LogWarning($"[FacebookSignInManager] Error fetching profile via Graph API: {result.Error}. Continuing with login anyway.");
                    // Continue with login even if profile fetch fails
                    OnFacebookSignInSuccess?.Invoke(accessToken, "", "", "");
                    return;
                }
                
                if (result.ResultDictionary != null)
                {
                    // Extract data from Graph API response
                    string name = "";
                    string email = "";
                    string imageUrl = "";
                    
                    // Get name
                    if (result.ResultDictionary.ContainsKey("name"))
                    {
                        name = result.ResultDictionary["name"]?.ToString() ?? "";
                    }
                    
                    // Get email
                    if (result.ResultDictionary.ContainsKey("email"))
                    {
                        email = result.ResultDictionary["email"]?.ToString() ?? "";
                    }
                    
                    // Get picture URL (nested in picture.data.url)
                    if (result.ResultDictionary.ContainsKey("picture"))
                    {
                        var pictureData = result.ResultDictionary["picture"] as Dictionary<string, object>;
                        if (pictureData != null && pictureData.ContainsKey("data"))
                        {
                            var data = pictureData["data"] as Dictionary<string, object>;
                            if (data != null && data.ContainsKey("url"))
                            {
                                imageUrl = data["url"]?.ToString() ?? "";
                            }
                        }
                    }
                    
                    Debug.Log($"[FacebookSignInManager] Profile fetched via Graph API - Name: {name}, Email: {email}, ImageURL: {imageUrl}");
                    
                    // Notify success with access token and profile data (including avatar)
                    OnFacebookSignInSuccess?.Invoke(accessToken, name, email, imageUrl);
                }
                else
                {
                    Debug.LogWarning("[FacebookSignInManager] Graph API result is null. Continuing with login anyway.");
                    OnFacebookSignInSuccess?.Invoke(accessToken, "", "", "");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FacebookSignInManager] Exception fetching profile: {ex.Message}. Continuing with login anyway.");
            // Continue with login even if profile fetch fails
            OnFacebookSignInSuccess?.Invoke(accessToken, "", "", "");
        }
        #else
        // On unsupported platforms, just pass empty strings
        OnFacebookSignInSuccess?.Invoke(accessToken, "", "", "");
        #endif
    }
    
    /// <summary>
    /// Signs out from Facebook account
    /// </summary>
    public void SignOut()
    {
        #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        try
        {
            FB.LogOut();
            Debug.Log("[FacebookSignInManager] Signed out from Facebook");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FacebookSignInManager] Error signing out: {ex.Message}");
        }
        #endif
    }
}

