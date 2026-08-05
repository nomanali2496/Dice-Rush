using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// A general script to create an infinite "coin rain" effect in the UI.
/// Coins of different sizes and types will drop from top to bottom.
/// </summary>
public class FallingCoinsManager : MonoBehaviour
{
    [Header("Coin Assets")]
    [SerializeField] private Sprite[] coinSprites; // 3 to 4 different coin sprites
    [SerializeField] private GameObject coinPrefab; // A simple UI Image prefab

    [Header("Spawn Settings")]
    [SerializeField] private int maxCoins = 30;
    [SerializeField] private float spawnInterval = 0.2f;
    [SerializeField] private RectTransform spawnArea; // The Canvas/Panel area

    [Header("Animation Settings")]
    [SerializeField] private Vector2 fallDurationRange = new Vector2(2f, 5f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.3f, 1.2f);
    
    private List<GameObject> coinPool = new List<GameObject>();
    private float nextSpawnTime;

    private void Start()
    {
        if (spawnArea == null) spawnArea = GetComponent<RectTransform>();
        
        // Pre-warm the pool
        for (int i = 0; i < maxCoins; i++)
        {
            GameObject coin = Instantiate(coinPrefab, transform);
            coin.SetActive(false);
            coinPool.Add(coin);
        }
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnCoin();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnCoin()
    {
        GameObject coin = GetInactiveCoin();
        if (coin == null) return;

        // Reset state
        coin.transform.DOKill();
        coin.SetActive(true);
        Image img = coin.GetComponent<Image>();
        if (img != null && coinSprites.Length > 0)
        {
            img.sprite = coinSprites[Random.Range(0, coinSprites.Length)];
        }

        // Random Position at the TOP
        float randomX = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
        float startY = spawnArea.rect.height / 2f + 100f; // Slightly above screen
        float endY = -spawnArea.rect.height / 2f - 100f; // Slightly below screen

        coin.transform.localPosition = new Vector3(randomX, startY, 0);
        
        // Random Scale
        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        coin.transform.localScale = Vector3.one * randomScale;

        // Animation: Fall to bottom
        float duration = Random.Range(fallDurationRange.x, fallDurationRange.y);
        
        coin.transform.DOLocalMoveY(endY, duration)
            .SetEase(Ease.Linear)
            .SetLink(coin)
            .OnComplete(() => {
                if (coin != null) coin.SetActive(false);
            });
    }

    private GameObject GetInactiveCoin()
    {
        foreach (var coin in coinPool)
        {
            if (!coin.activeInHierarchy) return coin;
        }
        return null;
    }

    private void OnDestroy()
    {
        // Kill all tweens on this transform and pooled coins to avoid errors
        transform.DOKill();
        foreach (var coin in coinPool)
        {
            if (coin != null) coin.transform.DOKill();
        }
    }
}
