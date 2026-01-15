using UnityEngine;
using System;
using System.Threading.Tasks;

#if UNITY_ANDROID || UNITY_IOS && !UNITY_EDITOR
using Firebase;
using Firebase.Extensions;
#endif


/// <summary>
/// Manages Firebase initialization and provides access to Firebase services
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    
    [Header("Firebase Status")]
    [SerializeField] private bool isInitialized = false;
    [SerializeField] private string initializationStatus = "Not initialized";
    
    public bool IsInitialized => isInitialized;
    public string InitializationStatus => initializationStatus;
    
    // Events
    public Action OnFirebaseInitialized;
    public Action<string> OnFirebaseInitializationFailed;
    
    #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    private Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
    #endif
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[FirebaseManager] Instance created");
        }
        else
        {
            Debug.Log("[FirebaseManager] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Initialize Firebase when the manager starts
        InitializeFirebase();
    }
    
    /// <summary>
    /// Initializes Firebase App
    /// </summary>
    public void InitializeFirebase()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            InitializeFirebaseAndroid();
        #elif UNITY_IOS && !UNITY_EDITOR
            InitializeFirebaseIOS();
        #else
            // Editor/unsupported platform
            Debug.LogWarning("[FirebaseManager] Firebase initialization is only supported on Android/iOS devices. Not available in editor.");
            initializationStatus = "Firebase not supported in editor";
            OnFirebaseInitializationFailed?.Invoke("Firebase not supported in editor. Please test on a device.");
        #endif
    }
    
    #if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeFirebaseAndroid()
    {
        if (isInitialized)
        {
            Debug.Log("[FirebaseManager] Firebase is already initialized");
            return;
        }
        
        Debug.Log("[FirebaseManager] Starting Firebase initialization for Android...");
        initializationStatus = "Initializing...";
        
        // Check Firebase dependencies
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Firebase is ready to use
                Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                Debug.Log($"[FirebaseManager] Firebase initialized successfully! App Name: {app.Name}");
                
                isInitialized = true;
                initializationStatus = "Initialized successfully";
                
                OnFirebaseInitialized?.Invoke();
            }
            else
            {
                string errorMessage = $"Could not resolve all Firebase dependencies: {dependencyStatus}";
                Debug.LogError($"[FirebaseManager] {errorMessage}");
                
                isInitialized = false;
                initializationStatus = $"Initialization failed: {dependencyStatus}";
                
                OnFirebaseInitializationFailed?.Invoke(errorMessage);
            }
        });
    }
    #endif
    
    #if UNITY_IOS && !UNITY_EDITOR
    private void InitializeFirebaseIOS()
    {
        if (isInitialized)
        {
            Debug.Log("[FirebaseManager] Firebase is already initialized");
            return;
        }
        
        Debug.Log("[FirebaseManager] Starting Firebase initialization for iOS...");
        initializationStatus = "Initializing...";
        
        // Check Firebase dependencies
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;
            
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Firebase is ready to use
                Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                Debug.Log($"[FirebaseManager] Firebase initialized successfully! App Name: {app.Name}");
                
                isInitialized = true;
                initializationStatus = "Initialized successfully";
                
                OnFirebaseInitialized?.Invoke();
            }
            else
            {
                string errorMessage = $"Could not resolve all Firebase dependencies: {dependencyStatus}";
                Debug.LogError($"[FirebaseManager] {errorMessage}");
                
                isInitialized = false;
                initializationStatus = $"Initialization failed: {dependencyStatus}";
                
                OnFirebaseInitializationFailed?.Invoke(errorMessage);
            }
        });
    }
    #endif
    
    /// <summary>
    /// Gets the default Firebase App instance
    /// </summary>
    public object GetFirebaseApp()
    {
        #if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (isInitialized)
        {
            return Firebase.FirebaseApp.DefaultInstance;
        }
        else
        {
            Debug.LogWarning("[FirebaseManager] Firebase is not initialized yet. Call InitializeFirebase() first.");
            return null;
        }
        #else
        Debug.LogWarning("[FirebaseManager] Firebase is not available in editor.");
        return null;
        #endif
    }
    
    /// <summary>
    /// Manually reinitialize Firebase (useful for testing or recovery)
    /// </summary>
    public void ReinitializeFirebase()
    {
        isInitialized = false;
        initializationStatus = "Reinitializing...";
        InitializeFirebase();
    }
}

