using UnityEngine;
using TMPro;

/// <summary>
/// Modular UI component that automatically updates a TextMeshPro 
/// element whenever its target currency changes in PlayFabManager.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class CurrencyDisplayUI : MonoBehaviour
{
    [Tooltip("Which currency should this UI element display?")]
    public CurrencyType currencyToWatch;
    
    [Tooltip("Optional prefix text before the amount, like 'Cash: $' or 'Coins: '")]
    public string prefix = "";

    // Event used to broadcast fake/temporary numbers during particle count-up animations
    public static System.Action<CurrencyType, float> OnCurrencyVisualTick;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Subscribe to changes globally
        PlayerModel.OnDataUpdated += HandleDataUpdated;
        OnCurrencyVisualTick += HandleCurrencyUpdated;
        
        // Fetch current value to initialize instantly
        HandleDataUpdated();
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        PlayerModel.OnDataUpdated -= HandleDataUpdated;
        OnCurrencyVisualTick -= HandleCurrencyUpdated;
    }

    private void HandleDataUpdated()
    {
        float newValue = currencyToWatch == CurrencyType.Cash 
            ? PlayerModel.CashBalance 
            : PlayerModel.Coins;
            
        HandleCurrencyUpdated(currencyToWatch, newValue);
    }

    /// <summary>
    /// Helper method to replace PlayerProfileUI.UpdateAllCurrencyDisplays
    /// Plugs directly into the game's existing animation scripts.
    /// </summary>
    public static void TriggerVisualAnimationTick(int chips, float cash)
    {
        OnCurrencyVisualTick?.Invoke(CurrencyType.Coins, chips);
        OnCurrencyVisualTick?.Invoke(CurrencyType.Cash, cash);
    }

    private void HandleCurrencyUpdated(CurrencyType type, float newAmount)
    {
        // Stop if this script is looking at the wrong currency type
        if (type != currencyToWatch || textComponent == null) return;

        // Apply formatting matching PlayerProfileUI
        if (type == CurrencyType.Cash)
        {
            textComponent.text = prefix + FormatCash(newAmount);
        }
        else
        {
            textComponent.text = prefix + FormatCoins(Mathf.RoundToInt(newAmount));
        }
    }

    public static string FormatCash(float amount)
    {
        if (amount >= 1000f)
        {
            float kValue = amount / 1000f;
            if (Mathf.Abs(kValue - Mathf.RoundToInt(kValue)) < 0.001f)
                return Mathf.RoundToInt(kValue).ToString() + "k";
            else
                return kValue.ToString("0.#") + "k";
        }
        
        int wholeNumberPart = Mathf.FloorToInt(amount);
        if (wholeNumberPart >= 100) return wholeNumberPart.ToString();
        if (Mathf.Abs(amount - Mathf.RoundToInt(amount)) < 0.001f) return Mathf.RoundToInt(amount).ToString();
        else return amount.ToString("0.##");
    }

    public static string FormatCoins(int amount)
    {
        if (amount >= 1000)
        {
            float kValue = amount / 1000f;
            if (Mathf.Abs(kValue - Mathf.RoundToInt(kValue)) < 0.001f)
                return Mathf.RoundToInt(kValue).ToString() + "k";
            else
                return kValue.ToString("0.#") + "k";
        }
        return amount.ToString();
    }
}
