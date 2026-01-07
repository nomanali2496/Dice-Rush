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
    
    private Coroutine rollCoroutine; // Coroutine for automatic rolling
    
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
    }
    
    private void OnDestroy()
    {
        // Stop rolling coroutine
        if (rollCoroutine != null)
        {
            StopCoroutine(rollCoroutine);
        }
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
        
        // Check if there's a visible powerup available
        GameObject visiblePowerup = numberGrid.GetCurrentlyVisiblePowerup();
        if (visiblePowerup != null)
        {
            // CPU has a chance to select the powerup (50% probability)
            // This makes CPUs competitive but not too aggressive
            if (Random.Range(0f, 1f) < 0.5f)
            {
                bool powerupSuccess = numberGrid.SelectPowerup(visiblePowerup, PlayerType.CPU, cpuIndex);
                if (powerupSuccess)
                {
                    Debug.Log($"[CPU {cpuIndex}] Selected visible powerup!");
                    yield break; // Powerup selected, done
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
        bool success = numberGrid.SelectBox(selectedBox, PlayerType.CPU, cpuIndex);
        
        if (success)
        {
            Debug.Log($"[CPU {cpuIndex}] Selected box with number {diceSum}!");
        }
        else
        {
            Debug.LogWarning($"[CPU {cpuIndex}] Failed to select box with number {diceSum}!");
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

