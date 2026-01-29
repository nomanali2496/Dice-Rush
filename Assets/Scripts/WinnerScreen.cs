using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages winner screen animations
/// Shows animation of chips and cash being added from 1st, 2nd, 3rd place positions to the top cash/chips display
/// Animation only plays when the player is in the top 3 positions
/// </summary>
public class WinnerScreen : MonoBehaviour
{
    [Header("Cash and Chips Sprites")]
    [SerializeField] private Sprite cashSprite; // Sprite for cash animation
    [SerializeField] private Sprite chipsSprite; // Sprite for chips animation
    
    [Header("1st Place Position")]
    [SerializeField] private RectTransform firstPlacePrizePosition; // Position of 1st place prize display (can use TextMeshProUGUI/Text component)
    [SerializeField] private TMPro.TextMeshProUGUI firstPlacePrizeText; // Alternative: 1st place prize text (will use its RectTransform)
    [SerializeField] private Image firstPlacePrizeIcon; // 1st place prize icon image (shows cash or chips sprite)
    
    [Header("2nd Place Position")]
    [SerializeField] private RectTransform secondPlacePrizePosition; // Position of 2nd place prize display (can use TextMeshProUGUI/Text component)
    [SerializeField] private TMPro.TextMeshProUGUI secondPlacePrizeText; // Alternative: 2nd place prize text (will use its RectTransform)
    [SerializeField] private Image secondPlacePrizeIcon; // 2nd place prize icon image (shows cash or chips sprite)
    
    [Header("3rd Place Position")]
    [SerializeField] private RectTransform thirdPlacePrizePosition; // Position of 3rd place prize display (can use TextMeshProUGUI/Text component)
    [SerializeField] private TMPro.TextMeshProUGUI thirdPlacePrizeText; // Alternative: 3rd place prize text (will use its RectTransform)
    [SerializeField] private Image thirdPlacePrizeIcon; // 3rd place prize icon image (shows cash or chips sprite)
    
    [Header("Target Position (Top Display)")]
    [SerializeField] private RectTransform targetCashPosition; // Target position for cash (top display)
    [SerializeField] private RectTransform targetChipsPosition; // Target position for chips (top display)
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 1.5f; // Duration of animation in seconds
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Animation curve for movement
    [SerializeField] private int baseParticles = 3; // Base number of particles (minimum)
    [SerializeField] private float particlesPerCash = 0.5f; // Number of particles per unit of cash (e.g., 0.5 = 1 particle per $2)
    [SerializeField] private float particlesPerChip = 0.1f; // Number of particles per chip (e.g., 0.1 = 1 particle per 10 chips)
    [SerializeField] private int maxParticles = 20; // Maximum number of particles to spawn
    [SerializeField] private float delayBetweenParticles = 0.1f; // Delay between spawning each particle (one by one effect)
    
    [Header("Prefab for Animated Sprites")]
    [SerializeField] private GameObject animatedSpritePrefab; // Prefab for animated cash/chips sprites (should have Image component)
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip cashSound; // Sound to play for each cash particle
    [SerializeField] private AudioClip chipsSound; // Sound to play for each chip particle
    
    private GameManager gameManager;
    private PlayerProfileUI playerProfileUI;
    private List<GameObject> activeAnimations = new List<GameObject>(); // Track active animation objects
    
