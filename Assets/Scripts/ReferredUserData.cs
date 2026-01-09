using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure for a referred user
/// </summary>
[Serializable]
public class ReferredUserData
{
    public string playFabId;
    public string playerName;
    public string imageUrl; // Profile image URL
    public string referredDate; // ISO 8601 format
    
    public ReferredUserData()
    {
        playFabId = "";
        playerName = "";
        imageUrl = "";
        referredDate = "";
    }
}

/// <summary>
/// Wrapper class for serializing list of referred users to JSON
/// </summary>
[Serializable]
public class ReferredUsersListWrapper
{
    public List<ReferredUserData> referredUsers;
    
    public ReferredUsersListWrapper()
    {
        referredUsers = new List<ReferredUserData>();
    }
}

