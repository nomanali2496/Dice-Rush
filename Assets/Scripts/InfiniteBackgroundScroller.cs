using UnityEngine;

/// <summary>
/// General-purpose infinite background scrolling system
/// Supports both horizontal and vertical scrolling with seamless looping
/// Works with both 2D sprites (SpriteRenderer) and UI elements (RectTransform)
/// 
/// HOW IT WORKS:
/// 1. Multiple background panels are positioned in sequence (vertically or horizontally)
/// 2. All panels scroll continuously in the specified direction
/// 3. When a panel moves completely off-screen, it's repositioned to the opposite side
/// 4. This creates a seamless infinite scrolling effect
/// 
/// USAGE:
/// 1. Attach this script to an empty GameObject in your scene
/// 2. Assign your background panels to the 'backgroundPanels' array in the inspector
/// 3. Configure scroll direction, speed, and panel size
/// 4. The script will automatically handle the infinite looping
/// </summary>
public class InfiniteBackgroundScroller : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Background Panels")]
    [Tooltip("Array of background panels that will scroll infinitely")]
    [SerializeField] private Transform[] backgroundPanels;
    
    [Header("Scroll Settings")]
    [Tooltip("Direction of scrolling")]
    [SerializeField] private ScrollDirection scrollDirection = ScrollDirection.Vertical;
    
    [Tooltip("Speed of scrolling (units per second)")]
    [SerializeField] private float scrollSpeed = 2f;
    
    [Tooltip("Should scrolling start automatically on Start()?")]
    [SerializeField] private bool autoStart = true;
    
    [Header("Panel Dimensions")]
    [Tooltip("Size of each panel (width for horizontal, height for vertical). Leave at 0 to auto-detect from sprite/RectTransform")]
    [SerializeField] private float panelSize = 0f;
    
    [Tooltip("If true, automatically calculates panel size from the first panel's renderer/RectTransform")]
    [SerializeField] private bool autoDetectPanelSize = true;
    
    [Header("Auto Setup")]
    [Tooltip("Automatically find and setup background panels by tag")]
    [SerializeField] private bool autoSetupByTag = false;
    
    [Tooltip("Tag to search for background panels (only used if autoSetupByTag is true)")]
    [SerializeField] private string backgroundTag = "Background";
    
    [Header("Camera Settings")]
    [Tooltip("Camera to calculate viewport bounds. If null, will use Camera.main")]
    [SerializeField] private Camera targetCamera;
    
    [Tooltip("Extra offset beyond viewport to trigger panel repositioning (prevents visible gaps)")]
    [SerializeField] private float resetOffset = 0.5f;
    
    #endregion
    
    #region Private Fields
    
    private bool isScrolling = false;
    private float resetThreshold;
    private float actualPanelSize;
    
    #endregion
    
    #region Public Enums
    
    public enum ScrollDirection
    {
        Vertical,   // Scrolls top to bottom
        Horizontal  // Scrolls left to right
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        InitializeBackgrounds();
        
        if (autoStart)
        {
            StartScrolling();
        }
    }
    
    private void Update()
    {
        if (isScrolling && backgroundPanels != null && backgroundPanels.Length > 0)
        {
            ScrollBackgrounds();
        }
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initializes the background panels for infinite scrolling
    /// </summary>
    private void InitializeBackgrounds()
    {
        // Auto-setup by tag if enabled
        if (autoSetupByTag && (backgroundPanels == null || backgroundPanels.Length == 0))
        {
            GameObject[] backgroundObjects = GameObject.FindGameObjectsWithTag(backgroundTag);
            if (backgroundObjects.Length > 0)
            {
                backgroundPanels = new Transform[backgroundObjects.Length];
                for (int i = 0; i < backgroundObjects.Length; i++)
                {
                    backgroundPanels[i] = backgroundObjects[i].transform;
                }
                Debug.Log($"[InfiniteBackgroundScroller] Auto-found {backgroundPanels.Length} background panels with tag '{backgroundTag}'");
            }
        }
        
        // Auto-setup from child transforms if backgroundPanels array is still empty
        if (backgroundPanels == null || backgroundPanels.Length == 0)
        {
            if (transform.childCount > 0)
            {
                System.Collections.Generic.List<Transform> children = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in transform)
                {
                    children.Add(child);
                }
                backgroundPanels = children.ToArray();
                Debug.Log($"[InfiniteBackgroundScroller] Auto-assigned {backgroundPanels.Length} child transforms as background panels.");
            }
        }

        // Validate backgrounds
        if (backgroundPanels == null || backgroundPanels.Length == 0)
        {
            Debug.Log("[InfiniteBackgroundScroller] No background panels assigned or found under transform. Disabling scroller.");
            isScrolling = false;
            return;
        }
        
        // Remove null references
        System.Collections.Generic.List<Transform> validPanels = new System.Collections.Generic.List<Transform>();
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null)
            {
                validPanels.Add(panel);
            }
        }
        backgroundPanels = validPanels.ToArray();
        
        if (backgroundPanels.Length == 0)
        {
            Debug.LogError("[InfiniteBackgroundScroller] No valid background panels found!");
            return;
        }
        
        // Auto-detect panel size if enabled
        if (autoDetectPanelSize && backgroundPanels.Length > 0 && backgroundPanels[0] != null)
        {
            actualPanelSize = GetPanelSize(backgroundPanels[0]);
            Debug.Log($"[InfiniteBackgroundScroller] Auto-detected panel size: {actualPanelSize}");
        }
        else
        {
            actualPanelSize = panelSize;
        }
        
        // Fallback if size is still 0
        if (actualPanelSize <= 0)
        {
            actualPanelSize = scrollDirection == ScrollDirection.Vertical ? 10f : 10f;
            Debug.LogWarning($"[InfiniteBackgroundScroller] Panel size not set or detected! Using fallback: {actualPanelSize}");
        }
        
        // Get camera for viewport calculations
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Calculate reset threshold based on scroll direction
        CalculateResetThreshold();
        
        // Position backgrounds for seamless looping
        PositionBackgroundsForLooping();
        
        Debug.Log($"[InfiniteBackgroundScroller] Initialized {backgroundPanels.Length} background panel(s) for infinite {scrollDirection} scrolling");
    }
    
    /// <summary>
    /// Calculates the threshold at which panels should be repositioned
    /// </summary>
    private void CalculateResetThreshold()
    {
        if (targetCamera != null)
        {
            if (scrollDirection == ScrollDirection.Vertical)
            {
                // Calculate bottom of camera viewport in world space
                float cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
                resetThreshold = cameraBottom - resetOffset;
            }
            else // Horizontal
            {
                // Calculate left edge of camera viewport
                float cameraLeft = targetCamera.transform.position.x - (targetCamera.orthographicSize * targetCamera.aspect);
                resetThreshold = cameraLeft - resetOffset;
            }
        }
        else
        {
            // Fallback: use a fixed threshold
            resetThreshold = scrollDirection == ScrollDirection.Vertical 
                ? -actualPanelSize - resetOffset 
                : -actualPanelSize - resetOffset;
        }
    }
    
    /// <summary>
    /// Positions background panels in sequence for seamless looping
    /// </summary>
    private void PositionBackgroundsForLooping()
    {
        if (backgroundPanels == null || backgroundPanels.Length == 0) return;
        
        if (scrollDirection == ScrollDirection.Vertical)
        {
            // Sort backgrounds by current Y position (top to bottom)
            System.Array.Sort(backgroundPanels, (a, b) => b.position.y.CompareTo(a.position.y));
            
            // Position them vertically one after another
            float currentTopEdge = 0f;
            for (int i = 0; i < backgroundPanels.Length; i++)
            {
                if (backgroundPanels[i] == null) continue;
                
                float size = GetPanelSize(backgroundPanels[i]);
                float centerPos = currentTopEdge - (size * 0.5f);
                
                Vector3 pos = backgroundPanels[i].position;
                pos.y = centerPos;
                backgroundPanels[i].position = pos;
                
                currentTopEdge -= size;
            }
        }
        else // Horizontal
        {
            // Sort backgrounds by current X position (left to right)
            System.Array.Sort(backgroundPanels, (a, b) => a.position.x.CompareTo(b.position.x));
            
            // Position them horizontally one after another
            float currentLeftEdge = 0f;
            for (int i = 0; i < backgroundPanels.Length; i++)
            {
                if (backgroundPanels[i] == null) continue;
                
                float size = GetPanelSize(backgroundPanels[i]);
                float centerPos = currentLeftEdge + (size * 0.5f);
                
                Vector3 pos = backgroundPanels[i].position;
                pos.x = centerPos;
                backgroundPanels[i].position = pos;
                
                currentLeftEdge += size;
            }
        }
    }
    
    #endregion
    
    #region Scrolling Logic
    
    /// <summary>
    /// Scrolls all background panels and repositions them when they go off-screen
    /// </summary>
    private void ScrollBackgrounds()
    {
        // Update reset threshold in case camera moved
        CalculateResetThreshold();
        
        for (int i = 0; i < backgroundPanels.Length; i++)
        {
            if (backgroundPanels[i] == null) continue;
            
            // Move background in scroll direction
            Vector3 position = backgroundPanels[i].position;
            
            if (scrollDirection == ScrollDirection.Vertical)
            {
                // Move downward
                position.y -= scrollSpeed * Time.deltaTime;
                backgroundPanels[i].position = position;
                
                // Check if panel is fully off-screen (top edge is well below camera bottom)
                float topEdge = GetPanelTopEdge(backgroundPanels[i]);
                float cameraBottom = targetCamera != null 
                    ? targetCamera.transform.position.y - targetCamera.orthographicSize 
                    : resetThreshold;
                
                // Only reposition when top edge is completely below visible area
                if (topEdge <= cameraBottom - resetOffset)
                {
                    RepositionPanelVertical(i);
                }
            }
            else // Horizontal
            {
                // Move leftward (scrolling right to left)
                position.x -= scrollSpeed * Time.deltaTime;
                backgroundPanels[i].position = position;
                
                // Check if panel is fully off-screen (right edge left of camera left)
                float rightEdge = GetPanelRightEdge(backgroundPanels[i]);
                float cameraLeft = targetCamera != null 
                    ? targetCamera.transform.position.x - (targetCamera.orthographicSize * targetCamera.aspect) 
                    : resetThreshold;
                
                // Only reposition when right edge is completely beyond visible area
                if (rightEdge <= cameraLeft - resetOffset)
                {
                    RepositionPanelHorizontal(i);
                }
            }
        }
    }
    
    /// <summary>
    /// Repositions a panel vertically to create seamless loop
    /// </summary>
    private void RepositionPanelVertical(int panelIndex)
    {
        // Find the highest panel's top edge (only consider panels that are positioned correctly)
        // We track the highest top edge to place the new panel seamlessly above it
        float highestTopEdge = float.MinValue;
        Transform highestPanel = null;
        
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null && panel != backgroundPanels[panelIndex])
            {
                float panelTopEdge = GetPanelTopEdge(panel);
                if (panelTopEdge > highestTopEdge)
                {
                    highestTopEdge = panelTopEdge;
                    highestPanel = panel;
                }
            }
        }
        
        // Get this panel's size
        float size = GetPanelSize(backgroundPanels[panelIndex]);
        
        // Position directly above the highest panel to create seamless connection
        // Place the bottom edge of this panel to touch the top edge of the highest panel
        float newCenterY = highestTopEdge + (size * 0.5f);
        
        Vector3 position = backgroundPanels[panelIndex].position;
        position.y = newCenterY;
        backgroundPanels[panelIndex].position = position;
    }
    
    /// <summary>
    /// Repositions a panel horizontally to create seamless loop
    /// </summary>
    private void RepositionPanelHorizontal(int panelIndex)
    {
        // Find the rightmost panel's right edge (only consider panels that are positioned correctly)
        float rightmostEdge = float.MinValue;
        Transform rightmostPanel = null;
        
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null && panel != backgroundPanels[panelIndex])
            {
                float panelRightEdge = GetPanelRightEdge(panel);
                if (panelRightEdge > rightmostEdge)
                {
                    rightmostEdge = panelRightEdge;
                    rightmostPanel = panel;
                }
            }
        }
        
        // Get this panel's size
        float size = GetPanelSize(backgroundPanels[panelIndex]);
        
        // Position directly to the right of the rightmost panel to create seamless connection
        // Place the left edge of this panel to touch the right edge of the rightmost panel
        float newCenterX = rightmostEdge + (size * 0.5f);
        
        Vector3 position = backgroundPanels[panelIndex].position;
        position.x = newCenterX;
        backgroundPanels[panelIndex].position = position;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Gets the actual size of a panel (height for vertical, width for horizontal)
    /// Works with both SpriteRenderer and RectTransform
    /// </summary>
    private float GetPanelSize(Transform panel)
    {
        // Try SpriteRenderer first (for 2D sprites)
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.magnitude > 0)
        {
            return scrollDirection == ScrollDirection.Vertical 
                ? spriteRenderer.bounds.size.y 
                : spriteRenderer.bounds.size.x;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            float size = scrollDirection == ScrollDirection.Vertical 
                ? rectTransform.rect.height * rectTransform.lossyScale.y
                : rectTransform.rect.width * rectTransform.lossyScale.x;
            
            if (size > 0) return size;
        }
        
        // Fallback to configured value
        return actualPanelSize;
    }
    
    /// <summary>
    /// Gets the top edge Y position of a panel
    /// </summary>
    private float GetPanelTopEdge(Transform panel)
    {
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.magnitude > 0)
        {
            return spriteRenderer.bounds.max.y;
        }
        
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.y + (rectTransform.rect.height * 0.5f * rectTransform.lossyScale.y);
        }
        
        return panel.position.y + (actualPanelSize * 0.5f);
    }
    
    /// <summary>
    /// Gets the bottom edge Y position of a panel
    /// </summary>
    private float GetPanelBottomEdge(Transform panel)
    {
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.magnitude > 0)
        {
            return spriteRenderer.bounds.min.y;
        }
        
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.y - (rectTransform.rect.height * 0.5f * rectTransform.lossyScale.y);
        }
        
        return panel.position.y - (actualPanelSize * 0.5f);
    }
    
    /// <summary>
    /// Gets the right edge X position of a panel
    /// </summary>
    private float GetPanelRightEdge(Transform panel)
    {
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.magnitude > 0)
        {
            return spriteRenderer.bounds.max.x;
        }
        
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.x + (rectTransform.rect.width * 0.5f * rectTransform.lossyScale.x);
        }
        
        return panel.position.x + (actualPanelSize * 0.5f);
    }
    
    /// <summary>
    /// Gets the left edge X position of a panel
    /// </summary>
    private float GetPanelLeftEdge(Transform panel)
    {
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.magnitude > 0)
        {
            return spriteRenderer.bounds.min.x;
        }
        
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.x - (rectTransform.rect.width * 0.5f * rectTransform.lossyScale.x);
        }
        
        return panel.position.x - (actualPanelSize * 0.5f);
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Starts the background scrolling animation
    /// </summary>
    public void StartScrolling()
    {
        isScrolling = true;
    }
    
    /// <summary>
    /// Stops the background scrolling animation
    /// </summary>
    public void StopScrolling()
    {
        isScrolling = false;
    }
    
    /// <summary>
    /// Toggles scrolling on/off
    /// </summary>
    public void ToggleScrolling()
    {
        isScrolling = !isScrolling;
    }
    
    /// <summary>
    /// Sets the scroll speed
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = Mathf.Max(0f, speed);
    }
    
    /// <summary>
    /// Gets the current scroll speed
    /// </summary>
    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }
    
    /// <summary>
    /// Checks if currently scrolling
    /// </summary>
    public bool IsScrolling()
    {
        return isScrolling;
    }
    
    /// <summary>
    /// Resets all background positions to initial state
    /// </summary>
    public void ResetBackgrounds()
    {
        PositionBackgroundsForLooping();
    }
    
    /// <summary>
    /// Changes the scroll direction at runtime
    /// </summary>
    public void SetScrollDirection(ScrollDirection newDirection)
    {
        if (scrollDirection != newDirection)
        {
            scrollDirection = newDirection;
            InitializeBackgrounds(); // Re-initialize with new direction
        }
    }
    
    #endregion
    
    #region Editor Helpers
    
    #if UNITY_EDITOR
    /// <summary>
    /// Draws gizmos to visualize background panel boundaries in the editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (backgroundPanels == null) return;
        
        Gizmos.color = Color.cyan;
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null)
            {
                Vector3 center = panel.position;
                Vector3 size;
                
                if (scrollDirection == ScrollDirection.Vertical)
                {
                    size = new Vector3(20f, actualPanelSize > 0 ? actualPanelSize : panelSize, 0.1f);
                }
                else
                {
                    size = new Vector3(actualPanelSize > 0 ? actualPanelSize : panelSize, 20f, 0.1f);
                }
                
                Gizmos.DrawWireCube(center, size);
            }
        }
        
        // Draw reset threshold line
        if (targetCamera != null)
        {
            Gizmos.color = Color.red;
            if (scrollDirection == ScrollDirection.Vertical)
            {
                float cameraWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;
                Vector3 start = new Vector3(targetCamera.transform.position.x - cameraWidth/2f, resetThreshold, 0f);
                Vector3 end = new Vector3(targetCamera.transform.position.x + cameraWidth/2f, resetThreshold, 0f);
                Gizmos.DrawLine(start, end);
            }
            else
            {
                float cameraHeight = targetCamera.orthographicSize * 2f;
                Vector3 start = new Vector3(resetThreshold, targetCamera.transform.position.y - cameraHeight/2f, 0f);
                Vector3 end = new Vector3(resetThreshold, targetCamera.transform.position.y + cameraHeight/2f, 0f);
                Gizmos.DrawLine(start, end);
            }
        }
    }
    #endif
    
    #endregion
}
