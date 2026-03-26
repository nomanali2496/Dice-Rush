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
    public string playFabId;
    public float cash;
    public int coins;
    public string imageUrl;
    public int selectedAvatarIndex = 0;
    public int selectedBackgroundIndex = 0;
    
    public PlayerData()
    {
        playerName = "";
        email = "";
        playFabId = "";
        cash = 0;
        coins = 0;
        imageUrl = "";
        selectedAvatarIndex = 0;
        selectedBackgroundIndex = 0;
    }
    
    public PlayerData(string name, string emailAddress, float initialCash = 0f, int initialCoins = 0, string id = "")
    {
        playerName = name;
        email = emailAddress;
        playFabId = id;
        cash = initialCash;
        coins = initialCoins;
        imageUrl = "";
        selectedAvatarIndex = 0;
        selectedBackgroundIndex = 0;
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

