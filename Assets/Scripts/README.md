# Dice Blitz - PlayFab Integration Scripts

## 🎮 Complete PlayFab Integration with Player Data Management

This folder contains all the scripts needed to integrate PlayFab into your Dice Blitz game, including authentication, player data management, and currency systems.

---

## 📁 Script Files

### Core Scripts

1. **PlayerData.cs**
   - Data model for player information
   - Stores: name, email, cash, coins, imageUrl
   - JSON serialization support

2. **PlayFabManager.cs** ⭐ (Main Manager)
   - Singleton pattern
   - Handles all PlayFab operations
   - Functions:
     - Registration & Login (Email, Custom ID)
     - Save/Load player data
     - Currency management (Add/Spend cash & coins)
     - Profile image updates

### UI Scripts

3. **SplashScreen.cs** (Updated)
   - Loading screen with progress bar
   - Auto-initializes PlayFabManager
   - Optional auto-login with device ID
   - Scene transition to Login

4. **LoginUI.cs**
   - Email/Password login
   - Guest login (device ID)
   - Navigation to registration
   - Error handling & status messages

5. **RegistrationUI.cs**
   - New account creation
   - Input validation (email format, password match, etc.)
   - Automatic login after registration
   - Initial player data setup (1000 cash, 100 coins)

6. **PlayerProfileUI.cs**
   - Display player information
   - Show cash & coins
   - Refresh data from PlayFab
   - Logout functionality

### Gameplay Scripts

7. **GameManager.cs** (Example)
   - Game session management
   - Win rewards (cash & coins)
   - Purchase system integration
   - Daily bonuses
   - Currency checks

8. **ShopItem.cs** (Example)
   - Individual shop items
   - Dual currency support (cash & coins)
   - Purchase validation
   - Ownership tracking

---

## 🚀 Quick Start

### Step 1: PlayFab Setup
1. Install PlayFab SDK from Unity Package Manager
2. Get your Title ID from PlayFab dashboard
3. Set Title ID in Unity: `Window > PlayFab > Editor Extensions`

### Step 2: Scene Setup
Create these scenes in your project:
- **SplashScreen** - Add SplashScreen.cs with UI Slider
- **LoginScene** - Add LoginUI.cs with input fields & buttons
- **RegistrationScene** - Add RegistrationUI.cs with input fields
- **GameScene** - Your main game with GameManager.cs

### Step 3: Add to Build Settings
File > Build Settings > Add all scenes in order

### Step 4: Test
1. Run SplashScreen scene
2. Register a new account
3. Login and play!

---

## 💰 Player Data Structure

```csharp
public class PlayerData
{
    string playerName;    // Display name
    string email;        // Email address
    int cash;           // Regular currency
    int coins;          // Premium currency
    string imageUrl;    // Profile picture URL
}
```

**Initial Values on Registration:**
- Cash: 1000
- Coins: 100

---

## 📖 Common Usage Examples

### Check if Logged In
```csharp
if (PlayFabManager.Instance.IsLoggedIn())
{
    // Player is authenticated
}
```

### Add Currency
```csharp
PlayFabManager.Instance.AddCash(100);
PlayFabManager.Instance.AddCoins(50);
```

### Spend Currency
```csharp
if (PlayFabManager.Instance.SpendCash(100))
{
    // Purchase successful
}
```

### Get Player Data
```csharp
PlayerData data = PlayFabManager.Instance.currentPlayerData;
Debug.Log($"Player: {data.playerName}");
Debug.Log($"Cash: {data.cash}");
```

### Subscribe to Events
```csharp
PlayFabManager.Instance.OnPlayerDataLoaded += (data) => {
    Debug.Log($"Loaded: {data.playerName}");
};
```

---

## 🎯 Features Included

✅ Email/Password Registration  
✅ Email/Password Login  
✅ Guest Login (Device ID)  
✅ Auto-Login on Splash Screen  
✅ Player Data (Name, Email, Cash, Coins, Image)  
✅ Cloud Save/Load  
✅ Currency System (Cash & Coins)  
✅ Profile Management  
✅ Shop System Example  
✅ Game Manager Example  
✅ Input Validation  
✅ Error Handling  
✅ Event System  

---

## 🔐 Security Notes

- ✅ Passwords are NEVER stored locally
- ✅ PlayFab handles all password encryption
- ✅ Data is stored securely in the cloud
- ✅ Guest accounts use device unique identifier

---

## 📚 Documentation

See **PLAYFAB_SETUP_GUIDE.md** for detailed setup instructions, troubleshooting, and advanced features.

---

## 🛠️ Customization Tips

1. **Currency Values**: Adjust initial cash/coins in `PlayFabManager.OnRegisterSuccess()`
2. **Win Rewards**: Change `winRewardCash` and `winRewardCoins` in GameManager
3. **UI Styling**: Customize all UI scripts to match your game's look
4. **Auto-Login**: Toggle in SplashScreen inspector
5. **Shop Items**: Duplicate ShopItem.cs for different products

---

## 📞 Support

- Check Unity Console for error messages
- Verify PlayFab Title ID is correct
- Ensure internet connection is active
- See PlayFab dashboard for backend data

---

**Version:** 1.0  
**Created for:** Dice Blitz Game
**Last Updated:** December 2025

Happy coding! 🎲🎮

