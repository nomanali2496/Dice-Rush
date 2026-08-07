using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Touch zone panel that detects horizontal swipe gestures for offers carousel
/// Attach this to a full-screen transparent Image panel behind your offer UI elements
/// </summary>
public class OfferSwipeTouchZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger
    [SerializeField] private bool enableSwipeDetection = true;
    
    [Header("Velocity-Based Scrolling")]
    [SerializeField] private bool useVelocityScrolling = true; // Enable multiple items per swipe based on distance
    [SerializeField] private float pixelsPerItem = 150f; // Pixels of swipe distance = 1 item scroll (wider for horizontal)
    [SerializeField] private int maxScrollItems = 3; // Maximum items to scroll in one swipe (offers has 7 total)
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Swipe tracking
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
            Debug.LogError("[OfferSwipeTouchZone] OffersCarouselManager not found! Swipe detection will not work.");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[OfferSwipeTouchZone] Successfully connected to OffersCarouselManager");
        }
    }
    
    /// <summary>
    /// Called when pointer/touch begins on this UI element
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enableSwipeDetection) return;
        
        swipeStartPos = eventData.position;
        isSwiping = true;
        
        if (showDebugLogs)
            Debug.Log($"[OfferSwipeTouchZone] Swipe started at: {swipeStartPos}");
    }
    
    /// <summary>
    /// Called when pointer/touch is released from this UI element
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!enableSwipeDetection || !isSwiping) return;
        
        Vector2 swipeEndPos = eventData.position;
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        
        if (showDebugLogs)
            Debug.Log($"[OfferSwipeTouchZone] Swipe ended at: {swipeEndPos}, Delta: {swipeDelta}");
        
        // Check if swipe is long enough (primarily HORIZONTAL)
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
                        
                        // Swipe LEFT (negative X) = scroll RIGHT (show next items)
                        if (swipeDelta.x < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log($"[OfferSwipeTouchZone] Swipe LEFT detected ({swipeDistance:F0}px) -> Scroll RIGHT by {itemsToScroll} items");
                            
                            // Use ScrollMultiple for sequential animations
                            offersManager.ScrollMultiple(itemsToScroll);
                        }
                        // Swipe RIGHT (positive X) = scroll LEFT (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log($"[OfferSwipeTouchZone] Swipe RIGHT detected ({swipeDistance:F0}px) -> Scroll LEFT by {itemsToScroll} items");
                            
                            // Use ScrollMultiple for sequential animations  
                            offersManager.ScrollMultiple(-itemsToScroll);
                        }
                    }
                    else
                    {
                        // Single-step scrolling (original behavior)
                        // Swipe LEFT = scroll RIGHT
                        if (swipeDelta.x < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log("[OfferSwipeTouchZone] Swipe LEFT detected -> Scroll RIGHT");
                            offersManager.ScrollRight();
                        }
                        // Swipe RIGHT = scroll LEFT
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log("[OfferSwipeTouchZone] Swipe RIGHT detected -> Scroll LEFT");
                            offersManager.ScrollLeft();
                        }
                    }
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("[OfferSwipeTouchZone] Vertical swipe detected, ignoring");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[OfferSwipeTouchZone] Swipe too short ({Mathf.Abs(swipeDelta.x)} < {swipeThreshold}), ignoring");
        }
        
        isSwiping = false;
    }
    
    /// <summary>
    /// Enable or disable swipe detection
    /// </summary>
    public void SetSwipeDetectionEnabled(bool enabled)
    {
        enableSwipeDetection = enabled;
        
        if (showDebugLogs)
            Debug.Log($"[OfferSwipeTouchZone] Swipe detection {(enabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Update the swipe threshold
    /// </summary>
    public void SetSwipeThreshold(float threshold)
    {
        swipeThreshold = Mathf.Max(10f, threshold);
        
        if (showDebugLogs)
            Debug.Log($"[OfferSwipeTouchZone] Swipe threshold updated to: {swipeThreshold}");
    }
}
