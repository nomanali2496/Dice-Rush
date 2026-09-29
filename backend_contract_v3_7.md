# Dice Rush: Backend & Real-time Contract (v3.7)

This document outlines the complete technical contract for the **Dice Rush** backend, including RESTful API endpoints and Socket.io event signatures.

---

## 1. Core Authentication

All protected routes require a Bearer token in the `Authorization` header.

**Header Format:**
```http
Authorization: Bearer <your_jwt_token>
```

**Socket.io Auth:**
Pass token via `auth` handshake:
```javascript
const socket = io(socketUrl, { 
  auth: { token: '...' } 
});
```

---

## 2. REST API Endpoints

### 2.1 User Authentication (`/api/auth`)
| Method | Endpoint | Description | Payload Example |
| :--- | :--- | :--- | :--- |
| `POST` | `/register` | Create a new user | `{ "username": "...", "email": "...", "password": "..." }` |
| `POST` | `/login` | Standard login | `{ "email": "...", "password": "..." }` |
| `POST` | `/google` | Google Social Auth | `{ "token": "..." }` |
| `POST` | `/facebook` | Facebook Social Auth | `{ "token": "..." }` |
| `POST` | `/discord` | Discord OAuth2 login (code exchange) | `{ "code": "...", "redirectUri": "..." }` |
| `POST` | `/discord/link` | Link Discord to the current account, import name/avatar | `{ "code": "...", "redirectUri": "..." }` — *(Requires Auth)* |
| `POST` | `/guest-login` | Anonymous guest access | `{ "deviceId": "..." }` |
| `POST` | `/forgot-password` | Request password reset email | `{ "email": "..." }` |
| `POST` | `/reset-password` | Reset password with token | `{ "token": "...", "newPassword": "..." }` |
| `GET` | `/me` | Get current user profile (live active/recent match) | *(Requires Auth)* |
| `POST` | `/logout` | Logout — invalidates server-side Redis cache | *(Requires Auth)* |
| `PATCH` | `/profile` | Update profile details | `{ "name": "...", "avatar": "...", "profileFrame": "..." }` |
| `POST` | `/upload-avatar`| Upload & process custom avatar to S3 | `multipart/form-data` (field: `avatar`) — *(Requires Auth)* |
| `DELETE` | `/avatar` ⭐ NEW in v3.1 | Remove custom avatar, reset to default preset | *(no body)* — *(Requires Auth)* |

**POST /upload-avatar Request:**
```
Content-Type: multipart/form-data
Authorization: Bearer <token>

Field name : "avatar"
File types : image/jpeg, image/jpg, image/png, image/webp
Max size   : 5 MB
```

**POST /upload-avatar Response:**
```json
{
  "success": true,
  "avatarUrl": "https://game.winjogames.com/avatars/<userId>-<ts>.webp",
  "user": {
    "id": "...",
    "name": "Alice",
    "avatar": "https://game.winjogames.com/avatars/<userId>-<ts>.webp",
    "cashBalance": 100.00
  }
}
```
> The server processes the image to a **512×512 circular PNG** (via sharp) before uploading to S3. The `avatarUrl` field is the public CDN URL. Unity should cache this URL and display it as a circular image. The `user.avatar` field in all subsequent API and socket responses reflects this URL.

**Avatar field interpretation:**
| Value | Meaning | How to display |
| :--- | :--- | :--- |
| `"avatarBoy"` | Built-in preset | Use local asset `avatarBoy` |
| `"avatarGirl"` | Built-in preset | Use local asset `avatarGirl` |
| `"https://..."` | Uploaded photo | Download and display as circular image |

> Rule: if `avatar.startsWith("https://")` → download URL. Otherwise → local preset asset.

**DELETE /avatar Response:** ⭐ NEW in v3.1
```json
{ "success": true, "message": "Avatar reset to default", "user": { "...": "same shape as /me" } }
```
> Resets `avatar` to `"avatarBoy"`. If the player had an uploaded photo, the old S3 object is deleted (best-effort — never blocks the response). Switching avatars via `PATCH /profile` or re-uploading via `POST /upload-avatar` also now deletes the previous uploaded photo from S3, so old avatars no longer accumulate as orphaned storage.

**POST /discord Request:**
```json
{ "code": "<oauth2-authorization-code>", "redirectUri": "https://game.winjogames.com/discord-callback" }
```
> `code` comes from Discord's OAuth2 consent screen (scopes `identify email`); `redirectUri` must exactly match the one used to obtain that code, per the OAuth2 spec. The server exchanges it server-side for an access token (using the app's client secret — never exposed to the client) and fetches the profile from Discord itself, so nothing about the identity is trusted from the client. Response shape is identical to `/google`/`/facebook` (`{ success, token, user }`). If no account exists with that Discord ID, one is either linked onto a matching **Discord-verified** email or created fresh.

**POST /discord/link Request/Response:**
```json
// Request
{ "code": "...", "redirectUri": "..." }
// Response
{ "success": true, "message": "Discord account linked successfully", "user": { "...": "same shape as /me" } }
```
> Attaches a Discord identity to the currently authenticated session (guest, email, or another provider) regardless of email match, and imports the Discord display name + avatar onto the profile. Returns `409` if that Discord account is already linked to a different player.

**GET /me Response:**
```json
{
  "success": true,
  "user": {
    "id": "664abc...",
    "name": "Alice",
    "email": "alice@example.com",
    "avatar": "avatarBoy",
    "profileFrame": "frame_2",
    "wins": 12,
    "losses": 4,
    "balance": 100.00,
    "cashBalance": 80.00,
    "bonusBalance": 20.00,
    "winningBalance": 0.00,
    "withdrawableBalance": 0.00,
    "coins": 150,
    "xp": 2400,
    "level": 5,
    "role": "user",
    "country": "Pakistan",
    "league": "Gold",
    "loginStreak": 56,
    "gamesPlayed": 337,
    "totalScore": 1245200,
    "highScore": 4200,
    "totalPlayTimeSeconds": 187200,
    "winStreak": 12,
    "bestStreak": 18,
    "likesReceived": 10000,
    "totalCreditsEarned": 5230,
    "discordLinked": false,
    "memberSince": "2024-05-12T00:00:00.000Z",
    "activeMatchId": "664def...",
    "activeRoomId": "match-1716000000-123",
    "recentMatch": {
      "matchId": "664abc...",
      "roomId": "match-1715900000-456",
      "status": "finished"
    }
  }
}
```
> **`activeMatchId` / `activeRoomId`**: Only present when the user has an ongoing match. These are **live-queried from the Match collection** on every `/me` call — not stored on the user document. Safe to use directly for reconnect decisions without staleness risk.
>
> **`recentMatch`**: Always the most recent finished match regardless of when it ended. `status` will be `"finished"` or `"cancelled"`. Use `recentMatchId` to display the last result screen. Absent if the user has never played.
>
> **`withdrawableBalance`**: Always equals `winningBalance`. This is the amount the user can request to withdraw. Only `winningBalance` (prize earnings) is withdrawable — `cashBalance` and `bonusBalance` are not.

**POST /logout Response:**
```json
{ "success": true }
```
> JWT tokens are stateless; the client must also clear `token` and `user` from localStorage and disconnect the socket. The server invalidates the Redis profile cache.

**POST /forgot-password Request:**
```json
{
  "email": "user@example.com"
}
```

**POST /forgot-password Response:**
```json
{
  "success": true,
  "message": "If this email is registered, you will receive a password reset link shortly."
}
```
> **Security**: Always returns success, even if email not registered (prevent user enumeration). User receives email with reset link valid for 1 hour. **Rate limit**: Max 5 requests per email per 24 hours; 6th request returns HTTP 429.

**POST /reset-password Request:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "newPassword": "newSecurePassword123"
}
```

**POST /reset-password Response:**
```json
{
  "success": true,
  "message": "Password reset successfully. You can now login with your new password."
}
```
> **Behavior**: Validates reset token (must not be expired or used before). Updates password, **invalidates all active sessions** (force re-login on all devices). Clears login attempt counter and account lockout. Sends confirmation email. Returns 400 if token invalid/expired, password too short, or same as old password.

### 2.2 Wallet & Transactions (`/api/wallet`)
| Method | Endpoint | Description | Payload Example |
| :--- | :--- | :--- | :--- |
| `GET` | `/balance` | Lightweight balance snapshot (for polling) | *(Requires Auth)* |
| `GET` | `/withdrawal-info` | Withdrawal limits, fee tiers, optional quote | `?amount=50` *(Requires Auth)* |
| `GET` | `/history` | Paginated transaction history | `?page=1&limit=20` *(Requires Auth)* |
| `POST` | `/deposit` | Create Stripe Checkout Session for plain deposit | `{ "amount": 10.00, "returnUrl": "blingodice://open/payment-success" }` |
| `POST` | `/withdraw` | Request cash withdrawal | `{ "amount": 25.00 }` |
| `POST` | `/sync-deposit/:id`| Sync manual payment (dev only) | `id` = transaction reference |

> ⚠️ **Breaking change from v2.6**: `POST /deposit` no longer returns `clientSecret` / `paymentIntentId`. It now returns `checkoutUrl` — a Stripe-hosted payment page URL. Unity must open this URL in a browser (see Section 2.10).

**POST /deposit Request:**
```json
{
  "amount": 10.00,
  "returnUrl": "blingodice://open/payment-success"
}
```
> `amount`: USD amount to deposit ($1.00–$500.00). `returnUrl`: deep link to return to app after payment (must start with `blingodice://` or `https://game.winjogames.com/`).

**POST /deposit Response:**
```json
{
  "success": true,
  "checkoutUrl": "https://checkout.stripe.com/pay/cs_test_...",
  "sessionId": "cs_test_..."
}
```
> Open `checkoutUrl` in Chrome using `Application.OpenURL(checkoutUrl)`. Stripe hosts the card form. After payment, Stripe redirects to `https://game.winjogames.com/payment-success?session_id=...&returnUrl=blingodice://open/payment-success`. Unity receives `blingodice://open/payment-success` via deep link — close the browser tab, then call `GET /api/wallet/balance` to refresh displayed balance (allow 2s for webhook to credit).

**GET /balance Response:**
```json
{
  "success": true,
  "data": {
    "coins": 160,
    "balance": 80.00,
    "cashBalance": 60.00,
    "bonusBalance": 20.00,
    "winningBalance": 0.00
  }
}
```
> Intended for polling after a rewarded ad completes. Hits DB with a single indexed `_id` query — cheap to call every 2 seconds. Compare `coins` before and after showing the ad to detect when the S2S reward has been credited.

**POST /withdraw Notes:**
> - Only `winningBalance` (= `withdrawableBalance`) can be withdrawn. Attempting to withdraw more than `winningBalance` returns HTTP 400.
> - **No KYC required.** Withdrawals go directly to `pending` status for admin review.
> - **Flow**: User requests → status `pending` → Admin approves (Stripe payout sent, status → `completed`) or rejects (funds returned to `winningBalance`, status → `failed`).
> - The `method` field is optional and informational only (not processed by the server).

**POST /deposit Notes:**
> - Accepts optional `bundleId` (for shop bundles) or `offerId` (for daily offers).
> - If `bundleId` or `offerId` is provided, the backend will validate the `amount` against the hardcoded price.
> - Upon successful fulfillment, the backend will automatically grant the corresponding base cash, bonus cash, and coins.
> - For daily offers, the claimed `offerId` will be automatically removed from the user's active offers list.


**GET /history Response:**
```json
{
  "success": true,
  "count": 20,
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 87,
    "pages": 5
  },
  "data": [
    {
      "id": "664abc...",
      "type": "entry_fee",
      "amount": -10.00,
      "balanceType": "cash",
      "status": "completed",
      "description": "Entry fee",
      "referenceId": "match-1716000000-123",
      "date": "2026-05-18T10:00:00.000Z"
    },
    {
      "id": "664def...",
      "type": "win_prize",
      "amount": 25.00,
      "balanceType": "winning",
      "status": "completed",
      "description": "Win Prize",
      "referenceId": "664matchId...",
      "date": "2026-05-18T10:02:30.000Z"
    }
  ]
}
```

**Transaction `type` values:**
| Value | Description |
| :--- | :--- |
| `entry_fee` | Match entry debit (negative amount) |
| `win_prize` | Prize credited after placing 1st/2nd/3rd |
| `refund` | Entry fee refunded (match cancelled/failed) |
| `deposit` | Player topped up via Stripe |
| `withdrawal` | Player cashed out |
| `bonus` | Promotional or daily reward credit |

**Transaction `status` values:**
| Value | Description |
| :--- | :--- |
| `completed` | Transaction successfully processed |
| `pending` | Awaiting processing (e.g. withdrawal under review) |
| `failed` | Transaction failed |
| `reversed` | Transaction was reversed/rolled back |

**Transaction `balanceType` values:**
| Value | Description |
| :--- | :--- |
| `cash` | Real deposited cash |
| `bonus` | Promotional balance (play-only) |
| `winning` | Earnings from match prizes |
| `coins` | In-game coin currency |

#### Withdrawal rules ⭐ NEW in v3.0

**Withdrawable Amount = `cashBalance` (Deposit) + `winningBalance`.** Bonus is never withdrawable.

Three rules now apply to every withdrawal — all enforced server-side, so the client must
surface them but cannot bypass them:

| Rule | Value |
| :--- | :--- |
| Minimum withdrawal | **$10** |
| Maximum withdrawal | **$300** |
| Fee: $0–$20 | **$1** |
| Fee: $21–$100 | **$4** |
| Fee: $100+ | **$10** |

- **The fee is withheld from the requested amount.** Requesting $10 debits $10 from the
  wallet and pays out **$9**; requesting $50 debits $50 and pays out **$46**. The fee is
  never added on top, so a player can never be charged more than they asked to withdraw.
