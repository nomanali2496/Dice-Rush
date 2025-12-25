using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages game state, player profiles, and CPU profiles
/// Handles profile image assignment from main menu and random CPU profile assignment
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Player Profile")]
    [SerializeField] private Image playerProfileImage; // Player's profile image display
    [SerializeField] private Image playerOutlineImage; // Player's outline image (for color assignment)
    [SerializeField] private MainMenuAvatarDisplay mainMenuAvatarDisplay; // Reference to main menu avatar display (to get user's selected avatar)
    
    [Header("CPU Profiles")]
    [SerializeField] private CPU[] cpuPlayers; // Array of CPU player scripts
    [SerializeField] private Image[] cpuProfileImages; // Array of CPU profile image displays (must match cpuPlayers length)
    [SerializeField] private Image[] cpuOutlineImages; // Array of CPU outline images (for color assignment, must match cpuPlayers length)
    [SerializeField] private int numberOfCPUs = 1; // Number of CPUs to use (1 or more)
    
    [Header("Profile Image Settings")]
    [SerializeField] private Sprite[] availableProfileSprites; // Available profile sprites for CPUs (random selection)
    [SerializeField] private bool excludePlayerAvatarFromCPU = true; // Don't use player's avatar for CPUs
    
    [Header("References")]
    [SerializeField] private NumberGrid numberGrid; // Reference to NumberGrid
    [SerializeField] private DicesController playerDicesController; // Reference to player's DicesController
    
    [Header("Gameplay Screen")]
    [SerializeField] private GameObject gameplayScreen; // Gameplay screen GameObject (set active when game starts)
    
    [Header("Timer")]
    [SerializeField] private TMPro.TextMeshProUGUI timerText; // Timer text display (TextMeshProUGUI)
    [SerializeField] private Text timerTextLegacy; // Timer text display (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private float gameTimer = 30f; // Game timer in seconds (30 seconds)
    
    [Header("Winner Screen")]
    [SerializeField] private GameObject winnerScreen; // Winner screen GameObject (set active when game ends)
    [SerializeField] private Image winnerProfileImage; // Winner's profile image display
    [SerializeField] private Image winnerOutlineImage; // Winner's outline image display
    [SerializeField] private TMPro.TextMeshProUGUI winnerNameText; // Winner's name text (TextMeshProUGUI)
    [SerializeField] private Text winnerNameTextLegacy; // Winner's name text (Legacy Text - use if not using TextMeshPro)
    
    private Sprite playerSelectedAvatar; // Store player's selected avatar sprite
    private List<Sprite> availableCPUAvatars; // List of available avatars for CPUs
    private Color playerOutlineColor; // Store player's outline color
    private Color[] cpuOutlineColors; // Store CPU outline colors
    
    private float currentTimer; // Current timer value
    private bool isGameActive = false; // Whether the game is still active (starts as false, becomes true when gameplay screen is active)
    private Coroutine timerCoroutine; // Coroutine for timer countdown
    private bool timerStarted = false; // Flag to prevent multiple timer starts
    
    private void Start()
    {
        // Subscribe to PlayFab events to update when player data loads
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
        
        // Subscribe to NumberGrid event for when all boxes are selected
        if (numberGrid != null)
        {
            numberGrid.OnAllBoxesSelected += OnAllBoxesSelected;
        }
        
        // Wait a bit for PlayFab and MainMenuAvatarDisplay to load avatar
        StartCoroutine(InitializeProfilesDelayed());
        SetupCPUs();
        
        // Hide winner screen initially
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(false);
        }
        
        // Check if gameplay screen is active and start timer if it is
        if (gameplayScreen != null && gameplayScreen.activeSelf)
        {
            StartGameTimer();
        }
    }
    
    /// <summary>
    /// Called when gameplay screen becomes active - starts the timer
    /// Call this method when you activate the gameplay screen, or it will be called automatically via Update()
    /// </summary>
    public void OnGameplayScreenActivated()
    {
        if (gameplayScreen != null && gameplayScreen.activeSelf && !timerStarted)
        {
            Debug.Log("[GameManager] Gameplay screen activated, starting timer...");
            timerStarted = true;
            StartGameTimer();
        }
    }
    
    private void Update()
    {
        // Check if gameplay screen just became active (to start timer)
        if (gameplayScreen != null && gameplayScreen.activeSelf && !timerStarted)
        {
            Debug.Log("[GameManager] Gameplay screen is active, starting timer...");
            OnGameplayScreenActivated();
        }
    }
    
    /// <summary>
    /// Starts the game timer countdown
    /// </summary>
    private void StartGameTimer()
    {
        // Only start if gameplay screen is active
        if (gameplayScreen != null && !gameplayScreen.activeSelf)
        {
            Debug.LogWarning("[GameManager] Cannot start timer - gameplay screen is not active!");
            return;
        }
        
        currentTimer = gameTimer;
        isGameActive = true;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        timerCoroutine = StartCoroutine(TimerCountdown());
        Debug.Log($"[GameManager] Game timer started! Timer: {gameTimer} seconds");
    }
    
    /// <summary>
    /// Coroutine for timer countdown
    /// </summary>
    private System.Collections.IEnumerator TimerCountdown()
    {
        // Update timer display immediately
        UpdateTimerDisplay();
        
        while (currentTimer > 0 && isGameActive)
        {
            yield return new WaitForSeconds(1f);
            currentTimer -= 1f;
            
            // Update timer display
            UpdateTimerDisplay();
        }
        
        // Timer reached zero
        if (isGameActive)
        {
            currentTimer = 0f;
            UpdateTimerDisplay();
            OnTimerExpired();
        }
    }
    
    /// <summary>
    /// Updates the timer text display
    /// </summary>
    private void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(currentTimer);
        
        if (timerText != null)
        {
            timerText.text = seconds.ToString();
        }
        
        if (timerTextLegacy != null)
        {
            timerTextLegacy.text = seconds.ToString();
        }
    }
    
    /// <summary>
    /// Called when the timer expires
    /// </summary>
    private void OnTimerExpired()
    {
        isGameActive = false;
        Debug.Log("[GameManager] Timer expired! Determining winner...");
        
        // Stop all CPU rolling
        if (cpuPlayers != null)
        {
            foreach (CPU cpu in cpuPlayers)
            {
                if (cpu != null)
                {
                    cpu.StopAutoRolling();
                }
            }
        }
        
        // Get winner information (who has the most selected boxes)
        var (winnerType, winnerCPUIndex, winnerCount) = numberGrid.GetWinner();
        
        // Show winner screen
        ShowWinnerScreen(winnerType, winnerCPUIndex, winnerCount);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        }
        
        if (numberGrid != null)
        {
            numberGrid.OnAllBoxesSelected -= OnAllBoxesSelected;
        }
    }
    
    /// <summary>
    /// Called when all boxes in the grid are selected
    /// </summary>
    private void OnAllBoxesSelected()
    {
        if (!isGameActive)
        {
            return; // Game already ended
        }
        
        isGameActive = false;
        timerStarted = false; // Reset flag so timer can be restarted if needed
        Debug.Log("[GameManager] All boxes selected! Determining winner...");
        
        // Stop timer
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Stop all CPU rolling
        if (cpuPlayers != null)
        {
            foreach (CPU cpu in cpuPlayers)
            {
                if (cpu != null)
                {
                    cpu.StopAutoRolling();
                }
            }
        }
        
        // Get winner information
        var (winnerType, winnerCPUIndex, winnerCount) = numberGrid.GetWinner();
        
        // Show winner screen
        ShowWinnerScreen(winnerType, winnerCPUIndex, winnerCount);
    }
    
    /// <summary>
    /// Shows the winner screen with winner's information
    /// </summary>
    private void ShowWinnerScreen(PlayerType winnerType, int winnerCPUIndex, int selectionCount)
    {
        if (winnerScreen == null)
        {
            Debug.LogWarning("[GameManager] Winner screen not assigned!");
            return;
        }
        
        // Set winner's profile image
        if (winnerProfileImage != null)
        {
            if (winnerType == PlayerType.Player)
            {
                // Player won
                if (playerProfileImage != null && playerProfileImage.sprite != null)
                {
                    winnerProfileImage.sprite = playerProfileImage.sprite;
                }
            }
            else // CPU won
            {
                // CPU won - get the CPU's profile image
                if (cpuProfileImages != null && winnerCPUIndex >= 0 && winnerCPUIndex < cpuProfileImages.Length)
                {
                    if (cpuProfileImages[winnerCPUIndex] != null && cpuProfileImages[winnerCPUIndex].sprite != null)
                    {
                        winnerProfileImage.sprite = cpuProfileImages[winnerCPUIndex].sprite;
                    }
                }
            }
        }
        
        // Set winner's outline color
        if (winnerOutlineImage != null)
        {
            if (winnerType == PlayerType.Player)
            {
                winnerOutlineImage.color = playerOutlineColor;
            }
            else // CPU
            {
                if (cpuOutlineColors != null && winnerCPUIndex >= 0 && winnerCPUIndex < cpuOutlineColors.Length)
                {
                    winnerOutlineImage.color = cpuOutlineColors[winnerCPUIndex];
                }
            }
        }
        
        // Set winner's name
        string winnerName = GetWinnerName(winnerType, winnerCPUIndex);
        if (winnerNameText != null)
        {
            winnerNameText.text = winnerName;
        }
        if (winnerNameTextLegacy != null)
        {
            winnerNameTextLegacy.text = winnerName;
        }
        
        // Hide gameplay screen
        if (gameplayScreen != null)
        {
            gameplayScreen.SetActive(false);
        }
        
        // Show winner screen
        if (winnerScreen != null)
        {
            winnerScreen.SetActive(true);
        }
        
        Debug.Log($"[GameManager] Winner: {winnerName} ({winnerType}, CPU index: {winnerCPUIndex}) with {selectionCount} selected boxes!");
    }
    
    /// <summary>
    /// Gets the display name for a winner
    /// </summary>
    private string GetWinnerName(PlayerType playerType, int cpuIndex)
    {
        if (playerType == PlayerType.Player)
        {
            return "You";
        }
        else // CPU
        {
            // CPU names: Index 0 = Charles, Index 1 = John
            switch (cpuIndex)
            {
                case 0:
                    return "Charles";
                case 1:
                    return "John";
                default:
                    return $"CPU {cpuIndex + 1}";
            }
        }
    }
    
    /// <summary>
    /// Called when PlayFab player data is loaded
    /// </summary>
    private void OnPlayerDataLoaded(PlayerData data)
    {
        // Retry loading player profile image when data is loaded
        if (data != null)
        {
            StartCoroutine(RetryLoadPlayerProfile());
        }
    }
    
    /// <summary>
    /// Coroutine to retry loading player profile after PlayFab data loads
    /// </summary>
    private System.Collections.IEnumerator RetryLoadPlayerProfile()
    {
        yield return new WaitForSeconds(0.5f); // Wait for MainMenuAvatarDisplay to update
        LoadPlayerProfileImage();
    }
    
    /// <summary>
    /// Coroutine to initialize profiles after a delay (to allow MainMenuAvatarDisplay to load)
    /// </summary>
    private System.Collections.IEnumerator InitializeProfilesDelayed()
    {
        // Wait for PlayFab to initialize and MainMenuAvatarDisplay to load avatar
        yield return new WaitForSeconds(1.5f);
        
        InitializeProfiles();
    }
    
    /// <summary>
    /// Initializes player and CPU profiles
    /// </summary>
    private void InitializeProfiles()
    {
        // Get player's profile image from main menu
        LoadPlayerProfileImage();
        
        // Setup available CPU avatars
        SetupAvailableCPUAvatars();
        
        // Assign random profile images to CPUs
        AssignCPUProfileImages();
        
        // Assign random colors to outline images
        AssignOutlineColors();
    }
    
    /// <summary>
    /// Loads player's profile image from main menu
    /// </summary>
    private void LoadPlayerProfileImage()
    {
        if (playerProfileImage == null)
        {
            Debug.LogWarning("[GameManager] Player profile image not assigned!");
            return;
        }
        
        // Try to get avatar from MainMenuAvatarDisplay
        if (mainMenuAvatarDisplay != null)
        {
            // Use the public method to get the current avatar sprite
            Sprite avatarSprite = mainMenuAvatarDisplay.GetCurrentAvatarSprite();
            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay");
                return;
            }
            
            // Fallback: Try to get the Image component directly
            Image mainMenuImage = mainMenuAvatarDisplay.GetComponent<Image>();
            if (mainMenuImage != null && mainMenuImage.sprite != null)
            {
                playerSelectedAvatar = mainMenuImage.sprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay (component fallback)");
                return;
            }
            
            // Also try to find Image component in children
            Image childImage = mainMenuAvatarDisplay.GetComponentInChildren<Image>();
            if (childImage != null && childImage.sprite != null)
            {
                playerSelectedAvatar = childImage.sprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log("[GameManager] Player profile image loaded from MainMenuAvatarDisplay (child fallback)");
                return;
            }
        }
        
        // Try to get from AvatarSelector
        AvatarSelector avatarSelector = FindObjectOfType<AvatarSelector>();
        if (avatarSelector != null)
        {
            // Try to get current selected avatar
            int selectedIndex = 0;
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
            {
                if (PlayFabManager.Instance.currentPlayerData != null)
                {
                    selectedIndex = PlayFabManager.Instance.currentPlayerData.selectedAvatarIndex;
                }
            }
            
            Sprite avatarSprite = avatarSelector.GetAvatarSpriteByIndex(selectedIndex);
            if (avatarSprite != null)
            {
                playerSelectedAvatar = avatarSprite;
                playerProfileImage.sprite = playerSelectedAvatar;
                Debug.Log($"[GameManager] Player profile image loaded from AvatarSelector (index: {selectedIndex})");
                return;
            }
        }
        
        // Try to get from PlayFabManager (custom uploaded image)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.IsLoggedIn())
        {
            if (PlayFabManager.Instance.currentPlayerData != null)
            {
                string imageUrl = PlayFabManager.Instance.currentPlayerData.imageUrl;
                
                // Check if it's a custom base64 image
                if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("data:image"))
                {
                    try
                    {
                        // Extract base64 part
                        string base64Data = imageUrl.Substring(imageUrl.IndexOf(",") + 1);
                        byte[] imageBytes = System.Convert.FromBase64String(base64Data);
                        
                        // Create texture
                        Texture2D texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageBytes))
                        {
                            // Convert to sprite
                            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                            playerSelectedAvatar = sprite;
                            playerProfileImage.sprite = playerSelectedAvatar;
                            Debug.Log("[GameManager] Player profile image loaded from PlayFab custom image");
                            return;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[GameManager] Failed to load custom image: {e.Message}");
                    }
                }
            }
        }
        
        Debug.LogWarning("[GameManager] Could not load player profile image from main menu. Using default or assigned sprite.");
    }
    
    /// <summary>
    /// Sets up available CPU avatars (excluding player's avatar if option is enabled)
    /// </summary>
    private void SetupAvailableCPUAvatars()
    {
        availableCPUAvatars = new List<Sprite>();
        
        // Add all available profile sprites
        if (availableProfileSprites != null && availableProfileSprites.Length > 0)
        {
            foreach (Sprite sprite in availableProfileSprites)
            {
                if (sprite != null)
                {
                    // If exclude option is enabled, don't add player's avatar
                    if (excludePlayerAvatarFromCPU && playerSelectedAvatar != null)
                    {
                        if (sprite == playerSelectedAvatar)
                        {
                            continue; // Skip player's avatar
                        }
                    }
                    availableCPUAvatars.Add(sprite);
                }
            }
        }
        
        // If no sprites assigned, try to get from AvatarSelector
        if (availableCPUAvatars.Count == 0)
        {
            AvatarSelector avatarSelector = FindObjectOfType<AvatarSelector>();
            if (avatarSelector != null)
            {
                // Get all avatar sprites from AvatarSelector
                for (int i = 0; i < 8; i++) // Assuming 8 avatars
                {
                    Sprite sprite = avatarSelector.GetAvatarSpriteByIndex(i);
                    if (sprite != null)
                    {
                        // If exclude option is enabled, don't add player's avatar
                        if (excludePlayerAvatarFromCPU && playerSelectedAvatar != null)
                        {
                            if (sprite == playerSelectedAvatar)
                            {
                                continue; // Skip player's avatar
                            }
                        }
                        availableCPUAvatars.Add(sprite);
                    }
                }
            }
        }
        
        if (availableCPUAvatars.Count == 0)
        {
            Debug.LogWarning("[GameManager] No available CPU avatars found! CPUs will not have profile images.");
        }
    }
    
    /// <summary>
    /// Assigns random profile images to CPUs
    /// </summary>
    private void AssignCPUProfileImages()
    {
        if (cpuProfileImages == null || cpuProfileImages.Length == 0)
        {
            Debug.LogWarning("[GameManager] CPU profile images array is empty!");
            return;
        }
        
        if (availableCPUAvatars == null || availableCPUAvatars.Count == 0)
        {
            Debug.LogWarning("[GameManager] No available CPU avatars to assign!");
            return;
        }
        
        // Create a copy of available avatars to avoid duplicates
        List<Sprite> availableAvatars = new List<Sprite>(availableCPUAvatars);
        
        // Assign random avatars to each CPU
        for (int i = 0; i < cpuProfileImages.Length && i < numberOfCPUs; i++)
        {
            if (cpuProfileImages[i] == null)
            {
                continue;
            }
            
            if (availableAvatars.Count == 0)
            {
                // If we run out of unique avatars, reset the list
                availableAvatars = new List<Sprite>(availableCPUAvatars);
            }
            
            // Randomly select an avatar
            int randomIndex = Random.Range(0, availableAvatars.Count);
            Sprite selectedAvatar = availableAvatars[randomIndex];
            
            // Assign to CPU profile image
            cpuProfileImages[i].sprite = selectedAvatar;
            
            // Remove from available list to avoid duplicates (if we have enough avatars)
            if (availableAvatars.Count > numberOfCPUs)
            {
                availableAvatars.RemoveAt(randomIndex);
            }
            
            Debug.Log($"[GameManager] Assigned profile image to CPU {i + 1}");
        }
    }
    
    /// <summary>
    /// Sets up CPU players based on numberOfCPUs
    /// </summary>
    private void SetupCPUs()
    {
        if (cpuPlayers == null || cpuPlayers.Length == 0)
        {
            Debug.LogWarning("[GameManager] CPU players array is empty!");
            return;
        }
        
        // Enable only the specified number of CPUs
        for (int i = 0; i < cpuPlayers.Length; i++)
        {
            if (cpuPlayers[i] != null)
            {
                if (i < numberOfCPUs)
                {
                    cpuPlayers[i].SetCPUIndex(i); // Set the CPU index
                    cpuPlayers[i].SetCPUEnabled(true);
                    Debug.Log($"[GameManager] CPU {i + 1} enabled with index {i} and started rolling");
                }
                else
                {
                    cpuPlayers[i].SetCPUEnabled(false);
                    Debug.Log($"[GameManager] CPU {i + 1} disabled");
                }
            }
        }
        
        Debug.Log($"[GameManager] Total CPUs active: {numberOfCPUs} out of {cpuPlayers.Length}");
    }
    
    /// <summary>
    /// Sets the number of CPUs to use
    /// </summary>
    public void SetNumberOfCPUs(int count)
    {
        numberOfCPUs = Mathf.Clamp(count, 1, cpuPlayers != null ? cpuPlayers.Length : 1);
        SetupCPUs();
        AssignCPUProfileImages(); // Reassign profile images
        Debug.Log($"[GameManager] Number of CPUs set to {numberOfCPUs}");
    }
    
    /// <summary>
    /// Manually refresh player profile image
    /// </summary>
    public void RefreshPlayerProfile()
    {
        LoadPlayerProfileImage();
    }
    
    /// <summary>
    /// Manually reassign CPU profile images
    /// </summary>
    public void ReassignCPUProfiles()
    {
        SetupAvailableCPUAvatars();
        AssignCPUProfileImages();
    }
    
    /// <summary>
    /// Gets the player's selected avatar sprite
    /// </summary>
    public Sprite GetPlayerAvatar()
    {
        return playerSelectedAvatar;
    }
    
    /// <summary>
    /// Gets the number of active CPUs
    /// </summary>
    public int GetNumberOfCPUs()
    {
        return numberOfCPUs;
    }
    
    /// <summary>
    /// Assigns random colors to outline images for player and CPUs (ensures no duplicates)
    /// </summary>
    private void AssignOutlineColors()
    {
        // Initialize CPU outline colors array
        cpuOutlineColors = new Color[cpuPlayers != null ? cpuPlayers.Length : 0];
        
        // Available colors: Red, Yellow, Green
        Color[] availableColors = new Color[]
        {
            new Color(0.7f, 0.1f, 0.1f, 1f), // Dark Red
            new Color(0.7f, 0.7f, 0.1f, 1f), // Dark Yellow
            new Color(0.1f, 0.7f, 0.1f, 1f)  // Dark Green
        };
        
        // Create a list of available colors to assign (ensures uniqueness)
        List<Color> availableColorsList = new List<Color>(availableColors);
        
        // Calculate total number of players (1 player + numberOfCPUs)
        int totalPlayers = 1 + numberOfCPUs;
        
        // Check if we have enough colors for all players
        if (totalPlayers > availableColors.Length)
        {
            Debug.LogWarning($"[GameManager] Not enough colors ({availableColors.Length}) for all players ({totalPlayers}). Some players will share colors.");
        }
        
        // Assign color to player outline (first player)
        if (playerOutlineImage != null)
        {
            if (availableColorsList.Count > 0)
            {
                int randomIndex = Random.Range(0, availableColorsList.Count);
                playerOutlineColor = availableColorsList[randomIndex]; // Store the color
                playerOutlineImage.color = playerOutlineColor;
                availableColorsList.RemoveAt(randomIndex); // Remove from available list
                Debug.Log($"[GameManager] Assigned color to player outline: {playerOutlineColor}");
            }
            else
            {
                // Fallback if no colors available
                playerOutlineColor = availableColors[0];
                playerOutlineImage.color = playerOutlineColor;
                Debug.LogWarning("[GameManager] No unique colors available, using default for player");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] Player outline image not assigned!");
        }
        
        // Assign unique colors to CPU outlines
        if (cpuOutlineImages != null && cpuOutlineImages.Length > 0)
        {
            for (int i = 0; i < cpuOutlineImages.Length && i < numberOfCPUs; i++)
            {
                if (cpuOutlineImages[i] != null)
                {
                    if (availableColorsList.Count > 0)
                    {
                        // Pick a random color from remaining available colors
                        int randomIndex = Random.Range(0, availableColorsList.Count);
                        Color selectedColor = availableColorsList[randomIndex];
                        cpuOutlineColors[i] = selectedColor; // Store the color
                        cpuOutlineImages[i].color = selectedColor;
                        availableColorsList.RemoveAt(randomIndex); // Remove from available list
                        Debug.Log($"[GameManager] Assigned unique color to CPU {i + 1} outline: {selectedColor}");
                    }
                    else
                    {
                        // Fallback: cycle through colors if we run out
                        Color fallbackColor = availableColors[i % availableColors.Length];
                        cpuOutlineColors[i] = fallbackColor;
                        cpuOutlineImages[i].color = fallbackColor;
                        Debug.LogWarning($"[GameManager] Using fallback color for CPU {i + 1}: {fallbackColor}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] CPU outline images array is empty or not assigned!");
        }
        
        // Update NumberGrid with the outline colors
        UpdateNumberGridColors();
    }
    
    /// <summary>
    /// Updates NumberGrid with player and CPU outline colors
    /// </summary>
    private void UpdateNumberGridColors()
    {
        if (numberGrid != null)
        {
            // Set player outline color
            numberGrid.SetPlayerOutlineColor(playerOutlineColor);
            
            // Set CPU outline colors
            if (cpuOutlineColors != null)
            {
                for (int i = 0; i < cpuOutlineColors.Length && i < numberOfCPUs; i++)
                {
                    numberGrid.SetCPUOutlineColor(i, cpuOutlineColors[i]);
                }
            }
        }
    }
    
    /// <summary>
    /// Gets the outline color for a specific player type
    /// </summary>
    public Color GetOutlineColor(PlayerType playerType, int cpuIndex = 0)
    {
        if (playerType == PlayerType.Player)
        {
            return playerOutlineColor;
        }
        else // CPU
        {
            if (cpuOutlineColors != null && cpuIndex >= 0 && cpuIndex < cpuOutlineColors.Length)
            {
                return cpuOutlineColors[cpuIndex];
            }
            return Color.red; // Default fallback
        }
    }
    
    /// <summary>
    /// Gets a random darker color for outline images (red, yellow, green only)
    /// </summary>
    private Color GetRandomColor()
    {
        // Only three colors: Red, Yellow, Green
        Color[] colors = new Color[]
        {
            new Color(0.7f, 0.1f, 0.1f, 1f), // Dark Red
            new Color(0.7f, 0.7f, 0.1f, 1f), // Dark Yellow
            new Color(0.1f, 0.7f, 0.1f, 1f)  // Dark Green
        };
        
        // Randomly select one of the three colors
        int randomIndex = Random.Range(0, colors.Length);
        return colors[randomIndex];
    }
    
    /// <summary>
    /// Manually reassign outline colors
    /// </summary>
    public void ReassignOutlineColors()
    {
        AssignOutlineColors();
    }
}

