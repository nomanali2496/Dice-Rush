using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;
using PlayFab;

/// <summary>
/// Handles Login and Registration in a single scene
/// Shows Login/Register screen first, then SplashScreen after authentication
/// </summary>
public class AuthenticationManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginRegisterPanel;
    [SerializeField] private GameObject splashScreenPanel;
    [SerializeField] private GameObject loadingScreenPanel; // Loading screen that shows until login screen hides
    [SerializeField] private GameObject internetPanel; // Internet error panel
    [SerializeField] private Button internetRetryButton; // Retry button on internet panel
    
    [Header("Login/Register Toggle")]
    [SerializeField] private GameObject loginSection;
    [SerializeField] private GameObject registerSection;
    [SerializeField] private Button switchToRegisterButton;
    [SerializeField] private Button switchToLoginButton;
    
    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button guestLoginButton;
    [SerializeField] private Button googleLoginButton;
    [SerializeField] private Button facebookLoginButton;
    [SerializeField] private Toggle loginPasswordToggle;
    
    [Header("Registration UI")]
    [SerializeField] private TMP_InputField registerNameInput;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    // [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Toggle registerPasswordToggle;
    
    [Header("Status")]
    // [SerializeField] private TextMeshProUGUI statusText;
    // [SerializeField] private GameObject loadingIndicator;
    
    [Header("Settings")]
    [SerializeField] private int minPasswordLength = 6;
    
    public Action OnAuthenticationSuccess;
    
    private bool isLoginPasswordVisible = false;
    private bool isRegisterPasswordVisible = false;
    private bool isAttemptingAutoLogin = false;
    private bool isRegistering = false; // Track if we're currently registering
    private Action lastAttemptedAction = null; // Store last action for retry
    
    private void Start()
    {
        // Initialize PlayFab
        InitializePlayFab();
        
        // Setup button listeners
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);
        
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterClicked);
        
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(OnGuestLoginClicked);
        
        if (googleLoginButton != null)
            googleLoginButton.onClick.AddListener(OnGoogleLoginClicked);
        
        if (facebookLoginButton != null)
            facebookLoginButton.onClick.AddListener(OnFacebookLoginClicked);
        
        if (switchToRegisterButton != null)
            switchToRegisterButton.onClick.AddListener(() => ShowRegisterSection());
        
        if (switchToLoginButton != null)
            switchToLoginButton.onClick.AddListener(() => ShowLoginSection());
        
        if (loginPasswordToggle != null)
            loginPasswordToggle.onValueChanged.AddListener(OnLoginPasswordToggleChanged);
        
        if (registerPasswordToggle != null)
            registerPasswordToggle.onValueChanged.AddListener(OnRegisterPasswordToggleChanged);
        
        if (internetRetryButton != null)
            internetRetryButton.onClick.AddListener(OnRetryClicked);
        
        // Hide internet panel initially
        if (internetPanel != null)
            internetPanel.SetActive(false);
        
        // Subscribe to PlayFab events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginComplete += OnLoginComplete;
            PlayFabManager.Instance.OnRegisterComplete += OnRegisterComplete;
        }
        
        // Show loading screen initially (covers everything)
        ShowLoadingScreen();
        
        // IMMEDIATE CHECK: If user is already logged in, show splash screen immediately (no delay)
        // This prevents the login screen from flashing briefly
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.Log("[AuthenticationManager] User is already logged in, hiding login screen and showing splash");
            // Hide login screen first, then show splash screen (loading screen will hide when splash shows)
            HideLoginScreen();
            ShowSplashScreenPanel();
            SetLoadingState(false);
            UpdateStatusText("");
            OnAuthenticationSuccess?.Invoke();
            return; // Don't attempt auto-login, user is already logged in
        }
        
        // If not logged in, attempt auto-login (will show login panel if auto-login fails)
        AttemptAutoLogin();
    }
    
    private void OnDestroy()
    {
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginComplete -= OnLoginComplete;
            PlayFabManager.Instance.OnRegisterComplete -= OnRegisterComplete;
        }
    }
    
    private void InitializePlayFab()
    {
        if (PlayFabManager.Instance == null)
        {
            GameObject playfabObj = new GameObject("PlayFabManager");
            playfabObj.AddComponent<PlayFabManager>();
            Debug.Log("PlayFabManager initialized");
        }
        
        // Initialize GoogleSignInManager if it doesn't exist
        if (GoogleSignInManager.Instance == null)
        {
            GameObject googleSignInObj = new GameObject("GoogleSignInManager");
            googleSignInObj.AddComponent<GoogleSignInManager>();
            Debug.Log("GoogleSignInManager initialized");
        }
        
        // Initialize FacebookSignInManager if it doesn't exist
        if (FacebookSignInManager.Instance == null)
        {
            GameObject facebookSignInObj = new GameObject("FacebookSignInManager");
            facebookSignInObj.AddComponent<FacebookSignInManager>();
            Debug.Log("FacebookSignInManager initialized");
        }
    }
    
    /// <summary>
    /// Attempts to auto-login: first checks for existing session, then tries device ID login
    /// </summary>
    private void AttemptAutoLogin()
    {
        lastAttemptedAction = AttemptAutoLogin;
        // Wait a frame to ensure PlayFabManager is fully initialized
        StartCoroutine(AttemptAutoLoginCoroutine());
    }
    
    private System.Collections.IEnumerator AttemptAutoLoginCoroutine()
    {
        // Wait a frame for PlayFabManager to initialize
        yield return null;
        
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[AuthenticationManager] PlayFabManager not found, showing login panel");
            // Hide loading screen and show login panel
            ShowLoginRegisterPanel();
            ShowLoginSection();
            SetLoadingState(false);
            UpdateStatusText("");
            yield break;
        }
        
        // Note: We already checked IsLoggedIn() in Start() immediately, so we skip that check here
        // This coroutine only runs if user is NOT already logged in
        
        // First check: If user has explicitly logged out, don't attempt auto-login
        if (PlayFabManager.Instance.HasUserExplicitlyLoggedOut())
        {
            Debug.Log("[AuthenticationManager] User has explicitly logged out, showing login panel (no auto-login)");
            isAttemptingAutoLogin = false;
            // Hide loading screen and show login panel
            ShowLoginRegisterPanel();
            ShowLoginSection();
            SetLoadingState(false);
            UpdateStatusText("");
            yield break;
        }
        
        SetLoadingState(true);
        UpdateStatusText("Checking for saved session...");
        isAttemptingAutoLogin = true;
        
        // Check if this is a new device
        bool isNewDevice = PlayFabManager.Instance.IsNewDevice();
        if (isNewDevice)
        {
            Debug.Log("[AuthenticationManager] New device detected - showing login screen");
            // On new device, always show login screen instead of auto-login
            isAttemptingAutoLogin = false;
            ShowLoginRegisterPanel();
            ShowLoginSection();
            SetLoadingState(false);
            UpdateStatusText("");
            yield break;
        }
        
        // Third check: Check if there's an existing PlayFab session
        if (PlayFabManager.Instance.HasExistingSession())
        {
            Debug.Log("[AuthenticationManager] Existing session found, attempting to restore...");
            UpdateStatusText("Restoring session...");
            
            PlayFabManager.Instance.RestoreSession((success) =>
            {
                if (success)
                {
                    Debug.Log("[AuthenticationManager] Session restored successfully!");
                    UpdateStatusText("Welcome back!");
                    isAttemptingAutoLogin = false;
                    
                    // Show splash screen after successful session restore
                    Invoke(nameof(ShowSplashScreenPanel), 0.5f);
                    
                    // Notify listeners
                    OnAuthenticationSuccess?.Invoke();
                }
                else
                {
                    Debug.Log("[AuthenticationManager] Session restore failed, checking last login method...");
                    // Session restore failed - check what login method was used last
                    HandleSessionRestoreFailed();
                }
                SetLoadingState(false);
            });
        }
        else
        {
            Debug.Log("[AuthenticationManager] No existing session, checking last login method...");
            // No existing session - check what login method was used last
            HandleSessionRestoreFailed();
        }
    }
    
    /// <summary>
    /// Handles the case when session restore fails - checks last login method and acts accordingly
    /// </summary>
    private void HandleSessionRestoreFailed()
    {
        string lastLoginMethod = PlayFabManager.Instance.GetLastLoginMethod();
        Debug.Log($"[AuthenticationManager] Last login method was: {lastLoginMethod ?? "Unknown"}");
        
        // Only auto-login as guest if the last login was actually guest
        if (lastLoginMethod == "Guest")
        {
            Debug.Log("[AuthenticationManager] Last login was guest - attempting device ID auto-login");
            AttemptDeviceIdLogin();
        }
        else if (lastLoginMethod == "Google")
        {
            // Try to silently restore Google login first
            Debug.Log("[AuthenticationManager] Last login was Google - attempting silent Google sign-in...");
            AttemptSilentGoogleLogin();
        }
        else if (lastLoginMethod == "Facebook")
        {
            // Try to silently restore Facebook login first
            Debug.Log("[AuthenticationManager] Last login was Facebook - attempting silent Facebook sign-in...");
            AttemptSilentFacebookLogin();
        }
        else if (lastLoginMethod == "Email")
        {
            // Try to auto-login with stored email credentials
            Debug.Log("[AuthenticationManager] Last login was Email - attempting auto-login with stored credentials...");
            AttemptEmailAutoLogin();
        }
        else
        {
            // Unknown login method - show login screen
            Debug.Log("[AuthenticationManager] Unknown last login method - showing login screen");
            isAttemptingAutoLogin = false;
            ShowLoginRegisterPanel();
            ShowLoginSection();
            SetLoadingState(false);
            UpdateStatusText("");
        }
    }
    
    /// <summary>
    /// Attempts to silently restore Google login
    /// </summary>
    private void AttemptSilentGoogleLogin()
    {
        lastAttemptedAction = AttemptSilentGoogleLogin;
        UpdateStatusText("Restoring Google login...");
        
        if (GoogleSignInManager.Instance != null)
        {
            // Subscribe to Google Sign-In events
            GoogleSignInManager.Instance.OnGoogleSignInSuccess += OnSilentGoogleSignInSuccess;
            GoogleSignInManager.Instance.OnGoogleSignInFailure += OnSilentGoogleSignInFailure;
            
            // Attempt silent sign-in
            GoogleSignInManager.Instance.AttemptSilentSignIn();
        }
        else
        {
            Debug.LogWarning("[AuthenticationManager] GoogleSignInManager not found, showing login screen");
            OnSilentGoogleSignInFailure("GoogleSignInManager not available");
        }
    }
    
    private void OnSilentGoogleSignInSuccess(string serverAuthCode, string idToken, string email, string displayName, string photoUrl)
    {
        Debug.Log($"[AuthenticationManager] Silent Google Sign-In successful! Restoring PlayFab session...");
        
        // Unsubscribe from events first
        if (GoogleSignInManager.Instance != null)
        {
            GoogleSignInManager.Instance.OnGoogleSignInSuccess -= OnSilentGoogleSignInSuccess;
            GoogleSignInManager.Instance.OnGoogleSignInFailure -= OnSilentGoogleSignInFailure;
        }
        
        UpdateStatusText("Logging into PlayFab...");
        
        if (PlayFabManager.Instance != null)
        {
            // Login to PlayFab with Google auth code
            PlayFabManager.Instance.LoginWithGoogle(serverAuthCode, email, displayName, photoUrl);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
            isAttemptingAutoLogin = false;
        }
    }
    
    private void OnSilentGoogleSignInFailure(string error)
    {
        Debug.Log($"[AuthenticationManager] Silent Google Sign-In failed: {error} - showing login screen");
        
        // Unsubscribe from events
        if (GoogleSignInManager.Instance != null)
        {
            GoogleSignInManager.Instance.OnGoogleSignInSuccess -= OnSilentGoogleSignInSuccess;
            GoogleSignInManager.Instance.OnGoogleSignInFailure -= OnSilentGoogleSignInFailure;
        }
        
        // Silent sign-in failed - show login screen so user can manually sign in
        isAttemptingAutoLogin = false;
        ShowLoginRegisterPanel();
        ShowLoginSection();
        SetLoadingState(false);
        UpdateStatusText("");

        // If failed due to network, show internet panel
        if (Application.internetReachability == NetworkReachability.NotReachable || (error != null && error.ToLower().Contains("network")))
        {
            ShowInternetPanel(AttemptSilentGoogleLogin);
        }
    }
    
    /// <summary>
    /// Attempts to silently restore Facebook login
    /// </summary>
    private void AttemptSilentFacebookLogin()
    {
        lastAttemptedAction = AttemptSilentFacebookLogin;
        UpdateStatusText("Restoring Facebook login...");
        
        if (FacebookSignInManager.Instance != null)
        {
            // Subscribe to Facebook Sign-In events
            FacebookSignInManager.Instance.OnFacebookSignInSuccess += OnSilentFacebookSignInSuccess;
            FacebookSignInManager.Instance.OnFacebookSignInFailure += OnSilentFacebookSignInFailure;
            
            // Wait a moment for Facebook SDK to initialize, then attempt silent sign-in
            StartCoroutine(AttemptSilentFacebookLoginCoroutine());
        }
        else
        {
            Debug.LogWarning("[AuthenticationManager] FacebookSignInManager not found, showing login screen");
            OnSilentFacebookSignInFailure("FacebookSignInManager not available");
        }
    }
    
    private System.Collections.IEnumerator AttemptSilentFacebookLoginCoroutine()
    {
        // Wait a moment for Facebook SDK to initialize
        yield return new WaitForSeconds(0.5f);
        
        // Attempt silent sign-in
        FacebookSignInManager.Instance.AttemptSilentSignIn();
    }
    
    private void OnSilentFacebookSignInSuccess(string accessToken, string facebookName, string facebookEmail, string facebookImageUrl)
    {
        Debug.Log($"[AuthenticationManager] Silent Facebook Sign-In successful! Restoring PlayFab session...");
        
        // Unsubscribe from events first
        if (FacebookSignInManager.Instance != null)
        {
            FacebookSignInManager.Instance.OnFacebookSignInSuccess -= OnSilentFacebookSignInSuccess;
            FacebookSignInManager.Instance.OnFacebookSignInFailure -= OnSilentFacebookSignInFailure;
        }
        
        UpdateStatusText("Logging into PlayFab...");
        
        if (PlayFabManager.Instance != null)
        {
            // Login to PlayFab with Facebook access token
            PlayFabManager.Instance.LoginWithFacebook(accessToken, facebookName, facebookEmail, facebookImageUrl);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
            isAttemptingAutoLogin = false;
        }
    }
    
    private void OnSilentFacebookSignInFailure(string error)
    {
        Debug.Log($"[AuthenticationManager] Silent Facebook Sign-In failed: {error} - showing login screen");
        
        // Unsubscribe from events
        if (FacebookSignInManager.Instance != null)
        {
            FacebookSignInManager.Instance.OnFacebookSignInSuccess -= OnSilentFacebookSignInSuccess;
            FacebookSignInManager.Instance.OnFacebookSignInFailure -= OnSilentFacebookSignInFailure;
        }
        
        // Silent sign-in failed - show login screen so user can manually sign in
        isAttemptingAutoLogin = false;
        ShowLoginRegisterPanel();
        ShowLoginSection();
        SetLoadingState(false);
        UpdateStatusText("");

        // If failed due to network, show internet panel
        if (Application.internetReachability == NetworkReachability.NotReachable || (error != null && error.ToLower().Contains("network")))
        {
            ShowInternetPanel(AttemptSilentFacebookLogin);
        }
    }
    
    /// <summary>
    /// Attempts to auto-login with stored email credentials
    /// </summary>
    private void AttemptEmailAutoLogin()
    {
        lastAttemptedAction = AttemptEmailAutoLogin;
        UpdateStatusText("Restoring email login...");
        
        if (PlayFabManager.Instance != null)
        {
            // This will attempt login if credentials are stored, or return false if not
            PlayFabManager.Instance.AttemptAutoLoginWithEmail((success) =>
            {
                if (!success)
                {
                    // No stored credentials or login failed - show login screen
                    Debug.Log("[AuthenticationManager] Email auto-login failed - showing login screen");
                    isAttemptingAutoLogin = false;
                    ShowLoginRegisterPanel();
                    ShowLoginSection();
                    SetLoadingState(false);
                    UpdateStatusText("");
                }
                // If success, OnLoginComplete will be called automatically
            });
        }
        else
        {
            Debug.LogWarning("[AuthenticationManager] PlayFabManager not found, showing login screen");
            isAttemptingAutoLogin = false;
            ShowLoginRegisterPanel();
            ShowLoginSection();
            SetLoadingState(false);
            UpdateStatusText("");
        }
    }
    
    /// <summary>
    /// Attempts to login with device ID (guest account)
    /// </summary>
    private void AttemptDeviceIdLogin()
    {
        lastAttemptedAction = AttemptDeviceIdLogin;
        UpdateStatusText("Logging in as guest...");
        
        string customId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log($"[AuthenticationManager] Attempting auto-login with device ID: {customId}");
        
        if (PlayFabManager.Instance != null)
        {
            // The OnLoginComplete callback will handle the result
            // We use isAttemptingAutoLogin flag to handle failures silently
            PlayFabManager.Instance.LoginWithCustomId(customId, true);
        }
        else
        {
            Debug.LogWarning("[AuthenticationManager] PlayFabManager not found, showing login panel");
            isAttemptingAutoLogin = false;
            ShowLoginRegisterPanel();
            ShowLoginSection();
            SetLoadingState(false);
            UpdateStatusText("");
        }
    }
    
    #region Panel Management
    
    /// <summary>
    /// Shows the login/register panel (public method for external access)
    /// </summary>
    public void ShowLoginRegisterPanel()
    {
        // Don't show login panel if user is already logged in
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            Debug.Log("[AuthenticationManager] User is already logged in, preventing login panel from showing");
            ShowSplashScreenPanel();
            return;
        }
        
        // Hide loading screen when login screen is shown
        HideLoadingScreen();
        
        if (loginRegisterPanel != null)
            loginRegisterPanel.SetActive(true);
        
        if (splashScreenPanel != null)
            splashScreenPanel.SetActive(false);
    }
    
    /// <summary>
    /// Hides the login/register panel
    /// </summary>
    private void HideLoginScreen()
    {
        if (loginRegisterPanel != null)
            loginRegisterPanel.SetActive(false);
    }
    private void HideRegisterScreen()
    {
        if (registerSection != null)
            registerSection.SetActive(false);
    }
    
    /// <summary>
    /// Shows the loading screen (covers everything)
    /// </summary>
    private void ShowLoadingScreen()
    {
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(true);
    }
    
    /// <summary>
    /// Hides the loading screen
    /// </summary>
    private void HideLoadingScreen()
    {
        if (loadingScreenPanel != null)
            loadingScreenPanel.SetActive(false);
    }
    
    private void ShowSplashScreenPanel()
    {
        // Hide login screen if it's showing
        HideLoginScreen();
        
        // Hide loading screen when showing splash screen
        HideLoadingScreen();
        
        if (splashScreenPanel != null)
            splashScreenPanel.SetActive(true);
    }
    
    /// <summary>
    /// Shows the splash screen/loading panel (public method for external access)
    /// </summary>
    public void ShowSplashScreenPanelPublic()
    {
        ShowSplashScreenPanel();
    }
    
    private void ShowLoginSection()
    {
        if (loginSection != null)
            loginSection.SetActive(true);
        
        if (registerSection != null)
            registerSection.SetActive(false);
        
        UpdateStatusText("");
    }
    
    private void ShowRegisterSection()
    {
        if (loginSection != null)
            loginSection.SetActive(false);
        
        if (registerSection != null)
            registerSection.SetActive(true);
        
        UpdateStatusText("");
    }
    
    #endregion
    
    #region Login
    
    private void OnRetryClicked()
    {
        if (internetPanel != null)
            internetPanel.SetActive(false);

        if (lastAttemptedAction != null)
        {
            Debug.Log($"[AuthenticationManager] Retrying action: {lastAttemptedAction.Method.Name}");
            lastAttemptedAction.Invoke();
        }
        else
        {
            Debug.LogWarning("[AuthenticationManager] No last action recorded for retry. Showing login screen.");
            ShowLoginRegisterPanel();
            ShowLoginSection();
        }
    }
    
    private void ShowInternetPanel(Action retryAction)
    {
        if (retryAction != null)
            lastAttemptedAction = retryAction;
            
        SetLoadingState(false);
        
        if (internetPanel != null && !internetPanel.activeSelf)
        {
            internetPanel.SetActive(true);
        }
    }
    
    private bool CheckInternetAndAction(Action action)
    {
        lastAttemptedAction = action;
        
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowInternetPanel(action);
            return false;
        }
        
        return true;
    }
    
    private void OnLoginClicked()
    {
        if (!CheckInternetAndAction(OnLoginClicked)) return;
        
        string email = loginEmailInput != null ? loginEmailInput.text : "";
        string password = loginPasswordInput != null ? loginPasswordInput.text : "";
        
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatusText("Please enter email and password!");
            return;
        }
        
        if (!IsValidEmail(email))
        {
            UpdateStatusText("Please enter a valid email address!");
            return;
        }
        
        SetLoadingState(true);
        UpdateStatusText("Logging in...");
        
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.LoginWithEmail(email, password);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
        }
    }
    
    private void OnGuestLoginClicked()
    {
        if (!CheckInternetAndAction(OnGuestLoginClicked)) return;
        
        SetLoadingState(true);
        UpdateStatusText("Logging in as guest...");
        
        string customId = SystemInfo.deviceUniqueIdentifier;
        
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.LoginWithCustomId(customId);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
        }
    }
    
    private void OnGoogleLoginClicked()
    {
        if (!CheckInternetAndAction(OnGoogleLoginClicked)) return;
        
        SetLoadingState(true);
        UpdateStatusText("Signing in with Google...");
        
        if (GoogleSignInManager.Instance != null)
        {
            // Subscribe to Google Sign-In events
            GoogleSignInManager.Instance.OnGoogleSignInSuccess += OnGoogleSignInSuccess;
            GoogleSignInManager.Instance.OnGoogleSignInFailure += OnGoogleSignInFailure;
            
            // Start Google Sign-In
            GoogleSignInManager.Instance.SignInWithGoogle();
        }
        else
        {
            UpdateStatusText("Error: GoogleSignInManager not found!");
            SetLoadingState(false);
        }
    }
    
    private void OnGoogleSignInSuccess(string serverAuthCode, string idToken, string email, string displayName, string photoUrl)
    {
        Debug.Log($"[AuthenticationManager] Google Sign-In successful, logging into PlayFab... Email: {email}, Name: {displayName}");
        UpdateStatusText("Logging into PlayFab...");
        
        if (PlayFabManager.Instance != null)
        {
            // PlayFab requires ServerAuthCode, and we pass Google profile data
            PlayFabManager.Instance.LoginWithGoogle(serverAuthCode, email, displayName, photoUrl);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
        }
        
        // Unsubscribe from events
        if (GoogleSignInManager.Instance != null)
        {
            GoogleSignInManager.Instance.OnGoogleSignInSuccess -= OnGoogleSignInSuccess;
            GoogleSignInManager.Instance.OnGoogleSignInFailure -= OnGoogleSignInFailure;
        }
    }
    
    private void OnGoogleSignInFailure(string error)
    {
        Debug.LogError($"[AuthenticationManager] Google Sign-In failed: {error}");
        UpdateStatusText($"Google Sign-In failed: {error}");
        SetLoadingState(false);
        
        // Unsubscribe from events
        if (GoogleSignInManager.Instance != null)
        {
            GoogleSignInManager.Instance.OnGoogleSignInSuccess -= OnGoogleSignInSuccess;
            GoogleSignInManager.Instance.OnGoogleSignInFailure -= OnGoogleSignInFailure;
        }

        // If failed due to network, show internet panel
        if (Application.internetReachability == NetworkReachability.NotReachable || (error != null && error.ToLower().Contains("network")))
        {
            ShowInternetPanel(OnGoogleLoginClicked);
        }
    }
    
    private void OnFacebookLoginClicked()
    {
        if (!CheckInternetAndAction(OnFacebookLoginClicked)) return;
        
        SetLoadingState(true);
        UpdateStatusText("Signing in with Facebook...");
        
        if (FacebookSignInManager.Instance != null)
        {
            // Subscribe to Facebook Sign-In events
            FacebookSignInManager.Instance.OnFacebookSignInSuccess += OnFacebookSignInSuccess;
            FacebookSignInManager.Instance.OnFacebookSignInFailure += OnFacebookSignInFailure;
            
            // Start Facebook Sign-In
            FacebookSignInManager.Instance.SignInWithFacebook();
        }
        else
        {
            UpdateStatusText("Error: FacebookSignInManager not found!");
            SetLoadingState(false);
        }
    }
    
    private void OnFacebookSignInSuccess(string accessToken, string facebookName, string facebookEmail, string facebookImageUrl)
    {
        Debug.Log($"[AuthenticationManager] Facebook Sign-In successful, logging into PlayFab... Name: {facebookName}, Email: {facebookEmail}, ImageURL: {facebookImageUrl}");
        UpdateStatusText("Logging into PlayFab...");
        
        if (PlayFabManager.Instance != null)
        {
            // Pass Facebook profile data (including avatar) to PlayFab
            PlayFabManager.Instance.LoginWithFacebook(accessToken, facebookName, facebookEmail, facebookImageUrl);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
        }
        
        // Unsubscribe from events
        if (FacebookSignInManager.Instance != null)
        {
            FacebookSignInManager.Instance.OnFacebookSignInSuccess -= OnFacebookSignInSuccess;
            FacebookSignInManager.Instance.OnFacebookSignInFailure -= OnFacebookSignInFailure;
        }
    }
    
    private void OnFacebookSignInFailure(string error)
    {
        Debug.LogError($"[AuthenticationManager] Facebook Sign-In failed: {error}");
        UpdateStatusText($"Facebook Sign-In failed: {error}");
        SetLoadingState(false);
        
        // Unsubscribe from events
        if (FacebookSignInManager.Instance != null)
        {
            FacebookSignInManager.Instance.OnFacebookSignInSuccess -= OnFacebookSignInSuccess;
            FacebookSignInManager.Instance.OnFacebookSignInFailure -= OnFacebookSignInFailure;
        }

        // If failed due to network, show internet panel
        if (Application.internetReachability == NetworkReachability.NotReachable || (error != null && error.ToLower().Contains("network")))
        {
            ShowInternetPanel(OnFacebookLoginClicked);
        }
    }
    
    #endregion
    
    #region Registration
    
    private void OnRegisterClicked()
    {
        if (!CheckInternetAndAction(OnRegisterClicked)) return;
        
        string name = registerNameInput != null ? registerNameInput.text : "";
        string email = registerEmailInput != null ? registerEmailInput.text : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        // string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";
        Debug.Log("Enter");
        if (!ValidateRegistration(name, email, password))
        {
        Debug.Log("Enter invalidate");
            
            return;
        }
        
        SetLoadingState(true);
        UpdateStatusText("Creating account...");
        isRegistering = true; // Mark that we're registering
        
        // Clear any existing PlayFab session to ensure clean registration
        if (PlayFabManager.Instance != null)
        {
            // Clear any stored email credentials before registration to avoid conflicts
            PlayFabManager.Instance.ClearStoredEmailCredentials();
            
            // Clear any existing PlayFab session
            PlayFabClientAPI.ForgetAllCredentials();
            
            // Register the new account
            PlayFabManager.Instance.RegisterPlayer(email, password, name);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
            isRegistering = false;
        }
    }
    
    private bool ValidateRegistration(string name, string email, string password)
    {
        if (string.IsNullOrEmpty(name))
        {
            UpdateStatusText("Please enter your name!");
            return false;
        }
        
        if (string.IsNullOrEmpty(email))
        {
            UpdateStatusText("Please enter your email!");
            return false;
        }
        
        if (!IsValidEmail(email))
        {
            UpdateStatusText("Please enter a valid email address!");
            return false;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            UpdateStatusText("Please enter a password!");
            return false;
        }
        
        if (password.Length < minPasswordLength)
        {
            UpdateStatusText($"Password must be at least {minPasswordLength} characters!");
            return false;
        }
        
        // if (password != confirmPassword)
        // {
        //     UpdateStatusText("Passwords do not match!");
        //     return false;
        // }
        
        return true;
    }
    
    #endregion
    
    #region Callbacks
    
    private void OnLoginComplete(bool success)
    {
        // If we're registering, ignore this callback - OnRegisterComplete will handle it
        if (isRegistering)
        {
            Debug.Log("[AuthenticationManager] Ignoring OnLoginComplete during registration - OnRegisterComplete will handle it");
            return;
        }
        
        SetLoadingState(false);
        
        if (success)
        {
            if (isAttemptingAutoLogin)
            {
                Debug.Log("[AuthenticationManager] Auto-login successful!");
                UpdateStatusText("Welcome!");
                isAttemptingAutoLogin = false;
            }
            else
            {
                UpdateStatusText("Login successful!");
            }
            
            // Show splash screen after successful login
            Invoke(nameof(ShowSplashScreenPanel), 0.5f);
            
            // Notify listeners
            OnAuthenticationSuccess?.Invoke();
        }
        else
        {
            if (isAttemptingAutoLogin)
            {
                // Auto-login failed silently, hide loading screen and show login panel
                Debug.Log("[AuthenticationManager] Auto-login failed, showing login panel");
                isAttemptingAutoLogin = false;
                ShowLoginRegisterPanel(); // This will hide loading screen and show login panel
                ShowLoginSection();
                UpdateStatusText("");
            }
            else
            {
                // Manual login failed, show error message
                // Try to get a more specific error message from PlayFabManager
                string errorMsg = "";
                if (PlayFabManager.Instance != null)
                {
                    errorMsg = PlayFabManager.Instance.GetLastErrorMessage();
                }
                
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    UpdateStatusText(errorMsg);
                }
                else
                {
                    UpdateStatusText("Login failed! Please check your credentials.");
                }

                // If login failed due to network, show internet panel
                if (Application.internetReachability == NetworkReachability.NotReachable || (errorMsg != null && errorMsg.ToLower().Contains("network")))
                {
                    ShowInternetPanel(lastAttemptedAction);
                }
            }
        }
    }
    
    private void OnRegisterComplete(bool success)
    {
        SetLoadingState(false);
        isRegistering = false; // Reset registration flag
        
        if (success)
        {
            UpdateStatusText("Registration successful! Logging you in...");
            
            // User is automatically logged in by PlayFab after registration
            Debug.Log("[AuthenticationManager] User registered and logged in automatically with NEW account!");
            
            // Clear registration input fields
            ClearRegistrationFields();
            
            // Hide the registration screen after 3.5 seconds
            // This gives user time to see the success message
            Invoke(nameof(HideRegisterScreen), 3.5f);
            
            // Show splash screen after successful registration
            Invoke(nameof(ShowSplashScreenPanel), 0.5f);
            
            // Notify listeners
            OnAuthenticationSuccess?.Invoke();
        }
        else
        {
            // Registration failed - clear the flag
            isRegistering = false;
            
            // Try to get a more specific error message from PlayFabManager
            string errorMsg = "";
            if (PlayFabManager.Instance != null)
            {
                errorMsg = PlayFabManager.Instance.GetLastErrorMessage();
            }
            
            if (!string.IsNullOrEmpty(errorMsg))
            {
                UpdateStatusText(errorMsg);
            }
            else
            {
                UpdateStatusText("Registration failed! Email might already be in use.");
            }
            
            // If registration failed due to network, show internet panel
            if (Application.internetReachability == NetworkReachability.NotReachable || (errorMsg != null && errorMsg.ToLower().Contains("network")))
            {
                ShowInternetPanel(OnRegisterClicked);
            }
            
            // Clear password field on failure (keep name and email for user convenience)
            if (registerPasswordInput != null)
            {
                registerPasswordInput.text = "";
            }
        }
    }
    
    /// <summary>
    /// Clears all registration input fields
    /// </summary>
    private void ClearRegistrationFields()
    {
        if (registerNameInput != null)
        {
            registerNameInput.text = "";
            Debug.Log("[AuthenticationManager] Cleared register name input");
        }
        
        if (registerEmailInput != null)
        {
            registerEmailInput.text = "";
            Debug.Log("[AuthenticationManager] Cleared register email input");
        }
        
        if (registerPasswordInput != null)
        {
            registerPasswordInput.text = "";
            Debug.Log("[AuthenticationManager] Cleared register password input");
        }
    }
    
    #endregion
    
    #region UI Helpers
    
    private void SetLoadingState(bool isLoading)
    {
        // if (loadingIndicator != null)
        //     loadingIndicator.SetActive(isLoading);
        //
        if (loginButton != null)
            loginButton.interactable = !isLoading;
        
        if (registerButton != null)
            registerButton.interactable = !isLoading;
        
        if (guestLoginButton != null)
            guestLoginButton.interactable = !isLoading;
        
        if (googleLoginButton != null)
            googleLoginButton.interactable = !isLoading;
        
        if (facebookLoginButton != null)
            facebookLoginButton.interactable = !isLoading;
        
        if (switchToRegisterButton != null)
            switchToRegisterButton.interactable = !isLoading;
        
        if (switchToLoginButton != null)
            switchToLoginButton.interactable = !isLoading;
    }
    
    private void UpdateStatusText(string message)
    {
        // if (statusText != null)
        //     statusText.text = message;
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    private void OnLoginPasswordToggleChanged(bool isOn)
    {
        if (loginPasswordInput == null) return;
        
        isLoginPasswordVisible = isOn;
        loginPasswordInput.contentType = isLoginPasswordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        loginPasswordInput.ForceLabelUpdate();
    }
    
    private void OnRegisterPasswordToggleChanged(bool isOn)
    {
        if (registerPasswordInput == null) return;
        
        isRegisterPasswordVisible = isOn;
        registerPasswordInput.contentType = isRegisterPasswordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        registerPasswordInput.ForceLabelUpdate();
    }
    
    #endregion
}

