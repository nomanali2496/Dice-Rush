# PlayFab Quick Reference Card 🎮

## 🔥 Most Common Commands

### Authentication
```csharp
// Register new player
PlayFabManager.Instance.RegisterPlayer("email@example.com", "password", "PlayerName");

// Login with email
PlayFabManager.Instance.LoginWithEmail("email@example.com", "password");

// Guest login
PlayFabManager.Instance.LoginWithCustomId(SystemInfo.deviceUniqueIdentifier);

// Check if logged in
bool loggedIn = PlayFabManager.Instance.IsLoggedIn();

// Logout
PlayFabManager.Instance.Logout();
```

### Currency Management
```csharp
// Add currency
PlayFabManager.Instance.AddCash(100);
PlayFabManager.Instance.AddCoins(50);

// Spend currency (returns true if successful)
bool success = PlayFabManager.Instance.SpendCash(100);
bool success = PlayFabManager.Instance.SpendCoins(50);

// Get current amounts
int cash = PlayFabManager.Instance.currentPlayerData.cash;
int coins = PlayFabManager.Instance.currentPlayerData.coins;
```

### Player Data
```csharp
// Access current player data
PlayerData data = PlayFabManager.Instance.currentPlayerData;
string name = data.playerName;
string email = data.email;
int cash = data.cash;
int coins = data.coins;
string imageUrl = data.imageUrl;

// Save data manually (auto-saved on currency changes)
PlayFabManager.Instance.SavePlayerData();

// Reload data from cloud
PlayFabManager.Instance.LoadPlayerData();

// Update profile image
PlayFabManager.Instance.UpdateProfileImage("https://example.com/image.jpg");
```

### Events
```csharp
// Subscribe to events
PlayFabManager.Instance.OnLoginComplete += (success) => {
    if (success) Debug.Log("Login successful!");
};

PlayFabManager.Instance.OnRegisterComplete += (success) => {
    if (success) Debug.Log("Registration successful!");
};

PlayFabManager.Instance.OnPlayerDataLoaded += (data) => {
    Debug.Log($"Loaded: {data.playerName}");
};

PlayFabManager.Instance.OnPlayerDataSaved += (success) => {
    if (success) Debug.Log("Data saved!");
};
```

---

## 📋 Scene Setup Checklist

### ✅ Splash Screen Scene
- [ ] Canvas with UI Slider
- [ ] GameObject with SplashScreen.cs
- [ ] Assign Slider to script
- [ ] Set "Next Scene Name" to "LoginScene"

### ✅ Login Scene
- [ ] Canvas with:
  - [ ] 2 InputFields (Email, Password) - TextMeshPro
  - [ ] 3 Buttons (Login, Register, Guest)
  - [ ] 1 Text (Status)
  - [ ] 1 Panel (Loading - optional)
- [ ] GameObject with LoginUI.cs
- [ ] Assign all UI elements
- [ ] Set "Game Scene Name"

### ✅ Registration Scene
- [ ] Canvas with:
  - [ ] 4 InputFields (Name, Email, Password, Confirm)
  - [ ] 2 Buttons (Register, Back)
  - [ ] 1 Text (Status)
  - [ ] 1 Panel (Loading - optional)
- [ ] GameObject with RegistrationUI.cs
- [ ] Assign all UI elements

### ✅ Game Scene
- [ ] Canvas with player info UI
- [ ] GameObject with PlayerProfileUI.cs
- [ ] GameObject with GameManager.cs
- [ ] Assign UI elements

### ✅ Build Settings
- [ ] Add SplashScreen scene (index 0)
- [ ] Add LoginScene scene
- [ ] Add RegistrationScene scene (optional)
- [ ] Add Game scene(s)

---

## 🎯 Initial Setup Steps

1. **Install PlayFab SDK**
   - Unity Package Manager → Add from git URL
   - URL: `https://github.com/PlayFab/UnitySDK.git`

2. **Get PlayFab Title ID**
   - Go to https://playfab.com
   - Create account → Create Title
   - Copy Title ID

3. **Configure Unity**
   - Window → PlayFab → Editor Extensions
   - Paste Title ID → Click "Set Title ID"

4. **Test**
   - Play SplashScreen scene
   - Register → Login → Play!

---

## 💡 Common Patterns

### Reward Player After Game Win
```csharp
public void OnGameWin()
{
    PlayFabManager.Instance.AddCash(50);
    PlayFabManager.Instance.AddCoins(10);
}
```

### Check Before Purchase
```csharp
public bool TryPurchase(int cost)
{
    if (PlayFabManager.Instance.currentPlayerData.cash >= cost)
    {
        return PlayFabManager.Instance.SpendCash(cost);
    }
    Debug.Log("Not enough cash!");
    return false;
}
```

### Daily Bonus
```csharp
public void ClaimDailyBonus()
{
    PlayFabManager.Instance.AddCash(100);
    PlayFabManager.Instance.AddCoins(25);
}
```

### Update UI After Changes
```csharp
void Start()
{
    PlayFabManager.Instance.OnPlayerDataLoaded += UpdateUI;
}

void UpdateUI(PlayerData data)
{
    cashText.text = "$" + data.cash;
    coinsText.text = data.coins.ToString();
}
```

---

## 🐛 Troubleshooting Quick Fixes

| Problem | Solution |
|---------|----------|
| "PlayFabManager not found" | Runs automatically from SplashScreen |
| Login fails | Check Title ID in Unity |
| Data not saving | Check internet connection |
| Password too short | Minimum 6 characters |
| Email invalid | Must be valid email format |

---

## 📊 Default Values

| Property | Initial Value |
|----------|---------------|
| Cash | 1000 |
| Coins | 100 |
| Min Password | 6 characters |

---

## 🔗 Quick Links

- **Full Setup Guide**: PLAYFAB_SETUP_GUIDE.md
- **Script Overview**: README.md
- **PlayFab Dashboard**: https://playfab.com
- **PlayFab Docs**: https://docs.microsoft.com/gaming/playfab/

---

**Pro Tip**: All currency methods auto-save to PlayFab cloud! 💾

Last Updated: Dec 2025

