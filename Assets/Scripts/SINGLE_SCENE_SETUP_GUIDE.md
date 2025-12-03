# Single Scene Setup Guide - PlayFab Integration
## Login/Register → SplashScreen → Game (All in ONE Scene)

---

## 🎯 Flow Overview

```
Start → Login/Register Panel → SplashScreen Panel → Game Panel
```

**User sees Login/Register FIRST, then SplashScreen after authentication!**

---

## 📁 Required Scripts

1. **AuthenticationManager.cs** - Handles Login & Registration (shows first)
2. **SplashScreen.cs** - Loading screen (shows after login)
3. **PlayFabManager.cs** - Backend manager (auto-created)
4. **PlayerData.cs** - Data model
5. **PlayerProfileUI.cs** - Optional: Display player info in game

---

## 🎨 Single Scene Setup (Step-by-Step)

### Scene Structure
```
MainScene
├── Canvas
│   ├── LoginRegisterPanel (Active at start)
│   │   ├── LoginSection
│   │   │   ├── Email InputField
│   │   │   ├── Password InputField
│   │   │   ├── Login Button
│   │   │   ├── Guest Login Button
│   │   │   └── Switch to Register Button
│   │   └── RegisterSection (Inactive at start)
│   │       ├── Name InputField
│   │       ├── Email InputField
│   │       ├── Password InputField
│   │       ├── Confirm Password InputField
│   │       ├── Register Button
│   │       └── Switch to Login Button
│   ├── SplashScreenPanel (Inactive at start)
│   │   └── Progress Slider
│   └── GamePanel (Inactive at start)
│       └── Your game UI here
└── AuthenticationManager (GameObject with script)
```

---

## 📝 Detailed Setup Instructions

### Step 1: Create Canvas
1. Right-click in Hierarchy
2. UI → Canvas
3. Set Canvas Scaler to "Scale With Screen Size"

---

### Step 2: Create Login/Register Panel

#### A. Main Panel
1. Right-click Canvas → UI → Panel
2. Rename to "LoginRegisterPanel"
3. Set to full screen (Anchor Presets: stretch/stretch)

#### B. Login Section
1. Right-click LoginRegisterPanel → Create Empty
2. Rename to "LoginSection"
3. Add these UI elements:

**Email Input:**
- Right-click LoginSection → UI → Input Field - TextMeshPro
- Rename: "LoginEmailInput"
- Placeholder: "Email"

**Password Input:**
- Right-click LoginSection → UI → Input Field - TextMeshPro
- Rename: "LoginPasswordInput"
- Placeholder: "Password"
- Content Type: Password

**Login Button:**
- Right-click LoginSection → UI → Button - TextMeshPro
- Rename: "LoginButton"
- Text: "LOGIN"

**Guest Login Button:**
- Right-click LoginSection → UI → Button - TextMeshPro
- Rename: "GuestLoginButton"
- Text: "PLAY AS GUEST"

**Switch Button:**
- Right-click LoginSection → UI → Button - TextMeshPro
- Rename: "SwitchToRegisterButton"
- Text: "Create Account"

#### C. Register Section
1. Right-click LoginRegisterPanel → Create Empty
2. Rename to "RegisterSection"
3. **Set to INACTIVE** (uncheck in Inspector)
4. Add these UI elements:

**Name Input:**
- UI → Input Field - TextMeshPro
- Rename: "RegisterNameInput"
- Placeholder: "Your Name"

**Email Input:**
- UI → Input Field - TextMeshPro
- Rename: "RegisterEmailInput"
- Placeholder: "Email"

**Password Input:**
- UI → Input Field - TextMeshPro
- Rename: "RegisterPasswordInput"
- Placeholder: "Password"
- Content Type: Password

**Confirm Password Input:**
- UI → Input Field - TextMeshPro
- Rename: "RegisterConfirmPasswordInput"
- Placeholder: "Confirm Password"
- Content Type: Password

**Register Button:**
- UI → Button - TextMeshPro
- Rename: "RegisterButton"
- Text: "CREATE ACCOUNT"

**Switch Button:**
- UI → Button - TextMeshPro
- Rename: "SwitchToLoginButton"
- Text: "Back to Login"

#### D. Status Text
- Right-click LoginRegisterPanel → UI → Text - TextMeshPro
- Rename: "StatusText"
- Position at bottom center
- Color: Red
- Alignment: Center

#### E. Loading Indicator (Optional)
- Right-click LoginRegisterPanel → UI → Image
- Rename: "LoadingIndicator"
- Add a loading spinner image

---

### Step 3: Create SplashScreen Panel

1. Right-click Canvas → UI → Panel
2. Rename to "SplashScreenPanel"
3. **Set to INACTIVE** (uncheck in Inspector)
4. Set to full screen

**Add Slider:**
- Right-click SplashScreenPanel → UI → Slider
- Rename: "ProgressSlider"
- Position at bottom center
- Min Value: 0
- Max Value: 1

**Optional: Add Logo/Text:**
- Add your game logo
- Add "Loading..." text

---

### Step 4: Create Game Panel

1. Right-click Canvas → UI → Panel
2. Rename to "GamePanel"
3. **Set to INACTIVE** (uncheck in Inspector)
4. Add your game UI here (buttons, score, etc.)

---

### Step 5: Setup AuthenticationManager

