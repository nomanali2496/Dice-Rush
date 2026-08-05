using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable fly animation: spawns coin/cash sprites that travel from a source
/// RectTransform to the target balance displays, incrementing the UI counter
/// on each particle arrival (same feel as DailyRewardManager).
///
/// Place this component anywhere in the scene (e.g. on ShopManager) and
/// assign the inspector references. Call PlayCoins / PlayCash from code.
/// </summary>
public class CurrencyFlyAnimator : MonoBehaviour
{
    [Header("Default Target Positions (main menu top-right)")]
    [SerializeField] public RectTransform targetCoinsPosition;
    [SerializeField] public RectTransform targetCashPosition;

    [Header("Shop Panel Target Positions")]
    [SerializeField] public RectTransform shopTargetCoinsPosition;
    [SerializeField] public RectTransform shopTargetCashPosition;

    [Header("Sprites")]
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite cashSprite;

    [Header("Prefab")]
    [SerializeField] private GameObject animatedSpritePrefab;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration     = 0.6f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private int   baseParticles         = 3;
    [SerializeField] private float particlesPerCoin      = 0.05f;
    [SerializeField] private float particlesPerCash      = 0.5f;
    [SerializeField] private int   maxParticles          = 12;
    [SerializeField] private float delayBetweenParticles = 0.06f;

    private float currentAnimatedCash;
    private int   currentAnimatedCoins;
    private readonly List<GameObject> activeParticles = new List<GameObject>();

    /// <summary>Animates coins flying from <paramref name="source"/> to the coins display.
    /// Pass <paramref name="targetOverride"/> to use a different target (e.g. shop-panel icon).</summary>
    public void PlayCoins(RectTransform source, int amount, int startingCoins, RectTransform targetOverride = null)
    {
        RectTransform target = targetOverride != null ? targetOverride : targetCoinsPosition;
        if (amount <= 0 || source == null || target == null || coinSprite == null) return;
        currentAnimatedCoins = startingCoins;
        StartCoroutine(AnimateParticles(source, target, coinSprite, amount, false));
    }

    /// <summary>Animates cash flying from <paramref name="source"/> to the cash display.
    /// Pass <paramref name="targetOverride"/> to use a different target (e.g. shop-panel icon).</summary>
    public void PlayCash(RectTransform source, float amount, float startingCash, RectTransform targetOverride = null)
    {
        RectTransform target = targetOverride != null ? targetOverride : targetCashPosition;
        if (amount <= 0f || source == null || target == null || cashSprite == null) return;
        currentAnimatedCash = startingCash;
        StartCoroutine(AnimateParticles(source, target, cashSprite, amount, true));
    }

    private IEnumerator AnimateParticles(RectTransform source, RectTransform target, Sprite sprite, float amount, bool isCash)
    {
        int count = isCash
            ? Mathf.RoundToInt(baseParticles + amount * particlesPerCash)
            : Mathf.RoundToInt(baseParticles + amount * particlesPerCoin);
        count = Mathf.Clamp(count, baseParticles, maxParticles);

        float increment = amount / count;
        Vector3 sourcePos = source.position;

        for (int i = 0; i < count; i++)
        {
            GameObject p = SpawnParticle(sourcePos, sprite);
            if (p != null)
            {
                activeParticles.Add(p);
                StartCoroutine(FlyParticle(p.transform, target.position, increment, isCash));
            }
            yield return new WaitForSeconds(delayBetweenParticles);
        }
    }

    private IEnumerator FlyParticle(Transform particle, Vector3 target, float increment, bool isCash)
    {
        Vector3 start   = particle.position;
        float   elapsed = 0f;
        bool    ticked  = false;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            particle.position = Vector3.Lerp(start, target, movementCurve.Evaluate(t));

            if (!ticked && t >= 0.85f)
            {
                ticked = true;
                if (isCash)
                    currentAnimatedCash += increment;
                else
                    currentAnimatedCoins += Mathf.RoundToInt(increment);

                CurrencyDisplayUI.TriggerVisualAnimationTick(currentAnimatedCoins, currentAnimatedCash);

                if (SoundManager.instance != null)
                {
                    if (isCash) SoundManager.instance.PlayCash();
                    else        SoundManager.instance.PlayChips();
                }
            }
            yield return null;
        }

        activeParticles.Remove(particle.gameObject);
        Destroy(particle.gameObject);
    }

    private GameObject SpawnParticle(Vector3 position, Sprite sprite)
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        GameObject obj = animatedSpritePrefab != null
            ? Instantiate(animatedSpritePrefab, parent)
            : new GameObject("CoinParticle");

        if (animatedSpritePrefab == null)
        {
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
            obj.AddComponent<Image>().sprite = sprite;
        }

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.position    = position;
            rect.localScale  = Vector3.one;
        }
        else
        {
            obj.transform.position   = position;
            obj.transform.localScale = Vector3.one;
        }

        Image img = obj.GetComponent<Image>();
        if (img != null) img.sprite = sprite;

        return obj;
    }
}
