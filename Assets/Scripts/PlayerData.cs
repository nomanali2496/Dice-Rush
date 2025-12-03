using System;
using UnityEngine;

/// <summary>
/// Stores player data that will be saved to PlayFab
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerName;
    public string email;
    public int cash;
    public int coins;
    public string imageUrl;
    
    // Constructor
    public PlayerData()
    {
        playerName = "";
        email = "";
        cash = 0;
        coins = 0;
        imageUrl = "";
    }
    
    public PlayerData(string name, string emailAddress, int initialCash = 1000, int initialCoins = 100)
    {
        playerName = name;
        email = emailAddress;
        cash = initialCash;
        coins = initialCoins;
        imageUrl = "";
    }
    
    /// <summary>
    /// Convert player data to JSON string
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    /// <summary>
    /// Load player data from JSON string
    /// </summary>
    public static PlayerData FromJson(string json)
    {
        return JsonUtility.FromJson<PlayerData>(json);
    }
}