- **The bonus balance is not affected by a withdrawal.** Bonus is excluded from
  `withdrawableBalance` in the first place, so a withdrawal can never pay out promotional
  funds and nothing is clawed back.

**GET /withdrawal-info Response** (`?amount=50` optional — omit for limits only):
```json
{
  "success": true,
  "data": {
    "minAmount": 10,
    "maxAmount": 300,
    "feeTiers": [
      { "upTo": 20,   "fee": 1 },
      { "upTo": 100,  "fee": 4 },
      { "upTo": null, "fee": 10 }
    ],
    "withdrawableBalance": 200,
    "bonusBalance": 3,
    "quote": {
      "valid": true,
      "amount": 50,
      "fee": 4,
      "receives": 46
    }
  }
}
```
> `feeTiers` is ordered ascending; `upTo: null` is the open-ended top tier. Drive the UI
> from these values rather than hardcoding — they are configurable server-side.
> When `?amount=` is supplied, `quote` previews that exact request using the same code the
> withdrawal itself runs. An invalid amount returns `{ "valid": false, "reason": "..." }`
> instead of the quote fields — show `reason` directly to the player.

**POST /withdraw Request:**
```json
{ "amount": 50.00 }
```

**POST /withdraw Response** (`data` is the pending transaction):
```json
{
  "success": true,
  "message": "Withdrawal request submitted and pending review.",
  "data": {
    "amount": -50,
    "withdrawalFee": 4,
    "netAmount": 46,
    "status": "pending",
    "type": "withdrawal"
  }
}
```
> Returns **400** with the reason in `error` when the amount is below the minimum, above the
> maximum, or exceeds `withdrawableBalance`. `netAmount` is what the player actually receives
> (`amount - withdrawalFee`); `amount` is what left their wallet. Transaction history rows for withdrawals carry the same
> `withdrawalFee` / `netAmount` fields (`null` for all other transaction types).

### 2.3 Shop & In-App Purchases (`/api/shop`) ⭐ NEW in v2.7

> **Replaces Unity IAP (InAppPurchaseManager.cs)**. All purchases go through our backend + Stripe Checkout. Unity IAP (`UnityEngine.Purchasing`) is no longer used for these items.

| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/shop/items` | Get all enabled shop items (offers + lucky deals) | Required |
| `POST` | `/shop/checkout` | Create Stripe Checkout Session for a shop item | Required |

---

#### GET /shop/items

Returns all enabled shop items grouped by type. Call this when the shop or offers screen opens to get current prices and rewards (admin can change these without an app update).

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "itemId": "offer_quick_boost",
      "type": "offer",
      "name": "Quick Boost",
      "price": 3.00,
      "awards": {
        "cashBalance": 6.00,
        "bonusBalance": 1.20,
        "coins": 120
      },
      "discountLabel": "20% Extra",
      "tag": "Best Deal",
      "enabled": true,
      "order": 1
    },
    {
      "itemId": "package_1",
      "type": "lucky_deal",
      "name": "50 Tokens",
      "price": 3.00,
      "awards": {
        "cashBalance": 3.00,
        "bonusBalance": 0.20,
        "coins": 50
      },
      "discountLabel": "",
      "tag": "",
      "enabled": true,
      "order": 1
    }
  ]
}
```

**Item types:**
| `type` | Screen | Description |
| :--- | :--- | :--- |
| `offer` | "All Offers" carousel | Bundle deals — pay less, get more cash + bonus + coins |
| `lucky_deal` | "Lucky Deals" grid | Deposit packages — cash credited in full + bonus tokens |

---

#### POST /shop/checkout

Call when user taps a "Buy" button. Returns a Stripe-hosted checkout URL. Unity opens it in Chrome.

**Request:**
```json
{
  "itemId": "offer_quick_boost",
  "returnUrl": "blingodice://open/payment-success"
}
```
> `itemId` must match one of the `itemId` values from `GET /shop/items` (same as Unity IAP product IDs). `returnUrl` must start with `blingodice://` or `https://game.winjogames.com/`.

**Response:**
```json
{
  "success": true,
  "checkoutUrl": "https://checkout.stripe.com/pay/cs_test_...",
  "sessionId": "cs_test_..."
}
```
> Open `checkoutUrl` with `Application.OpenURL(checkoutUrl)`. After payment, Stripe redirects to our success page which fires `blingodice://open/payment-success`. Unity receives this via deep link — close browser, call `GET /api/wallet/balance`.

---

#### Shop Item IDs (match Unity IAP Catalog product IDs exactly)

**Offers** (`type: "offer"`) — shown in "All Offers" carousel:

| `itemId` | Name | Price | Cash | Bonus | Coins | Tag |
| :--- | :--- | ---: | ---: | ---: | ---: | :--- |
| `offer_quick_boost` | Quick Boost | $3 | $6 | $1.20 | 120 | Best Deal |
| `offer_premium_value` | Premium Value | $17 | $17 | $4.20 | 220 | Special for You |
| `offer_smart_starter` | Smart Starter | $12 | $12 | $2.40 | 160 | Act Now |
| `offer_bonus_plus` | Bonus Plus | $4 | $4 | $1.20 | 52 | Best Deal |
| `offer_power_deal` | Power Deal | $5 | $5 | $2.50 | 70 | Limited Time |
| `offer_best_offers` | Best Offer | $6 | $3 | $1.20 | 140 | Claim It |
| `offer_mega_bundle` | Mega Bundle | $8 | $8 | $3.20 | 80 | Unlock the Deal |

**Lucky Deals** (`type: "lucky_deal"`) — shown in "Lucky Deals" grid:

| `itemId` | Name | Price | Cash | Bonus | Coins |
| :--- | :--- | ---: | ---: | ---: | ---: |
| `package_1` | 50 Tokens | $3 | $3 | $0.20 | 50 |
| `package_2` | 200 Tokens | $10 | $10 | $2.00 | 200 |
| `package_3` | 350 Tokens | $15 | $15 | $2.00 | 350 |
| `package_4` | 400 Tokens | $30 | $30 | $6.00 | 400 |
| `package_5` | 1000 Tokens | $50 | $50 | $10.00 | 1000 |
| `package_6` | 1200 Tokens | $75 | $75 | $25.00 | 1200 |

> **Awards** are credited automatically by the backend webhook when Stripe confirms payment — no `SyncDeposit` call needed. `cashBalance`, `bonusBalance`, and `coins` are all credited atomically.

---

#### Full Purchase Flow (replaces InAppPurchaseManager.cs)

```
1. [Shop screen opens]
   Unity → GET /api/shop/items
   Display items from response (prices/rewards are live from server)

2. [User taps Buy on an item]
   Unity → POST /api/shop/checkout { itemId, returnUrl: "blingodice://open/payment-success" }
   Response → { checkoutUrl }

3. [Open payment page]
   Application.OpenURL(checkoutUrl)
   Chrome opens → Stripe hosted checkout → user enters card → pays

4. [Payment completes]
   Stripe → redirects browser to https://game.winjogames.com/payment-success
   Our page → attempts window.location.href = "blingodice://open/payment-success"
   OS → intercepts deep link → returns focus to Unity app

5. [Unity receives deep link]
   Application.deepLinkActivated event fires with "blingodice://open/payment-success"
   → Show "Purchase successful!" toast
   → Wait 2 seconds (allow webhook to credit wallet)
   → Call GET /api/wallet/balance to refresh displayed balance
   → Update coins/cashBalance/bonusBalance in UI

6. [Background: Stripe webhook]
   Stripe → POST /api/wallet/webhook (checkout.session.completed)
   Backend → reads metadata.itemId + metadata.userId
   Backend → credits cashBalance + bonusBalance + coins atomically
```

**If payment is cancelled** (user taps Back on Stripe page):
- Stripe redirects to `https://game.winjogames.com/payment-cancel`
- Our page fires `blingodice://open/payment-cancel` deep link
- Unity receives it → show "Payment cancelled" message, no balance change

**Deep link URL scheme:**
```
blingodice://open/payment-success   ← payment completed
blingodice://open/payment-cancel    ← user cancelled (no charge)
```

**Platform setup required:**

*Android — AndroidManifest.xml* (already configured — host must be `"open"`):
```xml
<intent-filter>
    <data android:scheme="blingodice" android:host="open" />
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
</intent-filter>
```
> The payment page tries `blingodice://open/payment-success` first, then falls back to `intent://open/payment-success#Intent;scheme=blingodice;package=com.winjogames.blingodice;end` for Chrome on Android.

*iOS — Info.plist* (add if not already present):
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>blingodice</string>
        </array>
        <key>CFBundleURLName</key>
        <string>com.winjogames.blingodice</string>
    </dict>
</array>
```
> On iOS, Safari intercepts `blingodice://open/payment-success` directly via `window.location.href` — no special format needed. The Intent URL fallback is Android-only and is ignored by iOS.

**Deep link handling in Unity:**
```csharp
void Start()
{
    Application.deepLinkActivated += OnDeepLinkActivated;
    // Handle cold start (app opened via deep link while it was closed)
    if (!string.IsNullOrEmpty(Application.absoluteURL))
        OnDeepLinkActivated(Application.absoluteURL);
}

void OnDeepLinkActivated(string url)
{
    Debug.Log("[DeepLink] Received: " + url);

    if (url.Contains("open/payment-success"))
    {
        ShowToast("Payment successful! Updating wallet...");
        StartCoroutine(RefreshBalanceAfterPurchase());
    }
    else if (url.Contains("open/payment-cancel"))
    {
        ShowToast("Payment cancelled");
    }
}

IEnumerator RefreshBalanceAfterPurchase()
{
    // Wait 2s for Stripe webhook to credit the wallet on the backend
    yield return new WaitForSeconds(2f);
    BackendAuthService.Instance.GetBalance((success, balance) =>
    {
        if (success)
        {
            PlayerModel.UpdateBalance(balance);
            CurrencyDisplayUI.TriggerVisualAnimationTick(balance.coins, balance.cashBalance);
        }
    });
}
```

> **Remove from InAppPurchaseManager.cs**: `TriggerLocalCheckoutFlow`, `SimulateLocalPaymentCoroutine`, `BackendAuthService.Instance.SyncDeposit(...)`. The entire simulate-then-sync flow is replaced by the webview flow above. `BackendAuthService.Instance.Deposit(...)` is also replaced by `POST /api/shop/checkout`.

---

### 2.4 Support Tickets (`/api/support-tickets`) ⭐ NEW in v2.9

User support ticket submission and admin management system.

**User Endpoints:**
| Method | Endpoint | Description | Auth | Rate Limit |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/` | Create support ticket | Rate Limited | 3 per email per 24h |
| `GET` | `/` | Get user's tickets | Required | — |
| `GET` | `/:id` | Get single ticket | Required | — |

**Admin Endpoints:**
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/admin/tickets` | List all tickets (filterable) | Bearer + Admin |
| `PATCH` | `/:id/reply` | Add reply to ticket | Bearer + Admin |
| `POST` | `/:id/close` | Close ticket | Bearer + Admin |

---

#### POST /api/support-tickets - Create Ticket

**Request:**
```json
{
  "email": "user@example.com",
  "subject": "Cannot withdraw funds",
  "description": "I've been trying to withdraw my winnings for 2 days but it keeps failing with an error code",
  "category": "payment"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Support ticket created. You will receive a confirmation email shortly.",
  "data": {
    "ticketId": "507f1f77bcf86cd799439011",
    "status": "open",
    "category": "payment"
  }
}
```

**Categories:** `technical`, `payment`, `account`, `complaint`, `other`

**Rate Limit:** Max 3 tickets per email per 24 hours (returns 429 if exceeded)

---

#### GET /api/support-tickets - User's Tickets

Returns all tickets created by the authenticated user.

**Response:**
```json
{
  "success": true,
  "count": 2,
  "data": [
    {
      "_id": "507f1f77bcf86cd799439011",
      "userId": "user123",
      "userEmail": "user@example.com",
      "userName": "John Doe",
      "subject": "Cannot withdraw funds",
      "description": "...",
      "category": "payment",
      "status": "in_progress",
      "priority": "medium",
      "replies": [
        {
          "userId": "admin456",
          "userName": "Support Admin",
          "message": "We've reviewed your account. The issue was...",
          "isAdmin": true,
          "createdAt": "2026-06-15T14:30:00Z"
        }
      ],
      "createdAt": "2026-06-15T10:00:00Z",
      "updatedAt": "2026-06-15T14:30:00Z"
    }
  ]
}
```

---

#### GET /api/admin/support-tickets - Admin List

Returns all tickets with optional filtering. Admin only.

**Query params:**
- `?status=open` — Filter by status (`open`, `in_progress`, `resolved`, `closed`)
- `?priority=high` — Filter by priority (`low`, `medium`, `high`)

**Response:**
```json
{
  "success": true,
  "count": 5,
  "data": [
    {
      "_id": "507f1f77bcf86cd799439011",
      "userId": "user123",
      "userEmail": "user@example.com",
      "userName": "John Doe",
      "userAvatar": "avatarBoy",
      "subject": "Cannot withdraw funds",
      "category": "payment",
      "status": "open",
      "priority": "high",
      "replyCount": 0,
      "createdAt": "2026-06-15T10:00:00Z"
    }
  ]
}
```

---

#### PATCH /api/admin/support-tickets/:id/reply - Add Reply

Admin adds a reply to a ticket. Automatically sends email notification to user.

**Request:**
```json
{
  "message": "Thank you for contacting us. We've reviewed your case and found the issue. Here's the solution..."
}
```

**Response:**
```json
{
  "success": true,
  "message": "Reply added and user notified",
  "data": {
    "_id": "507f1f77bcf86cd799439011",
    "status": "in_progress",
    "replies": [
      {
        "userId": "admin456",
        "userName": "John Support",
        "message": "Thank you for...",
        "isAdmin": true,
        "createdAt": "2026-06-15T14:30:00Z"
      }
    ]
  }
}
```

