# AirXonix (Unity, iOS + Android)

A playable Xonix / AirXonix-style game: steer a marker around the field, cut into
the water, and reclaim the sections that don't have a ball trapped inside them.
Claim **75%** of the water to clear a level. Balls that touch you *or your
unfinished trail* cost a life.

Everything (grid, player, enemies, HUD, menus, on-screen D-pad, sound) is created
in code at runtime, so there is nothing to wire up in the Inspector.

---

## Requirements

- **Unity 6000.0 LTS** (Unity 6) or **2022.3 LTS**. Any nearby version works —
  Unity Hub will offer to open/upgrade. The pinned version is in
  `ProjectSettings/ProjectVersion.txt`.
- The project depends on the built-in **uGUI** package (`com.unity.ugui`, already
  listed in `Packages/manifest.json`). It resolves offline from the editor.
- Add these modules when installing the editor (Unity Hub → *Installs* → gear → *Add modules*):
  - **Android Build Support** (+ *OpenJDK*, *Android SDK & NDK Tools*)
  - **iOS Build Support** (macOS only)

## Open & play

1. Unity Hub → **Add** → select this folder (`AirXonix/`).
2. Open the project. On first import it compiles the scripts and automatically
   creates **`Assets/AirXonix/Scenes/Game.unity`**, adds it to *Build Settings*,
   and applies mobile player settings. Watch the Console for
   `[AirXonix] Created Assets/AirXonix/Scenes/Game.unity`.
3. Open that scene (double-click it in the Project window) and press **Play**.
   - If a script ever gets out of sync, use the menu **AirXonix ▸ Rebuild Game Scene**.

### Controls

| | Desktop / Editor | Mobile |
|---|---|---|
| Move | Arrow keys / WASD | Swipe anywhere, or the on-screen D-pad |
| Pause | `Esc` or the `| |` button | `| |` button |

## Build

### Android
- Menu **AirXonix ▸ Build ▸ Android (.apk)** → output in `Builds/Android/AirXonix.apk`.
- Or *File ▸ Build Settings ▸ Android ▸ Switch Platform ▸ Build*.
- Bundle id is preset to `com.airxonix.game`, min SDK 24. Change in
  *Project Settings ▸ Player* if you want your own.
- Headless / CI:
  ```bash
  /path/to/Unity -batchmode -quit -projectPath . \
    -executeMethod AirXonix.EditorTools.BuildScript.CI_Android
  ```

### iOS
- Menu **AirXonix ▸ Build ▸ iOS (Xcode project)** → output folder `Builds/iOS/`.
- Open `Builds/iOS/Unity-iPhone.xcodeproj` in Xcode, pick your signing **Team**,
  select a device, **Run**.
- Headless / CI: `-executeMethod AirXonix.EditorTools.BuildScript.CI_iOS`.

## Input handling

The game uses Unity's classic Input Manager. If your editor is set to
*Input System Package (New)* **only**, the project auto-switches
*Active Input Handling* to **Both** on first import (restart the editor once if
prompted). You can also set it manually in *Project Settings ▸ Player ▸ Other Settings*.

## Tuning

Select the **AirXonix** object in the scene — all knobs are on `GameManager`:

| Field | Meaning |
|---|---|
| `cols`, `rows`, `border` | field size in cells |
| `targetFraction` | fraction of water to claim per level (default `0.75`) |
| `startLives` | lives at game start |
| `baseBalls`, `ballsPerLevel` | ball count = `baseBalls + (level-1)*ballsPerLevel` |
| `baseBallSpeed`, `ballSpeedPerLevel`, `maxBallSpeed` | ball speed ramp (cells/sec) |
| `patrolFromLevel` | first level the pink shoreline enemy appears |

Colours are on the `Grid` object (`GridView`): `emptyColor`, `filledColor`, `trailColor`.
Player step speed is `stepInterval` on the `Player` object.

## Project layout

```
Assets/AirXonix/
  Scripts/
    Core/      Bootstrap, GameManager, Util
    Grid/      GridModel (data), CaptureSolver (flood-fill claim), GridView (render)
    Entities/  PlayerController, BallEnemy, PatrolEnemy
    Input/     TouchInput (swipe + keyboard)
    UI/        GameHud (HUD, menus, D-pad)
    Audio/     Sfx (procedural blips)
  Editor/      SceneBuilder (auto scene + player settings), BuildScript
  Scenes/      Game.unity  (generated on first import)
```

## Notes

- Art is procedural (flat colours + generated sprites). Swap `GridView` for a
  tilemap/shader and replace the entity sprites for a richer look; the game logic
  doesn't care.
- The capture rule is the classic one: closing a trail fills every open region a
  ball can't reach, plus the trail itself.
