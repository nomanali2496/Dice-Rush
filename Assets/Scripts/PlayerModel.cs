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
    public static int Xp { get; private set; }
    public static int Level { get; private set; }
    public static string Role { get; private set; }

    // Event fired whenever the player data changes
    public static event Action OnDataUpdated;

    /// <summary>
    /// Updates the global player model and notifies listeners.
    /// Called exclusively by the Backend/Networking layer.
    /// </summary>
    public static void UpdateData(
        string id, string name, string email, int wins, int losses,
        int balance, int cashBalance, int bonusBalance, int winningBalance, int coins,
        string avatar, int xp, int level, string role)
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
        Avatar = avatar;
        Xp = xp;
        Level = level;
        Role = role;

        OnDataUpdated?.Invoke();
    }
}
