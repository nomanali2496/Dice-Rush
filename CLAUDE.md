# Dice Blitz — Claude Code Guide

## Backend Contract

**Always read the contract before touching any API endpoint, socket event, or response model.**

```
/Users/nasir41/Unity Projects/Github/winjo-dice-rush/backend_contract.md
```

Rules:
- Check the contract for the correct HTTP method, path, and response shape before adding or changing any `BackendAuthService` method
- Check Section 3.3 (Server → Client socket events) before adding any `socket.On` listener
- Never call server-to-server endpoints from Unity (e.g. `GET /api/rewards/ad-complete` is S2S only)
- `GET /api/wallet/balance` is the correct polling endpoint after rewarded ads — not a custom reward claim

## Project Structure

| Path | Purpose |
|---|---|
| `Assets/Scripts/BackendAuthService.cs` | All REST API calls + response models |
| `Assets/Scripts/SocketManager.cs` | Socket.IO connection lifecycle + global socket events |
| `Assets/Scripts/MatchmakingModel.cs` | Matchmaking + in-game socket events |
| `Assets/Scripts/GameManager.cs` | Game state, timer, score, game over flow |
| `Assets/Scripts/HorizontalLobbyManager.cs` | Lobby UI, matchmaking entry, socket lifecycle for lobby |
| `Assets/Scripts/PlayerModel.cs` | Static player state (cash, coins, id, etc.) |
| `Assets/Scripts/BackendConfig.cs` | ScriptableObject — dev/prod environment URLs |
| `Assets/Resources/BackendConfig.asset` | Live config asset (set `activeEnvironment` here) |
| `Assets/Editor/BackendEnvironmentMenu.cs` | Tools → Dice Blitz → Environment switcher |
| `Assets/Editor/BackendBuildPreprocessor.cs` | Blocks Android/iOS builds if still on Development env |
| `Assets/Scripts/AdManager.cs` | LevelPlay/IronSource ad SDK wrapper |
| `Assets/Scripts/WatchVideoRewardController.cs` | Rewarded ad flow — polls `/api/wallet/balance` |

## Currencies

Two currencies only:
- **Cash** (`PlayerModel.CashBalance`) — real money
- **Coins / Tokens** (`PlayerModel.Coins`) — interchangeable terms in UI

All balance mutations go through `PlayerModel.UpdateData()`. Server is always authoritative.

## Environment Switching

- Switch via **Tools → Dice Blitz → Environment → Development / Production**
- Or select `Assets/Resources/BackendConfig.asset` in Inspector
- Android/iOS builds prompt to switch to Production if still on Development

## Socket Lifecycle Rule

- Socket connects when the **lobby panel becomes active** (`HorizontalLobbyManager.OnEnable`)
- Socket stays alive through gameplay → back to lobby
- Socket disconnects **only** when navigating to the Home/Main screen (`OnNavigateToHome`)
- Never disconnect socket on game over or when returning from gameplay to lobby
