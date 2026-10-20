# _Game

All game code and content lives here. Layers (see /design/architecture.md):

- **Scripts/Core** - pure C# game rules (board, merges, chests, storage, castles). Assembly `Game.Core`, no UnityEngine references, so it is easy to unit-test.
- **Scripts/Content** - ScriptableObjects with game data, converted into Core models.
- **Scripts/View** - MonoBehaviours that draw the board and play animations, driven by Core state.
- **Scripts/UI** - menus, HUD, popups.
- **Scripts/Platform** - `IPlatform` and its implementations for Yandex Games (WebGL) and Google Play (Android): ads, purchases, saves, leaderboards.
- **Data** - ScriptableObject assets.
- **Art** - sprites, fonts, audio.
- **Scenes** - game scenes.
- **Tests/EditMode** - NUnit tests for Game.Core (assembly `Game.Core.Tests`).