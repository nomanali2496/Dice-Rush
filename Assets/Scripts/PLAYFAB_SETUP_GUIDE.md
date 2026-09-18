# PlayFab Integration Setup Guide

## Overview
This guide will help you set up and use the PlayFab integration for your Dice Blitz game. The integration includes player authentication, data management, and currency systems.

## Files Created
1. **PlayerData.cs** - Data model for player information
2. **PlayFabManager.cs** - Main PlayFab manager (Singleton)
3. **SplashScreen.cs** - Updated with PlayFab initialization
4. **LoginUI.cs** - Login screen UI controller
5. **RegistrationUI.cs** - Registration screen UI controller
6. **PlayerProfileUI.cs** - Player profile display

## PlayFab Setup Steps

### 1. Install PlayFab SDK
If you haven't already:
1. Open Unity Package Manager (Window > Package Manager)
2. Click the "+" button and select "Add package from git URL"
3. Enter: `https://github.com/PlayFab/UnitySDK.git`
4. Or download from Asset Store: "PlayFab SDK"

### 2. Configure PlayFab
1. Go to [PlayFab.com](https://playfab.com) and create an account
2. Create a new Title in the PlayFab dashboard
3. Copy your **Title ID**
4. In Unity: Window > PlayFab > Editor Extensions
5. Paste your Title ID and click "Set Title ID"

### 3. Scene Setup

#### Splash Screen Scene
1. Create a new scene called "SplashScreen"
2. Add a Canvas with a Slider
3. Create an empty GameObject and attach `SplashScreen.cs`
4. Assign the Slider to the script
5. Set "Next Scene Name" to "LoginScene"

#### Login Scene
1. Create a new scene called "LoginScene"
2. Add a Canvas with:
   - 2 InputFields (Email, Password) - use TextMeshPro
   - 3 Buttons (Login, Register, Guest Login)
   - 1 Text field for status messages
   - Optional: Loading panel
3. Create an empty GameObject and attach `LoginUI.cs`
4. Assign all UI elements to the script
5. Set "Game Scene Name" to your main game scene

#### Registration Scene (Optional)
1. Create a new scene called "RegistrationScene"
2. Add a Canvas with:
   - 4 InputFields (Name, Email, Password, Confirm Password)
   - 2 Buttons (Register, Back)
   - 1 Text field for status messages
   - Optional: Loading panel
3. Create an empty GameObject and attach `RegistrationUI.cs`
4. Assign all UI elements to the script

#### Game Scene (Player Profile)
1. In your main game scene
2. Add UI elements:
   - Text fields for Name, Email, Cash, Coins
   - Image for profile picture
   - Buttons for Refresh and Logout
3. Create an empty GameObject and attach `PlayerProfileUI.cs`
4. Assign all UI elements to the script

### 4. Build Settings
Add all scenes to Build Settings (File > Build Settings):
1. SplashScreen
2. LoginScene
3. RegistrationScene (if using)
4. Your Game Scene(s)

## Player Data Structure

The system stores the following player data:
- **playerName**: Player's display name
- **email**: Player's email address
- **password**: Managed by PlayFab (never stored directly)
- **cash**: In-game currency (integer)
- **coins**: Premium currency (integer)
- **imageUrl**: URL to player's profile image

## Usage Examples

### Register a New Player
```csharp
PlayFabManager.Instance.RegisterPlayer(
    "player@example.com",
    "password123",
    "PlayerName"
);
```

### Login with Email
```csharp
PlayFabManager.Instance.LoginWithEmail(
    "player@example.com",
    "password123"
);
```

### Guest Login (Device ID)
```csharp
string deviceId = SystemInfo.deviceUniqueIdentifier;
PlayFabManager.Instance.LoginWithCustomId(deviceId);
```

### Add Currency
```csharp
// Add cash
PlayFabManager.Instance.AddCash(100);

// Add coins
PlayFabManager.Instance.AddCoins(50);
```

### Spend Currency
```csharp
// Try to spend cash
bool success = PlayFabManager.Instance.SpendCash(100);
if (success)
{
    Debug.Log("Purchase successful!");
}
else
{
    Debug.Log("Not enough cash!");
}
```

### Update Profile Image
```csharp
PlayFabManager.Instance.UpdateProfileImage("https://example.com/avatar.jpg");
```

### Save Player Data
```csharp
// Automatically saved when using currency methods
// Or manually:
PlayFabManager.Instance.SavePlayerData();
```

### Load Player Data
```csharp
// Automatically loaded after login
// Or manually refresh:
PlayFabManager.Instance.LoadPlayerData();
```

### Access Current Player Data
```csharp
PlayerData data = PlayFabManager.Instance.currentPlayerData;
Debug.Log($"Player: {data.playerName}");
Debug.Log($"Cash: {data.cash}");
Debug.Log($"Coins: {data.coins}");
```

### Subscribe to Events
```csharp
void Start()
{
    PlayFabManager.Instance.OnLoginComplete += OnLoginComplete;
    PlayFabManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
}

void OnLoginComplete(bool success)
{
    if (success)
    {
        Debug.Log("Login successful!");
    }
}

void OnPlayerDataLoaded(PlayerData data)
{
    Debug.Log($"Loaded data for: {data.playerName}");
}
```

## Important Notes

### Password Security
- Passwords are NEVER stored in your game
- PlayFab handles all password encryption and security
- Only PlayFab's secure authentication system accesses passwords

### Data Persistence
- Player data is automatically saved to PlayFab cloud
- Data persists across devices when using email login
- Guest accounts (device ID) are device-specific

### Auto-Login
The `SplashScreen` can attempt auto-login:
- Enable "Attempt Auto Login" in SplashScreen inspector
- Uses device unique identifier for guest access
- Perfect for casual games

### Testing
1. Test registration with a valid email
2. Test login with correct/incorrect credentials
3. Test guest login
4. Verify data saves and loads correctly
5. Test currency add/spend functions

## Troubleshooting

### "PlayFabManager not found"
- Make sure PlayFabManager GameObject exists in the scene
- Or let SplashScreen create it automatically

### "Invalid Title ID"
- Double-check your Title ID in PlayFab dashboard
- Verify it's correctly set in Unity (Window > PlayFab > Editor Extensions)

### Login/Registration Fails
- Check your internet connection
- Verify PlayFab Title ID is correct
- Check Unity Console for detailed error messages

### Data Not Saving
- Ensure player is logged in (`PlayFabManager.Instance.IsLoggedIn()`)
- Check PlayFab dashboard for data errors
- Verify internet connection

## Next Steps

1. **Customize UI**: Style the login/registration screens to match your game
2. **Add Features**: Implement leaderboards, achievements, etc.
3. **Error Handling**: Add more user-friendly error messages
4. **Profile Images**: Implement image upload functionality
5. **Social Features**: Add friend systems, multiplayer, etc.

## Support
- PlayFab Documentation: https://docs.microsoft.com/gaming/playfab/
- PlayFab Forums: https://community.playfab.com/

---
**Created for Dice Blitz Game**
*Player data includes: Name, Email, Cash, Coins, and Profile Image*

