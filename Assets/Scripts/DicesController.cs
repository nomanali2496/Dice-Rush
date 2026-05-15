using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    
    [Header("Number Grid Reference")]
    [SerializeField] private NumberGrid numberGrid; // Reference to NumberGrid to update based on dice sum
    
    [Header("Dice Sum Display")]
    [SerializeField] private TextMeshProUGUI diceSumText; // Text to display the dice sum (TextMeshProUGUI)
    [SerializeField] private Text diceSumTextLegacy; // Text to display the dice sum (Legacy Text - use if not using TextMeshPro)
    [SerializeField] private string sumDisplayFormat = "Sum: {0}"; // Format string for displaying sum (e.g., "Sum: {0}" or "Total: {0}")
    
    [Header("Animation Settings")]
    [SerializeField] private float rollDuration = 1.5f; // Duration of the rolling animation
    [SerializeField] private float spinSpeed = 720f; // Degrees per second for spinning
    [SerializeField] private int minSpins = 3; // Minimum number of spins before stopping
    [SerializeField] private int maxSpins = 6; // Maximum number of spins before stopping
    
    [Header("Dice Face Rotations")]
    [Tooltip("Euler angles for each face value (1-6). Index 0 = Face 1, Index 1 = Face 2, etc.")]
    [SerializeField] private Vector3[] faceRotations = new Vector3[6]
    {
        new Vector3(0, 90, 0),      // Face 1 (value 1) - (0, 90, 0) = value 1
        new Vector3(0, 0, 0),       // Face 2 (value 2) - (0, 0, 0) = value 2
        new Vector3(90, 0, 0),      // Face 3 (value 3) - (90, 0, 0) = value 3
        new Vector3(180, 0, 0),     // Face 4 (value 4) - (180, 0, 0) = value 4
        new Vector3(0, 270, 0),     // Face 5 (value 5) - (0, 270, 0) = value 5
        new Vector3(270, 0, 0)      // Face 6 (value 6) - (270, 0, 0) = value 6
    };
    
    private bool isRolling = false; // Flag to prevent multiple simultaneous rolls
    private Coroutine rollCoroutine1; // Coroutine for dice 1
    private Coroutine rollCoroutine2; // Coroutine for dice 2
    private Coroutine processCoroutine; // Coroutine for post-roll processing
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
        
        // Validate faceRotations array
        ValidateFaceRotations();
        
        // Subscribe to server events
        MatchmakingModel.OnDiceRolledEvent += HandleServerDiceRolled;
        MatchmakingModel.OnSocketErrorEvent += HandleSocketError;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        MatchmakingModel.OnDiceRolledEvent -= HandleServerDiceRolled;
        MatchmakingModel.OnSocketErrorEvent -= HandleSocketError;
        
        // Clean up coroutines
        if (rollCoroutine1 != null) StopCoroutine(rollCoroutine1);
        if (rollCoroutine2 != null) StopCoroutine(rollCoroutine2);
        if (processCoroutine != null) StopCoroutine(processCoroutine);
    }
    
    private Vector3 NormalizeEulerForComparison(Vector3 euler)
    {
        float x = NormalizeAngle(euler.x);
        float y = NormalizeAngle(euler.y);
        float z = NormalizeAngle(euler.z);
        
        // Convert negative angles to positive equivalents (e.g., -90 to 270)
        if (x < 0) x = 360f + x;
        if (y < 0) y = 360f + y;
        if (z < 0) z = 360f + z;
        
        // Normalize to 0-360 range
        x = NormalizeAngle(x);
        y = NormalizeAngle(y);
        z = NormalizeAngle(z);
        
        return new Vector3(x, y, z);
    }
    
    /// <summary>
    /// Validates that the faceRotations array is correctly set up
    /// </summary>
    private void ValidateFaceRotations()
    {
        if (faceRotations == null || faceRotations.Length != 6)
        {
            Debug.LogError("[DicesController] faceRotations array must have exactly 6 elements!");
        }
    }
    
    /// <summary>
    /// Requests a dice roll from the server
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
        
        // Get the current room ID from GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        string roomId = gm != null ? gm.GetCurrentRoomId() : null;
        
        if (string.IsNullOrEmpty(roomId))
        {
            Debug.LogError("[DicesController] Cannot roll dice - no active match found!");
            return;
        }
        
        // Start "rolling" state (waiting for server)
        isRolling = true;
        
        // Disable button during roll
        if (rollButton != null)
        {
            rollButton.interactable = false;
        }
        
        // Ask the server to roll
        MatchmakingModel.RequestDiceRoll(roomId);
    }

    /// <summary>
    /// Called when the server broadcasts the dice roll result
    /// </summary>
    private void HandleServerDiceRolled(DiceRolledData data)
    {
        // We might want to check if the roll was by this user or the CPU,
        // but since Dice Rush often shares the same dice roll for both,
        // we'll just animate it anyway.
        
        targetDice1Value = data.d1;
        targetDice2Value = data.d2;
        
        // Ensure we are in rolling state (even if CPU rolled)
        isRolling = true;
        if (rollButton != null) rollButton.interactable = false;
        
        // Reset completion counter
        completedRolls = 0;
        
        // Start coroutines for both dice
        if (rollCoroutine1 != null) StopCoroutine(rollCoroutine1);
        if (rollCoroutine2 != null) StopCoroutine(rollCoroutine2);
        if (processCoroutine != null) StopCoroutine(processCoroutine);
        
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
    /// Resets the roll state if the server returns an error
    /// </summary>
    private void HandleSocketError(string error)
    {
        if (isRolling && (error.Contains("roll") || error.Contains("Pending")))
        {
            Debug.Log("[DicesController] Resetting roll state due to server error: " + error);
            isRolling = false;
            if (rollButton != null)
            {
                rollButton.interactable = true;
            }
        }
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
        
        // Check if dice is a child (use local rotation) or root (use world rotation)
        bool useLocalRotation = dice.transform.parent != null;
        
        // Get initial rotation
        Quaternion startRotation = useLocalRotation ? dice.transform.localRotation : dice.transform.rotation;
        
        // Calculate target rotation for the face (exact, no offset)
        Vector3 targetEuler = faceRotations[targetFace - 1];
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        
        Debug.Log($"[DicesController] Rolling dice to face {targetFace}, using rotation {targetEuler} from faceRotations[{targetFace - 1}]");
        
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
                if (useLocalRotation)
                {
                    dice.transform.localRotation = baseRotation * spinRotation;
                }
                else
                {
                    dice.transform.rotation = baseRotation * spinRotation;
                }
            }
            else
            {
                // Phase 2: Smooth snap to exact target (last 25% of animation)
                float phase2T = (elapsed - snapPhaseStart) / (rollDuration - snapPhaseStart);
                float snapT = EaseInOutCubic(phase2T);
                
                // Get current rotation and smoothly snap to exact target
                Quaternion currentRot = useLocalRotation ? dice.transform.localRotation : dice.transform.rotation;
                Quaternion newRotation = Quaternion.Slerp(currentRot, targetRotation, snapT);
                
                if (useLocalRotation)
                {
                    dice.transform.localRotation = newRotation;
                }
                else
                {
                    dice.transform.rotation = newRotation;
                }
            }
            
            yield return null;
        }
        
        // Final guarantee: ensure rotation is exactly the target face (no tilt, perfect alignment)
        // This ensures the dice stops perfectly on the face without any tilt or misalignment
        // Set rotation multiple times to ensure Unity applies it correctly
        for (int i = 0; i < 3; i++)
        {
            if (useLocalRotation)
            {
                dice.transform.localRotation = targetRotation;
            }
            else
            {
                dice.transform.rotation = targetRotation;
            }
            yield return null; // Wait a frame between each set
        }
        
        // Verify the rotation was set correctly
        Vector3 finalRotation = useLocalRotation ? dice.transform.localEulerAngles : dice.transform.eulerAngles;
        Debug.Log($"[DicesController] Dice animation complete. Target face: {targetFace}, Target rotation: {targetEuler}, Final rotation: {finalRotation}");
        
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
            
            // Re-enable button immediately when dice finish rolling
            if (rollButton != null)
            {
                rollButton.interactable = true;
            }
            
            // Wait one frame to ensure rotations are fully applied, then get actual values
            if (processCoroutine != null) StopCoroutine(processCoroutine);
            processCoroutine = StartCoroutine(ProcessRollComplete());
        }
    }
    
    /// <summary>
    /// Processes roll completion after ensuring rotations are applied
    /// </summary>
    private IEnumerator ProcessRollComplete()
    {
        // Wait one frame to ensure rotations are fully applied
        yield return null;
        
        // Ensure dice are at exact target rotations (one of the 6 defined rotations)
        bool useLocal1 = dice1.transform.parent != null;
        bool useLocal2 = dice2.transform.parent != null;
        
        Vector3 target1Rotation = faceRotations[targetDice1Value - 1];
        Vector3 target2Rotation = faceRotations[targetDice2Value - 1];
        
        // Force exact rotations multiple times to ensure Unity applies them correctly
        // Unity sometimes converts rotations to equivalent forms, so we force it multiple times
        for (int i = 0; i < 3; i++)
        {
            if (useLocal1)
            {
                dice1.transform.localRotation = Quaternion.Euler(target1Rotation);
            }
            else
            {
                dice1.transform.rotation = Quaternion.Euler(target1Rotation);
            }
            
            if (useLocal2)
            {
                dice2.transform.localRotation = Quaternion.Euler(target2Rotation);
            }
            else
            {
                dice2.transform.rotation = Quaternion.Euler(target2Rotation);
            }
            
            yield return null; // Wait a frame between each set
        }
        
        // Verify the actual rotations match
        Vector3 actual1Rotation = useLocal1 ? dice1.transform.localEulerAngles : dice1.transform.eulerAngles;
        Vector3 actual2Rotation = useLocal2 ? dice2.transform.localEulerAngles : dice2.transform.eulerAngles;
        
        // Validate that the rotations match what we expect
        // Normalize rotations to handle equivalent forms (e.g., -90 = 270, 360 = 0, -90 360 0 = 270 0 0)
        Vector3 normalizedActual1 = NormalizeEulerForComparison(actual1Rotation);
        Vector3 normalizedTarget1 = NormalizeEulerForComparison(target1Rotation);
        Vector3 normalizedActual2 = NormalizeEulerForComparison(actual2Rotation);
        Vector3 normalizedTarget2 = NormalizeEulerForComparison(target2Rotation);
        
        // Check if the actual rotations are close to the target rotations
        float tolerance = 5f; // 5 degree tolerance
        bool dice1Matches = Vector3.Distance(normalizedActual1, normalizedTarget1) < tolerance;
        bool dice2Matches = Vector3.Distance(normalizedActual2, normalizedTarget2) < tolerance;
        
        if (!dice1Matches)
        {
            Debug.LogWarning($"[DicesController] Dice1 rotation mismatch! Target: {target1Rotation}, Actual: {actual1Rotation}. Using target value {targetDice1Value}.");
        }
        if (!dice2Matches)
        {
            Debug.LogWarning($"[DicesController] Dice2 rotation mismatch! Target: {target2Rotation}, Actual: {actual2Rotation}. Using target value {targetDice2Value}.");
        }
        
        // Get actual face values by detecting which face is pointing up
        // This ensures we get the correct value even if rotations don't match exactly
        int actualDice1Value = GetDiceValue(dice1);
        int actualDice2Value = GetDiceValue(dice2);
        
        // If detection failed, fall back to target values
        if (actualDice1Value < 1 || actualDice1Value > 6)
        {
            actualDice1Value = targetDice1Value;
        }
        if (actualDice2Value < 1 || actualDice2Value > 6)
        {
            actualDice2Value = targetDice2Value;
        }
        
        // Calculate sum based on target values (which match the exact rotations)
        int diceSum = actualDice1Value + actualDice2Value;
        
        Debug.Log($"[DicesController] Roll complete! Dice 1: {actualDice1Value} (target rotation: {target1Rotation}, actual: {actual1Rotation}), Dice 2: {actualDice2Value} (target rotation: {target2Rotation}, actual: {actual2Rotation}), Sum: {diceSum}");
        
        // Update dice sum display
        UpdateDiceSumDisplay(diceSum);
        
            // Update NumberGrid with the dice sum (for Player)
            if (numberGrid != null)
            {
                numberGrid.SetDiceSum(diceSum, PlayerType.Player);
            }
        else
        {
            Debug.LogWarning("[DicesController] NumberGrid reference is not assigned!");
        }

        
        // Use actual values (what's visually displayed)
        // Trigger event
        OnDiceRollComplete?.Invoke(actualDice1Value, actualDice2Value);
        
        Debug.Log($"[DicesController] Roll complete! Dice 1: {actualDice1Value}, Dice 2: {actualDice2Value}, Sum: {diceSum}");
    }
    
    /// <summary>
    /// Normalizes euler angles to 0-360 range and handles equivalent rotations
    /// </summary>
    private Vector3 NormalizeEulerAngles(Vector3 euler)
    {
        float x = NormalizeAngle(euler.x);
        float y = NormalizeAngle(euler.y);
        float z = NormalizeAngle(euler.z);
        
        // Handle equivalent rotations (e.g., (0, 180, 180) = (180, 0, 0))
        // Convert to standard form by checking if we can simplify
        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 180f) && Mathf.Approximately(z, 180f))
        {
            return new Vector3(180f, 0f, 0f);
        }
        if (Mathf.Approximately(x, 180f) && Mathf.Approximately(y, 180f) && Mathf.Approximately(z, 0f))
        {
            return new Vector3(0f, 0f, 180f);
        }
        
        return new Vector3(x, y, z);
    }
    
    [Header("Dice Face Detection")]
    [Tooltip("Reference direction to check which face is pointing (usually Vector3.up for top face)")]
    [SerializeField] private Vector3 faceCheckDirection = Vector3.up; // Direction to check for face (usually up)
    [Tooltip("Tolerance angle in degrees for face detection")]
    [SerializeField] private float faceDetectionTolerance = 15f; // How close the face needs to be to the check direction
    
    /// <summary>
    /// Gets the current face value of a dice by checking which face is pointing up (face up = value 1)
    /// This method checks which face rotation, when applied to the dice, makes that face point up
    /// </summary>
    private int GetDiceValue(GameObject dice)
    {
        if (dice == null) return 1;
        
        // Get the dice's current rotation
        Quaternion diceRotation = dice.transform.parent != null ? dice.transform.localRotation : dice.transform.rotation;
        
        // The world up direction (what we're checking against)
        Vector3 worldUp = Vector3.up;
        
        // For each face, check if when the dice is at that face's rotation, the face points up
        float maxDot = -1f;
        int bestFace = 1;
        
        for (int i = 0; i < faceRotations.Length; i++)
        {
            // Get the face rotation
            Quaternion faceRotation = Quaternion.Euler(faceRotations[i]);
            
            // When dice is at this face rotation, transform the up direction to see where it points
            Vector3 faceUpDirection = faceRotation * faceCheckDirection;
            
            // Now check: if the dice is currently at diceRotation, and we want face i+1 to be up,
            // we need to see if diceRotation rotated by faceRotation makes faceUpDirection point to worldUp
            // Actually, simpler: check if the current dice rotation matches the face rotation
            float angle = Quaternion.Angle(diceRotation, faceRotation);
            
            // Convert angle to a dot product-like score (smaller angle = better match)
            float score = 1f - (angle / 180f); // Score from 1 (perfect match) to -1 (opposite)
            
            if (score > maxDot)
            {
                maxDot = score;
                bestFace = i + 1;
            }
        }
        
        // Convert score back to angle
        float bestAngle = (1f - maxDot) * 180f;
        
        // If the angle is too large, the face detection might be wrong
        if (bestAngle > faceDetectionTolerance)
        {
            Debug.LogWarning($"[DicesController] Face detection angle too large ({bestAngle}°), using target value as fallback");
            if (dice == dice1)
            {
                return targetDice1Value;
            }
            else if (dice == dice2)
            {
                return targetDice2Value;
            }
        }
        
        Debug.Log($"[DicesController] GetDiceValue - Detected face: {bestFace} (angle: {bestAngle}°, score: {maxDot})");
        return bestFace;
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
    
    /// <summary>
    /// Sets the interactability of the roll button
    /// </summary>
    public void SetRollButtonInteractable(bool interactable)
    {
        if (rollButton != null)
        {
            rollButton.interactable = interactable;
        }
    }

    /// <summary>
    /// Resets the rolling state and stops any active roll coroutines.
    /// Used when recovering a match to ensure the dice state is clean.
    /// </summary>
    public void ResetRollingState()
    {
        isRolling = false;
        completedRolls = 0;
        
        if (rollCoroutine1 != null) StopCoroutine(rollCoroutine1);
        if (rollCoroutine2 != null) StopCoroutine(rollCoroutine2);
        if (processCoroutine != null) StopCoroutine(processCoroutine);
        
        // Force dice to snap to their last known valid orientations
        // This prevents them from starting the next roll from a tilted position
        if (dice1 != null && targetDice1Value >= 1 && targetDice1Value <= 6)
        {
            Vector3 rot = faceRotations[targetDice1Value - 1];
            if (dice1.transform.parent != null) dice1.transform.localRotation = Quaternion.Euler(rot);
            else dice1.transform.rotation = Quaternion.Euler(rot);
        }
        
        if (dice2 != null && targetDice2Value >= 1 && targetDice2Value <= 6)
        {
            Vector3 rot = faceRotations[targetDice2Value - 1];
            if (dice2.transform.parent != null) dice2.transform.localRotation = Quaternion.Euler(rot);
            else dice2.transform.rotation = Quaternion.Euler(rot);
        }
        
        if (rollButton != null)
        {
            rollButton.interactable = true;
        }
    }

    /// <summary>
    /// Updates the dice sum display text
    /// </summary>
    private void UpdateDiceSumDisplay(int sum)
    {
        string displayText = string.Format(sumDisplayFormat, sum);
        
        // Try TextMeshProUGUI first
        if (diceSumText != null)
        {
            diceSumText.text = displayText;
        }
        // Fallback to legacy Text component
        else if (diceSumTextLegacy != null)
        {
            diceSumTextLegacy.text = displayText;
        }
    }
    
    /// <summary>
    /// Clears the dice sum display
    /// </summary>
    public void ClearDiceSumDisplay()
    {
        if (diceSumText != null)
        {
            diceSumText.text = "";
        }
        if (diceSumTextLegacy != null)
        {
            diceSumTextLegacy.text = "";
        }
    }
    
    #region Test Methods
    
    [ContextMenu("Roll Dices")]
    private void TestRollDices()
    {
        RollDices();
    }
    
    #endregion
    
    #region Dice Visibility Control
    
    /// <summary>
    /// Hides both dice (called when panels are shown over gameplay)
    /// </summary>
    public void HideDices()
    {
        if (dice1 != null)
        {
            dice1.SetActive(false);
        }
        if (dice2 != null)
        {
            dice2.SetActive(false);
        }
        Debug.Log("[DicesController] Dice hidden - panel is active");
    }
    
    /// <summary>
    /// Shows both dice (called when on gameplay with no panels)
    /// </summary>
    public void ShowDices()
    {
        if (dice1 != null)
        {
            dice1.SetActive(true);
        }
        if (dice2 != null)
        {
            dice2.SetActive(true);
        }
        Debug.Log("[DicesController] Dice visible - gameplay active");
    }
    
    #endregion
}

