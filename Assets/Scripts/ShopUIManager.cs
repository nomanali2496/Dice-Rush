using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;
using DOTweenTween = DG.Tweening.Tween;

/// <summary>
/// Manages hover scale animations for shop UI elements using DOTween
/// </summary>
public class ShopUIManager : MonoBehaviour
{
    [Header("Shop Elements")]
    [SerializeField] private RectTransform[] shopElements; // Array of shop UI elements to apply hover effect
    
    [Header("Hover Animation Settings")]
    [SerializeField] private float hoverScale = 1.15f; // Scale factor when hovering (default 1.15 = 15% larger)
    [SerializeField] private float animationDuration = 0.2f; // Duration of scale animation in seconds
    [SerializeField] private Ease scaleEase = Ease.OutBack; // Easing function for scale animation
    
    private Dictionary<RectTransform, DOTweenTween> elementTweens = new Dictionary<RectTransform, DOTweenTween>();
    private Dictionary<RectTransform, Vector3> elementOriginalScales = new Dictionary<RectTransform, Vector3>();
    
    private void Awake()
    {
        // Setup hover animations for all shop elements
        SetupHoverAnimations();
    }
    
    private void OnDestroy()
    {
        // Cleanup all tweens to prevent memory leaks
        CleanupAllAnimations();
    }
    
    /// <summary>
    /// Sets up hover animations for all shop elements using EventTrigger
    /// </summary>
    private void SetupHoverAnimations()
    {
        if (shopElements == null || shopElements.Length == 0)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<RectTransform> rects = new List<RectTransform>();
            foreach (var btn in buttons)
            {
                if (btn != null) rects.Add(btn.GetComponent<RectTransform>());
            }
            shopElements = rects.ToArray();
        }

        if (shopElements == null || shopElements.Length == 0)
        {
            Debug.LogWarning("[ShopUIManager] No shop elements assigned!");
            return;
        }
        
        foreach (RectTransform element in shopElements)
        {
            if (element != null)
            {
                SetupElementHoverAnimation(element);
            }
        }
    }
    
    /// <summary>
    /// Sets up hover animation for a single shop element using EventTrigger
    /// </summary>
    private void SetupElementHoverAnimation(RectTransform element)
    {
        if (element == null) return;
        
        // Get or add EventTrigger component
        EventTrigger eventTrigger = element.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = element.gameObject.AddComponent<EventTrigger>();
        }
        
        // Store original scale
        if (!elementOriginalScales.ContainsKey(element))
        {
            elementOriginalScales[element] = element.localScale;
        }
        
        // Remove existing entries if any
        eventTrigger.triggers.Clear();
        
        // Add PointerEnter event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnElementPointerEnter(element); });
        eventTrigger.triggers.Add(pointerEnter);
        
        // Add PointerExit event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnElementPointerExit(element); });
        eventTrigger.triggers.Add(pointerExit);
    }
    
    /// <summary>
    /// Called when pointer enters a shop element (hover start)
    /// </summary>
    private void OnElementPointerEnter(RectTransform element)
    {
        if (element == null) return;
        
        // Get original scale
        Vector3 originalScale = Vector3.one;
        if (elementOriginalScales.ContainsKey(element))
        {
            originalScale = elementOriginalScales[element];
        }
        else
        {
            originalScale = element.localScale;
            elementOriginalScales[element] = originalScale;
        }
        
        // Kill any existing tween for this element
        if (elementTweens.ContainsKey(element) && elementTweens[element] != null && elementTweens[element].IsActive())
        {
            elementTweens[element].Kill();
        }
        
        // Scale up using DOTween
        Vector3 targetScale = originalScale * hoverScale;
        elementTweens[element] = element.DOScale(targetScale, animationDuration)
            .SetEase(scaleEase)
            .SetLink(element.gameObject);
    }
    
    /// <summary>
    /// Called when pointer exits a shop element (hover end)
    /// </summary>
    private void OnElementPointerExit(RectTransform element)
    {
        if (element == null) return;
        
        // Get original scale
        Vector3 originalScale = Vector3.one;
        if (elementOriginalScales.ContainsKey(element))
        {
            originalScale = elementOriginalScales[element];
        }
        else
        {
            originalScale = element.localScale;
            elementOriginalScales[element] = originalScale;
        }
        
        // Kill any existing tween for this element
        if (elementTweens.ContainsKey(element) && elementTweens[element] != null && elementTweens[element].IsActive())
        {
            elementTweens[element].Kill();
        }
        
        // Scale back to original using DOTween
        elementTweens[element] = element.DOScale(originalScale, animationDuration)
            .SetEase(scaleEase)
            .SetLink(element.gameObject);
    }
    
    /// <summary>
    /// Cleans up all animations and tweens
    /// </summary>
    private void CleanupAllAnimations()
    {
        // Kill all active tweens
        foreach (var kvp in elementTweens)
        {
            if (kvp.Value != null && kvp.Value.IsActive())
            {
                kvp.Value.Kill();
            }
        }
        
        // Clear dictionaries
        elementTweens.Clear();
        elementOriginalScales.Clear();
    }

    private void OnDisable()
    {
        CleanupAllAnimations();
    }
    
    /// <summary>
    /// Adds a shop element at runtime (useful for dynamically created shop items)
    /// </summary>
    public void AddShopElement(RectTransform element)
    {
        if (element == null) return;
        
        // Resize array to include new element
        System.Array.Resize(ref shopElements, shopElements.Length + 1);
        shopElements[shopElements.Length - 1] = element;
        
        // Setup hover animation for the new element
        SetupElementHoverAnimation(element);
    }
    
    /// <summary>
    /// Removes a shop element and cleans up its animations
    /// </summary>
    public void RemoveShopElement(RectTransform element)
    {
        if (element == null) return;
        
        // Kill tween if exists
        if (elementTweens.ContainsKey(element))
        {
            if (elementTweens[element] != null && elementTweens[element].IsActive())
            {
                elementTweens[element].Kill();
            }
            elementTweens.Remove(element);
        }
        
        // Remove from original scales
        elementOriginalScales.Remove(element);
        
        // Remove from array
        List<RectTransform> elementList = new List<RectTransform>(shopElements);
        elementList.Remove(element);
        shopElements = elementList.ToArray();
    }
}






