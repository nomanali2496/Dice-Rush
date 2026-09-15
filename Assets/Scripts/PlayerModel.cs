using System;

public static class PlayerModel
{
    public static string Id { get; private set; }
    public static string Name { get; private set; }
    public static string Email { get; private set; }
    public static int Wins { get; private set; }
    public static int Losses { get; private set; }
    public static int Balance { get; private set; }
    public static int CashBalance { get; private set; }
    public static int BonusBalance { get; private set; }
    public static int WinningBalance { get; private set; }
    public static int Coins { get; private set; }
    public static string Avatar { get; private set; }
    public static string ProfileFrame { get; private set; }
    public static int Xp { get; private set; }
    public static int Level { get; private set; }
    public static string Role { get; private set; }
    public static int SelectedAvatarIndex { get; private set; }
    public static string ActiveMatchId { get; private set; }
    public static string ActiveRoomId { get; private set; }
    public static int WithdrawableBalance { get; private set; }
    public static string RecentMatchId { get; private set; }
    public static string RecentRoomId { get; private set; }
    public static string RecentMatchStatus { get; private set; }

    // Event fired whenever the player data changes
    public static event Action OnDataUpdated;

    /// <summary>
    /// Updates the global player model and notifies listeners.
    /// Called exclusively by the Backend/Networking layer.
    /// </summary>
    public static void UpdateData(
        string id, string name, string email, int wins, int losses,
        int balance, int cashBalance, int bonusBalance, int winningBalance, int coins,
        string avatar, int xp, int level, string role, int selectedAvatarIndex = 0,
        string activeMatchId = null, string activeRoomId = null,
        int? withdrawableBalance = null, string recentMatchId = null, string recentRoomId = null, string recentMatchStatus = null)
    {
        Id = id;
        Name = name;
        Email = email;
        Wins = wins;
        Losses = losses;
        Balance = balance;
        CashBalance = cashBalance;
        BonusBalance = bonusBalance;
        WinningBalance = winningBalance;
        Coins = coins;
        if (!string.IsNullOrEmpty(avatar))
        {
            Avatar = avatar;
        }
        else if (id == null)
        {
            Avatar = null; // Clear when logging out
        }
        Xp = xp;
        Level = level;
        Role = role;
        if (selectedAvatarIndex != 0 || id == null)
        {
            SelectedAvatarIndex = selectedAvatarIndex;
        }
        ActiveMatchId = activeMatchId;
        ActiveRoomId = activeRoomId;
        if (withdrawableBalance.HasValue)
        {
            WithdrawableBalance = withdrawableBalance.Value;
        }
        RecentMatchId = recentMatchId;
        RecentRoomId = recentRoomId;
        RecentMatchStatus = recentMatchStatus;

        OnDataUpdated?.Invoke();
    }

    /// <summary>
    /// Forces UI elements listening to OnDataUpdated to refresh their visuals (useful for local changes like frame updates).
    /// </summary>
    public static void TriggerDataUpdated()
    {
        OnDataUpdated?.Invoke();
    }

    public static void SetProfileFrame(string frame)
    {
        if (!string.IsNullOrEmpty(frame))
        {
            ProfileFrame = frame;
        }

        OnDataUpdated?.Invoke();
    }
}