1. Right-click in Hierarchy → Create Empty
2. Rename to "AuthenticationManager"
3. Add Component → AuthenticationManager script

**Assign References in Inspector:**

**Panels:**
- Login Register Panel: Drag LoginRegisterPanel
- Splash Screen Panel: Drag SplashScreenPanel

**Login/Register Toggle:**
- Login Section: Drag LoginSection
- Register Section: Drag RegisterSection
- Switch To Register Button: Drag SwitchToRegisterButton
- Switch To Login Button: Drag SwitchToLoginButton

**Login UI:**
- Login Email Input: Drag LoginEmailInput
- Login Password Input: Drag LoginPasswordInput
- Login Button: Drag LoginButton
- Guest Login Button: Drag GuestLoginButton

**Registration UI:**
- Register Name Input: Drag RegisterNameInput
- Register Email Input: Drag RegisterEmailInput
- Register Password Input: Drag RegisterPasswordInput
- Register Confirm Password Input: Drag RegisterConfirmPasswordInput
- Register Button: Drag RegisterButton

**Status:**
- Status Text: Drag StatusText
- Loading Indicator: Drag LoadingIndicator (optional)

---

### Step 6: Setup SplashScreen

1. Select SplashScreenPanel
2. Add Component → SplashScreen script

**Assign References in Inspector:**
- Progress Slider: Drag the Slider from SplashScreenPanel
- Game Panel: Drag GamePanel
- Loading Duration: 3 (seconds)

---

### Step 7: PlayFab Configuration

1. Install PlayFab SDK (if not done):
   - Package Manager → Add from git URL
   - `https://github.com/PlayFab/UnitySDK.git`

2. Get Title ID:
   - Go to https://playfab.com
   - Create account & Title
   - Copy Title ID

3. Set in Unity:
   - Window → PlayFab → Editor Extensions
   - Paste Title ID → Set Title ID

---

## ✅ Final Checklist

- [ ] Canvas created with proper scaling
- [ ] LoginRegisterPanel active at start
- [ ] LoginSection active, RegisterSection inactive
- [ ] SplashScreenPanel inactive at start
- [ ] GamePanel inactive at start
- [ ] AuthenticationManager GameObject created
- [ ] All UI references assigned in AuthenticationManager
- [ ] SplashScreen script added to SplashScreenPanel
- [ ] Slider assigned to SplashScreen
- [ ] GamePanel assigned to SplashScreen
- [ ] PlayFab SDK installed
- [ ] PlayFab Title ID set
- [ ] Scene saved!

---

## 🎮 How It Works

1. **Scene Starts:**
   - LoginRegisterPanel is visible
   - User sees Login form

2. **User Actions:**
   - Can login with email/password
   - Can play as guest
   - Can switch to Register section

3. **After Successful Login/Register:**
   - LoginRegisterPanel hides
   - SplashScreenPanel shows
   - Progress bar fills

4. **After Loading Complete:**
   - SplashScreenPanel hides
   - GamePanel shows
   - Player can play!

---

## 🎨 Layout Tips

### Organize Login Section (Example Layout)
```
LoginSection (vertical layout)
├── Title Text ("Welcome to Dice Rush!")
├── Email Input
├── Password Input
├── Login Button
├── Guest Login Button
├── Divider
└── Switch to Register Button ("Create Account")
```

### Organize Register Section (Example Layout)
```
RegisterSection (vertical layout)
├── Title Text ("Create Account")
├── Name Input
├── Email Input
├── Password Input
├── Confirm Password Input
├── Register Button
└── Switch to Login Button ("Back to Login")
```

---

## 🎯 Testing Flow

1. **Press Play**
2. See Login screen first ✓
3. Click "Create Account"
4. Fill registration form
5. Click "CREATE ACCOUNT"
6. See SplashScreen with loading bar ✓
7. After loading, see GamePanel ✓

---

## 💡 Pro Tips

1. **Use Layout Groups** for automatic positioning:
   - Add Vertical Layout Group to LoginSection
   - Add Vertical Layout Group to RegisterSection

2. **Style consistently:**
   - Same colors for all buttons
   - Same font sizes
   - Proper spacing

3. **Add animations:**
   - Fade in/out panels
   - Button hover effects
   - Slider pulse effect

4. **Error handling:**
   - StatusText shows errors in red
   - Shows success messages in green

---

## 🔧 Common Issues

**Problem:** "Nothing happens when I press Play"
- Check: Is LoginRegisterPanel active?
- Check: Is AuthenticationManager in scene?

**Problem:** "Buttons don't work"
- Check: All buttons assigned in AuthenticationManager?
- Check: EventSystem exists in scene?

**Problem:** "Can't see Register section"
- Check: Switch buttons assigned?
- Check: RegisterSection exists?

**Problem:** "Stuck on login screen after successful login"
- Check: SplashScreenPanel assigned to AuthenticationManager?
- Check: SplashScreen script on SplashScreenPanel?

**Problem:** "Stuck on splash screen"
- Check: GamePanel assigned to SplashScreen script?

---

## 📱 Single Scene Benefits

✅ No scene loading delays  
✅ Smoother transitions  
✅ Better for mobile  
✅ Easier to manage  
✅ All in one place  

---

**You now have everything in ONE scene!** 🎉

Flow: **Login/Register → SplashScreen → Game**

