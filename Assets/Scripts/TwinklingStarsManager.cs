using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// A manager to create a background effect of randomly spawning and twinkling stars.
/// Borrowing the object pooling concept from FallingCoinsManager.
/// </summary>
public class TwinklingStarsManager : MonoBehaviour
{
    [Header("Star Assets")]
    [SerializeField] private Sprite[] starSprites;
    [SerializeField] private GameObject starPrefab; // A UI Image prefab

    [Header("Spawn Settings")]
    [SerializeField] private int maxStars = 50;
    [SerializeField] private float spawnInterval = 0.1f;
    [SerializeField] private RectTransform spawnArea;

    [Header("Animation Settings")]
    [SerializeField] private Vector2 twinkleDurationRange = new Vector2(1.5f, 4f); // Total time to fade in and out
    [SerializeField] private Vector2 scaleRange = new Vector2(0.3f, 1.2f);
    [SerializeField] private Vector2 maxOpacityRange = new Vector2(0.5f, 1f); // How bright the star can get
    
    private List<GameObject> starPool = new List<GameObject>();
    private float nextSpawnTime;

    private void Start()
    {
        if (spawnArea == null) spawnArea = GetComponent<RectTransform>();
        
        // Pre-warm the pool
        for (int i = 0; i < maxStars; i++)
        {
            GameObject star = Instantiate(starPrefab, transform);
            star.SetActive(false);
            starPool.Add(star);
        }
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnStar();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnStar()
    {
        GameObject star = GetInactiveStar();
        if (star == null) return;

        Image img = star.GetComponent<Image>();
        if (img != null) img.DOKill();
        star.transform.DOKill();

        star.SetActive(true);
        
        if (img != null && starSprites.Length > 0)
        {
            // Pick random star sprite
            img.sprite = starSprites[Random.Range(0, starSprites.Length)];

            // Start completely transparent
            Color startColor = img.color;
            startColor.a = 0f;
            img.color = startColor;

            // Random Scale
            float randomScale = Random.Range(scaleRange.x, scaleRange.y);
            star.transform.localScale = Vector3.one * randomScale;

            // Random Position anywhere within the spawn area
            float randomX = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
            float randomY = Random.Range(-spawnArea.rect.height / 2f, spawnArea.rect.height / 2f);
            star.transform.localPosition = new Vector3(randomX, randomY, 0);

            // Animate: Twinkle (Fade In then Fade Out)
            float totalDuration = Random.Range(twinkleDurationRange.x, twinkleDurationRange.y);
            float halfDuration = totalDuration / 2f; 
            float targetAlpha = Random.Range(maxOpacityRange.x, maxOpacityRange.y);

            // Create a DOTween sequence to fade up, then down, then deactivate
            Sequence twinkleSeq = DOTween.Sequence();
            twinkleSeq.Append(img.DOFade(targetAlpha, halfDuration).SetEase(Ease.InOutSine))
                      .Append(img.DOFade(0f, halfDuration).SetEase(Ease.InOutSine))
                      .SetLink(star)
                      .OnComplete(() => { if (star != null) star.SetActive(false); });
        }
    }

    private GameObject GetInactiveStar()
    {
        foreach (var star in starPool)
        {
            if (!star.activeInHierarchy) return star;
        }
        return null; // All pool objects are currently twinkling!
    }

    private void OnDestroy()
    {
        // Cancel all tweens on the stars when this manager is destroyed to prevent safe-kill errors
        foreach (var star in starPool)
        {
            if (star != null)
            {
                Image img = star.GetComponent<Image>();
                if (img != null)
                {
                    img.DOKill();
                }
            }
        }
    }
}
