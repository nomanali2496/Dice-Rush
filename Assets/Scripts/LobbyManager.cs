using UnityEngine;

/// <summary>
/// Manages the lobby scene with infinite vertical background scrolling animation
/// Handles continuous background movement to create a seamless scrolling effect
/// </summary>
public class LobbyManager : MonoBehaviour
{
    [Header("Background Settings")]
    [SerializeField] private Transform[] backgroundPanels; // Array of background panels for seamless looping
    [SerializeField] private float scrollSpeed = 2f; // Speed of background scrolling (units per second)
    
    [Header("Background Dimensions")]
    [SerializeField] private float backgroundHeight = 10f; // Height of each background panel (for seamless looping)
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupBackgrounds = true; // Automatically find and setup backgrounds
    [SerializeField] private string backgroundTag = "Background"; // Tag to search for backgrounds
    
    [Header("Camera Settings (Optional)")]
    [SerializeField] private Camera targetCamera; // Camera to calculate viewport bounds (auto-finds Main Camera if null)
    [SerializeField] private float resetOffset = 2f; // Extra offset below viewport to trigger reset early
    
    private bool isScrolling = true;
    private float resetThresholdY;
    
    private void Start()
    {
        InitializeBackgrounds();
    }
    
    private void Update()
    {
        if (isScrolling && backgroundPanels != null && backgroundPanels.Length > 0)
        {
            ScrollBackgrounds();
        }
    }
    
    /// <summary>
    /// Gets the actual height of a panel using its renderer bounds
    /// </summary>
    private float GetPanelHeight(Transform panel)
    {
        // Try SpriteRenderer first
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.y > 0)
        {
            return spriteRenderer.bounds.size.y;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.rect.height * rectTransform.lossyScale.y;
        }
        
