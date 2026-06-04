using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen blocking overlay shown during the Stripe checkout API call.
///
/// Setup in Unity Inspector:
///   1. On the main Canvas, create an empty child → name it "PurchaseLoadingOverlay"
///      (drag it to the BOTTOM of the Canvas children so it renders on top of everything)
///   2. Add this script to it
///   3. Add a child Image "Background" — stretch to fill, semi-transparent black, Raycast Target ON
///   4. Add a child TextMeshProUGUI "StatusText" — centred, white, ~18pt
///   5. (Optional) Add a child Image "Spinner" — small circular sprite, Raycast Target OFF
///   6. Wire up overlayRoot, statusText, spinnerImage in Inspector
///   7. Assign this component to InAppPurchaseManager → Loading Overlay
/// </summary>
public class PurchaseLoadingOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject      overlayRoot;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image           spinnerImage;

    [Header("Spinner")]
    [SerializeField] private float spinSpeed = 200f;

    private bool isSpinning;

    private void Awake()
    {
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    private void Update()
    {
        if (isSpinning && spinnerImage != null)
            spinnerImage.transform.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
    }

    public void Show(string status)
    {
        if (statusText != null) statusText.text = status;
        if (overlayRoot != null) overlayRoot.SetActive(true);
        isSpinning = true;
    }

    public void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
        if (overlayRoot != null && !overlayRoot.activeSelf) overlayRoot.SetActive(true);
    }

    public void Hide()
    {
        isSpinning = false;
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }
}
