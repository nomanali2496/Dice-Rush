using UnityEngine;
using UnityEngine.UI;

public class PrivateTableCard : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    
    [Header("Popup Reference")]
    [Tooltip("Drag the ArenaHostJoinPanel UI GameObject here so it opens when clicked.")]
    public GameObject privateTablePopupPanel;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPrivateTableClicked);
        }
    }

    private void OnPrivateTableClicked()
    {
        Debug.Log("[PrivateTableCard] Clicked! Opening Host/Join Popup...");
        if (privateTablePopupPanel != null)
        {
            privateTablePopupPanel.SetActive(true);
            
            // If you are using the ArenaHostJoinPanel script on the popup:
            ArenaHostJoinPanel panelScript = privateTablePopupPanel.GetComponent<ArenaHostJoinPanel>();
            if (panelScript != null)
            {
                // Pass the backend preset ID for private arenas and a default stake if needed
                panelScript.OpenPanel("arena-custom-table", 0f);
            }
        }
        else
        {
            Debug.LogWarning("[PrivateTableCard] No Popup Panel assigned in the inspector!");
        }
    }
}