        // Fallback to configured value
        return backgroundHeight;
    }
    
    /// <summary>
    /// Gets the actual bottom edge of a panel using its renderer bounds
    /// </summary>
    private float GetPanelBottomEdge(Transform panel)
    {
        // Try SpriteRenderer first
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.y > 0)
        {
            return spriteRenderer.bounds.min.y;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.y - (rectTransform.rect.height * 0.5f * rectTransform.lossyScale.y);
        }
        
        // Fallback to calculated value
        return panel.position.y - (backgroundHeight * 0.5f);
    }
    
    /// <summary>
    /// Gets the actual top edge of a panel using its renderer bounds
    /// </summary>
    private float GetPanelTopEdge(Transform panel)
    {
        // Try SpriteRenderer first
        SpriteRenderer spriteRenderer = panel.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.bounds.size.y > 0)
        {
            return spriteRenderer.bounds.max.y;
        }
        
        // Try RectTransform for UI elements
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.position.y + (rectTransform.rect.height * 0.5f * rectTransform.lossyScale.y);
        }
        
        // Fallback to calculated value
        return panel.position.y + (backgroundHeight * 0.5f);
    }
    
    /// <summary>
    /// Initializes the background panels for infinite scrolling
    /// </summary>
    private void InitializeBackgrounds()
    {
        // Auto-setup if enabled
        if (autoSetupBackgrounds && (backgroundPanels == null || backgroundPanels.Length == 0))
        {
            GameObject[] backgroundObjects = GameObject.FindGameObjectsWithTag(backgroundTag);
            if (backgroundObjects.Length > 0)
            {
                backgroundPanels = new Transform[backgroundObjects.Length];
                for (int i = 0; i < backgroundObjects.Length; i++)
                {
                    backgroundPanels[i] = backgroundObjects[i].transform;
                }
            }
        }
        
        // Validate backgrounds
        if (backgroundPanels == null || backgroundPanels.Length == 0)
        {
            Debug.LogWarning("[LobbyManager] No background panels assigned! Please assign backgrounds in the inspector.");
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
            Debug.LogError("[LobbyManager] No valid background panels found!");
            return;
        }
        
        // Get camera for viewport calculations
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        // Calculate reset threshold - reposition when bottom edge goes below this point
        // This ensures panels are repositioned before visible gaps appear
        if (targetCamera != null)
        {
            // Calculate bottom of camera viewport in world space
            float cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
            resetThresholdY = cameraBottom - resetOffset;
        }
        else
        {
            // Fallback: use a fixed threshold below zero
            // Use actual panel height if available
            float avgHeight = backgroundHeight;
            if (backgroundPanels.Length > 0 && backgroundPanels[0] != null)
            {
                avgHeight = GetPanelHeight(backgroundPanels[0]);
            }
            resetThresholdY = -avgHeight - resetOffset;
        }
        
        // Position backgrounds vertically for seamless looping
        PositionBackgroundsForLooping();
        
        Debug.Log($"[LobbyManager] Initialized {backgroundPanels.Length} background panel(s) for infinite scrolling");
    }
    
    /// <summary>
    /// Positions background panels vertically for seamless infinite looping
    /// </summary>
    private void PositionBackgroundsForLooping()
    {
        if (backgroundPanels == null || backgroundPanels.Length == 0) return;
        
        // Sort backgrounds by current Y position (top to bottom)
        System.Array.Sort(backgroundPanels, (a, b) => b.position.y.CompareTo(a.position.y));
        
        // Position them one after another using actual panel heights
        float currentTopEdge = 0f;
        for (int i = 0; i < backgroundPanels.Length; i++)
        {
            if (backgroundPanels[i] == null) continue;
            
            float panelHeight = GetPanelHeight(backgroundPanels[i]);
            float panelCenterY = currentTopEdge - (panelHeight * 0.5f);
            
            Vector3 pos = backgroundPanels[i].position;
            pos.y = panelCenterY;
            backgroundPanels[i].position = pos;
            
            // Next panel starts where this one ends
            currentTopEdge -= panelHeight;
        }
    }
    
    /// <summary>
    /// Scrolls all background panels downward and resets them when they go off-screen
    /// </summary>
    private void ScrollBackgrounds()
    {
        // Calculate camera bottom for off-screen detection
        float cameraBottom = resetThresholdY;
        if (targetCamera != null)
        {
            cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
        }
        
        for (int i = 0; i < backgroundPanels.Length; i++)
        {
            if (backgroundPanels[i] == null) continue;
            
            // Move background down
            Vector3 position = backgroundPanels[i].position;
            position.y -= scrollSpeed * Time.deltaTime;
            backgroundPanels[i].position = position;
            
            // Get actual top and bottom edges of this panel (from renderer bounds)
            float topEdge = GetPanelTopEdge(backgroundPanels[i]);
            float bottomEdge = GetPanelBottomEdge(backgroundPanels[i]);
            
            // Only reposition when the panel is FULLY off-screen (top edge is below camera bottom)
            // This ensures at least one panel is always visible
            if (topEdge <= cameraBottom)
            {
                // Find the highest background panel's top edge (using actual bounds)
                float highestTopEdge = float.MinValue;
                foreach (Transform panel in backgroundPanels)
                {
                    if (panel != null && panel != backgroundPanels[i])
                    {
                        float panelTopEdge = GetPanelTopEdge(panel);
                        if (panelTopEdge > highestTopEdge)
                        {
                            highestTopEdge = panelTopEdge;
                        }
                    }
                }
                
                // Get this panel's actual height
                float panelHeight = GetPanelHeight(backgroundPanels[i]);
                
                // Place this panel directly above the highest one (seamless connection)
                // Position so its bottom edge touches the highest panel's top edge
                float newCenterY = highestTopEdge + (panelHeight * 0.5f);
                
                position.y = newCenterY;
                backgroundPanels[i].position = position;
            }
        }
    }
    
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
    /// Resets all background positions to initial state
    /// </summary>
    public void ResetBackgrounds()
    {
        PositionBackgroundsForLooping();
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor helper: Draws gizmos to visualize background boundaries
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (backgroundPanels == null) return;
        
        Gizmos.color = Color.yellow;
        foreach (Transform panel in backgroundPanels)
        {
            if (panel != null)
            {
                Vector3 center = panel.position;
                Vector3 size = new Vector3(20f, backgroundHeight, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
    #endif
}

