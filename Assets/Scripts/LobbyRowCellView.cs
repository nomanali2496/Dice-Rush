using UnityEngine;
using EnhancedUI.EnhancedScroller;

/// <summary>
/// Row cell view that contains 2 LobbyGameObjectUI items for grid layout
/// </summary>
public class LobbyRowCellView : EnhancedScrollerCellView
{
    [Header("Lobby Game Object Items")]
    [SerializeField] private LobbyGameObjectUI item1; // First item in the row
    [SerializeField] private LobbyGameObjectUI item2; // Second item in the row
    
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
            }
            else
            {
                item1.gameObject.SetActive(false);
            }
        }
        
        // Set second item
        if (item2 != null)
        {
            if (data2 != null)
            {
                item2.SetData(data2);
                item2.gameObject.SetActive(true);
            }
            else
            {
                item2.gameObject.SetActive(false);
            }
        }
    }
}

