using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Controls 2 dice animation and rolling to random faces when button is clicked
/// </summary>
public class DicesController : MonoBehaviour
{
    [Header("Dice References")]
    [SerializeField] private GameObject dice1; // First dice GameObject
    [SerializeField] private GameObject dice2; // Second dice GameObject
    
    [Header("Roll Button")]
    [SerializeField] private Button rollButton; // Button to trigger dice roll
    
    [Header("Animation Settings")]
    [SerializeField] private float rollDuration = 1.5f; // Duration of the rolling animation
    [SerializeField] private float spinSpeed = 720f; // Degrees per second for spinning
    [SerializeField] private int minSpins = 3; // Minimum number of spins before stopping
    [SerializeField] private int maxSpins = 6; // Maximum number of spins before stopping
    
    [Header("Dice Face Rotations")]
    [Tooltip("Euler angles for each face (1-6). Adjust based on your dice model orientation.")]
    [SerializeField] private Vector3[] faceRotations = new Vector3[6]
    {
        new Vector3(0, 0, 0),      // Face 1
        new Vector3(90, 0, 0),      // Face 2
        new Vector3(0, 0, -90),    // Face 3
        new Vector3(0, 0, 90),     // Face 4
        new Vector3(-90, 0, 0),    // Face 5
        new Vector3(180, 0, 0)     // Face 6
    };
    
    private bool isRolling = false; // Flag to prevent multiple simultaneous rolls
    private Coroutine rollCoroutine1; // Coroutine for dice 1
    private Coroutine rollCoroutine2; // Coroutine for dice 2
    private int completedRolls = 0; // Counter for completed dice rolls
    private int targetDice1Value = 1; // Target value for dice 1
    private int targetDice2Value = 1; // Target value for dice 2
    
    // Events
    public System.Action<int, int> OnDiceRollComplete; // Called when roll finishes (dice1Value, dice2Value)
    
    private void Start()
    {
        // Setup button listener
        if (rollButton != null)
        {
            rollButton.onClick.RemoveAllListeners();
            rollButton.onClick.AddListener(RollDices);
        }
        else
        {
            Debug.LogWarning("[DicesController] Roll button is not assigned!");
        }
        
        // Validate dice references
        if (dice1 == null || dice2 == null)
        {
            Debug.LogWarning("[DicesController] One or both dice GameObjects are not assigned!");
        }
    }
    
    /// <summary>
    /// Rolls both dice to random faces
    /// </summary>
    public void RollDices()
    {
        if (isRolling)
        {
            Debug.Log("[DicesController] Dice are already rolling!");
            return;
        }
        
        if (dice1 == null || dice2 == null)
        {
            Debug.LogError("[DicesController] Cannot roll dice - dice GameObjects are not assigned!");
            return;
        }
        
        // Generate random face values (1-6)
        targetDice1Value = Random.Range(1, 7);
        targetDice2Value = Random.Range(1, 7);
        
        // Start rolling animation
        isRolling = true;
        
        // Disable button during roll
        if (rollButton != null)
        {
            rollButton.interactable = false;
        }
        
        // Reset completion counter
        completedRolls = 0;
        
        // Start coroutines for both dice
        if (rollCoroutine1 != null) StopCoroutine(rollCoroutine1);
        if (rollCoroutine2 != null) StopCoroutine(rollCoroutine2);
        
        rollCoroutine1 = StartCoroutine(RollDiceCoroutine(dice1, targetDice1Value, () =>
        {
            completedRolls++;
            CheckRollComplete();
        }));
        
        rollCoroutine2 = StartCoroutine(RollDiceCoroutine(dice2, targetDice2Value, () =>
        {
            completedRolls++;
            CheckRollComplete();
        }));
    }
    
    /// <summary>
    /// Coroutine that animates a single dice rolling to a specific face
    /// </summary>
    private IEnumerator RollDiceCoroutine(GameObject dice, int targetFace, System.Action onComplete)
    {
        if (dice == null || targetFace < 1 || targetFace > 6)
        {
            onComplete?.Invoke();
            yield break;
        }
        
        // Get initial rotation
        Quaternion startRotation = dice.transform.rotation;
        
        // Calculate target rotation for the face (exact, no offset)
        Vector3 targetEuler = faceRotations[targetFace - 1];
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        
        // Calculate number of spins (random between min and max)
        int numSpins = Random.Range(minSpins, maxSpins + 1);
        float totalSpinDegrees = numSpins * 360f;
        
        // Choose random axis for spinning (for visual variety)
        Vector3 spinAxis = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
        
        // Animate the roll
        float elapsed = 0f;
        float snapPhaseStart = rollDuration * 0.75f; // Last 25% of animation for precise alignment
        
        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rollDuration);
            
