# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

This is a Unity 6000.0 LTS project (C#) with no package manager, linter, or test
runner — there are zero `.asmdef` files and zero test files/folders anywhere in the
project.

- **Opening the project**: first import auto-creates `Assets/AirXonix/Scenes/Game.unity`
  via `Editor/SceneBuilder.cs`, adds it to Build Settings, and applies mobile player
  settings. If a script edit ever leaves the scene out of sync, use the Editor menu
  **AirXonix ▸ Rebuild Game Scene**.
- **Headless/CI builds** (`Editor/BuildScript.cs`):
  ```
  Unity -batchmode -quit -projectPath . -executeMethod AirXonix.EditorTools.BuildScript.CI_Android
  Unity -batchmode -quit -projectPath . -executeMethod AirXonix.EditorTools.BuildScript.CI_iOS
  ```
  Output goes to `Builds/Android/AirXonix.apk` / `Builds/iOS/`.
- **Gotcha**: a headless `-batchmode` invocation fails with "another Unity instance is
  running with this project open" if the project is already open in an Editor window —
  you cannot run a second headless instance against the same project concurrently. If
  the user's Editor already has the project open, verify compilation through that
  Editor's own Console instead of trying to shell out to Unity.
- **Not a git repo**: there is no `.git` directory anywhere (only a `.gitignore` file
  exists) — `git` commands won't work here unless the user initializes one.

## Architecture

- **Everything is generated at runtime in code** — no hand-authored prefabs, scenes, or
  art assets anywhere. This is an enforced convention: `Editor/SceneBuilder.cs`
  auto-creates the one scene on first import, and `Core/Bootstrap.cs` is a runtime
  safety net (`RuntimeInitializeOnLoadMethod`) that rebuilds the camera/light/
  `GameManager` if the scene ever loses them. These two intentionally duplicate the same
  camera/lighting setup and must be kept in sync if either changes.
- **Strict data/logic vs. presentation split**: `Grid/GridModel.cs` (pure `Cell[]` data,
  no Unity types) and `Grid/CaptureSolver.cs` (the flood-fill capture rule) never
  reference rendering. `Grid/GridView.cs` is the only thing that turns grid state into
  visuals. Entities place themselves purely via `GridView.CellToWorld`/
  `CellSpaceToWorld` — never by touching `GridModel` directly for positioning.
- **`Core/GameManager.cs` is the central orchestrator**: it spawns the grid, player,
  enemies, and HUD, owns the `GameState` state machine (`Menu`/`Playing`/`Paused`/
  `LifeLost`/`LevelComplete`/`GameOver`), and drives the per-frame game loop. Entities
  and UI are wired together through `Init(...)` calls made here, not serialized
  Inspector references.
- **Visual layer is 2.5D**: the ground plane is XZ with real height. `GridView` builds a
  blocky voxel/heightmap mesh — per-cell flat-topped quads at a height determined by
  cell state (water sunken, trail mid-height, claimed land raised), plus generated
  vertical cliff-wall quads at every height boundary and at the field perimeter. The
  camera is a tilted (55°) orthographic camera that softly tracks the player while
  clamping its pan so most of the field stays visible (`GameManager.FitCamera` /
  `TrackCamera`). Entities are `GameObject.CreatePrimitive` meshes (Cube/Sphere/
  Cylinder for player/ball/patrol enemy respectively), not flat sprites.
- **Zero physics anywhere by design** — no `Rigidbody`/`Collider` in the project.
  Entity movement and collision are hand-rolled (`BallEnemy` does manual per-axis
  bounce against filled cells; `PatrolEnemy` wall-follows the land/water boundary).
  Primitive meshes are fetched through a cached helper (`Core/Util.cs → PrimMesh`)
  specifically so entities never pick up an auto-added `Collider`.
- **Shader constraint**: only the `Standard` shader is confirmed present in
  `GraphicsSettings.m_AlwaysIncludedShaders`. Since nothing in this project is a
  serialized `.mat` asset for Unity's build-time shader stripper to discover, any other
  shader risks coming back `null` on-device despite working fine in the Editor.
- Uses the classic Input Manager, not the new Input System; `SceneBuilder` auto-switches
  *Active Input Handling* to "Both" on first import if needed.
- No `.asmdef` anywhere — everything compiles into the default
  `Assembly-CSharp` / `Assembly-CSharp-Editor`.

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
  Editor/      SceneBuilder (auto scene + player settings), BuildScript (CI builds)
  Scenes/      Game.unity  (generated on first import)
```

## Tuning knobs

Field size, difficulty, and progression constants live on `GameManager`; terrain
colors/heights are on `GridView`; player step speed is on `PlayerController`.
