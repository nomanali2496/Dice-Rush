using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ScoreFeedbackUIManager : MonoBehaviour
{
    public static ScoreFeedbackUIManager Instance;

    [Header("Sprites Configuration")]
    [SerializeField] private Sprite goodSprite;    // For +100
    [SerializeField] private Sprite greatSprite;   // For +200
    [SerializeField] private Sprite oopsSprite;    // For -50

    [Header("Animation Settings")]
    [SerializeField, Tooltip("Prefab with an Image component for sprite feedback")] 
    private GameObject feedbackPrefab; 
    
    [SerializeField, Tooltip("Prefab with TextMeshProUGUI for point value feedback")] 
    private GameObject pointsTextPrefab; 
    
    [SerializeField, Tooltip("The UI target where points should fly towards")] 
    private RectTransform pointsTarget; 

    [Header("Colors")]
    [SerializeField] private Color positiveColor = Color.yellow;
    [SerializeField] private Color negativeColor = Color.red;
    private Color originalTargetColor = Color.white;

    [Header("Timing and Motion")]
    [SerializeField, Range(0.5f, 5.0f)] private float spriteShowDuration = 1.5f;
    [SerializeField, Range(0f, 500f)] private float floatingDistance = 180f;
    [SerializeField, Range(0.1f, 3.0f)] private float scaleMultiplier = 1.25f;
    [SerializeField, Range(0.5f, 5.0f)] private float flyToTargetDuration = 1.2f;
    [SerializeField, Range(0f, 2.0f)] private float secondaryDelay = 0.4f;
    [SerializeField, Range(0f, 1.0f)] private float endScale = 0.6f;

    private int currentDisplayScore = 0;
    private TMPro.TextMeshProUGUI targetScoreDisplay;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (pointsTarget != null)
        {
            targetScoreDisplay = pointsTarget.GetComponent<TMPro.TextMeshProUGUI>();
            if (targetScoreDisplay == null) targetScoreDisplay = pointsTarget.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            
            if (targetScoreDisplay != null)
            {
                originalTargetColor = targetScoreDisplay.color;
            }
        }
    }

    /// <summary>
    /// Triggers the appropriate feedback animation based on the score change.
    /// </summary>
    /// <param name="scoreChange">The added/subtracted score.</param>
    /// <param name="targetPosition">The world position where the feedback should appear.</param>
    public void ShowScoreFeedback(int scoreChange, Vector3 targetPosition)
    {
        Sprite spriteToUse = null;

        if (scoreChange == 100)
        {
            spriteToUse = goodSprite;
        }
        else if (scoreChange == 200)
        {
            spriteToUse = greatSprite;
        }
        else if (scoreChange == -50)
        {
            spriteToUse = oopsSprite;
        }

        if (spriteToUse == null)
        {
            Debug.LogWarning($"[ScoreFeedbackUIManager] No sprite found for score change: {scoreChange}");
            return;
        }

        if (feedbackPrefab == null)
        {
            Debug.LogError("[ScoreFeedbackUIManager] Feedback prefab is not assigned!");
            return;
        }

        // Instantiate the sprite feedback (IMAGE) in the CENTER of the manager (usually screen center)
        GameObject feedback = Instantiate(feedbackPrefab, transform);
        feedback.transform.localPosition = Vector3.zero; // Center relative to this Canvas manager

        // Reset scale first
        feedback.transform.localScale = Vector3.zero;

        // Set the sprite
        Image feedbackImage = feedback.GetComponent<Image>();
        if (feedbackImage == null)
        {
            feedbackImage = feedback.GetComponentInChildren<Image>();
        }

        if (feedbackImage != null)
        {
            feedbackImage.sprite = spriteToUse;
            feedbackImage.SetNativeSize();
            
            // Initial animation: Scale up with pop
            feedback.transform.DOScale(Vector3.one * scaleMultiplier, 0.4f).SetEase(Ease.OutBack);

            // Floating animation: Move up and fade out from CENTER
            Vector3 startPos = feedback.transform.position;
            feedback.transform.DOMoveY(startPos.y + floatingDistance, spriteShowDuration).SetEase(Ease.OutQuad);
            feedbackImage.DOFade(0f, spriteShowDuration * 0.4f).SetDelay(spriteShowDuration * 0.6f);

            // Clean up
            Destroy(feedback, spriteShowDuration + 0.1f);

            // Trigger secondary animation: Text flying to target (still starts from BOX position)
            if (pointsTextPrefab != null && pointsTarget != null)
            {
                AnimateTextToTarget(scoreChange, targetPosition);
            }
        }
        else
        {
            Debug.LogError("[ScoreFeedbackUIManager] Feedback prefab must have an Image component!");
            Destroy(feedback);
        }
    }

    private void AnimateTextToTarget(int scoreChange, Vector3 startPosition)
    {
        GameObject textObj = Instantiate(pointsTextPrefab, transform);
        textObj.transform.position = startPosition;

        // Find all TextMeshProUGUI components (Main + Shadow/Outline)
        TMPro.TextMeshProUGUI[] texts = textObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>();

        if (texts != null && texts.Length > 0)
        {
            string prefix = scoreChange > 0 ? "+" : "";
            string scoreString = prefix + scoreChange.ToString();
            
            foreach (var txt in texts)
            {
                txt.text = scoreString;
            }
            
            // Initial pop
            textObj.transform.localScale = Vector3.zero;
            textObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            // Move to target AND Zoom out (Scale down)
            textObj.transform.DOMove(pointsTarget.position, flyToTargetDuration)
                .SetDelay(secondaryDelay)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    // Update the target score display
                    UpdateTargetScore(scoreChange);
                    
                    // Slight punch to the target UI for "impact"
                    pointsTarget.DOPunchScale(Vector3.one * 0.15f, 0.3f);
                    Destroy(textObj);
                });
            
            // Zoom out animation: Scale towards endScale instead of zero
            textObj.transform.DOScale(Vector3.one * endScale, flyToTargetDuration)
                .SetDelay(secondaryDelay)
                .SetEase(Ease.InBack);
            
            // Note: Removed DOFade here so it stays solid until it reaches the target (as requested)
        }
        else
        {
            Destroy(textObj);
        }
    }

    private void UpdateTargetScore(int scoreChange)
    {
        if (targetScoreDisplay != null)
        {
            int startValue = currentDisplayScore;
            currentDisplayScore += scoreChange;
            
            // Change color based on positive/negative
            Color flashColor = scoreChange >= 0 ? positiveColor : negativeColor;
            targetScoreDisplay.color = flashColor;
            
            // Animate the text counting up/down
            DOTween.To(() => startValue, x => {
                targetScoreDisplay.text = x.ToString();
            }, currentDisplayScore, 0.3f);

            // Revert color back to original
            targetScoreDisplay.DOColor(originalTargetColor, 0.5f).SetDelay(0.3f);
        }
        else
        {
            // Fallback: If we don't have a text component, we can't update it
            Debug.LogWarning("[ScoreFeedbackUIManager] Target Score Display is missing!");
        }
    }
}
