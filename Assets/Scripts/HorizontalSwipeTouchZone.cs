using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Touch zone panel that detects horizontal swipe gestures without blocking UI interactions.
/// When the user performs a click/tap (not a swipe), raycast target is turned off and/or passed through to underlying UI elements.
/// Attach this to a full-screen transparent Image panel behind your UI elements.
/// </summary>
public class HorizontalSwipeTouchZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f; // Minimum swipe distance to trigger
    [SerializeField] private bool enableSwipeDetection = true;

    [Header("Click / Raycast Settings")]
    [Tooltip("Turn off raycast target when user performs a click (not a swipe)")]
    [SerializeField] private bool disableRaycastOnClick = true;
    [Tooltip("Forward click event to underlying UI elements when user performs a click (not a swipe)")]
    [SerializeField] private bool passClickToUnderlyingUI = true;
    
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
    private HorizontalLobbyManager lobbyManager;

    // Component references for raycast toggling
    private Graphic cachedGraphic;
    private CanvasGroup cachedCanvasGroup;

    private void Awake()
    {
        cachedGraphic = GetComponent<Graphic>();
        cachedCanvasGroup = GetComponent<CanvasGroup>();
    }
    
    private void Start()
    {
        // Find HorizontalLobbyManager in the scene
        lobbyManager = FindFirstObjectByType<HorizontalLobbyManager>();
        
        if (lobbyManager == null)
        {
            Debug.LogError("[HorizontalSwipeTouchZone] HorizontalLobbyManager not found! Swipe detection will not work.");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[HorizontalSwipeTouchZone] Successfully connected to HorizontalLobbyManager");
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
            Debug.Log($"[HorizontalSwipeTouchZone] Swipe started at: {swipeStartPos}");
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
            Debug.Log($"[HorizontalSwipeTouchZone] Swipe ended at: {swipeEndPos}, Delta: {swipeDelta}");
        
        bool isSwipe = false;

        // Check if swipe is long enough (primarily horizontal)
        if (Mathf.Abs(swipeDelta.x) > swipeThreshold)
        {
            // Ensure horizontal swipe is more dominant than vertical
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                isSwipe = true;

                if (lobbyManager != null)
                {
                    if (useVelocityScrolling)
                    {
                        // Calculate how many items to scroll based on swipe distance
                        float swipeDistance = Mathf.Abs(swipeDelta.x);
                        int itemsToScroll = Mathf.RoundToInt(swipeDistance / pixelsPerItem);
                        
                        // Clamp to max scroll items
                        itemsToScroll = Mathf.Clamp(itemsToScroll, 1, maxScrollItems);
                        
                        // Swipe left (negative x) = scroll right (show next items)
                        if (swipeDelta.x < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log($"[HorizontalSwipeTouchZone] Swipe LEFT detected ({swipeDistance:F0}px) -> Scroll RIGHT by {itemsToScroll} items");
                            lobbyManager.ScrollMultiple(itemsToScroll);
                        }
                        // Swipe right (positive x) = scroll left (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log($"[HorizontalSwipeTouchZone] Swipe RIGHT detected ({swipeDistance:F0}px) -> Scroll LEFT by {itemsToScroll} items");
                            lobbyManager.ScrollMultiple(-itemsToScroll);
                        }
                    }
                    else
                    {
                        // Single-step scrolling (original behavior)
                        // Swipe left = scroll right (show next items)
                        if (swipeDelta.x < 0)
                        {
                            if (showDebugLogs)
                                Debug.Log("[HorizontalSwipeTouchZone] Swipe LEFT detected -> Scroll RIGHT");
                            lobbyManager.ScrollRight();
                        }
                        // Swipe right = scroll left (show previous items)
                        else
                        {
                            if (showDebugLogs)
                                Debug.Log("[HorizontalSwipeTouchZone] Swipe RIGHT detected -> Scroll LEFT");
                            lobbyManager.ScrollLeft();
                        }
                    }
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("[HorizontalSwipeTouchZone] Vertical swipe detected, ignoring");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[HorizontalSwipeTouchZone] Swipe too short ({Mathf.Abs(swipeDelta.x)} < {swipeThreshold}), click detected.");
        }
        
        // If not a valid swipe (user only clicked), turn off raycast / pass click to underlying UI
        if (!isSwipe)
        {
            HandleClickPassThrough(eventData);
        }

        isSwiping = false;
    }

    /// <summary>
    /// Handles when the user clicks without swiping.
    /// Turns off raycast target and/or passes click through to underlying UI elements.
    /// </summary>
    private void HandleClickPassThrough(PointerEventData eventData)
    {
        if (cachedGraphic == null) cachedGraphic = GetComponent<Graphic>();
        if (cachedCanvasGroup == null) cachedCanvasGroup = GetComponent<CanvasGroup>();

        bool wasGraphicRaycastTarget = cachedGraphic != null && cachedGraphic.raycastTarget;
        bool wasCanvasGroupBlocksRaycasts = cachedCanvasGroup != null && cachedCanvasGroup.blocksRaycasts;

        // Disable raycast target temporarily so RaycastAll ignores this TouchZone panel
        if (cachedGraphic != null) cachedGraphic.raycastTarget = false;
        if (cachedCanvasGroup != null) cachedCanvasGroup.blocksRaycasts = false;

        if (passClickToUnderlyingUI && EventSystem.current != null)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject != null && result.gameObject != gameObject && !result.gameObject.transform.IsChildOf(transform))
                {
                    if (showDebugLogs)
                        Debug.Log($"[HorizontalSwipeTouchZone] Passing click to underlying UI: {result.gameObject.name}");

                    ExecuteEvents.ExecuteHierarchy(result.gameObject, eventData, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.ExecuteHierarchy(result.gameObject, eventData, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.ExecuteHierarchy(result.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                    break;
                }
            }
        }

        // Keep raycast off if disableRaycastOnClick is true; otherwise restore original state
        if (!disableRaycastOnClick)
        {
            if (cachedGraphic != null) cachedGraphic.raycastTarget = wasGraphicRaycastTarget;
            if (cachedCanvasGroup != null) cachedCanvasGroup.blocksRaycasts = wasCanvasGroupBlocksRaycasts;
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[HorizontalSwipeTouchZone] Raycast target turned OFF after click.");
        }
    }
    
    /// <summary>
    /// Enable or disable swipe detection
    /// </summary>
    public void SetSwipeDetectionEnabled(bool enabled)
    {
        enableSwipeDetection = enabled;
        
        if (showDebugLogs)
            Debug.Log($"[HorizontalSwipeTouchZone] Swipe detection {(enabled ? "enabled" : "disabled")}");
    }
    
    /// <summary>
    /// Update the swipe threshold
    /// </summary>
    public void SetSwipeThreshold(float threshold)
    {
        swipeThreshold = Mathf.Max(10f, threshold);
        
        if (showDebugLogs)
            Debug.Log($"[HorizontalSwipeTouchZone] Swipe threshold updated to: {swipeThreshold}");
    }

    /// <summary>
    /// Enable or disable raycast target on this touch zone
    /// </summary>
    public void SetRaycastTargetEnabled(bool enabled)
    {
        if (cachedGraphic == null) cachedGraphic = GetComponent<Graphic>();
        if (cachedCanvasGroup == null) cachedCanvasGroup = GetComponent<CanvasGroup>();

        if (cachedGraphic != null) cachedGraphic.raycastTarget = enabled;
        if (cachedCanvasGroup != null) cachedCanvasGroup.blocksRaycasts = enabled;

        if (showDebugLogs)
            Debug.Log($"[HorizontalSwipeTouchZone] Raycast target {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Turn off raycast target on this touch zone
    /// </summary>
    public void DisableRaycastTarget()
    {
        SetRaycastTargetEnabled(false);
    }

    /// <summary>
    /// Turn on raycast target on this touch zone
    /// </summary>
    public void EnableRaycastTarget()
    {
        SetRaycastTargetEnabled(true);
    }
}

