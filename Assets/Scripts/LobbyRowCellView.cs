using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Row cell view that contains 2 LobbyGameObjectUI items for grid layout
/// </summary>
public class LobbyRowCellView : EnhancedScrollerCellView
{
    [Header("Lobby Game Object Items")]
    [SerializeField] private LobbyGameObjectUI item1; // First item in the row
    [SerializeField] private LobbyGameObjectUI item2; // Second item in the row
    
    [Header("Play Buttons")]
    [SerializeField] private Button playButton1; // Play button for first item
    [SerializeField] private Button playButton2; // Play button for second item
    
    private LobbyManager lobbyManager;
    
    private void Awake()
    {
        // Find LobbyManager in the scene
        lobbyManager = FindObjectOfType<LobbyManager>();
        
        // Setup button listeners
        if (playButton1 != null)
        {
            playButton1.onClick.AddListener(OnPlayButton1Clicked);
        }
        
        if (playButton2 != null)
        {
            playButton2.onClick.AddListener(OnPlayButton2Clicked);
        }
    }
    
    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks
        if (playButton1 != null)
        {
            playButton1.onClick.RemoveListener(OnPlayButton1Clicked);
        }
        
        if (playButton2 != null)
        {
            playButton2.onClick.RemoveListener(OnPlayButton2Clicked);
        }
    }
    
    /// <summary>
    /// Sets the data for this row (2 items)
    /// </summary>
    /// <param name="data1">Data for first item (can be null)</param>
    /// <param name="data2">Data for second item (can be null)</param>
    public void SetData(LobbyGameObjectData data1, LobbyGameObjectData data2)
    {
        // Set first item
        if (item1 != null)
        {
            if (data1 != null)
            {
                item1.SetData(data1);
                item1.gameObject.SetActive(true);
                
                // Show and update play button for first item
                if (playButton1 != null)
                {
                    playButton1.gameObject.SetActive(true);
                    UpdatePlayButtonInteractability(playButton1, data1.lobbyIndex);
                }
            }
            else
            {
                item1.gameObject.SetActive(false);
                
                // Hide play button if no data
                if (playButton1 != null)
                {
                    playButton1.gameObject.SetActive(false);
                }
            }
        }
        
        // Set second item
        if (item2 != null)
        {
            if (data2 != null)
            {
                item2.SetData(data2);
                item2.gameObject.SetActive(true);
                
                // Show and update play button for second item
                if (playButton2 != null)
                {
                    playButton2.gameObject.SetActive(true);
                    UpdatePlayButtonInteractability(playButton2, data2.lobbyIndex);
                }
            }
            else
            {
                item2.gameObject.SetActive(false);
                
                // Hide play button if no data
                if (playButton2 != null)
                {
                    playButton2.gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Updates the interactability of a play button based on whether player can afford entry fee
    /// </summary>
    private void UpdatePlayButtonInteractability(Button playButton, int lobbyIndex)
    {
        if (playButton == null || lobbyManager == null)
        {
            return;
        }
        
        // Check if player can afford entry fee
        bool canAfford = lobbyManager.CanPlayerAffordEntryFee(lobbyIndex);
        playButton.interactable = canAfford;
        
        if (!canAfford)
        {
            Debug.Log($"[LobbyRowCellView] Play button disabled for Lobby {lobbyIndex + 1} - player cannot afford entry fee");
        }
    }
    
    /// <summary>
    /// Refreshes the interactability of play buttons (call this when player currency changes)
    /// </summary>
    public void RefreshPlayButtonsInteractability()
    {
        if (item1 != null && playButton1 != null)
        {
            LobbyGameObjectData data1 = item1.GetCurrentData();
            if (data1 != null)
            {
                UpdatePlayButtonInteractability(playButton1, data1.lobbyIndex);
            }
        }
        
        if (item2 != null && playButton2 != null)
        {
            LobbyGameObjectData data2 = item2.GetCurrentData();
            if (data2 != null)
            {
                UpdatePlayButtonInteractability(playButton2, data2.lobbyIndex);
            }
        }
    }
    
    /// <summary>
    /// Called when play button 1 is clicked
    /// </summary>
    private void OnPlayButton1Clicked()
    {
        if (lobbyManager != null && item1 != null)
        {
            LobbyGameObjectData data = item1.GetCurrentData();
            if (data != null)
            {
                // Pass the lobby index to open the correct lobby configuration
                lobbyManager.OpenGameplayObject(data.lobbyIndex);
            }
        }
        else
        {
            Debug.LogWarning("[LobbyRowCellView] LobbyManager or item1 is null! Cannot open gameplay object.");
        }
    }
    
    /// <summary>
    /// Called when play button 2 is clicked
    /// </summary>
    private void OnPlayButton2Clicked()
    {
        if (lobbyManager != null && item2 != null)
        {
            LobbyGameObjectData data = item2.GetCurrentData();
            if (data != null)
            {
                // Pass the lobby index to open the correct lobby configuration
                lobbyManager.OpenGameplayObject(data.lobbyIndex);
            }
        }
        else
        {
            Debug.LogWarning("[LobbyRowCellView] LobbyManager or item2 is null! Cannot open gameplay object.");
        }
    }
}

