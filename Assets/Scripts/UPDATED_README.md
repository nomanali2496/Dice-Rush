# Dice Rush - PlayFab Integration (SINGLE SCENE VERSION)

## 🎮 Complete PlayFab Integration - Single Scene Setup

**Flow: Login/Register → SplashScreen → Game (All in ONE scene!)**

---

## 📁 Core Scripts

### Main Scripts
1. **AuthenticationManager.cs** ⭐ (NEW - Controls everything)
   - Handles Login & Registration UI
   - Shows Login/Register panel FIRST
   - Then shows SplashScreen after authentication
   - Manages panel transitions

2. **SplashScreen.cs** (UPDATED)
   - Shows AFTER successful login
   - Loading animation with progress bar
   - Transitions to Game panel

3. **PlayFabManager.cs** (Backend)
   - Singleton manager
   - Authentication (Email, Guest)
   - Cloud save/load
   - Currency management

4. **PlayerData.cs**
   - Data model: name, email, cash, coins, imageUrl

5. **PlayerProfileUI.cs** (Optional)
   - Display player info in game
   - Show currency

---

## 🚀 Quick Start (3 Steps)

### Step 1: PlayFab Setup
1. Install PlayFab SDK from Unity Package Manager
2. Get Title ID from PlayFab.com
3. Window → PlayFab → Editor Extensions → Set Title ID

### Step 2: Create Single Scene
Follow the detailed guide in **SINGLE_SCENE_SETUP_GUIDE.md**

Quick summary:
```
Canvas
├── LoginRegisterPanel (Active)
│   ├── LoginSection (Active)
│   └── RegisterSection (Inactive)
├── SplashScreenPanel (Inactive)
└── GamePanel (Inactive)
```

### Step 3: Test!
1. Press Play
2. See Login screen
3. Create account or login
4. See SplashScreen loading
5. Play game!

---

## 📊 Player Data (As Requested)

```csharp
PlayerData {
    ✓ playerName (string)   // Display name
    ✓ email (string)        // Email address
    ✓ password (secure)     // Handled by PlayFab (never stored)
    ✓ cash (int)            // Regular currency (starts: 1000)
    ✓ coins (int)           // Premium currency (starts: 100)
    ✓ imageUrl (string)     // Profile picture URL
}
```

---

## 💡 Key Features

✅ **Single Scene Architecture** - No scene loading!  
✅ **Login First** - User authenticates before seeing game  
✅ **Email/Password Auth** - Secure registration  
✅ **Guest Login** - Quick play with device ID  
✅ **SplashScreen After Login** - Shows after authentication  
✅ **Smooth Transitions** - Panel-based UI switching  
✅ **Cloud Save** - Auto-saves player data  
✅ **Currency System** - Cash & Coins  
✅ **Input Validation** - Email format, password matching  
✅ **Error Messages** - User-friendly status text  

---

## 📖 Common Usage

### Access Player Data
```csharp
PlayerData data = PlayFabManager.Instance.currentPlayerData;
Debug.Log($"Player: {data.playerName}");
Debug.Log($"Cash: {data.cash}");
Debug.Log($"Coins: {data.coins}");
```

### Add Currency
```csharp
PlayFabManager.Instance.AddCash(100);
PlayFabManager.Instance.AddCoins(50);
```

### Spend Currency
```csharp
bool success = PlayFabManager.Instance.SpendCash(100);
if (success) {
    Debug.Log("Purchase successful!");
}
```

---

## 🎯 Scene Setup Summary

### Panel Flow
1. **LoginRegisterPanel** (Shows FIRST)
   - Login form (default)
   - Register form (toggle)
   - Guest login option

2. **SplashScreenPanel** (Shows AFTER login)
   - Loading bar animation
   - Game initialization

3. **GamePanel** (Shows AFTER loading)
   - Your game UI
   - Player profile
   - Gameplay elements

### Scripts Assignment
- **AuthenticationManager** → Controls Login/Register/SplashScreen
- **SplashScreen** → Attached to SplashScreenPanel
- **PlayerProfileUI** → Attached to GamePanel (optional)

---

## 📚 Documentation Files

📖 **SINGLE_SCENE_SETUP_GUIDE.md** ⭐ - Complete step-by-step setup  
📖 **PLAYFAB_SETUP_GUIDE.md** - PlayFab configuration details  
📖 **QUICK_REFERENCE.md** - Quick commands  
📖 **IMPLEMENTATION_SUMMARY.txt** - Overview  

---

## 🔐 Security

- Passwords NEVER stored locally ✓
- PlayFab handles all encryption ✓
- Secure cloud storage ✓
- Device ID for guest accounts ✓

---

## 🎨 UI Structure Example

```
MainScene
  └── Canvas
      ├── LoginRegisterPanel [ACTIVE]
      │   ├── LoginSection [ACTIVE]
      │   │   ├── EmailInput
      │   │   ├── PasswordInput
      │   │   ├── LoginButton
      │   │   ├── GuestButton
      │   │   └── SwitchToRegisterButton
      │   ├── RegisterSection [INACTIVE]
      │   │   ├── NameInput
      │   │   ├── EmailInput
      │   │   ├── PasswordInput
      │   │   ├── ConfirmPasswordInput
      │   │   ├── RegisterButton
      │   │   └── SwitchToLoginButton
      │   └── StatusText
      ├── SplashScreenPanel [INACTIVE]
      │   └── ProgressSlider
      └── GamePanel [INACTIVE]
          └── Your Game UI
```

---

## ✅ Checklist

Setup:
- [ ] PlayFab SDK installed
- [ ] Title ID configured
- [ ] Canvas created
- [ ] All panels created (Login, Splash, Game)
- [ ] AuthenticationManager added
- [ ] All UI references assigned
- [ ] SplashScreen script added
- [ ] References assigned in SplashScreen

Test:
- [ ] Login screen shows first
- [ ] Can switch between Login/Register
- [ ] Can login with email
- [ ] Can register new account
- [ ] SplashScreen shows after login
- [ ] Game panel shows after loading

---

## 🆕 Changes from Multi-Scene Version

**Before:** SplashScreen → Login → Game (3 scenes)  
**Now:** Login → SplashScreen → Game (1 scene)

**Benefits:**
- ✅ Faster transitions
- ✅ No scene loading overhead
- ✅ Better user experience
- ✅ Easier to manage
- ✅ Mobile-friendly

**New Scripts:**
- AuthenticationManager.cs (replaces LoginUI.cs + RegistrationUI.cs)

**Updated Scripts:**
- SplashScreen.cs (now works with panels, not scenes)

---

## 🎯 Testing

1. **Start:** See Login screen ✓
2. **Register:** Create account with name, email, password ✓
3. **Login:** Enter credentials ✓
4. **Splash:** See loading bar ✓
5. **Game:** Play! ✓

---

## 💻 Quick Commands

```csharp
// Check if logged in
bool isLoggedIn = PlayFabManager.Instance.IsLoggedIn();

// Get player name
string name = PlayFabManager.Instance.currentPlayerData.playerName;

// Add cash reward
PlayFabManager.Instance.AddCash(50);

// Save data
PlayFabManager.Instance.SavePlayerData();
```

---

## 🔗 Support

- PlayFab Dashboard: https://playfab.com
- Documentation: https://docs.microsoft.com/gaming/playfab/
- Unity Console: Check for errors

---

**Perfect for:** Mobile games, WebGL games, and any project that needs fast, smooth transitions!

**Version:** 2.0 (Single Scene)  
**Updated:** December 2025  
**Game:** Dice Rush  

🎲 Happy Gaming! 🎮

