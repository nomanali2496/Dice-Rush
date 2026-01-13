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
    
    public Action<string> OnFacebookSignInSuccess; // Access Token
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
    
    #if UNITY_ANDROID && !UNITY_EDITOR || UNITY_IOS && !UNITY_EDITOR
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
            
            // Notify success with access token (PlayFab uses this)
            OnFacebookSignInSuccess?.Invoke(accessToken);
        }
        else
        {
            Debug.LogError("[FacebookSignInManager] Facebook login succeeded but access token is null");
            OnFacebookSignInFailure?.Invoke("Access token not received from Facebook");
        }
    }
    #endif
    
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

