# _Game

All game code and content lives here. Layers follow the project architecture doc (design/architecture.md in the shared project files, not in this repo):

- **Scripts/Core** - pure C# game rules (board, merges, chests, storage, castles). Assembly `Game.Core`, no UnityEngine references, so it is easy to unit-test.
- **Scripts/Content** - ScriptableObjects with game data, converted into Core models.
- **Scripts/View** - MonoBehaviours that draw the board and play animations, driven by Core state.
- **Scripts/UI** - menus, HUD, popups.
- **Scripts/Platform** - `IPlatform` and its implementations for Yandex Games (WebGL) and Google Play (Android): ads, purchases, saves, leaderboards.
- **Data** - ScriptableObject assets.
- **Art** - sprites, fonts, audio.
- **Scenes** - game scenes.
- **Tests/EditMode** - NUnit tests for Game.Core (assembly `Game.Core.Tests`).
## Game.Core status

`Scripts/Core` is a one-to-one port of `prototype/core.js` (board, chests, storage, merging, castles, runs, broom and hammer, daily gift), plus the balance bot from `prototype/sim.js` in `Scripts/Core/Sim`. The demo castles live in `DemoContent.cs` until they exist as ScriptableObjects.

Run the tests: Window → General → Test Runner → EditMode → Run All. Or from the command line:

```
Unity.exe -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults results.xml
```

`BotPlaysTheSameGamesAsThePrototype` checks that the C# bot plays exactly the same games as `node prototype/sim.js` with the same seed. If the rules change, change both and update the expected numbers.
