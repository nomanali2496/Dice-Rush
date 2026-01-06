using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CPU/Bot player that automatically rolls dice (backend only, no animations) and selects boxes from NumberGrid
/// CPU selections are highlighted in red to distinguish from player selections
/// </summary>
public class CPU : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NumberGrid numberGrid; // Reference to NumberGrid
    
    [Header("CPU Settings")]
    [SerializeField] private float rollInterval = 2f; // Time between CPU dice rolls (in seconds)
    [SerializeField] private float selectionDelay = 0.5f; // Delay before CPU makes selection after rolling (in seconds)
    [SerializeField] private bool enableCPU = true; // Enable/disable CPU player
    [SerializeField] private bool autoRoll = true; // Automatically roll dice at intervals
    [SerializeField] private int cpuIndex = 0; // Index of this CPU (0, 1, 2, etc.) - set by GameManager
    
    [Header("Powerup Settings")]
    [SerializeField] private float powerupCheckInterval = 0.5f; // How often to check for visible powerups (in seconds)
    [SerializeField] private float powerupSelectionProbability = 0.1f; // Probability (0-1) that CPU will select a visible powerup (70% default)
    [SerializeField] private float powerupSelectionDelay = 0.3f; // Delay before CPU selects powerup (to simulate reaction time)
    
    private Coroutine rollCoroutine; // Coroutine for automatic rolling
    private Coroutine powerupCheckCoroutine; // Coroutine for checking visible powerups
    
    private void Start()
    {
        if (numberGrid == null)
        {
            Debug.LogWarning("[CPU] NumberGrid reference is not assigned!");
        }
        
        // Start automatic rolling if enabled
        if (autoRoll && enableCPU)
        {
            StartAutoRolling();
        }
        
        // Start powerup checking coroutine
        if (enableCPU)
        {
            StartPowerupChecking();
        }
    }
    
    private void OnDestroy()
    {
        // Stop rolling coroutine
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
        }
        
        // Stop powerup checking coroutine
        StopPowerupChecking();
    }
    
    /// <summary>
    /// Starts automatic dice rolling at intervals
    /// </summary>
    public void StartAutoRolling()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
        }
        rollCoroutine = StartCoroutine(AutoRollCoroutine());
    }
    
    /// <summary>
    /// Stops automatic dice rolling
    /// </summary>
    public void StopAutoRolling()
    {
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
            rollCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that automatically rolls dice at intervals
    /// </summary>
    private IEnumerator AutoRollCoroutine()
    {
        while (enableCPU && autoRoll)
        {
            yield return new WaitForSeconds(rollInterval);
            
            if (enableCPU)
            {
                RollDice();
            }
        }
    }
    
    /// <summary>
    /// Rolls dice (backend only, no animations) and selects a box
    /// </summary>
    public void RollDice()
    {
        if (!enableCPU || numberGrid == null)
        {
            return;
        }
        
        // Generate random dice values (1-6 for each die)
        int dice1Value = Random.Range(1, 7);
        int dice2Value = Random.Range(1, 7);
        int diceSum = dice1Value + dice2Value;
        
        Debug.Log($"[CPU {cpuIndex}] Rolled dice: {dice1Value} + {dice2Value} = {diceSum}");
        
        // Set the dice sum in NumberGrid for this CPU (with CPU index)
        numberGrid.SetDiceSum(diceSum, PlayerType.CPU, cpuIndex);
        
        // Wait a bit before making selection (to simulate thinking time)
        StartCoroutine(SelectBoxAfterDelay(diceSum));
    }
    
    /// <summary>
    /// Coroutine to select a box after a delay
    /// </summary>
    private IEnumerator SelectBoxAfterDelay(int diceSum)
    {
        yield return new WaitForSeconds(selectionDelay);
        
        if (numberGrid == null)
        {
            yield break;
        }
        
        // First check if there's a visible powerup - prioritize powerups
        GameObject visiblePowerup = numberGrid.GetVisiblePowerup();
        if (visiblePowerup != null)
        {
            // Decide whether to select the powerup based on probability
            float randomValue = Random.Range(0f, 1f);
            if (randomValue <= powerupSelectionProbability)
            {
                // Select the powerup
                bool success = numberGrid.SelectPowerup(visiblePowerup, PlayerType.CPU, cpuIndex);
                if (success)
                {
                    Debug.Log($"[CPU {cpuIndex}] Selected visible powerup!");
                    yield break; // Don't select number box if powerup was selected
                }
            }
        }
        
        // Get available boxes for this sum (for this specific CPU)
        List<GameObject> availableBoxes = numberGrid.GetAvailableBoxes(diceSum, PlayerType.CPU, cpuIndex);
        
        if (availableBoxes == null || availableBoxes.Count == 0)
        {
            Debug.Log($"[CPU {cpuIndex}] No available boxes for sum {diceSum}!");
            yield break;
        }
        
        // Randomly select one of the available boxes
        int randomIndex = Random.Range(0, availableBoxes.Count);
        GameObject selectedBox = availableBoxes[randomIndex];
        
        // Select the box (mark as CPU selection with this CPU's index)
        bool success2 = numberGrid.SelectBox(selectedBox, PlayerType.CPU, cpuIndex);
        
        if (success2)
        {
            Debug.Log($"[CPU {cpuIndex}] Selected box with number {diceSum}!");
        }
        else
        {
            Debug.LogWarning($"[CPU {cpuIndex}] Failed to select box with number {diceSum}!");
        }
    }
    
    /// <summary>
    /// Starts the powerup checking coroutine
    /// </summary>
    private void StartPowerupChecking()
    {
        if (powerupCheckCoroutine != null)
        {
            StopCoroutine(powerupCheckCoroutine);
        }
        powerupCheckCoroutine = StartCoroutine(PowerupCheckLoop());
    }
    
    /// <summary>
    /// Stops the powerup checking coroutine
    /// </summary>
    private void StopPowerupChecking()
    {
        if (powerupCheckCoroutine != null)
        {
            StopCoroutine(powerupCheckCoroutine);
            powerupCheckCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that continuously checks for visible powerups and selects them
    /// </summary>
    private IEnumerator PowerupCheckLoop()
    {
        while (enableCPU)
        {
            yield return new WaitForSeconds(powerupCheckInterval);
            
            if (!enableCPU || numberGrid == null)
            {
                yield break;
            }
            
            // Check for visible powerup
            GameObject visiblePowerup = numberGrid.GetVisiblePowerup();
            if (visiblePowerup != null)
            {
                // Decide whether to select the powerup based on probability
                float randomValue = Random.Range(0f, 1f);
                if (randomValue <= powerupSelectionProbability)
                {
                    // Wait a bit before selecting (simulate reaction time)
                    yield return new WaitForSeconds(powerupSelectionDelay);
                    
                    // Double-check the powerup is still visible (might have been selected by another player)
                    visiblePowerup = numberGrid.GetVisiblePowerup();
                    if (visiblePowerup != null)
                    {
                        // Select the powerup
                        bool success = numberGrid.SelectPowerup(visiblePowerup, PlayerType.CPU, cpuIndex);
                        if (success)
                        {
                            Debug.Log($"[CPU {cpuIndex}] Selected visible powerup!");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Enables or disables the CPU player
    /// </summary>
    public void SetCPUEnabled(bool enabled)
    {
        enableCPU = enabled;
        
        // Start or stop rolling based on enabled state
        if (enabled && autoRoll)
        {
            StartAutoRolling();
        }
        else
        {
            StopAutoRolling();
        }
        
        // Start or stop powerup checking based on enabled state
        if (enabled)
        {
            StartPowerupChecking();
        }
        else
        {
            StopPowerupChecking();
        }
    }
    
    /// <summary>
    /// Sets the delay before CPU makes a selection
    /// </summary>
    public void SetSelectionDelay(float delay)
    {
        selectionDelay = Mathf.Max(0f, delay);
    }
    
    /// <summary>
    /// Sets the CPU index (used for color assignment and tracking)
    /// </summary>
    public void SetCPUIndex(int index)
    {
        cpuIndex = index;
        Debug.Log($"[CPU] CPU index set to {index}");
    }
    
    /// <summary>
    /// Gets the CPU index
    /// </summary>
    public int GetCPUIndex()
    {
        return cpuIndex;
    }
}

