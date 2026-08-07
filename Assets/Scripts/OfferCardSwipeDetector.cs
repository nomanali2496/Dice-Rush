using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add this component to each offer GameObject to detect swipes directly on the offer
/// Forwards swipes to OffersCarouselManager for scrolling
/// </summary>
public class OfferCardSwipeDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Swipe Detection")]
    [SerializeField] private bool enableSwipeOnCard = true; // Enable swipe detection on this card
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger
    [SerializeField] private bool useVelocityScrolling = true; // Use velocity-based scrolling
    [SerializeField] private float pixelsPerItem = 150f; // Pixels per item for velocity scrolling
    [SerializeField] private int maxScrollItems = 3; // Max items to scroll
    
    private Vector2 swipeStartPos;
    private bool isSwiping = false;
    
    // Reference to OffersCarouselManager
    private OffersCarouselManager offersManager;
    
    private void Start()
    {
        // Find OffersCarouselManager in the scene
        offersManager = FindFirstObjectByType<OffersCarouselManager>();
        
        if (offersManager == null)
        {
            Debug.LogError("[OfferCardSwipeDetector] OffersCarouselManager not found! Swipe detection will not work.");
        }
    }
    
    /// <summary>
    /// Called when pointer/touch begins on this offer card
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableSwipeOnCard) return;
        
        swipeStartPos = eventData.position;
        isSwiping = true;
    }
    
    /// <summary>
    /// Called when pointer/touch is released from this offer card
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enableSwipeOnCard || !isSwiping) return;
        
        Vector2 swipeEndPos = eventData.position;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        
        // Check if swipe is long enough (primarily horizontal)
        if (Mathf.Abs(swipeDelta.x) > swipeThreshold)
        {
            // Ensure horizontal swipe is more dominant than vertical
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                if (offersManager != null)
                {
                    if (useVelocityScrolling)
                    {
                        // Calculate how many items to scroll based on swipe distance
                        float swipeDistance = Mathf.Abs(swipeDelta.x);
                        int itemsToScroll = Mathf.RoundToInt(swipeDistance / pixelsPerItem);
                        
                        // Clamp to max scroll items
                        itemsToScroll = Mathf.Clamp(itemsToScroll, 1, maxScrollItems);
                        
                        // Forward to OffersCarouselManager
                        if (swipeDelta.x < 0)
                        {
                            // Swipe left = scroll right
                            offersManager.ScrollMultiple(itemsToScroll);
                        }
                        else
                        {
                            // Swipe right = scroll left
                            offersManager.ScrollMultiple(-itemsToScroll);
                        }
                    }
                    else
                    {
                        // Single-step scrolling
                        if (swipeDelta.x < 0)
                        {
                            offersManager.ScrollRight();
                        }
                        else
                        {
                            offersManager.ScrollLeft();
                        }
                    }
                }
            }
        }
        
        isSwiping = false;
    }
}