**Side Effects:**
- Ticket status auto-changes from `open` → `in_progress` (if open)
- Email sent to user: "Your Support Ticket Has Been Updated"
- Audit log created

---

#### POST /api/admin/support-tickets/:id/close - Close Ticket

**Response:**
```json
{
  "success": true,
  "message": "Ticket closed",
  "data": {
    "_id": "507f1f77bcf86cd799439011",
    "status": "closed"
  }
}
```

---

### 2.5 Leaderboard (`/api/leaderboard`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/` | Leaderboard for a period/category, plus caller's rank | Required |

**Query params:**
- `limit` — max 100, default 50
- `period` ⭐ **NEW in v3.2** — `weekly` \| `monthly` \| `all-time` (default `all-time`). Weekly resets Monday 00:00 server-local time (same boundary as weekly tasks); monthly resets the 1st.
- `category` ⭐ **NEW in v3.2** — `wins` (default) \| `matches_played` \| `credits_earned` \| `current_credits`

> **v3.2**: `period`/`category` are additive — a request with neither (what every existing caller sends today) behaves exactly as before: all-time wins, with `wins`/`losses` on every entry and on `me`. `LeaderboardSnapshot` (a frozen-per-period-rollup model) exists in the schema but its rollover worker was never built; weekly/monthly here are computed live from `Match`/`Transaction` data bounded by the period's start date, not from a snapshot. This is accurate for the current, in-progress period; once match/transaction history ages past the retention window (see `archivalService`), a *closed* historical week/month may no longer be reconstructable this way — durable historical leaderboards still need the originally-planned snapshot worker.

**GET /leaderboard Response (default: period=all-time, category=wins):**
```json
{
  "success": true,
  "period": "all-time",
  "category": "wins",
  "me": {
    "rank": 47,
    "userId": "664abc...",
    "name": "Guest 7159",
    "avatar": "avatarBoy",
    "profileFrame": "",
    "value": 0,
    "wins": 0,
    "losses": 2
  },
  "leaderboard": [
    {
      "rank": 1,
      "userId": "664def...",
      "name": "Player012",
      "avatar": "https://game.winjogames.com/avatars/...",
      "profileFrame": "frame_5",
      "value": 9932,
      "wins": 9932,
      "losses": 12
    },
    {
      "rank": 2,
      "userId": "664ghi...",
      "name": "Player050",
      "avatar": "avatarBoy",
      "profileFrame": "",
      "value": 9463,
      "wins": 9463,
      "losses": 34
    }
  ]
}
```
> `me` always reflects the calling user's actual rank for the requested period/category, even if they are outside the top N (computed directly, not by scanning the visible page). `value` is the generic ranked metric for any category — present on every entry. `wins`/`losses` are additionally included only when `category=wins`, for existing callers. For `category=matches_played` / `credits_earned` / `current_credits`, only `value` is meaningful (there's no `wins`/`losses` concept). `current_credits` always reports `period: "all-time"` regardless of the requested period — it's a live wallet snapshot, not something accumulated over time. Avatar follows the same display rule: `startsWith("https://")` → download; otherwise → local preset. `profileFrame` ⭐ **NEW in v3.7** is an opaque client-defined frame id (empty string = no frame), present on `me` and every `leaderboard` entry.

**GET /leaderboard?period=weekly&category=matches_played Response:**
```json
{
  "success": true,
  "period": "weekly",
  "category": "matches_played",
  "me": { "rank": 12, "userId": "664abc...", "name": "Guest 7159", "avatar": "avatarBoy", "profileFrame": "", "value": 6 },
  "leaderboard": [
    { "rank": 1, "userId": "664def...", "name": "Player012", "avatar": "avatarBoy", "profileFrame": "frame_5", "value": 41 }
  ]
}
```

---

### 2.4 Invite System (`/api/invites`) ⭐ NEW
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `POST` | `/invitedby` | Assign inviter — call once on first app open via invite link | Required |
| `GET` | `/history` | List of users this user has invited, with online status | Required |

**POST /invites/invitedby Request:**
```json
{ "inviterId": "<userId from Branch.io deep link>" }
```

