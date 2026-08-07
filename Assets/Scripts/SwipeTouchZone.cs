using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Touch zone panel that detects swipe gestures without blocking UI interactions
/// Attach this to a full-screen transparent Image panel behind your UI elements
/// </summary>
public class SwipeTouchZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger
    [SerializeField] private bool enableSwipeDetection = true;
    
    [Header("Velocity-Based Scrolling")]
    [SerializeField] private bool useVelocityScrolling = true; // Enable multiple items per swipe based on distance
    [SerializeField] private float pixelsPerItem = 100f; // Pixels of swipe distance = 1 item scroll
    [SerializeField] private int maxScrollItems = 5; // Maximum items to scroll in one swipe
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Swipe tracking
    private Vector2 swipeStartPos;
    private bool isSwiping = false;
    
    // Reference to HorizontalLobbyManager
    private HorizontalLobbyManager horizontalLobbyManager;
    
    private void Start()
    {
        // Find HorizontalLobbyManager in the scene
        horizontalLobbyManager = FindFirstObjectByType<HorizontalLobbyManager>();
        
        if (horizontalLobbyManager == null)
        {
            Debug.LogError("[SwipeTouchZone] HorizontalLobbyManager not found! Swipe detection will not work.");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[SwipeTouchZone] Successfully connected to HorizontalLobbyManager");
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
            Debug.Log($"[SwipeTouchZone] Swipe started at: {swipeStartPos}");
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
            Debug.Log($"[SwipeTouchZone] Swipe ended at: {swipeEndPos}, Delta: {swipeDelta}");
        
        // Check if swipe is long enough (primarily horizontal for HorizontalLobbyManager)
        if (Mathf.Abs(swipeDelta.x) > swipeThreshold)
        {
            // Ensure horizontal swipe is more dominant than vertical
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                if (horizontalLobbyManager != null)
                {
                    if (useVelocityScrolling)
                    {
                        // Calculate how many items to scroll based on swipe distance
                        float swipeDistance = Mathf.Abs(swipeDelta.x);
                        int itemsToScroll = Mathf.RoundToInt(swipeDistance / pixelsPerItem);
                        
                        // Clamp to max scroll items
                        itemsToScroll = Mathf.Clamp(itemsToScroll, 1, maxScrollItems);
                        
                        // Swipe left = scroll right (show next items)
                        if (swipeDelta.x < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log($"[SwipeTouchZone] Swipe LEFT detected ({swipeDistance:F0}px) -> Scroll RIGHT by {itemsToScroll} items");
                            horizontalLobbyManager.ScrollMultiple(itemsToScroll);
                        }
                        // Swipe right = scroll left (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log($"[SwipeTouchZone] Swipe RIGHT detected ({swipeDistance:F0}px) -> Scroll LEFT by {itemsToScroll} items");
                            horizontalLobbyManager.ScrollMultiple(-itemsToScroll);
                        }
                    }
                    else
                    {
                        // Single-step scrolling
                        // Swipe left = scroll right (show next items)
                        if (swipeDelta.x < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log("[SwipeTouchZone] Swipe LEFT detected -> Scroll RIGHT");
                            horizontalLobbyManager.ScrollRight();
                        }
                        // Swipe right = scroll left (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log("[SwipeTouchZone] Swipe RIGHT detected -> Scroll LEFT");
                            horizontalLobbyManager.ScrollLeft();
                        }
                    }
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("[SwipeTouchZone] Horizontal swipe detected, ignoring");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[SwipeTouchZone] Swipe too short ({Mathf.Abs(swipeDelta.y)} < {swipeThreshold}), ignoring");
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
            Debug.Log($"[SwipeTouchZone] Swipe detection {(enabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Update the swipe threshold
    /// </summary>
    public void SetSwipeThreshold(float threshold)
    {
        swipeThreshold = Mathf.Max(10f, threshold);
        
        if (showDebugLogs)
            Debug.Log($"[SwipeTouchZone] Swipe threshold updated to: {swipeThreshold}");
    }
}