            if (elapsed < snapPhaseStart)
            {
                // Phase 1: Spinning animation with gradual approach to target
                float phase1T = elapsed / snapPhaseStart;
                float easedT = EaseOutCubic(phase1T);
                
                // Calculate remaining spin (decreases as animation progresses)
                float remainingSpin = totalSpinDegrees * (1f - easedT);
                
                // Gradually transition from start to target rotation using Slerp
                Quaternion baseRotation = Quaternion.Slerp(startRotation, targetRotation, easedT * 0.9f);
                
                // Apply spinning rotation
                Quaternion spinRotation = Quaternion.AngleAxis(remainingSpin, spinAxis);
                dice.transform.rotation = baseRotation * spinRotation;
            }
            else
            {
                // Phase 2: Smooth snap to exact target (last 25% of animation)
                float phase2T = (elapsed - snapPhaseStart) / (rollDuration - snapPhaseStart);
                float snapT = EaseInOutCubic(phase2T);
                
                // Get current rotation and smoothly snap to exact target
                Quaternion currentRot = dice.transform.rotation;
                dice.transform.rotation = Quaternion.Slerp(currentRot, targetRotation, snapT);
            }
            
            yield return null;
        }
        
        // Final guarantee: ensure rotation is exactly the target face (no tilt, perfect alignment)
        // This ensures the dice stops perfectly on the face without any tilt or misalignment
        dice.transform.rotation = targetRotation;
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Checks if both dice have finished rolling
    /// </summary>
    private void CheckRollComplete()
    {
        if (completedRolls >= 2)
        {
            isRolling = false;
            completedRolls = 0;
            
            // Re-enable button
            if (rollButton != null)
            {
                rollButton.interactable = true;
            }
            
            // Use target values (they should match the final rotation)
            // Trigger event
            OnDiceRollComplete?.Invoke(targetDice1Value, targetDice2Value);
            
            Debug.Log($"[DicesController] Roll complete! Dice 1: {targetDice1Value}, Dice 2: {targetDice2Value}");
        }
    }
    
    /// <summary>
    /// Gets the current face value of a dice based on its rotation
    /// This is a simplified version - you may need to adjust based on your dice model
    /// </summary>
    private int GetDiceValue(GameObject dice)
    {
        if (dice == null) return 1;
        
        Vector3 euler = dice.transform.eulerAngles;
        
        // Normalize angles to 0-360
        float x = NormalizeAngle(euler.x);
        float y = NormalizeAngle(euler.y);
        float z = NormalizeAngle(euler.z);
        
        // Find closest matching face rotation
        float minDistance = float.MaxValue;
        int closestFace = 1;
        
        for (int i = 0; i < faceRotations.Length; i++)
        {
            Vector3 faceEuler = faceRotations[i];
            float distance = Vector3.Distance(
                new Vector3(NormalizeAngle(faceEuler.x), NormalizeAngle(faceEuler.y), NormalizeAngle(faceEuler.z)),
                new Vector3(x, y, z)
            );
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFace = i + 1;
            }
        }
        
        return closestFace;
    }
    
    /// <summary>
    /// Normalizes an angle to 0-360 range
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 360f;
        while (angle >= 360) angle -= 360f;
        return angle;
    }
    
    /// <summary>
    /// Easing function for smooth deceleration
    /// </summary>
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    /// <summary>
    /// Easing function for smooth acceleration and deceleration
    /// </summary>
    private float EaseInOutCubic(float t)
    {
        return t < 0.5f 
            ? 4f * t * t * t 
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    /// <summary>
    /// Manually sets dice to specific face values (for testing)
    /// </summary>
    public void SetDiceFaces(int dice1Face, int dice2Face)
    {
        if (dice1 != null && dice1Face >= 1 && dice1Face <= 6)
        {
            dice1.transform.rotation = Quaternion.Euler(faceRotations[dice1Face - 1]);
        }
        
        if (dice2 != null && dice2Face >= 1 && dice2Face <= 6)
        {
            dice2.transform.rotation = Quaternion.Euler(faceRotations[dice2Face - 1]);
        }
    }
    
    /// <summary>
    /// Gets current face values of both dice
    /// </summary>
    public (int dice1, int dice2) GetCurrentDiceValues()
    {
        int d1 = dice1 != null ? GetDiceValue(dice1) : 1;
        int d2 = dice2 != null ? GetDiceValue(dice2) : 1;
        return (d1, d2);
    }
    
    private void OnDestroy()
    {
        // Clean up coroutines
        if (rollCoroutine1 != null) StopCoroutine(rollCoroutine1);
        if (rollCoroutine2 != null) StopCoroutine(rollCoroutine2);
    }
    
    #region Test Methods
    
    [ContextMenu("Roll Dices")]
    private void TestRollDices()
    {
        RollDices();
    }
    
    #endregion
}