**POST /invites/invitedby Response:**
```json
{
  "success": true,
  "alreadyAssigned": false
}
```
> Call this on every cold start if Branch.io provides an `inviterId`. The server performs a **one-time assignment only** — if `invitedBy` is already set, the request is silently ignored (`alreadyAssigned: true`). Safe to call on every launch.
>
> **Integration flow:**
> 1. User A shares invite link (Branch.io encodes User A's `userId`)
> 2. User B opens link → installs app → Branch.io SDK delivers User A's `userId` to the client
> 3. Client calls `POST /api/invites/invitedby` with `{ inviterId: userAId }` after login
> 4. Server assigns `invitedBy = userAId` on User B's profile (only if not already set)
> 5. ⭐ **NEW in v3.1** — the first time this assignment succeeds, User A (the inviter) is credited `SystemConfig.economy.referralBonus` to their bonus balance. Idempotent per (inviter, invitee) pair, so it cannot be paid twice even if this endpoint is called repeatedly.

**GET /invites/history Response:**
```json
{
  "success": true,
  "count": 3,
  "data": [
    {
      "userId": "664abc...",
      "name": "Nasir",
      "avatar": "avatarBoy",
      "wins": 5,
      "losses": 2,
      "isOnline": true,
      "joinedAt": "2026-05-10T12:00:00.000Z"
    },
    {
      "userId": "664def...",
      "name": "Guest 4421",
      "avatar": "https://game.winjogames.com/avatars/...",
      "wins": 0,
      "losses": 1,
      "isOnline": false,
      "joinedAt": "2026-05-15T08:30:00.000Z"
    }
  ]
}
```
> `isOnline` is live from Redis — set when the user connects a socket, cleared on disconnect (with 5-minute TTL safety net). `joinedAt` is the account creation timestamp.

---

### 2.4b Player Profile (`/api/profile`) ⭐ NEW in v3.1
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/:userId` | Public profile card — works for your own ID or anyone else's | Required |
| `POST` | `/:userId/like` | Like a player's profile (once per viewer, deduplicated) | Required |

> This is the **public** profile view (self or another player) — no email or wallet/balance data, which stays private to `GET /api/auth/me`. `GET /api/auth/me` was also extended in v3.1 with the same stat fields (see its response example above) for the authenticated player's own full profile.

**GET /profile/:userId Response:**
```json
{
  "success": true,
  "data": {
    "id": "664abc...",
    "name": "Omega Shenron",
    "avatar": "avatarBoy",
    "profileFrame": "frame_2",
    "country": "Pakistan",
    "league": "Gold",
    "level": 12,
    "xp": 3400,
    "wins": 235,
    "losses": 102,
    "gamesPlayed": 337,
    "winRate": 70,
    "winStreak": 12,
    "bestStreak": 18,
    "totalScore": 1245200,
    "highScore": 4200,
    "totalPlayTimeSeconds": 187200,
    "loginStreak": 56,
    "likesReceived": 10000,
    "rank": 2314,
    "memberSince": "2024-05-12T00:00:00.000Z"
  }
}
```
> `rank` uses the same "count of users with strictly more wins, +1" convention as `GET /api/leaderboard`. `gamesPlayed`/`totalScore`/`highScore`/`totalPlayTimeSeconds`/`winStreak`(`currentStreak`)/`bestStreak` are live counters updated atomically from `matchService`/`settlementWorker` whenever a match finishes — never trust these from the client.

**POST /profile/:userId/like Response:**
```json
{ "success": true, "alreadyLiked": false, "likesReceived": 10001 }
```
> `400` if you try to like your own profile. A repeat like from the same viewer is a no-op (`alreadyLiked: true`) — it does not increment `likesReceived` again.

---

### 2.5 Reward Systems (`/api/rewards` & `/api/offers`)
| Method | Endpoint | Description | Notes |
| :--- | :--- | :--- | :--- |
| `GET` | `/rewards/status`| 7-day streak status | Returns current day & claim status |
| `POST` | `/rewards/claim` | Claim daily reward | Credits user wallet |
| `GET` | `/rewards/ad-complete` | LevelPlay S2S reward callback | **Server-to-server only** — called by IronSource, not the Unity client |
| `GET` | `/offers/daily` | Get 3 random offers | Refreshes every 24 hours |
| `POST` | `/offers/claim` | Claim an offer | Requires `offerId` |

**GET /rewards/ad-complete — LevelPlay Server-Side Rewarding (S2S)**

> **Not called by Unity directly.** Configure this URL in the LevelPlay dashboard under your app's rewarded ad unit → S2S Reward Callback.

**Callback URL to enter in LevelPlay dashboard:**
```
https://api.winjogames.com/api/rewards/ad-complete?appUserId=[USER_ID]&rewards=[REWARDS]&eventId=[EVENT_ID]
```
> LevelPlay substitutes `[USER_ID]`, `[REWARDS]`, `[EVENT_ID]` before calling your server, and appends `&signature=<md5>` automatically.

**Signature verification:** `MD5(appUserId + rewards + eventId + IRONSOURCE_PRIVATE_KEY)`

**Security layers:**
| Layer | Detail |
| :--- | :--- |
| Signature | MD5 of all params + private key — client cannot forge without the key |
| ObjectId validation | `appUserId` must be a valid 24-char hex MongoDB ID |
| Idempotency | `eventId` used as idempotency key — LevelPlay retries are safe, no double-credit |
| Null userId guard | Literal `"null"` / `""` / `"undefined"` rejected with 200 (stops retry loop) |

**Unity SDK setup** — must call `setUserId` before loading any ad:
```csharp
IronSource.Agent.setUserId(userId); // MongoDB _id string
IronSource.Agent.init("YOUR_APP_KEY");
IronSource.Agent.loadRewardedVideo(); // only after init
```

**How Unity knows reward was credited** — poll `GET /api/wallet/balance` every 2s after SDK fires `onRewardedVideoAdRewarded`:
```
Ad completes (SDK callback) → snapshot coins → poll /balance every 2s
→ coins increased? → show "+N coins!" animation → stop polling
→ 15s timeout? → show "Reward arriving soon" toast (coins still come, just delayed)
```
> If the socket happens to be connected (e.g. user is in lobby), the server also emits `adRewardGranted` + `userStatsUpdated` — see Section 3.3.

### 2.5b Weekly Tasks (`/api/tasks`) ⭐ NEW in v3.1
| Method | Endpoint | Description | Notes |
| :--- | :--- | :--- | :--- |
| `GET` | `/tasks/weekly` | This week's tasks + caller's progress/claim state | *(Requires Auth)* |
| `POST` | `/tasks/weekly/claim` | Claim a completed task's reward | `{ "taskId": "..." }` — *(Requires Auth)* |

**GET /tasks/weekly Response:**
```json
{
  "success": true,
  "data": {
    "weekStart": "2026-09-01T00:00:00.000Z",
    "tasks": [
      { "taskId": "play_5_matches", "title": "Play 5 Matches", "type": "play_matches", "target": 5, "progress": 3, "completed": false, "claimed": false, "reward": 150 },
      { "taskId": "play_15_matches", "title": "Play 15 Matches", "type": "play_matches", "target": 15, "progress": 3, "completed": false, "claimed": false, "reward": 400 }
    ]
  }
}
```
> Only `play_matches` is supported today — progress increments by 1 for every finished match (any outcome: win/loss/timeout/forfeit/abandonment), server-side, from `matchService.endGame`. Never trust/send progress from the client. Week resets every Monday 00:00. Task definitions (title/target/reward, more types later) live in the same admin-editable `SystemConfig` as lobbies — see Section 2.8.

**POST /tasks/weekly/claim Response:**
```json
{ "success": true, "message": "Task reward claimed successfully", "data": { "success": true, "taskId": "play_5_matches", "reward": 150 } }
```
> `400` if the task isn't complete yet, is already claimed, or the week has since reset. Reward is credited to `coins`, idempotent per (week, task, user) — a retried claim never double-pays.

### 2.6 Admin Panel (`/api/admin`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/withdrawals` | List all pending withdrawal requests with user info | Admin |
| `POST` | `/withdrawals/:id/action` | Approve or reject a withdrawal | Admin |
| `POST` | `/users/:id/credit` | Manually credit a user's balance | Admin |
| `POST` | `/users/:id/verify-kyc` | Set user KYC status (`verified` / `rejected`) | Admin |

**GET /admin/withdrawals Response:**
```json
{
  "success": true,
  "count": 2,
  "data": [
    {
      "_id": "664abc...",
      "userId": "664def...",
      "userName": "Nasir",
      "userEmail": "nasir@example.com",
      "amount": 25.00,
      "status": "pending",
      "description": "Withdrawal Request (Pending Review)",
      "createdAt": "2026-05-18T12:00:00.000Z"
    }
  ]
}
```

**POST /admin/withdrawals/:id/action Request:**
```json
{ "action": "approve" }
```
or
```json
{ "action": "reject" }
```
> `approve` — executes Stripe payout (mock in dev), clears locked balance, marks transaction `completed`.  
> `reject` — returns funds to user's `winningBalance`, marks transaction `failed`.  
> If Stripe payout fails during approval, funds are automatically returned to `winningBalance` and transaction is marked `failed`.

**POST /admin/users/:id/credit Request:**
```json
{
  "amount": 50.00,
  "type": "cash",
  "description": "Promotional credit"
}
```
> `type` accepts: `cash` | `bonus` | `winning` | `coins`. Defaults to `cash`.

### 2.8 Game Configuration (`/api/admin/config`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/config` | Get system config (lobbies, modes, bot levels) | Admin |
| `PATCH` | `/config` | Update system config | Admin |
| `POST` | `/config/seed-lobbies` | Reset lobbies to defaults | Admin |

> `xpPerLevel` ⭐ **NEW in v3.7** — flat XP-per-level curve applied after every settled match: `level = floor(xp / xpPerLevel) + 1`. `winXP`/`lossXP` are unchanged (still credited to `xp` on every match), this just controls when `level` increments.

**GET /config Response:**
```json
{
  "success": true,
  "data": {
    "rakePercent": 0.1,
    "winXP": 100,
    "lossXP": 30,
    "xpPerLevel": 1000,
    "matchTimeoutSeconds": 120,
    "botFillTimeoutMs": 10000,
    "winConditionTarget": 25,
    "gameModes": [
      {
        "name": "2p",
        "players": 2,
        "entryFee": 10,
        "prizePool": 18,
        "winConditionTarget": 25,
        "durationSeconds": 120,
        "enabled": true,
        "iconCount": 3,
        "iconDurationMs": 6000,
        "iconReactivateMinMs": 3000,
        "iconReactivateMaxMs": 8000
      }
    ],
    "botLevels": [
      {
        "name": "Rookie",
        "level": "rookie",
        "accuracy": 0.55,
        "reactionTimeRange": [4000, 7000],
        "rollIntervalRange": [7000, 11000],
        "winRateTarget": 0.15,
        "enabled": true
      },
      {
        "name": "Pro",
        "level": "pro",
        "accuracy": 0.82,
        "reactionTimeRange": [1800, 3200],
        "rollIntervalRange": [4500, 7000],
        "winRateTarget": 0.40,
        "enabled": true
      },
      {
        "name": "Master",
        "level": "master",
        "accuracy": 0.96,
        "reactionTimeRange": [700, 1400],
        "rollIntervalRange": [2500, 4000],
        "winRateTarget": 0.70,
        "enabled": true
      }
    ],
    "revenueTargetPer10Matches": 50,
    "botSelectionStrategy": "revenue_driven",
    "defaultBotLevel": "pro",
    "lobbies": [
      {
        "id": "combo-dice",
        "name": "COMBO DICE",
        "mode": "2p",
        "playerCount": 2,
        "entryFeeCash": 5,
        "entryFeeCoins": 0,
        "prizePool": 9,
        "firstPlaceCash": 9,
        "secondPlaceCash": 0,
        "thirdPlaceCash": 0,
        "firstPlaceCoins": 0,
        "secondPlaceCoins": 0,
        "thirdPlaceCoins": 0,
        "durationSeconds": 120,
        "winConditionTarget": 22,
        "enabled": true
      },
      {
        "id": "dice-royale",
        "name": "Dice Royale",
        "mode": "4p",
        "playerCount": 4,
        "entryFeeCash": 10,
        "entryFeeCoins": 0,
        "prizePool": 36,
        "firstPlaceCash": 25,
        "secondPlaceCash": 11,
        "thirdPlaceCash": 0,
        "firstPlaceCoins": 0,
        "secondPlaceCoins": 0,
        "thirdPlaceCoins": 0,
        "durationSeconds": 120,
        "winConditionTarget": 22,
        "enabled": true
      }
    ]
  }
}
```

### 2.9 Revenue Analytics (`/api/admin/analytics`)
| Method | Endpoint | Description | Auth |
| :--- | :--- | :--- | :--- |
| `GET` | `/analytics/revenue?limit=10&lobbyId=combo-dice` | Last N finished matches with analytics | Admin |

**GET /analytics/revenue Response:**
```json
{
  "success": true,
  "data": {
    "matches": [
      {
        "_id": "...",
        "roomId": "match-123...",
        "players": ["user1", "bot-123", "user2"],
        "hadBots": true,
        "botWon": false,
        "winnerId": "user1",
        "rakeCollected": 5.50,
        "humanPayout": 45.00,
        "stake": 10,
        "endTime": "2026-05-06T09:22:01Z"
      }
    ],
    "summary": {
      "totalRake": 55.00,
      "botWinRate": 0.30,
      "botMatchRate": 0.80,
      "totalHumanPayout": 325.00,
      "revenueTarget": 50,
      "gap": -5.00,
      "gapStatus": "on_track"
    }
  }
}
```

---

## 3. Real-time Socket.io Contract

### 3.1 Connection & Lifecycle
- **URL**: `wss://<api_domain>` (WebSocket Secure)
- **Transports**: WebSocket (primary), polling fallback
- **Reconnection**: Auto-enabled with exponential backoff
- **Auth**: Token in handshake `{ auth: { token } }`

### 3.2 Client → Server (Inbound Events)

#### Matchmaking
| Event | Payload | Description |
| :--- | :--- | :--- |
| `findMatch` | `{ mode, stake, lobbyId }` ⭐ **CORRECTED in v3.2** | Join matchmaking queue for lobby |
| `cancelMatchmaking` | `{ lobbyId }` | Leave matchmaking queue |
| `startNow` | `{ lobbyId }` | Skip the remaining bot-fill wait and start immediately (fills empty seats with bots). Caller must currently be queued for that lobby. |

**findMatch Example:**
```javascript
socket.emit('findMatch', {
  mode: '3p',               // one of: '1v1' | 'bot' | '2p' | '3p' | '4p' | '5p' | '6p' | '10p' — must match the target lobby's own mode (see MODE_LOBBY_MISMATCH below)
  stake: 100,                // cash entry fee — actual value used is lobbyConfig.entryFeeCash if set
  lobbyId: 'board-monaco-riviera-small'
});
```
> ⚠️ **v3.2 correction**: earlier revisions of this doc documented `{ lobbyId, target }`, which does **not** match the server's zod schema (`findMatchSchema` in `server/src/socket/socketSchemas.ts`) and would be rejected with `INVALID_PAYLOAD`. `mode` and `lobbyId` are both required; `stake` is required by the schema but the server actually uses `lobbyConfig.entryFeeCash` when the lobby defines one, so any non-negative number is accepted. If the Unity client is currently sending the old shape, this is very likely the source of "Invalid payload" errors reported in production — see `findMatch` in the error payload's `fields` object (server now returns per-field validation errors, not just a bare message; see §3.3 Error & Status).

#### Private Arena (Host / Join by Code) ⭐ NEW in v3.6

A private, invite-only 1v1 alternative to pooled matchmaking: one player hosts a table against a lobby preset, gets back a 6-character code, and shares it out-of-band (chat, voice, etc.) with whoever they want to play. Fully implemented server-side today (`server/src/services/arenaService.ts`) — this section was simply never documented before, which is very likely why it hasn't been wired up client-side yet.

| Event | Payload | Description |
| :--- | :--- | :--- |
| `hostArena` | `{ lobbyId, tableName, stake? }` | Create a table against an Arena-preset lobby. `tableName` is a **required**, host-chosen label (max 40 chars) — two tables against the same preset still need to be tellable apart, and per the product spec the display convention is "(Player Name) Arena", so a reasonable default to pre-fill the input with is `${hostDisplayName}'s Arena`, editable before hosting. `stake` only required for the one custom-range preset (`entryFeeCash === 0`, e.g. `arena-custom-table` / "Player's Arena"); ignored for every fixed-price preset, which uses its own `entryFeeCash`. |
| `joinArena` | `{ arenaCode }` | Join an existing table by its 6-character code (case-insensitive). |
| `cancelArena` | `{ arenaCode }` | Host-only. Cancels a table nobody has joined yet and refunds the stake. |

**Which lobbies are Arena presets:** filter `GET /api/lobby`'s response for `isArenaPreset: true`. Every Arena preset has a fixed `entryFeeCash` **except** `arena-custom-table`, which has `entryFeeCash: 0` and instead carries `arenaStakeMin`/`arenaStakeMax` (admin-configurable, default $10,000–$100,000) — that's the one case `hostArena` needs an explicit `stake` for.

**hostArena Example:**
```javascript
socket.emit('hostArena', {
  lobbyId: 'arena-monaco-riviera',   // a fixed-tier preset — $100 entry
  tableName: "Ali's Arena",          // required — whatever the host typed (or accepted as the pre-filled default)
  // stake omitted — not the custom table, server uses the preset's own entryFeeCash
});

socket.on('arenaHosted', (d) => {
  // { arenaCode: 'WYR4BK', tableName: "Ali's Arena", hostName: 'Ali', expiresAt: 1789649968004, stake: 100, lobbyId: 'arena-monaco-riviera' }
  // Show d.arenaCode to the host to share. hostName is the host's own account name,
  // sent back so the "hosting as ___" label doesn't need a separate lookup. d.expiresAt
  // is a ms timestamp — the table auto-cancels and refunds if nobody joins by then (10 min by default).
});
```
> The host's stake is debited **immediately** on `arenaHosted`, not when someone joins — the table is real and the money is locked the moment it's created, not just a UI placeholder. `GET /api/wallet/history` / a balance refresh will already reflect the debit before anyone else even sees the code.
>
> `tableName` also comes back in `startGame` (§3.3) for both host and joiner once a match actually starts from this table — so the in-match UI (and the joiner, who never saw the `arenaHosted` box) can show which table this was. The joiner also already gets the host's name for free via `startGame.playerAvatars[hostUserId].name`, same as any other match — no separate lookup needed there either.

**joinArena Example:**
```javascript
socket.emit('joinArena', { arenaCode: 'WYR4BK' });
// On success, BOTH the host's and the joiner's sockets receive the exact same
// 'startGame' event (§3.3) they'd get from regular findMatch — same roomId for
// both, so this is the client's confirmation "yes, I joined the table this code
// was for." There is no separate arena-specific start event; wire it through the
// same startGame handler already used for pooled matches.
```

**cancelArena Example:**
```javascript
socket.emit('cancelArena', { arenaCode: 'WYR4BK' }); // only works while status is still "waiting" — no-ops with an error once someone has joined
socket.on('arenaCancelled', (d) => { /* { arenaCode: 'WYR4BK' } — hide the "waiting for opponent" UI, stake already refunded */ });
```

**Errors** (all bare `{ message }` on the `error` event, same as other business-rule errors — see §6.2):
- `Already in an active match` — can't host or join while already in a live match
- `This lobby is currently disabled` / `This lobby is not an Arena preset` — bad `lobbyId` on `hostArena`
- `Stake must be between {min} and {max}` — custom-table `stake` outside `arenaStakeMin`/`arenaStakeMax`
- `Daily Arena limit reached (3/day)` — `hostArena` daily cap, admin-configurable (`maxArenasPerDay`)
- `Insufficient funds. Balance: $X, Needed: $Y` — on both `hostArena` and `joinArena`
- `Invalid or expired arena code` / `Arena has expired` / `Arena was already joined or cancelled` — `joinArena` on a dead/claimed code
- `You cannot join your own arena` — self-join attempt
- `Arena not found or already resolved` — `cancelArena` on a code that's already been joined, cancelled, or expired, or that doesn't belong to the caller

**arenaExpired** (server → client, host only): if nobody joins within the expiry window (default 10 minutes, `arenaExpiryMs`), the table auto-cancels and refunds — same effect as `arenaCancelled`, just triggered by a timeout instead of the host acting:
```javascript
socket.on('arenaExpired', (d) => { /* { arenaCode: 'WYR4BK' } */ });
```

#### In-Game Actions
| Event | Payload | Description |
| :--- | :--- | :--- |
| `rollDiceRequest` | `{ roomId }` | Request dice roll (1s cooldown enforced) |
| `tapNumber` | `{ roomId, index, val, sequence }` | Lock a board cell |
| `forfeitMatch` | `{ roomId }` | Surrender match (no winnings) |
| `reconnectMatch` | `{ activeMatchId, activeRoomId }` | Resume disconnected match |

**tapNumber Examples:**

*Tapping a dice number cell (val = 2-12):*
```javascript
socket.emit('tapNumber', {
  roomId: 'match-1234567890',
  index: 5,           // 0-24 cell position
  val: 7,             // dice sum cell value (2-12) — must match matrix[index]
  sequence: 42        // incremental counter (resets on new roll)
});
```

*Tapping an icon cell (val = -1, -2, or -3):*
```javascript
socket.emit('tapNumber', {
  roomId: 'match-1234567890',
  index: 3,           // 0-24 cell position where icon is located
  val: -2,            // icon marker from matrix[index] (-1, -2, or -3)
  sequence: 15        // different sequence per tap
});
```

**Anti-Cheat Validation:**
- **Integrity Check**: `matrix[index]` must exactly equal `val`. If the icon expired or the cell is already locked, the value won't match.
- **Icon Detection**: Negative `val` (-1, -2, -3) indicates an icon cell. Regular dice cells are always positive (2-12).
- **Sequence**: Must increment per tap; resets when a new roll starts.

#### Socket Management
| Event | Payload | Description |
| :--- | :--- | :--- |
| `disconnect` | *(auto)* | Client disconnects (server cleans up after timeout) |

### 3.3 Server → Client (Outbound Events)

#### Matchmaking Phase
| Event | Payload | Description |
| :--- | :--- | :--- |
| `matchmakingUpdate` | `{ current, target, status, players }` | Queue size updated (real-time updates) |
| `error` | `{ message }` | Matchmaking error (insufficient funds, lobby disabled) |
| `arenaHosted` | `{ arenaCode, tableName, hostName, expiresAt, stake, lobbyId }` | Private Arena table created — see §3.2 Private Arena |
| `arenaCancelled` | `{ arenaCode }` | Private Arena table cancelled by its host, stake refunded |
| `arenaExpired` | `{ arenaCode }` | Private Arena table auto-cancelled, nobody joined in time, stake refunded |

**matchmakingUpdate Example:**
```javascript
{
  current: 2,
  target: 5,
  status: 'searching',
  players: [
    { userId: '664abc...', name: 'Alice', avatar: 'avatarBoy', isBot: false },
    { userId: '664def...', name: 'Bob',   avatar: 'https://...',  isBot: false }
  ]
}
```
> `players` ⭐ NEW in v3.1 — the display roster (name/avatar) for everyone currently in the queue, so the waiting-room screen can render individual player cards instead of just a `current/target` counter. Not present on every historical emit path — treat a missing/empty array as "no roster info yet," not as an empty queue.

#### Match Start
| Event | Payload | Description |
| :--- | :--- | :--- |
| `startGame` | *(see below)* | Match ready, game begins |

**startGame Payload (per-player):**
```javascript
{
  roomId: 'match-1234567890',
  matrix: [2, 3, 4, ..., 12, -1, -2, -3],  // 25 elements (2-12 = dice nums, -1/-2/-3 = icons)
  playerSides: {
    "user1": "A",
    "bot-123": "B"
  },
  playerAvatars: {
    // avatar = HTTPS URL (custom upload) or preset key string ("avatarBoy"/"avatarGirl")
    "user1": { name: "Alice", avatar: "https://game.winjogames.com/avatars/user1-1234.webp", isBot: false },
    "user2": { name: "Bob",   avatar: "avatarBoy", isBot: false },
    "bot-123": { name: "Bot", avatar: "avatarBoy", isBot: true }
  },
  duration: 120,                        // total gameplay seconds (excludes countdown)
  countdown: 3,                         // seconds of 3-2-1 countdown before gameplay
  gameplayStartsAt: 1746789123000,      // Unix ms timestamp when gameplay begins
  winConditionTarget: 25,               // cells to lock (configurable)
  stake: 10,                            // cash entry fee per player (0 for coins-only lobbies)
  stakeCoins: 0,                        // coins entry fee per player (0 for cash-only lobbies)
  prizes: [                             // ⭐ CORRECTED in v3.2 — array, one entry per paid rank
    { rank: 1, cash: 9, coins: 0 }
    // additional { rank, cash, coins } entries when lobby.prizeDistribution pays more than 1st place
  ]
}
```
> ⚠️ **v3.2 correction**: earlier revisions documented `prizes` as a flat object (`firstPlaceCash`/`secondPlaceCash`/`thirdPlaceCash`/…). The server replaced that with `lobby.prizeDistribution` (any number of paid ranks, each `{ rank, cashMultiplier, coinsAmount }` in config) and emits it here as an array of `{ rank, cash, coins }` — `cash = stake * entry.cashMultiplier`. A lobby with no `prizeDistribution` configured falls back to a single `{ rank: 1, cash: prizePool, coins: 0 }` entry (winner takes the pool).
> After `startGame`, the server immediately emits dedicated `countdown` tick events (see below). Clients should show the overlay immediately on `startGame` and update the number on each `countdown` event.

#### Countdown Phase (between `startGame` and gameplay)
| Event | Payload | Description |
| :--- | :--- | :--- |
| `countdown` | `{ count: number }` | ⭐ NEW: Server-driven countdown tick — emitted at T+0s (count=3), T+1s (count=2), T+2s (count=1) |

**countdown Payload:**
```javascript
{ count: 3 }  // then { count: 2 }, then { count: 1 }
```
> After `count: 1` arrives, wait ~1s then show "GO!" and enable roll/forfeit buttons. `gameplayStartsAt` in `startGame` gives the exact ms when gameplay begins for timer sync.

#### Gameplay Events
| Event | Payload | Description |
| :--- | :--- | :--- |
| `diceRolled` | `{ d1, d2, sum, userId, score }` | Dice roll result — **includes updated player score** |
| `numberLocked` | private (owner): `{ index, userId, val, score }` · broadcast (others): `{ userId, score }` ⭐ **CORRECTED in v3.2** | Player locked a cell — **includes updated score**. Only the locking player receives `index`/`val`; everyone else in the room gets a minimized progress ping with no board detail. |
| `playerMilestone` | `{ userId, lockedCount, score }` | Player reached win target cells — **includes final score with +1200 bonus** |
| `tapResult` | `{ correct, scoreDelta, score, userId }` | Tap validation result — **includes updated score** |
| `matchRecovered` | *(full state)* | Reconnection: complete game state — **includes score for all players** |

> **CRITICAL FIX (v2.8):** All gameplay events now include the player's **current score** to ensure score consistency. The server also persists scores to the Match document during gameplay, so final scores are always accurate even if a player disconnects.

**diceRolled Example:**
```javascript
{
  d1: 3,
  d2: 4,
  sum: 7,
  userId: "user1",
  score: 450  // updated score after this roll
}
```

**numberLocked Examples:**
```javascript
// Sent ONLY to the player who locked the cell:
{
  index: 5,     // board position
  userId: "user1",
  val: 7,       // the cell's matrix value (dice number 2-12, or icon marker -1/-2/-3)
  score: 550    // score increased by +100 for correct tap (or +200 for an icon)
}

// Sent to everyone else in the match room (no index/val — opponents' boards stay private):
{
  userId: "user1",
  score: 550
}
```

**tapResult Example:**
```javascript
{
  correct: true,
  scoreDelta: 100,
  score: 550,    // updated total score
  userId: "user1"
}
```

**playerMilestone Example:**
```javascript
{
  userId: "user1",
  lockedCount: 25,      // cells locked on the board
  score: 1650           // includes +1200 completion bonus
}
```

**matchRecovered Example:**
```javascript
{
  roomId: 'match-1234567890',
  matrix: [2, 3, 4, ...],          // reconnecting player's own board
  playerSides: {
    "user1": "A",
    "bot-123": "B"
  },
  playerAvatars: {
    "user1": { name: "Alice", avatar: "https://game.winjogames.com/avatars/user1-1234.webp", isBot: false },
    "bot-123": { name: "Bot", avatar: "avatarBoy", isBot: true }
  },
  gameState: {                      // full live state per player from Redis
    "user1": {
      currentSum: 7,                // dice sum currently active
      lockedIndices: [0, 3, 5, 7], // cells already locked
      score: 450,
      reachedWinTarget: false,
      reachedWinTargetTime: 0,
      side: "A"
    },
    "bot-123": {
      currentSum: 0,
      lockedIndices: [1, 2, 4],
      score: 320,
      reachedWinTarget: false,
      reachedWinTargetTime: 0,
      side: "B"
    }
  },
  duration: 120,
  winConditionTarget: 25,
  timeRemaining: 85,              // actual seconds left (elapsed = duration - timeRemaining)
  gameplayStartsAt: 1746789123000,// Unix ms timestamp — use for precise timer: elapsed = (Date.now() - gameplayStartsAt) / 1000
  stake: 10
}
```
> `timeRemaining` is computed server-side as `duration - Math.floor((now - match.startTime) / 1000)`. `gameplayStartsAt` is the authoritative anchor; use it to keep the client timer in sync after reconnect.

#### Match End
| Event | Payload | Description |
| :--- | :--- | :--- |
| `gameOver` | *(see below)* | Match finished, results broadcast |

**gameOver Payload:**
```javascript
{
  winnerId: "user1",
  scores: {
    "user1": 1650,
    "user2": 900,
    "user3": 750,
    "user4": 600
  },
  totalLocks: {
    "user1": 25,
    "user2": 18,
    "user3": 15,
    "user4": 12
  },
  reason: "victory",   // "victory" | "forfeit" | "timer"
  stake: 10,
  prizePool: 36,
  earnings: {
    // Per-rank payouts from lobby config (firstPlaceCash / secondPlaceCash / thirdPlaceCash)
    "user1": 25,        // 1st place — lobby.firstPlaceCash
    "user2": 11,        // 2nd place — lobby.secondPlaceCash
    "user3": 0,         // 3rd place — lobby.thirdPlaceCash (0 for this lobby)
    "user4": 0
  },
  players: {            // full player profiles for results screen
    "user1": {
      name: "Alice",
      avatar: "https://game.winjogames.com/avatars/user1-1234.webp",  // URL or preset key
      isBot: false,
      side: "A",
      rank: 1,          // 1 = winner (highest score / first to reach target)
      wins: 12,
      losses: 4,
      level: 5,
      xp: 2400
    },
    "user2": {
      name: "Bob",
      avatar: "avatarBoy",
      isBot: false,
      side: "B",
      rank: 2,
      wins: 8,
      losses: 7,
      level: 3,
      xp: 1200
    }
  }
}
```
> `earnings` reflects per-rank payouts computed from the lobby's `prizeDistribution` (`cash = stake * entry.cashMultiplier` per rank — see the `startGame.prizes` correction above). The settlement worker credits each real player by rank. Bots always receive $0.

#### Wallet / Reward Updates
| Event | Payload | Description |
| :--- | :--- | :--- |
| `userStatsUpdated` | *(see below)* | Wallet + stats refreshed — emitted after match settlement and after ad reward credit |
| `adRewardGranted` | *(see below)* | Emitted after a rewarded ad is successfully credited via S2S callback |

**userStatsUpdated Payload:**
```javascript
{
  balance: 80.00,
  cashBalance: 60.00,
  bonusBalance: 20.00,
  winningBalance: 0.00,
  coins: 160,
  wins: 12,
  losses: 4,
  xp: 2400,
  level: 5
}
```
> Emitted after every match settlement and after an ad reward is credited. Always update your local user state from this event.

**adRewardGranted Payload:**
```javascript
{
  coins: 160,        // new total coins balance after credit
  coinsAwarded: 10,  // coins just earned from this ad
  rewardLabel: "Rewarded Ad"
}
```
> Only emitted if the socket is **already connected** when the S2S callback arrives (e.g. user is in lobby). **Do not rely on this event alone** — use `GET /api/wallet/balance` polling as the primary detection mechanism. `adRewardGranted` is best-effort bonus UX.

#### Error & Status
| Event | Payload | Description |
| :--- | :--- | :--- |
| `error` | `{ message }` | Generic error (invalid action, stale state, etc) |
| `notification` | `{ type, message }` | Info/warning (cooldown remaining, etc) |

---

## 4. Game Logic Rules (Technical)

### 4.1 Board
- **Size**: 5×5 grid (25 indices: 0–24)
- **Content**: Numbers 2–12 (dice sums), plus icon cells (values = -1, -2, -3)
- **Generation**: Cryptographically secure random using `crypto.randomInt()`
- **Icon Cells**: Represent special tappable icons that grant bonus points. Each player's board has 3 icon cells (up to 3 unique icons: -1, -2, -3)

### 4.2 Win Condition
1. **Primary Condition**: First player to lock `winConditionTarget` cells wins immediately
2. **Completion Detection**:
   - Server tracks `reachedWinTarget` + `reachedWinTargetTime` per player in Redis
   - When a player reaches the target, server broadcasts `playerMilestone`, awards +1200 bonus, then calls `endGame` with that player as winner
   - **No waiting** for other players — first to finish wins
3. **Match End Triggers**:
   - **First player reaches target**: Match ends immediately, that player is the winner
   - **Timer expires**: Match ends, winner determined by tiebreaker (highest score)
   - **Player forfeits**: Match ends immediately, forfeit player gets 0 earnings

### 4.3 Timer Tiebreaker (if no one reaches target before time runs out)
```
Sort players by:
1. Score (highest first)
2. lockedIndices.length (most cells locked)
3. lastActionTime (earliest)
Winner = players[0]
```

### 4.4 Timeout Tiebreaker (if no one reaches 25)
```
Sort players by:
1. lockedIndices.length (most cells)
2. Score (highest)
3. lastActionTime (earliest)
Winner = players[0]
```

### 4.5 Turn System
- **Type**: Real-time (asynchronous), no turn order
- **Dice Roll Cooldown**: 2 seconds between rolls per player (enforced server-side)
- **Stall Detection**: If player inactive >20s, system logs but doesn't auto-tap (must roll manually)
- **Bot Actions**: Bots roll with realistic delays (difficulty-dependent)

### 4.6 Bot Difficulty Levels
| Level | Accuracy | Reaction Time | Win Rate Target |
| :--- | :--- | :--- | :--- |
| Rookie | 0.55 | 4–7s | 15% |
| Amateur | 0.70 | 3–5s | 25% |
| Pro | 0.82 | 1.8–3.2s | 40% |
| Expert | 0.90 | 1.2–2s | 55% |
| Master | 0.96 | 0.7–1.4s | 70% |

**Selection Strategy**: 
- If `botSelectionStrategy === 'revenue_driven'`:
  - Calculate revenue gap (target - last 10 matches rake)
  - If behind target: select harder bots
  - If on track: select easier bots
  - Distribute levels across multiple bots (e.g., 3 bots: Master, Expert, Pro)
- If `revenue_driven`: random enabled levels
- If `fixed`: always use `defaultBotLevel`

### 4.7 Anti-Cheat Measures
- **Sequence Validation**: `sequence` field increments per tap; resets on new roll
- **Cell Integrity**: Tapped cell index must exist in locked matrix range [0–24]
- **Matrix Value Matching** ⭐ **NEW in v2.10**: Tapped cell value (`val`) must exactly match `matrix[index]`. This prevents race conditions where icons expire between tap and validation.
- **Icon Cell Detection**: Negative values (-1, -2, -3) indicate icon cells. Positive values (2-12) are dice number cells.
- **Duplicate Prevention**: Already-locked cells cannot be re-tapped
- **Cooldown Enforcement**: Dice roll requests <2s apart are rejected

### 4.8 Icon Cells (Bonus Mechanic) ⭐ UPDATED in v2.10
- **Setup**: 3 icon cells per 25-cell board (configurable via `iconCount`); each board has exactly one each of `-1`, `-2`, `-3`
- **Matrix Representation**: Icon cells are stored in the matrix with negative integer values (`-1`, `-2`, `-3`) instead of `0`
- **Frontend Display**: When displaying the board, treat any negative value as an icon cell and show the icon UI element
- **Activation**: Random icon activates every 6 seconds (configurable); server tracks `activeIconIndex` (the board position, not the cell value)
- **Tapping Icons**: When user taps an icon cell, frontend sends `{ val: <the negative marker from matrix>, index: <position> }`. Server validates:
  - Icon exists at that position: `matrix[index] === val` (e.g., `matrix[3] === -1`)
  - Icon is currently active: `activeIconIndex === index`
- **Scoring**:
  - ✅ **Correct tap** (right icon, within time window): +200 bonus
  - ❌ **Wrong icon** (matrix value doesn't match, or already locked): -50 penalty
  - ❌ **Late tap** (icon expired): -50 penalty (integrity check fails because activeIconIndex changed)
- **Reactivation**: After 3–8 seconds, a new random unlocked icon activates

**Frontend Integration Example:**
```javascript
// Receive matrix with icons
{ matrix: [2, 5, -1, 7, 3, -2, 9, 12, 4, 8, -3, 6, ...] }
                 ↑icon      ↑icon              ↑icon

// Display board
for (let i = 0; i < matrix.length; i++) {
  if (matrix[i] < 0) {
    // Render icon cell (negative values are icons)
    showIconCell(i, matrix[i]); // -1, -2, or -3 can be used for styling if needed
  } else {
    // Render dice number cell
    showDiceCell(i, matrix[i]); // 2-12
  }
}

// When user taps an icon
socket.emit('tapNumber', {
  roomId: roomId,
  index: 2,                    // position on board where icon is
  val: matrix[2],              // send the actual value (-1, -2, or -3)
  sequence: sequenceCounter++
});
```

### 4.9 Stagger & Bot Joining (NEW in v2.0)
- **Real Players**: Join immediately upon socket connection
- **Bots**: Added with 0.8–2.3s delays between each bot to simulate realistic joining
- **Visibility**: `matchmakingUpdate` events emitted per bot join so UI shows progress
- **Match Start**: Triggers only when all target slots filled

---

## 5. Analytics & Revenue Tracking (NEW in v2.0)

Every finished match records:
```javascript
{
  rakeCollected: Number,      // stake * playerCount - prizePool
  hadBots: Boolean,           // any bot in players array
  botWon: Boolean,            // winnerId.startsWith('bot-')
  humanPayout: Number         // prizePool if human won, else 0
}
```

**Admin Dashboard** (Bot Levels + Revenue tabs):
- Shows last 10 matches with rake, bot stats, human payout
- Revenue vs target gap (green = on track, red = behind)
- Live bot level editor (all 5 levels editable)
- Configurable revenue target + bot selection strategy

---

## 6. Error Codes & Responses

### 6.1 REST errors
REST error responses use `{ success: false, error: <message> }` (some routes use `{ success: false, message: <message> }` — check the specific endpoint). HTTP status code is the primary signal:

| Status | Meaning |
| :--- | :--- |
| 400 | Bad request — invalid input, insufficient funds, business-rule violation |
| 401 | Missing/invalid/expired token, or session superseded by a newer login |
| 402 | Payment not yet confirmed (e.g. `/wallet/sync-deposit` before Stripe confirms) |
| 403 | Forbidden — not an admin, or acting on another user's resource |
| 404 | Resource not found |
| 429 | Rate limited |
| 500 | Unexpected server error |

### 6.2 Socket errors ⭐ **CORRECTED in v3.2**
Earlier revisions of this doc implied a `{ code, message }` shape was already standard. In the actual implementation, most socket errors are still a bare `{ message }`. The one place a real structured shape exists today is **payload validation** — every inbound event validated by a zod schema (`findMatch`, `startNow`, `hostArena`, `joinArena`, `cancelArena`, `joinRoom`, `rollDiceRequest`, `tapNumber`, `forfeitMatch`) now emits:

```javascript
socket.on('error', (err) => {
  // {
  //   event: 'tapNumber',            // which inbound event failed validation
  //   code: 'INVALID_PAYLOAD',
  //   message: "Invalid payload for 'tapNumber'",
  //   fields: { roomId: 'Room ID is required', val: 'Number must be less than or equal to 12' }
  // }
});
```
`fields` maps each invalid field's dotted path to zod's own message for it — use it to show a specific inline error instead of a generic toast. Business-rule errors (insufficient funds, already in a match, lobby disabled, etc.) still emit only `{ message }` — treat any `error` payload without a `code` as a generic, message-only error.

---

## 7. Frontend Integration Checklist

### Auth
- [ ] `POST /api/auth/logout` — call server logout, then clear `token`/`user` from localStorage and disconnect socket
- [ ] `POST /api/auth/forgot-password` — user enters email, frontend shows "check your email" screen, calls backend with `{ email }`
- [ ] `POST /api/auth/reset-password` — user clicks reset link `https://game.winjogames.com/reset-password?token=XXX`, enters new password, frontend calls backend with `{ token, newPassword }`
- [ ] Password reset flow: Frontend pages for `/forgot-password` and `/reset-password` routes; add "Forgot Password?" link to login screen
- [ ] `GET /api/auth/me` on app start — `activeMatchId`/`activeRoomId` present only if match is genuinely ongoing (live DB check, no stale state). Use for reconnect decision
- [ ] `GET /api/auth/me` — check `recentMatch.matchId` + `recentMatch.status` to decide whether to show last result screen on launch
- [ ] `POST /api/auth/upload-avatar` — multipart/form-data, field `"avatar"`, max 5MB; response gives `avatarUrl` (public S3 URL)
- [ ] Avatar display: if `avatar.startsWith("https://")` → download and show as circle image; else → use local preset asset by key
- [ ] "Remove Photo" / reset-avatar action on the Edit Profile screen → `DELETE /api/auth/avatar` (no body needed)
- [ ] `profileFrame` ⭐ **NEW in v3.7** — `PATCH /api/auth/profile` with `{ "profileFrame": "<client-defined frame id>" }` to save the selected frame; returned on `GET /api/auth/me`, `GET /api/profile/:userId`, and every `GET /api/leaderboard` entry (`""` = no frame selected)

### Discord Login ⭐ NEW in v3.1
- [ ] "Sign in with Discord" opens Discord's OAuth consent screen (scopes `identify email`) in a webview/browser, using your own registered `redirectUri`
- [ ] On redirect callback, extract the `code` query param and call `POST /api/auth/discord { code, redirectUri }` — `redirectUri` must be the exact same string used to start the flow
- [ ] "Link Discord" (from an existing logged-in session — guest/email/etc.) instead calls `POST /api/auth/discord/link { code, redirectUri }`; this imports the Discord display name + avatar onto the current account
- [ ] Rich Presence, in-Discord Activities embedding, server leaderboards, and bot-driven match notifications are **not yet implemented** (need a separate persistent Discord bot service) — don't build client UI expecting these yet

### Player Profile ⭐ NEW in v3.1
- [ ] `GET /api/profile/:userId` — use for both "my profile" and "other player's profile" screens (pass your own ID or theirs); no edit affordance when viewing someone else
- [ ] `POST /api/profile/:userId/like` — heart/like button on another player's profile; disable it (or ignore the tap) if `alreadyLiked` comes back `true`
- [ ] `GET /api/auth/me` now carries everything needed for the self-view profile card too (country, league, streaks, totals) — no need to also call `/api/profile/:userId` with your own ID unless it's simpler to reuse one code path for both screens

### Weekly Tasks ⭐ NEW in v3.1
- [ ] `GET /api/tasks/weekly` — fetch on the weekly tasks screen; render `progress`/`target` as a bar, show a claim button when `completed && !claimed`
- [ ] `POST /api/tasks/weekly/claim { taskId }` on claim tap — `400` if not actually complete/already claimed/week rolled over since the screen loaded (refetch status in that case)

### Leaderboard
- [ ] `GET /api/leaderboard?limit=50` — fetch on leaderboard screen open; `me` gives current user's rank even if outside top N
- [ ] Render top 3 on podium (rank 1 centre/gold, rank 2 left/silver, rank 3 right/bronze)
- [ ] Render rank 4+ in scrollable list with rank number, avatar, name, wins

### Invites
- [ ] On first cold start after Branch.io delivers an `inviterId`: call `POST /api/invites/invitedby` with `{ inviterId }` immediately after login — safe to call every launch (server ignores if already assigned)
- [ ] `GET /api/invites/history` — fetch on invite/friends screen; use `isOnline` to show presence indicator

### Shop & In-App Purchases ⭐ NEW in v2.7
- [ ] Remove Unity IAP (`UnityEngine.Purchasing`) dependency — all purchases now go through `POST /api/shop/checkout`
- [ ] On shop/offers screen open: `GET /api/shop/items` — display live prices and rewards from server (admin can update without app release)
- [ ] Filter by `type`: `"offer"` → All Offers screen; `"lucky_deal"` → Lucky Deals screen
- [ ] On Buy tap: `POST /api/shop/checkout { itemId, returnUrl: "blingodice://open/payment-success" }` → get `checkoutUrl`
- [ ] Open `checkoutUrl` with `Application.OpenURL(checkoutUrl)` — this opens Chrome with Stripe's hosted checkout
- [ ] Register `Application.deepLinkActivated` handler (and check `Application.absoluteURL` on cold start)
- [ ] On `blingodice://open/payment-success` deep link: wait 2s → `GET /api/wallet/balance` → update coins/balance UI
- [ ] On `blingodice://open/payment-cancel` deep link: show "Payment cancelled" toast
- [ ] **Remove** from `InAppPurchaseManager.cs`: `TriggerLocalCheckoutFlow`, `SimulateLocalPaymentCoroutine`, `SyncDeposit` calls, and all `Deposit(totalCash, productId, ...)` calls
- [ ] `itemId` values in `POST /api/shop/checkout` are the same strings as Unity IAP product IDs (`package_1`–`package_6`, `offer_quick_boost`, etc.)

### Wallet
- [ ] `GET /api/wallet/history?page=1&limit=20` — paginate using `pagination.page` / `pagination.pages`
- [ ] Display `type` as human-readable label (e.g. `entry_fee` → "Match Entry", `win_prize` → "Prize Won", `refund` → "Refund")
- [ ] Show `status` badge: `completed` = green, `pending` = yellow, `failed` = red, `reversed` = grey
- [ ] Amount sign: negative = debit (entry_fee, withdrawal), positive = credit (win_prize, deposit, refund, bonus)
- [ ] `GET /api/wallet/withdrawal-info` — call before showing the withdraw screen; drive the min/max note and fee display from the response, never hardcode
- [ ] `GET /api/wallet/withdrawal-info?amount=X` — call as the player types to show the live fee and "you will receive" figure; show `reason` verbatim when `valid: false`
- [ ] Show the fee and the "you will receive" amount before the player confirms — the fee is taken **out of** the amount they enter (enter $10 → receive $9)
- [ ] `POST /api/wallet/withdraw` — only allow withdrawal up to `withdrawableBalance` (from `/me`), between `minAmount` and `maxAmount`; no KYC check; withdrawal will show as `pending` until admin approves
- [ ] `withdrawableBalance` in `/me` response = spendable prize earnings; display this as the withdrawable amount (not `cashBalance` or `balance`)

### Rewarded Ads
- [ ] Call `IronSource.Agent.setUserId(userId)` **before** `init()` and `loadRewardedVideo()` — if userId is null when the ad completes, reward cannot be credited
- [ ] On SDK `onRewardedVideoAdRewarded`: snapshot `coins` value, show "Claiming..." UI, then poll `GET /api/wallet/balance` every 2 seconds
- [ ] Stop polling when `coins` increases (reward arrived) — show "+N coins!" animation
- [ ] Stop polling after 15s timeout — show "Reward arriving soon" toast (coins will still credit eventually)
- [ ] If socket is connected, also listen for `adRewardGranted` as a faster notification — but always have the polling fallback

### Match Flow
- [ ] `socket.on('startGame')` — store `countdown`, `gameplayStartsAt`, `winConditionTarget`, `playerAvatars`
- [ ] `socket.on('countdown', d)` — show overlay immediately on `startGame`, update number on each tick; after `count: 1`, show GO! for 700ms then enable roll/forfeit and start timer
- [ ] `socket.on('matchRecovered')` — restore `gameState[uid].lockedIndices`, `gameState[uid].score`, `gameState[uid].currentSum` (pending sum) for each player; use `gameplayStartsAt` for timer sync
- [ ] `socket.on('playerMilestone')` — show milestone banner; note: match ends immediately after first player reaches target
- [ ] `socket.emit('reconnectMatch', { activeMatchId, activeRoomId })` — on app start if active match exists

### Results Screen
- [ ] `gameOver.players[uid].rank` — sort results table by rank (1 = winner)
- [ ] `gameOver.players[uid].name` / `.avatar` — show real player names (not just user ID)
- [ ] `gameOver.earnings[uid]` — per-rank payout: 1st gets `firstPlaceCash`, 2nd gets `secondPlaceCash`, 3rd gets `thirdPlaceCash`
- [ ] Show `🥇 🥈 🥉` rank icons in results

### Lobbies
- [ ] Lobby cards show `firstPlaceCash`, `secondPlaceCash`, `thirdPlaceCash` prizes separately
- [ ] `winConditionTarget` comes from lobby config (not hardcoded 22)
- [ ] `findMatch` emits `{ lobbyId, mode, stake }` — `lobbyId` is required

### Private Arena ⭐ NEW in v3.6
- [ ] "Host a private table" UI: filter `GET /api/lobby` for `isArenaPreset: true`, let the player pick one
- [ ] Custom-stake preset (`entryFeeCash === 0`, `arena-custom-table`): show a stake input bounded by that lobby's own `arenaStakeMin`/`arenaStakeMax`; every other preset uses its fixed `entryFeeCash`, no input needed
- [ ] Table name input, required — pre-fill with `"${player's own name}'s Arena"` as a sensible default (matches the product spec's "(Player Name) Arena" convention) but let the host edit it before confirming
- [ ] `socket.emit('hostArena', { lobbyId, tableName, stake? })` → on `arenaHosted`, show the `arenaCode` prominently with a share/copy action, the `tableName`/`hostName` so the host can confirm which table this is, plus a countdown to `expiresAt`
- [ ] "Join a private table" UI: a 6-character code entry field → `socket.emit('joinArena', { arenaCode })`
- [ ] Both host and joiner resolve through the **same** `startGame` handler already used for pooled matches — no separate Arena game screen needed
- [ ] Host-side "cancel table" button while waiting → `socket.emit('cancelArena', { arenaCode })`, listen for `arenaCancelled`
- [ ] Listen for `arenaExpired` the same way as `arenaCancelled` (table died from timeout, not a host action) — both mean "stake refunded, go back to the waiting-for-opponent screen's dismissal"
- [ ] Surface Arena-specific `error` messages distinctly where useful (e.g. `Daily Arena limit reached (3/day)`, `Invalid or expired arena code`) — see §3.2 Private Arena for the full list

### Admin
- [ ] Lobby editor: `id` (editable), `firstPlaceCash`, `secondPlaceCash`, `thirdPlaceCash` fields
- [ ] Bot Levels tab: 5 levels editable (Rookie → Master)
- [ ] Revenue Analytics tab: rake, bot win rate, gap vs target
- [ ] Withdrawals tab: table shows player name, email, amount, status badge, date — approve/reject buttons
- [ ] User credit: search user and post `POST /api/admin/users/:id/credit` to add balance (cash/bonus/winning/coins)

---

**Status**: Production Ready (v2.10)  
**Last Updated**: 2026-09-29  
**Changes in v3.7** ⭐ **10-player board fix, profile frames, level-up logic, richer leaderboard entries**:
- Fixed: `Match.mode` schema enum was missing `'10p'` — every 10-player match (the "Large" board size added in v3.1) failed with `Match validation failed: mode: '10p' is not a valid enum value` as soon as the match document was persisted. Added to the enum in `server/src/models/Match.ts`.
- Fixed: some named-board lobbies (e.g. "Monaco Riviera Large Board") had `playerCount` drift out of sync with `mode` in the live `SystemConfig` doc (mode `'10p'`, `playerCount` stuck at an old value) — likely from an admin `PATCH /api/admin/config` edit that changed `mode` without also updating `playerCount`. One-off fix applied to existing data (`server/scripts/fix-board-player-counts.ts`), and `PATCH /api/admin/config` now re-derives `playerCount` from `mode` server-side for known board sizes so this can't drift again.
- Added: `profileFrame` (string, client-defined id, `""` = none) on the `User` model — settable via `PATCH /api/auth/profile`, returned on `GET /api/auth/me`, `GET /api/profile/:userId`, and every entry (`leaderboard[]` + `me`) in `GET /api/leaderboard`.
- Added: level-up logic — `level` now actually increments after a match (`level = floor(xp / xpPerLevel) + 1`, new `SystemConfig.xpPerLevel`, default 1000). Previously `xp` was credited on every match (`winXP`/`lossXP`, unchanged) but `level` was never recomputed from it and stayed at its initial value forever.
- Note: custom avatar upload (`POST /api/auth/upload-avatar`, S3-backed) was already implemented as of v2.3 — no change needed there, just confirming it against this round's report.

**Changes in v3.6** ⭐ **Private Arena (host/join-by-code) documented for the first time**:
- Not a new backend feature — `hostArena`/`joinArena`/`cancelArena`/`arenaHosted`/`arenaCancelled`/`arenaExpired` were already fully implemented (`server/src/services/arenaService.ts`) but never had their own contract section, only incidental mentions. This is very likely why it isn't wired up client-side yet. Full request/response shapes, error list, and a frontend checklist added — see §3.2 Private Arena and §7 Private Arena.
- Verified end-to-end against a live local backend before documenting: host debited on `arenaHosted`, joiner matched into the same `startGame` room via the code, cancel refunds correctly.
- **This pass also added a genuinely new field**, not just documentation: `hostArena` now takes a **required** `tableName` (`server/src/socket/socketSchemas.ts`, `server/src/models/Arena.ts`), echoed back in `arenaHosted` alongside a new `hostName` (the host's own account name), and both are carried through to `startGame.tableName` for the joiner. Tables were previously identifiable only by lobby preset name + code; this lets a host actually label their own table, per the product spec's "(Player Name) Arena" convention. If you already had a Private Arena UI in progress before this doc existed, this is the one thing in this section that's an actual backend change, not just a write-up of existing behavior.
- Also fixed a stale example: the `findMatch` sample payload referenced lobby `combo-dice`. That lobby (along with 11 other legacy presets — Triple Chance, Treasure Roll, Lucky Rollers, The Yacht Club, Dice Royale, Dice Race, Roll on the Beach, Mega Rush, Golden Throw, Token Mania, Dice Island) was removed from `GET /api/lobby` — `server/src/services/configService.ts` no longer lists them in `DEFAULT_LOBBIES`, so they won't come back on config self-heal either. Remove any client-side references to these 12 lobby IDs. Swapped the example for a lobby that's actually still returned.

**Changes in v3.5** ⭐ **Regular matchmaking is now coins-only — no real money changes hands in a normal match**:
- Product decision: the app is not charging/paying real money in gameplay right now — only coins. Cash/Stripe (`/api/wallet/deposit`, `/withdraw`, the Stripe webhook, shop checkout) are **unchanged and still fully functional** as REST endpoints; they're simply no longer wired into match entry/payout. **Arena (host/join-by-code private lobbies) is unaffected and still cash-based** — this change is scoped to regular pooled matchmaking (`findMatch`) only.
- `findMatch`/`startGame`/`gameOver` payload shapes are **unchanged** — same fields as before, but cash-side values now consistently read as zero for a regular match, and coins carry the real economy:
  ```javascript
  // startGame (unchanged shape, new values):
  { stake: 0, stakeCoins: 100, prizes: [{ rank: 1, cash: 0, coins: 180 }] }
  // gameOver (unchanged shape, new values):
  { prizePool: 0, prizePoolCoins: 180, earnings: { "<userId>": 0 }, coinsEarnings: { "<userId>": 180 } }
  ```
  If your UI reads `stake`/`prizes[].cash`/`earnings`/`prizePool` for display, switch to `stakeCoins`/`prizes[].coins`/`coinsEarnings`/`prizePoolCoins` — the cash fields aren't removed (so nothing breaks by omission), they just won't reflect real activity anymore for a normal match.
- Entry fee is now `lobby.entryFeeCoins`, not `lobby.entryFeeCash` — `GET /api/lobby` still returns both fields on every lobby (cash values are kept for display/Arena reuse), but only `entryFeeCoins` is actually charged/checked for a regular match join. Coin values were derived from each lobby's existing cash price at 20 coins per $1 (matching the shop's own existing coin-package rate — see `server/scripts/migrate-coins-economy.ts`), and are trivially admin-editable per lobby afterward like any other config value.
- `POST /api/wallet/sync-deposit`, `/deposit`, `/withdraw`, Stripe checkout, and shop purchases are **completely untouched** by this — deposits/withdrawals of real cash still work exactly as documented elsewhere in this contract, they're just decoupled from gameplay for now.

**Changes in v3.4** ⭐ **Response to frontend-reported production issues (WebSocket timeout, stale reconnect, player-count concern)**:
- **New**: `findMatch` now validates that `mode` actually matches the target lobby's configured mode. Previously `mode` was accepted but never checked against anything — the real match size always came from the lobby config (looked up by `lobbyId` alone), so a wrong `mode` could never corrupt an actual match, but it also went completely unnoticed server-side. A mismatch now returns immediately:
  ```json
  { "event": "findMatch", "code": "MODE_LOBBY_MISMATCH", "message": "Lobby 'triple-chance' is mode '3p', not '2p'. Your lobby list may be stale — refresh it.", "fields": { "mode": "expected '3p' for this lobby" } }
  ```
  If you see this in production, it means the client's lobby list is stale relative to what it actually sent as `lobbyId` — refetch `GET /api/lobby` before retrying.
- **Fixed**: `reconnectMatch` against a roomId/matchId the server has no record of now always returns a structured, actionable error instead of either a bare message or — in one specific case — **nothing at all** (a genuine silent-failure bug: if a resolved match existed but was in a status other than `ongoing`/`finished`, or its Redis state couldn't be recovered, the handler previously emitted no response whatsoever):
  ```json
  { "event": "reconnectMatch", "code": "NO_ACTIVE_MATCH", "message": "No active match found to reconnect. Clear your cached match/room reference and return to the lobby." }
  ```
  On receiving `NO_ACTIVE_MATCH`, the client should clear any cached `activeRoomId`/`activeMatchId` and return to the lobby — the match genuinely does not exist server-side (this is expected after a server restart, a very old cached reference, or a match that already ended and aged out of the 300s post-game replay window).
- **Improved**: Socket.IO heartbeat is now explicit (`pingInterval: 20000`, `pingTimeout: 20000`) instead of relying on the library's implicit defaults, sending keepalive traffic frequently enough to stay inside typical reverse-proxy/tunnel idle windows. This is a server-side mitigation only — if disconnects persist in a specific production environment (e.g. behind a Cloudflare Tunnel), the tunnel/proxy's own WebSocket upgrade and idle-timeout configuration needs to be checked on the ops side; it isn't something visible or fixable from this repo. See `docs/BACKEND_AUDIT.md` §23.

**Changes in v3.3** ⭐ **Two bugs found only by actually running the full player-to-player flow** (see `docs/BACKEND_AUDIT.md` "Live E2E Verification" for the full writeup — neither was visible from code review alone):
- **CRITICAL FIX — socket connection race**: `io.on('connection')` awaited `redisService.setOnline(userId)` *before* registering any event listener (`findMatch`, `rollDiceRequest`, `tapNumber`, etc). A client that emits `findMatch` immediately on connect — completely normal client behavior — could have that packet arrive before the listener existed, and it was silently dropped: no error, no `startGame`, nothing. Reproduced live: two players emitting `findMatch` back-to-back with no artificial delay failed to form a match in 4/4 attempts before the fix, and formed one in 8/8 attempts immediately after. Fixed by registering listeners synchronously before the first `await`.
- **Fixed — `reconnectMatch` couldn't recover from the most common disconnect case**: recovery required a client-supplied `activeMatchId`, but `startGame`'s payload never includes a match id (only `roomId`), and `User.activeMatchId`/`activeRoomId` aren't real schema fields (they're computed live, only inside the `/auth/me` response). A client reconnecting purely at the socket level after a brief drop — the single most common disconnect scenario — therefore had no way to ever supply `activeMatchId` and reconnection always failed with "No active match found," even though `roomId` alone was already enough to look the match up. Now resolves the match by `roomId` whenever `activeMatchId` is missing or invalid, not only when it's invalid.
- Both were caught by actually running two real Unity-equivalent clients (register → matchmake → play → disconnect/reconnect) against a live server with real MongoDB/Redis, not by reading the code.

**Changes in v3.2** ⭐ **Security fixes, contract corrections, weekly/monthly leaderboard**:
- **CRITICAL FIX**: `POST /api/wallet/sync-deposit/:id` let any authenticated user fulfill an arbitrary pending deposit (their own, unpaid, or another user's) with no ownership check and no verification that Stripe actually confirmed payment. Now enforces ownership and re-verifies directly against the Stripe API before crediting. The webhook path (already signature-verified) is unaffected. See `docs/BACKEND_AUDIT.md` §9a.
- **Fixed**: settlement could double-count a player's wins/losses/XP/games-played/streak/playtime if BullMQ retried a settlement job (prize/coin credits were already idempotent; the stats update wasn't). Now guarded the same way every other credit in this codebase is. See §9b.
- **Fixed**: `SystemConfig.rakePercent` (admin-editable house-edge %) was never actually read anywhere — the real fallback rake was a hardcoded 10%. Now wired up; editing it in the admin panel actually changes the payout. See §9c.
- **Fixed**: `findMatch`'s documented payload (`{ lobbyId, target }`) never matched what the server actually validates (`{ mode, stake, lobbyId }`) — very likely the cause of reported "Invalid payload" errors. Corrected below; server behavior unchanged (it was already correct — see §1).
- **Fixed**: `numberLocked` and `startGame.prizes` payload documentation corrected to match actual server output (see inline `⭐ CORRECTED` notes below) — both had drifted from the real per-rank-prize-distribution implementation.
- **New**: every zod-validated socket event (`findMatch`, `joinRoom`, `rollDiceRequest`, `tapNumber`, `forfeitMatch`, `startNow`, `hostArena`, `joinArena`, `cancelArena`) now returns a structured `{ event, code: 'INVALID_PAYLOAD', message, fields }` error instead of a bare `{ message: 'Invalid payload' }` — `fields` names exactly which field failed and why. See §6.2.
- **Fixed**: concurrent-session ("logged in on another device") enforcement was broken on both transports — socket auth detected a stale session but let it connect anyway (contradicting the documented "old socket is disconnected" behavior), and REST auth had no session check at all, so a kicked session kept full API access for up to 30 days. Both now correctly reject/disconnect stale sessions.
- **Fixed**: socket auth accepted a JWT via query string unconditionally (leaks into access logs) — now only accepted outside production; `auth.token` (what real clients use) is unaffected.
- **New**: `GET /api/leaderboard` now supports `?period=weekly|monthly|all-time&category=wins|matches_played|credits_earned|current_credits` (previously all-time-wins only, hardcoded). Fully backward compatible — a request with no params behaves exactly as before. See §2.5.
- Removed dead code: `server/src/config/shopItems.ts` (`SHOP_BUNDLES`) was entirely unreferenced.
- Fixed an unrelated pre-existing test-environment bug (Stripe mock-mode detection didn't recognize a non-Stripe-shaped placeholder key) that was causing 3 test suites to fail for reasons unrelated to their actual subject matter.
- See `docs/BACKEND_AUDIT.md` and `docs/CONTRACT_GAPS.md` for the full audit this release came out of.

**Changes in v3.1** ⭐ **Weekly Tasks, Discord Login, Player Profiles, Matchmaking Waiting-Room Roster**:
- `GET /api/tasks/weekly` / `POST /api/tasks/weekly/claim` — **new**: weekly "play X matches" task system, config-driven definitions, atomic/idempotent claims, resets every Monday
- `POST /api/auth/discord` / `POST /api/auth/discord/link` — **new**: Discord OAuth2 login (authorization-code exchange, not a client-supplied token) and an explicit link-to-current-account flow that imports Discord name/avatar
- `GET /api/profile/:userId` / `POST /api/profile/:userId/like` — **new**: public profile card (self or another player) with wins/losses/games played/streaks/total & high score/play time/rank, and a one-per-viewer likes system
- `GET /api/auth/me` now also returns `country`, `league`, `loginStreak`, `gamesPlayed`, `totalScore`, `highScore`, `totalPlayTimeSeconds`, `winStreak`, `bestStreak`, `likesReceived`, `totalCreditsEarned`, `discordLinked`, `memberSince`
- New live counters, updated atomically whenever a match finishes: `gamesPlayed`, `totalScore`, `highScore`, `totalPlayTimeSeconds`, and a working `currentStreak`/`bestStreak` (previously present in the schema but never actually incremented)
- `lifetimeCreditsEarned` (`totalCreditsEarned` in the API) now actually accumulates on every wallet credit except deposits/refunds — previously present in the schema but never wired up
- `matchmakingUpdate` now includes a `players` roster (name/avatar per queued player), not just a `current/target` count — lets the waiting-room screen show individual player cards
- `startNow` — **new** client→server event: lets a queued player skip the remaining bot-fill wait and start immediately
- Referral invites now actually pay `SystemConfig.economy.referralBonus` to the inviter (previously tracked the relationship only, no reward)
- Fixed: daily-reward claim could occasionally be double-credited under concurrent requests; now atomic
- `DELETE /api/auth/avatar` — **new**: reset avatar to the default preset, deletes the old uploaded photo from S3
- Fixed: uploading a new avatar (or switching via `PATCH /profile`) never deleted the previous uploaded photo from S3 — orphaned files accumulated in storage indefinitely; now cleaned up (best-effort) on every avatar change
- Fixed: linking a social login (Google/Facebook/Apple/Discord) onto an existing by-email account never actually persisted the provider ID — silently no-opped every time due to a repository allow-list gap. Login still worked via the email-match fallback, but the ID was never saved
- Fixed: lobby/board presets (Arena tiers, named Small/Medium/Large boards) added to code after a database's config already existed could silently never appear via `GET /api/lobby` — config now self-heals missing presets on every load
- Disconnecting mid-match now auto-forfeits the player after a grace period (default 30s) if they don't reconnect anywhere, instead of the match sitting idle until the full match timer expires
- **Not included** (flagged for a future infra project, not a code change): Discord Rich Presence, "Play inside Discord Activities", Discord server leaderboards, and Discord match notifications all require a persistent Discord bot process — out of scope for a REST/socket backend change

**Changes in v3.0** ⭐ **Withdrawal Fees & Limits, Transactional Emails**:
- **Stripe now emails its own payment receipt automatically** (`receipt_email` on the Checkout Session / PaymentIntent) — previously receipts had to be sent by hand from the Stripe Dashboard. This is separate from our own deposit confirmation: Stripe's is the payment receipt (card brand, last4, Stripe reference), ours confirms the wallet credit
- **Deposit, withdrawal and welcome emails are now sent** — the templates existed but had no callers, which is why QA reported them as not working. The withdrawal email fires when the payout is actually sent (admin completion), not when it is requested, and quotes the net amount received
- `GET /api/wallet/withdrawal-info` — **new endpoint**; returns min/max withdrawal amounts, the fee tier table, the player's withdrawable balance, and (with `?amount=`) a quote showing the exact fee and payout before they commit
- **Withdrawal fee** now withheld from the requested amount: $0–$20 → $1, $21–$100 → $4, $100+ → $10. Requesting $50 debits $50 and pays out $46
- **Minimum $10 / maximum $300** per withdrawal, now enforced server-side (`minWithdrawalAmount` existed in config but was never read)
- **Bonus balance is unaffected by withdrawals** — it is already excluded from `withdrawableBalance`, so a withdrawal cannot pay out promotional funds
- `POST /api/wallet/withdraw` — returns `withdrawalFee` and `netAmount` on the transaction; rejects with 400 and a specific reason when limits are breached
- `GET /api/wallet/history` — withdrawal rows now include `withdrawalFee` and `netAmount` (`null` for other types)
- Fee tiers, min and max are configurable via `SystemConfig` (`withdrawalFeeTiers`, `minWithdrawalAmount`, `maxWithdrawalAmount`) — the client must read them from the API rather than hardcoding
- Admin payout: `lockedBalance` is released by the full amount; the actual sum to pay the player is `netAmount`

**Changes in v2.10** ⭐ **Icon Tap Race Condition Fix**:
- Icon cells now use negative integer markers (`-1`, `-2`, `-3`) instead of `0` in the matrix
- This makes icon identity explicit at tap time — frontend sends the actual cell value it's tapping
- **Eliminates race condition**: Previously, all icons were sent as `val: 0`, making it impossible to distinguish if an icon expired. Now each icon has a unique marker.
- **Integrity check now works**: `matrix[index] === val` validation catches if an icon expired between tap and server processing
- Frontend integration: treat any negative matrix value as an icon cell; when tapping an icon, send `{ val: matrix[index], index: position }` where `val` is `-1`, `-2`, or `-3`
- Anti-cheat: wrong taps (value mismatch), late taps (icon expired), and already-locked cells are all caught by the integrity check
- `tapNumber` payload now documents icon tapping examples with icon markers

**Changes in v2.9**:
- `POST /api/support-tickets` — new endpoint; user creates support ticket with email, subject, description, category; rate limited 3 per email per 24h; confirmation email sent automatically
- `GET /api/support-tickets` — user retrieves their own tickets
- `GET /api/admin/support-tickets` — admin retrieves all tickets (filterable by status/priority); joins with User data (name, avatar)
- `PATCH /api/admin/support-tickets/:id/reply` — admin adds reply to ticket; auto-changes status `open` → `in_progress`; sends reply notification email to user
- `POST /api/admin/support-tickets/:id/close` — admin closes ticket
- `SupportTicket` MongoDB model with status (`open`, `in_progress`, `resolved`, `closed`), replies array, priority, category
- `emailService` new methods: `sendSupportTicketConfirmationEmail()` and `sendSupportTicketReplyEmail()` via Resend
- Admin UI: `AdminSupportPage` lists all tickets; `AdminSupportDetailPage` shows detail view with reply form
- Audit logging for all ticket actions (created, replied, closed)

**Changes in v2.8**:
- `POST /api/auth/forgot-password` — new endpoint; requests password reset email (rate limited: 5 per email per 24h); always returns success (don't reveal if email registered)
- `POST /api/auth/reset-password` — new endpoint; resets password with time-limited token (1 hour expiry); invalidates all user sessions (force re-login on all devices)
- `emailService` — new Resend integration for email delivery (welcome, password reset, payment confirmations)
- Frontend pages: `/forgot-password` (email entry) and `/reset-password` (new password form with token validation)
- "Forgot Password?" link added to login screen
- Password reset confirmation email sent after successful reset
- User model extended with `resetToken` and `resetTokenExpires` fields; TTL index auto-cleans expired tokens

**Changes in v2.7**:
- `GET /api/shop/items` — new endpoint; returns all enabled shop items (offers + lucky deals) with live prices and award amounts; admin can edit without app release
- `POST /api/shop/checkout` — new endpoint; creates Stripe Checkout Session for a shop item; returns `checkoutUrl`; Unity opens URL in Chrome via `Application.OpenURL()`; Stripe handles card form and redirects to our success page which fires `blingodice://` deep link back to Unity
- `POST /api/wallet/deposit` — **breaking change**: no longer returns `clientSecret`/`paymentIntentId`; now returns `checkoutUrl`/`sessionId` (Stripe Checkout Session); accepts optional `returnUrl` parameter
- All 13 Unity IAP product IDs mapped to backend: `package_1`–`package_6` (Lucky Deals) and `offer_quick_boost`, `offer_premium_value`, `offer_smart_starter`, `offer_bonus_plus`, `offer_power_deal`, `offer_best_offers`, `offer_mega_bundle` (Offers)
- Payment award webhook: backend credits `cashBalance` + `bonusBalance` + `coins` atomically when Stripe fires `checkout.session.completed`; no `SyncDeposit` call needed
- Payment pages: `https://game.winjogames.com/deposit`, `/payment-success`, `/payment-cancel` — hosted on our React frontend; handle `returnUrl` deep link redirect back to Unity
- Deep link scheme: `blingodice://open/payment-success` (success) and `blingodice://open/payment-cancel` (cancelled) — Unity listens via `Application.deepLinkActivated`
- Shop admin panel: admins can edit prices, awards, labels, and enable/disable items via admin panel → "Shop & Offers" tab; `POST /api/admin/shop/seed` seeds default items; `PUT /api/admin/shop/:itemId` updates an item
- Unity integration: remove `TriggerLocalCheckoutFlow`, `SimulateLocalPaymentCoroutine`, and `SyncDeposit` from `InAppPurchaseManager.cs` — replace with `POST /api/shop/checkout` + `Application.OpenURL()` flow

**Changes in v2.6**:
- `GET /api/wallet/balance` — new lightweight balance snapshot endpoint; returns `coins`, `balance`, `cashBalance`, `bonusBalance`, `winningBalance`; used for polling after rewarded ads to detect coin credit
- `GET /api/rewards/ad-complete` — LevelPlay/IronSource S2S reward callback; no JWT auth; verified via MD5 signature (`appUserId + rewards + eventId + privateKey`); idempotent via `eventId`; null/invalid userId returns 200 to stop LevelPlay retry loops; MongoDB ObjectId validated before any DB query; coins credited via `walletService.credit` with idempotency key `ad_${eventId}`
- Socket events `adRewardGranted` and `userStatsUpdated` — emitted best-effort via Redis emitter after ad reward; non-fatal if socket not connected (ads shown outside lobby/gameplay)
- Lobby schema extended with `entryFeeCoins`, `firstPlaceCoins`, `secondPlaceCoins`, `thirdPlaceCoins` for coin-based lobbies (all default to 0)
- `startGame` socket payload extended with `stakeCoins` and `prizes` object (`{ firstPlaceCash, secondPlaceCash, thirdPlaceCash, firstPlaceCoins, secondPlaceCoins, thirdPlaceCoins }`)
- Unity integration notes: `IronSource.Agent.setUserId(userId)` must be called before `init()` and `loadRewardedVideo()`; poll `GET /api/wallet/balance` every 2s after `onRewardedVideoAdRewarded` fires; stop on coins increase or 15s timeout

**Changes in v2.5**:
- `GET /api/auth/me` — added `withdrawableBalance` field (alias for `winningBalance`; only prize earnings can be withdrawn)
- `POST /api/wallet/withdraw` — KYC requirement removed; withdrawals go directly to `pending` for admin review; documented withdrawal flow (request → pending → admin approve/reject)
- `GET /api/admin/withdrawals` — response now includes `userName` and `userEmail` (populated from User collection); previously returned raw userId only
- `POST /api/admin/withdrawals/:id/action` — documented approve/reject flow including Stripe payout and automatic fund-revert on failure
- `POST /api/admin/users/:id/credit` — documented manual balance credit endpoint
- Admin UI: withdrawals table now shows player name, email, status badge

**Changes in v2.8**:
- **Score Counting Bug Fixed** — all gameplay events now **always include** the player's current `score`: `diceRolled`, `numberLocked`, `playerMilestone`, `tapResult`, `matchRecovered`
- **Persistent Score Storage** — scores are now persisted to the Match document during gameplay (on every roll and tap), not just at match end, ensuring accurate final scores even if process crashes
- **Score Recovery on Reconnect** — `matchRecovered` now includes `score` for all players in the `gameState` object, so clients always have the authoritative score after reconnecting
- **playerMilestone Updated** — now includes `lockedCount` to show progress, and `score` which includes the +1200 completion bonus

**Changes in v2.4**:
- `GET /api/auth/me` — `activeMatchId`/`activeRoomId` now live-queried from Match collection (never stale); new `recentMatch: { matchId, roomId, status }` field always returns the most recent finished match
- `GET /api/wallet/history` — cleaned response shape (strips internal fields); proper `type` enum (`win_prize`, `entry_fee`, `refund`, `deposit`, `withdrawal`, `bonus`); full `status` / `balanceType` documentation
- `GET /api/leaderboard` — new global leaderboard ranked by wins; returns top N players + calling user's own rank/stats regardless of position
- `POST /api/invites/invitedby` — new one-time inviter assignment (Branch.io integration); safe to call on every cold start
- `GET /api/invites/history` — new invite history endpoint; returns list of users invited by current user with live `isOnline` status from Redis
- Online presence system added: user marked online on socket connect, offline on disconnect (5-min TTL safety net)
- Match status lifecycle hardened: match always marked `finished` in DB even when both players disconnect; `startMatch` error path marks match `cancelled`; `earnings` field stored on Match document; recovery `gameOver` re-emit now uses stored earnings (previously used wrong formula)

**Changes in v2.3**: Avatar upload endpoint (`POST /upload-avatar`) — server processes image to 512×512 circular PNG, uploads to S3 `avatars/` prefix; `avatar` field in all socket events is now either an HTTPS URL (custom upload) or a preset key string (e.g. `"avatarBoy"`); Redis avatar cache populated on login so no per-match DB lookups  
**Changes in v2.2**: Per-rank prize payouts (firstPlaceCash/secondPlaceCash/thirdPlaceCash), editable lobby ID, win condition fixed (first to reach target wins immediately), settlement uses DB player list, `matchRecovered` sends full `gameState` per player  
**Changes in v2.1**: Logout endpoint, server-driven `countdown` socket event, `players` profiles in `gameOver`, `timeRemaining` + `gameplayStartsAt` in `matchRecovered`  
**Changes in v2.0**: Configurable bot levels, revenue analytics, new win condition system, staggered bot joining
