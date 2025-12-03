using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Handles Login and Registration in a single scene
/// Shows Login/Register screen first, then SplashScreen after authentication
/// </summary>
public class AuthenticationManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginRegisterPanel;
    [SerializeField] private GameObject splashScreenPanel;
    
    [Header("Login/Register Toggle")]
    [SerializeField] private GameObject loginSection;
    [SerializeField] private GameObject registerSection;
    [SerializeField] private Button switchToRegisterButton;
    [SerializeField] private Button switchToLoginButton;
    
    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    // [SerializeField] private Button guestLoginButton;
    
    [Header("Registration UI")]
    [SerializeField] private TMP_InputField registerNameInput;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private Button registerButton;
    
    [Header("Status")]
    // [SerializeField] private TextMeshProUGUI statusText;
    // [SerializeField] private GameObject loadingIndicator;
    
    [Header("Settings")]
    [SerializeField] private int minPasswordLength = 6;
    
    public Action OnAuthenticationSuccess;
    
    private void Start()
    {
        // Initialize PlayFab
        InitializePlayFab();
        
        // Setup button listeners
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);
        
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterClicked);
        
        // if (guestLoginButton != null)
        //     guestLoginButton.onClick.AddListener(OnGuestLoginClicked);
        
        if (switchToRegisterButton != null)
            switchToRegisterButton.onClick.AddListener(() => ShowRegisterSection());
        
        if (switchToLoginButton != null)
            switchToLoginButton.onClick.AddListener(() => ShowLoginSection());
        
        // Subscribe to PlayFab events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnLoginComplete += OnLoginComplete;
            PlayFabManager.Instance.OnRegisterComplete += OnRegisterComplete;
        }
        
        // Show login/register panel first
        ShowLoginRegisterPanel();
        ShowLoginSection();
        SetLoadingState(false);
        UpdateStatusText("");
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
    }
    
    #region Panel Management
    
    private void ShowLoginRegisterPanel()
    {
        if (loginRegisterPanel != null)
            loginRegisterPanel.SetActive(true);
        
        if (splashScreenPanel != null)
            splashScreenPanel.SetActive(false);
    }
    
    private void ShowSplashScreenPanel()
    {
        if (loginRegisterPanel != null)
            loginRegisterPanel.SetActive(false);
        
        if (splashScreenPanel != null)
            splashScreenPanel.SetActive(true);
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
    
    private void OnLoginClicked()
    {
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
    
    #endregion
    
    #region Registration
    
    private void OnRegisterClicked()
    {
        string name = registerNameInput != null ? registerNameInput.text : "";
        string email = registerEmailInput != null ? registerEmailInput.text : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";
        
        if (!ValidateRegistration(name, email, password, confirmPassword))
        {
            return;
        }
        
        SetLoadingState(true);
        UpdateStatusText("Creating account...");
        
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.RegisterPlayer(email, password, name);
        }
        else
        {
            UpdateStatusText("Error: PlayFabManager not found!");
            SetLoadingState(false);
        }
    }
    
    private bool ValidateRegistration(string name, string email, string password, string confirmPassword)
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
        
        if (password != confirmPassword)
        {
            UpdateStatusText("Passwords do not match!");
            return false;
        }
        
        return true;
    }
    
    #endregion
    
    #region Callbacks
    
    private void OnLoginComplete(bool success)
    {
        SetLoadingState(false);
        
        if (success)
        {
            UpdateStatusText("Login successful!");
            
            // Show splash screen after successful login
            Invoke(nameof(ShowSplashScreenPanel), 0.5f);
            
            // Notify listeners
            OnAuthenticationSuccess?.Invoke();
        }
        else
        {
            UpdateStatusText("Login failed! Please check your credentials.");
        }
    }
    
    private void OnRegisterComplete(bool success)
    {
        SetLoadingState(false);
        
        if (success)
        {
            UpdateStatusText("Registration successful! Logging you in...");
            
            // User is automatically logged in by PlayFab after registration
            Debug.Log("User registered and logged in automatically!");
            
            // Show splash screen after successful registration
            Invoke(nameof(ShowSplashScreenPanel), 0.5f);
            
            // Notify listeners
            OnAuthenticationSuccess?.Invoke();
        }
        else
        {
            UpdateStatusText("Registration failed! Email might already be in use.");
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
        
        // if (guestLoginButton != null)
        //     guestLoginButton.interactable = !isLoading;
        
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
    
    #endregion
}

