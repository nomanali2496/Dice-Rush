using UnityEngine;

/// <summary>
/// Configuration data for a single lobby
/// Each lobby has different number of players, entry fee, and prize structure
/// </summary>
[System.Serializable]
public class LobbyConfig
{
    [Header("Lobby Settings")]
    public int lobbyIndex; // Index of this lobby (0-12)
    public int numberOfPlayers; // Total number of players (1 player + CPUs)
    public string lobbyName; // Optional name for the lobby
    public string lobbyId; // Unique ID from database (e.g. "combo-dice")
    public string mode; // Mode from database (e.g. "2p")
    public string imageUrl; // Remote image path
    public float gameTimer = 60f; // Match duration in seconds
    
    [Header("Entry Fee")]
    public float entryFeeCash; // Entry fee in cash (0 if not using cash)
    public int entryFeeChips; // Entry fee in chips (0 if not using chips)
    
    [Header("1st Place Prize")]
    public float firstPlaceCash; // 1st place prize in cash (0 if not using cash)
    public int firstPlaceChips; // 1st place prize in chips (0 if not using chips)
    
    [Header("2nd Place Prize")]
    public float secondPlaceCash; // 2nd place prize in cash (0 if not using cash)
    public int secondPlaceChips; // 2nd place prize in chips (0 if not using chips)
    
    [Header("3rd Place Prize")]
    public float thirdPlaceCash; // 3rd place prize in cash (0 if not using cash)
    public int thirdPlaceChips; // 3rd place prize in chips (0 if not using chips)
    
[Header("Arena Settings")]
    public bool isArenaPreset;
    public float arenaStakeMin;
    public float arenaStakeMax;
    public bool enabled; // Whether this lobby is active on the server


    /// <summary>
    /// Constructor for creating a lobby configuration
    /// </summary>
    public LobbyConfig(int index, int players, float entryCash, int entryChips,
                      float firstCash, int firstChips, float secondCash, int secondChips,
                      float thirdCash, int thirdChips, float duration = 60f,
                      bool isArena = false, float arenaMin = 0f, float arenaMax = 0f)
    {
        lobbyIndex = index;
        numberOfPlayers = players;
        entryFeeCash = entryCash;
        entryFeeChips = entryChips;
        firstPlaceCash = firstCash;
        firstPlaceChips = firstChips;
        secondPlaceCash = secondCash;
        secondPlaceChips = secondChips;
        thirdPlaceCash = thirdCash;
        thirdPlaceChips = thirdChips;
        gameTimer = duration;
        lobbyName = $"Lobby {index + 1}";
        
        isArenaPreset = isArena;
        arenaStakeMin = arenaMin;
        arenaStakeMax = arenaMax;
        enabled = true; // Default to enabled

    }
    
    /// <summary>
    /// Now returns 0 as CPU functionality has been removed.
    /// </summary>
    public int GetNumberOfCPUs()
    {
        return 0; // Forced to zero per user request
    }
}

