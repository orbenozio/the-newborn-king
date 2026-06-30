# The Newborn King - agent notes

A standalone game built on the **Crossroads engine** (pulled as a git UPM package). This file covers what's
specific to this game; the full build/content/press/release playbook lives in the engine repo:
**`orbenozio/Crossroads` -> `CLAUDE.md`** (https://github.com/orbenozio/Crossroads/blob/master/CLAUDE.md).
Read that for the recipe and the gotchas (Unity won't re-resolve a manifest live -> restart; use batchmode
`-runTests` not the bridge's; move files with their `.meta`; etc.).

## This game

- **Format:** Reigns-style (swipe a card, 4 meters, break = game over). The original **POC** for the whole
  engine - its authoring bridge tools (`nbk_*`, `wire_game_scene`, `set_*`) are the template the other games
  copy from.
- **Meters (4):** sleep / sanity / money / baby. **Deck:** ~15-25 cards with flags + branching endings + a
  reign length (`maxTurns`).
- **Identity:** productName `Newborn King`, app id `com.crossroads.newbornking`.
- **Engine pin:** `com.orbenozio.crossroads` at `#v0.1.1` (see `Packages/manifest.json`).

## Layout

- `Assets/Game/` - `GameBootstrap.cs`, `Crossroads.Game.NewbornKing.asmdef` (-> Engine + UI),
  `Content/{story.json,resources.asset,theme.asset}`, `Scenes/Game.unity`, `Art/`, `Audio/`.
- `Assets/Game/Tests/` - content + QA (M1/M3) + architecture tests (local `Assets/Game/Content/...` paths).
- `Assets/Editor/` - `build_webgl.cs` / `build_android.cs` (scene `Assets/Game/Scenes/Game.unity`).
- `press/` (tracked) - cover, screenshots, gif, mp4. `itch/` (gitignored) - the WebGL build zip.

## Common tasks

- **Test:** `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults out.xml -logFile log`
  then read `out.xml`.
- **Build:** the `build_webgl` / `build_android` bridge tools (block; also write `Builds/last_build.json`).
- **Bump engine:** re-pin `Packages/manifest.json` to the new tag, open the editor once to re-resolve, commit
  the updated `packages-lock.json`.