    // Values for count-up animation
    private float initialCash = 0f;
    private int initialChips = 0;
    private float targetCash = 0f;
    private int targetChips = 0;
    private float currentAnimatedCash = 0f;
    private int currentAnimatedChips = 0;
    
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        playerProfileUI = FindObjectOfType<PlayerProfileUI>();
    }
    
    private void OnEnable()
    {
        // Note: Animation is triggered by GameManager via PlayAnimationIfPlayerInTop3()
        // This OnEnable is kept as a fallback but will likely not find the config
        // since ResetGameState() is called before showing the winner screen
        // StartCoroutine(CheckAndPlayAnimationDelayed());
    }
    
    /// <summary>
    /// Called by GameManager to trigger animation with rankings
    /// This is the preferred method as it uses the exact same rankings as GameManager
    /// </summary>
    public void PlayAnimationIfPlayerInTop3(List<(PlayerType playerType, int cpuIndex, int count)> rankings, LobbyConfig config)
    {
        if (rankings == null || config == null)
        {
            Debug.LogWarning("[WinnerScreen] Rankings or config is null!");
            return;
        }
        
        // Get initial cash and chips values (before prize was awarded)
        if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
        {
            // The prize was already awarded, so we need to subtract it to get initial value
            // We'll get the current value and subtract the prize we're about to animate
        }
        
        // Check if player is in top 3
        bool playerInTop3 = false;
        int playerPlace = -1;
        float playerCashPrize = 0f;
        int playerChipsPrize = 0;
        
        // Check top 3 positions
        for (int i = 0; i < rankings.Count && i < 3; i++)
        {
            if (rankings[i].playerType == PlayerType.Player)
            {
                playerInTop3 = true;
                playerPlace = i + 1; // 1, 2, or 3
                
                // Get prize based on place
                switch (playerPlace)
                {
                    case 1:
                        playerCashPrize = config.firstPlaceCash;
                        playerChipsPrize = config.firstPlaceChips;
                        break;
                    case 2:
                        playerCashPrize = config.secondPlaceCash;
                        playerChipsPrize = config.secondPlaceChips;
                        break;
                    case 3:
                        playerCashPrize = config.thirdPlaceCash;
                        playerChipsPrize = config.thirdPlaceChips;
                        break;
                }
                break;
            }
        }
        
        // Only play animation if player is in top 3
        if (playerInTop3)
        {
            // Get current values (prizes were already awarded)
            if (PlayFabManager.Instance != null && PlayFabManager.Instance.currentPlayerData != null)
            {
                targetCash = PlayFabManager.Instance.currentPlayerData.cash;
                targetChips = PlayFabManager.Instance.currentPlayerData.coins;
                
                // Calculate initial values (current - prize)
                initialCash = targetCash - playerCashPrize;
                initialChips = targetChips - playerChipsPrize;
                
                // Set current animated values to initial
                currentAnimatedCash = initialCash;
                currentAnimatedChips = initialChips;
                
                // Update UI to show initial values (before animation)
                if (playerProfileUI != null)
                {
                    playerProfileUI.UpdateAllCurrencyDisplays(initialChips, initialCash);
                }
            }
            
            Debug.Log($"[WinnerScreen] Player is in {playerPlace}st place! Playing animation for {playerCashPrize} cash and {playerChipsPrize} chips");
            Debug.Log($"[WinnerScreen] Initial: {initialCash} cash, {initialChips} chips | Target: {targetCash} cash, {targetChips} chips");
            StartCoroutine(PlayAnimationDelayed(playerPlace, playerCashPrize, playerChipsPrize));
        }
        else
        {
            Debug.Log("[WinnerScreen] Player is not in top 3, skipping animation");
        }
    }
    
    private IEnumerator PlayAnimationDelayed(int playerPlace, float cashPrize, int chipsPrize)
    {
        yield return new WaitForSeconds(0.5f); // Small delay before animation starts
        PlayPrizeAnimation(playerPlace, cashPrize, chipsPrize);
    }
    
    private IEnumerator CheckAndPlayAnimationDelayed()
    {
        // Wait a few frames to ensure GameManager has finished setting up winner screen
        yield return new WaitForSeconds(0.1f);
        yield return null;
        
        StartCoroutine(CheckAndPlayAnimation());
    }
    
    private void OnDisable()
    {
        // Clean up any active animations when screen is disabled
        CleanupAnimations();
    }
    
    /// <summary>
    /// Checks if player is in top 3 and plays animation if so
    /// </summary>
    private IEnumerator CheckAndPlayAnimation()
    {
        // Wait a frame to ensure GameManager has finished setting up winner screen
        yield return null;
        
        if (gameManager == null)
        {
            Debug.LogWarning("[WinnerScreen] GameManager not found!");
            yield break;
        }
        
        // Get current lobby config to check prizes
        LobbyConfig config = gameManager.GetCurrentLobbyConfig();
        if (config == null)
        {
            Debug.LogWarning("[WinnerScreen] No lobby configuration found!");
            yield break;
        }
        
        // Get rankings from NumberGrid
        NumberGrid numberGrid = FindObjectOfType<NumberGrid>();
        if (numberGrid == null)
        {
            Debug.LogWarning("[WinnerScreen] NumberGrid not found!");
            yield break;
        }
        
        Dictionary<string, int> selectionCounts = numberGrid.GetSelectionCounts();
        
        // Check if player is in top 3
        bool playerInTop3 = false;
        int playerPlace = -1;
        float playerCashPrize = 0f;
        int playerChipsPrize = 0;
        
        // Get all players with their selection counts
        List<(PlayerType playerType, int cpuIndex, int count)> allRankings = new List<(PlayerType, int, int)>();
        
        // Add player
        int playerCount = selectionCounts.ContainsKey("Player") ? selectionCounts["Player"] : 0;
        allRankings.Add((PlayerType.Player, -1, playerCount));
        
        // Add CPUs
        if (gameManager != null)
        {
            int numberOfCPUs = config.GetNumberOfCPUs();
            for (int i = 0; i < numberOfCPUs; i++)
            {
                string cpuKey = $"CPU_{i}";
                int cpuCount = selectionCounts.ContainsKey(cpuKey) ? selectionCounts[cpuKey] : 0;
                allRankings.Add((PlayerType.CPU, i, cpuCount));
            }
        }
        
        // Sort by count (descending)
        allRankings.Sort((a, b) => b.count.CompareTo(a.count));
        
        // Check if player is in top 3 and get their place
        for (int i = 0; i < allRankings.Count && i < 3; i++)
        {
            if (allRankings[i].playerType == PlayerType.Player)
            {
                playerInTop3 = true;
                playerPlace = i + 1; // 1, 2, or 3
                
                // Get prize based on place
                switch (playerPlace)
                {
                    case 1:
                        playerCashPrize = config.firstPlaceCash;
                        playerChipsPrize = config.firstPlaceChips;
                        break;
                    case 2:
                        playerCashPrize = config.secondPlaceCash;
                        playerChipsPrize = config.secondPlaceChips;
                        break;
                    case 3:
                        playerCashPrize = config.thirdPlaceCash;
                        playerChipsPrize = config.thirdPlaceChips;
                        break;
                }
                break;
            }
        }
        
        // Only play animation if player is in top 3
        if (playerInTop3)
        {
            Debug.Log($"[WinnerScreen] Player is in {playerPlace}st place! Playing animation for {playerCashPrize} cash and {playerChipsPrize} chips");
            yield return new WaitForSeconds(0.5f); // Small delay before animation starts
            PlayPrizeAnimation(playerPlace, playerCashPrize, playerChipsPrize);
        }
        else
        {
            Debug.Log("[WinnerScreen] Player is not in top 3, skipping animation");
        }
        
        // Log debug info
        Debug.Log($"[WinnerScreen] Debug - Player in top 3: {playerInTop3}, Place: {playerPlace}, Cash: {playerCashPrize}, Chips: {playerChipsPrize}");
        Debug.Log($"[WinnerScreen] Debug - Cash sprite assigned: {cashSprite != null}, Chips sprite assigned: {chipsSprite != null}");
    }
    
    /// <summary>
    /// Gets RectTransform from either direct reference, TextMeshProUGUI component, or Image component
    /// </summary>
    private RectTransform GetRectTransform(RectTransform directRef, TMPro.TextMeshProUGUI textRef, Image imageRef = null)
    {
        if (directRef != null)
        {
            return directRef;
        }
        if (textRef != null)
        {
            return textRef.GetComponent<RectTransform>();
        }
        if (imageRef != null)
        {
            return imageRef.GetComponent<RectTransform>();
        }
        return null;
    }
    
    /// <summary>
    /// Plays the animation of cash and chips moving from place position to top display
    /// </summary>
    private void PlayPrizeAnimation(int playerPlace, float cashPrize, int chipsPrize)
    {
        Debug.Log($"[WinnerScreen] PlayPrizeAnimation called - Place: {playerPlace}, Cash: {cashPrize}, Chips: {chipsPrize}");
        
        RectTransform sourcePosition = null;
        Sprite prizeSprite = null;
        float prizeAmount = 0f;
        bool isCash = false;
        
        // Determine which prize to animate (cash or chips - only one exists at a time)
        if (cashPrize > 0f)
        {
            isCash = true;
            prizeAmount = cashPrize;
            prizeSprite = cashSprite;
        }
        else if (chipsPrize > 0)
        {
            isCash = false;
            prizeAmount = chipsPrize;
            prizeSprite = chipsSprite;
        }
        else
        {
            Debug.Log($"[WinnerScreen] No prize to animate for place {playerPlace}");
            return;
        }
        
        // Get source position based on player's place
        switch (playerPlace)
        {
            case 1:
                sourcePosition = GetRectTransform(firstPlacePrizePosition, firstPlacePrizeText, firstPlacePrizeIcon);
                Debug.Log($"[WinnerScreen] 1st place - Prize pos: {sourcePosition != null}");
                break;
            case 2:
                sourcePosition = GetRectTransform(secondPlacePrizePosition, secondPlacePrizeText, secondPlacePrizeIcon);
                Debug.Log($"[WinnerScreen] 2nd place - Prize pos: {sourcePosition != null}");
                break;
            case 3:
                sourcePosition = GetRectTransform(thirdPlacePrizePosition, thirdPlacePrizeText, thirdPlacePrizeIcon);
                Debug.Log($"[WinnerScreen] 3rd place - Prize pos: {sourcePosition != null}");
                break;
        }
        
        if (sourcePosition == null)
        {
            Debug.LogWarning($"[WinnerScreen] Source position not assigned for place {playerPlace}!");
            return;
        }
        
        RectTransform targetPosition = isCash ? targetCashPosition : targetChipsPosition;
        if (targetPosition == null)
        {
            Debug.LogWarning($"[WinnerScreen] Target position not assigned for {(isCash ? "cash" : "chips")}!");
            return;
        }
        
        if (prizeSprite == null)
        {
            Debug.LogWarning($"[WinnerScreen] Prize sprite not assigned for {(isCash ? "cash" : "chips")}!");
            return;
        }
        
        Debug.Log($"[WinnerScreen] Starting animation - Prize: {prizeAmount} {(isCash ? "cash" : "chips")}");
        StartCoroutine(AnimateParticles(sourcePosition, targetPosition, prizeSprite, prizeAmount, isCash));
    }
    
    /// <summary>
    /// Animates particles moving from source to target position
    /// Particles spawn one by one from the exact source transform position
    /// Number of particles is based on the prize amount
    /// Also animates the count-up of cash/chips display
    /// </summary>
    private IEnumerator AnimateParticles(RectTransform source, RectTransform target, Sprite sprite, float amount, bool isCash)
    {
        Debug.Log($"[WinnerScreen] AnimateParticles - Type: {(isCash ? "Cash" : "Chips")}, Amount: {amount}, Source: {source.position}, Target: {target.position}");
        
        // Calculate particle count based on prize amount
        int particleCount;
        if (isCash)
        {
            // For cash: base particles + (amount * particlesPerCash)
            particleCount = Mathf.RoundToInt(baseParticles + (amount * particlesPerCash));
        }
        else
        {
            // For chips: base particles + (amount * particlesPerChip)
            particleCount = Mathf.RoundToInt(baseParticles + (amount * particlesPerChip));
        }
        
        // Clamp to max particles
        particleCount = Mathf.Clamp(particleCount, baseParticles, maxParticles);
        
        Debug.Log($"[WinnerScreen] Calculated {particleCount} particles for {(isCash ? "cash" : "chips")} prize of {amount}");
        
        // Calculate increment per particle
        float incrementPerParticle = amount / particleCount;
        
        // Get the exact source position (world position for UI)
        Vector3 sourceWorldPos = source.position;
        
        // Spawn particles one by one with delay
        for (int i = 0; i < particleCount; i++)
        {
            // Create particle at exact source position
            GameObject particle = CreateAnimatedSprite(sourceWorldPos, sprite);
            if (particle != null)
            {
                activeAnimations.Add(particle);
                
                // Set particle to exact source position (no random spread)
                RectTransform particleRect = particle.GetComponent<RectTransform>();
                if (particleRect != null)
                {
                    particleRect.position = sourceWorldPos;
                }
                else
                {
                    particle.transform.position = sourceWorldPos;
                }
                
                Debug.Log($"[WinnerScreen] Created particle {i + 1}/{particleCount} at exact source position {sourceWorldPos}");
                
                // Play sound for this particle
                PlayParticleSound(isCash);
                
                // Animate this particle to target and update count-up
                StartCoroutine(AnimateSingleParticleWithCountUp(particle.transform, target.position, animationDuration, incrementPerParticle, isCash));
            }
            else
            {
                Debug.LogWarning($"[WinnerScreen] Failed to create particle {i + 1}");
            }
            
            // Wait before spawning next particle (one by one effect)
            if (i < particleCount - 1) // Don't wait after the last particle
            {
                yield return new WaitForSeconds(delayBetweenParticles);
            }
        }
        
        // Ensure final values are exact (in case of rounding errors)
        yield return new WaitForSeconds(animationDuration + 0.1f);
        if (playerProfileUI != null)
        {
            playerProfileUI.UpdateAllCurrencyDisplays(targetChips, targetCash);
        }
        
        yield return null;
    }
    
    /// <summary>
    /// Creates an animated sprite GameObject
    /// </summary>
    private GameObject CreateAnimatedSprite(Vector3 position, Sprite sprite)
    {
        GameObject spriteObj;
        
        // Find the Canvas to parent the sprite to (for proper UI rendering)
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;
        
        if (animatedSpritePrefab != null)
        {
            spriteObj = Instantiate(animatedSpritePrefab, parentTransform);
        }
        else
        {
            // Create a simple GameObject with Image component if no prefab
            spriteObj = new GameObject("AnimatedSprite");
            spriteObj.transform.SetParent(parentTransform);
            
            // Add RectTransform for UI element
            RectTransform rectTransform = spriteObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100f, 100f); // Default size
            
            Image img = spriteObj.AddComponent<Image>();
            img.sprite = sprite;
        }
        
        // Set position using RectTransform for UI elements
        RectTransform rect = spriteObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position = position;
        }
        else
        {
            spriteObj.transform.position = position;
        }
        
        // Set sprite if Image component exists
        Image image = spriteObj.GetComponent<Image>();
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
        }
        
        return spriteObj;
    }
    
    /// <summary>
    /// Animates a single particle from source to target (without count-up)
    /// </summary>
    private IEnumerator AnimateSingleParticle(Transform particle, Vector3 targetPosition, float duration)
    {
        RectTransform rectTransform = particle.GetComponent<RectTransform>();
        Vector3 startPosition;
        
        // Get start position based on whether it's a UI element or world object
        if (rectTransform != null)
        {
            startPosition = rectTransform.position;
        }
        else
        {
            startPosition = particle.position;
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = movementCurve.Evaluate(t);
            
            // Interpolate position
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            if (rectTransform != null)
            {
                rectTransform.position = currentPosition;
            }
            else
            {
                particle.position = currentPosition;
            }
            
            // Keep size the same throughout animation (no scaling)
            particle.localScale = Vector3.one;
            
            yield return null;
        }
        
        // Ensure final position
        if (rectTransform != null)
        {
            rectTransform.position = targetPosition;
        }
        else
        {
            particle.position = targetPosition;
        }
        
        // Destroy particle after a short delay
        yield return new WaitForSeconds(0.1f);
        
        if (particle != null && particle.gameObject != null)
        {
            activeAnimations.Remove(particle.gameObject);
            Destroy(particle.gameObject);
        }
    }
    
    /// <summary>
    /// Animates a single particle from source to target and updates count-up display
    /// </summary>
    private IEnumerator AnimateSingleParticleWithCountUp(Transform particle, Vector3 targetPosition, float duration, float increment, bool isCash)
    {
        RectTransform rectTransform = particle.GetComponent<RectTransform>();
        Vector3 startPosition;
        
        // Get start position based on whether it's a UI element or world object
        if (rectTransform != null)
        {
            startPosition = rectTransform.position;
        }
        else
        {
            startPosition = particle.position;
        }
        
        float elapsed = 0f;
        bool countUpApplied = false;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = movementCurve.Evaluate(t);
            
            // Interpolate position
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            if (rectTransform != null)
            {
                rectTransform.position = currentPosition;
            }
            else
            {
                particle.position = currentPosition;
            }
            
            // Apply count-up when particle reaches halfway (or near target)
            if (!countUpApplied && t >= 0.8f) // Apply when 80% of the way there
            {
                if (isCash)
                {
                    currentAnimatedCash += increment;
                    currentAnimatedCash = Mathf.Min(currentAnimatedCash, targetCash); // Don't exceed target
                }
                else
                {
                    currentAnimatedChips += Mathf.RoundToInt(increment);
                    currentAnimatedChips = Mathf.Min(currentAnimatedChips, targetChips); // Don't exceed target
                }
                
                // Update UI display
                if (playerProfileUI != null)
                {
                    playerProfileUI.UpdateAllCurrencyDisplays(currentAnimatedChips, currentAnimatedCash);
                }
                
                countUpApplied = true;
            }
            
            // Keep size the same throughout animation (no scaling)
            particle.localScale = Vector3.one;
            
            yield return null;
        }
        
        // Ensure final position
        if (rectTransform != null)
        {
            rectTransform.position = targetPosition;
        }
        else
        {
            particle.position = targetPosition;
        }
        
        // Destroy particle after a short delay
        yield return new WaitForSeconds(0.1f);
        
        if (particle != null && particle.gameObject != null)
        {
            activeAnimations.Remove(particle.gameObject);
            Destroy(particle.gameObject);
        }
    }
    
    /// <summary>
    /// Plays the appropriate sound for a particle (cash or chips)
    /// </summary>
    private void PlayParticleSound(bool isCash)
    {
        AudioClip soundToPlay = isCash ? cashSound : chipsSound;
        
        if (soundToPlay != null)
        {
            AudioSource.PlayClipAtPoint(soundToPlay, Camera.main.transform.position);
        }
        else
        {
            Debug.LogWarning($"[WinnerScreen] {(isCash ? "Cash" : "Chips")} sound not assigned!");
        }
    }
    
    /// <summary>
    /// Cleans up all active animations
    /// </summary>
    private void CleanupAnimations()
    {
        foreach (GameObject anim in activeAnimations)
        {
            if (anim != null)
            {
                Destroy(anim);
            }
        }
        activeAnimations.Clear();
    }
    
    private void OnDestroy()
    {
        CleanupAnimations();
    }
    
    /// <summary>
    /// Gets the cash sprite (for use by GameManager)
    /// </summary>
    public Sprite GetCashSprite()
    {
        return cashSprite;
    }
    
    /// <summary>
    /// Gets the chips sprite (for use by GameManager)
    /// </summary>
    public Sprite GetChipsSprite()
    {
        return chipsSprite;
    }
    
    /// <summary>
    /// Gets the 1st place prize icon image
    /// </summary>
    public Image GetFirstPlacePrizeIcon()
    {
        return firstPlacePrizeIcon;
    }
    
    /// <summary>
    /// Gets the 1st place prize text
    /// </summary>
    public TMPro.TextMeshProUGUI GetFirstPlacePrizeText()
    {
        return firstPlacePrizeText;
    }
    
    /// <summary>
    /// Gets the 2nd place prize icon image
    /// </summary>
    public Image GetSecondPlacePrizeIcon()
    {
        return secondPlacePrizeIcon;
    }
    
    /// <summary>
    /// Gets the 2nd place prize text
    /// </summary>
    public TMPro.TextMeshProUGUI GetSecondPlacePrizeText()
    {
        return secondPlacePrizeText;
    }
    
    /// <summary>
    /// Gets the 3rd place prize icon image
    /// </summary>
    public Image GetThirdPlacePrizeIcon()
    {
        return thirdPlacePrizeIcon;
    }
    
    /// <summary>
    /// Gets the 3rd place prize text
    /// </summary>
    public TMPro.TextMeshProUGUI GetThirdPlacePrizeText()
    {
        return thirdPlacePrizeText;
    }
}

