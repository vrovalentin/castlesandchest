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

## Game scene

`Scenes/Main.unity` holds a camera and an `AppController`. At Play it loads the save (PlayerPrefs, `Game.Core.SaveMapper` + JsonUtility),
gives the daily gift and opens the build screen, or the mine if a run is in progress. Everything else is built in code:

- `BuildScreen`: castle slots by tier, "needed by the castle", chests, storage by tier, merging 2 → 1, castle done → next castle (storage burns, figurines stay).
- `MineController` + `BoardView`: the board; `BoardView` animates each move phase by phase (remove, fall, chests drop out, refill, shuffle); `CellView` is one block or chest with its stars.
- The game is saved after every action.

Sprites are placeholders drawn in code (`SpriteFactory`), and the screens are temporary IMGUI, until the real art and UI.

Castle art (`CastleArt`) is layered: `Art/Resources/Castles/<castle id>/` holds `c<N>_base.png` (faded blueprint), one `c<N>_<slot>_<part>.png` per slot in slot order, and `<castle id>_full.png`, all on the same canvas. The build screen draws the blueprint and the filled slots over it. A castle without art (castle_2 for now) shows only the slot buttons. Empty slots also get an outline, `c<N>_line_<slot>.png`.

Board blocks and closed chests come from `Art/Resources/Board/block_<colour>.png` and `chest_<colour>.png` (256 × 256, transparent); open chests are `chest_<colour>_open.png` (512). Part icons are `Art/Resources/Parts/part_<part id>.png` (256), screen backgrounds `Art/Resources/UI/bg_mine.png` and `bg_build.png`, the sparkle `Art/Resources/Fx/fx_sparkle.png`. `GameArt` loads them; anything missing falls back to code-drawn shapes. The scripts in `tools/art` cut the art: `cut_blocks.py <in> <out> [prefix] [size]` removes the white background and crops square, `recut_castle_1.py` cuts the castle layers. New drawings arrive unprocessed in `art_source/incoming/`.

Recreate the scene and portrait player settings with the menu **Castles → Setup project**, or
`Unity.exe -batchmode -projectPath unity -executeMethod Game.EditorTools.ProjectSetup.Run -quit`.

PlayMode tests (`Tests/PlayMode`) cover the mine (taps, hammer, views match the board) and the app (chests, placing, merging, next castle, leaving and continuing a run). They never touch the real save.
