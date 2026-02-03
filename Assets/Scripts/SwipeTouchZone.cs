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
    
    // Reference to LobbyManager
    private LobbyManager lobbyManager;
    
    private void Start()
    {
        // Find LobbyManager in the scene
        lobbyManager = FindObjectOfType<LobbyManager>();
        
        if (lobbyManager == null)
        {
            Debug.LogError("[SwipeTouchZone] LobbyManager not found! Swipe detection will not work.");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[SwipeTouchZone] Successfully connected to LobbyManager");
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
        
        // Check if swipe is long enough (primarily vertical)
        if (Mathf.Abs(swipeDelta.y) > swipeThreshold)
        {
            // Ensure vertical swipe is more dominant than horizontal
            if (Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
            {
                if (lobbyManager != null)
                {
                    if (useVelocityScrolling)
                    {
                        // Calculate how many items to scroll based on swipe distance
                        float swipeDistance = Mathf.Abs(swipeDelta.y);
                        int itemsToScroll = Mathf.RoundToInt(swipeDistance / pixelsPerItem);
                        
                        // Clamp to max scroll items
                        itemsToScroll = Mathf.Clamp(itemsToScroll, 1, maxScrollItems);
                        
                        // Swipe up = scroll down (show next items)
                        if (swipeDelta.y < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log($"[SwipeTouchZone] Swipe UP detected ({swipeDistance:F0}px) -> Scroll DOWN by {itemsToScroll} items");
                            lobbyManager.ScrollMultiple(itemsToScroll);
                        }
                        // Swipe down = scroll up (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log($"[SwipeTouchZone] Swipe DOWN detected ({swipeDistance:F0}px) -> Scroll UP by {itemsToScroll} items");
                            lobbyManager.ScrollMultiple(-itemsToScroll);
                        }
                    }
                    else
                    {
                        // Single-step scrolling (original behavior)
                        // Swipe up = scroll down (show next items)
                        if (swipeDelta.y < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log("[SwipeTouchZone] Swipe UP detected -> Scroll DOWN");
                            lobbyManager.ScrollDown();
                        }
                        // Swipe down = scroll up (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log("[SwipeTouchZone] Swipe DOWN detected -> Scroll UP");
                            lobbyManager.ScrollUp();
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
