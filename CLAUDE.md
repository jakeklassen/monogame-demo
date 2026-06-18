# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A MonoGame (DesktopGL, .NET 10) shoot-'em-up called **CherryBomb** ("Short Shwave Shmup"). Despite the repo name `monogame-demo`, the code namespace and game are CherryBomb. It is a wave-based shmup rendered at an internal **128×128** resolution and scaled up to a 512×512 window.

## Commands

```bash
dotnet tool restore   # restore mgcb + csharpier (required once; also runs automatically before Restore)
dotnet build          # builds; MonoGame.Content.Builder.Task compiles Content/ automatically
dotnet run            # build + run the game
dotnet csharpier .    # format all C# (config in .csharpierrc-equivalent defaults; uses TABS)
```

There is no test suite. Content is built automatically during `dotnet build` via `MonoGame.Content.Builder.Task` driving `Content/Content.mgcb`.

### Content pipeline notes
- `Content/Content.mgcb` is the asset manifest. Edit it with `dotnet mgcb-editor` or by hand.
- `pipeline-references/MonoGame.Extended.Content.Pipeline.dll` must exist for the content build (referenced via `MonoGameExtendedPipelineReferencePath` in the csproj and `/reference:` in the .mgcb). It is gitignored-adjacent — do not delete it.
- Sprites use magenta (`255,0,255`) as the color key for transparency.

### In-game debug keys
`D` toggles debug rendering (collider boxes), `F` toggles fixed timestep, `V` toggles vsync, `Esc`/gamepad Back quits. These are handled in `Game1.Update`.

## Architecture

The game is built on the **Arch ECS** library plus **MonoGame.Extended** (screens, tweening, bitmap fonts, input listeners, camera). The two big-picture patterns:

### Screens own everything (ECS per screen)
`Game1` is thin: it sets up graphics, a `BoxingViewportAdapter` + `OrthographicCamera` (128×128 logical), shared caches (`FontCache`, `TextureCache`), global `Config` and `State`, and hands control to MonoGame.Extended's `ScreenManager`. It starts on `TitleScreen` → `GameplayScreen`.

Each screen (`Screens/`) creates its **own** Arch `World`, its own `Tweener`, and two ordered lists of systems: `_updateSystems` and `_drawSystems`. `LoadContent` registers systems and spawns initial entities; `Update`/`Draw` just iterate the respective list calling `system.Update(gameTime)`. `UnloadContent` calls `World.Destroy(_world)`. To change gameplay, you almost always add/modify a system and register it in the screen's `LoadContent` — order matters.

### Systems
- `Systems/SystemBase<T>` is the base: holds a `World`, exposes `abstract void Update(in T state)` (T is always `GameTime`). Systems are plain classes constructed with the `World` plus whatever else they need (Game, Tweener, GraphicsDevice, Camera, caches).
- Systems query via Arch `QueryDescription().WithAll<...>().WithNone<...>()` and either `World.Query(...)` with a ref-struct lambda or `World.GetEntities(...)`.
- **Update systems** mutate component state (movement, collision, player input, wave spawning, particles, tweened animations).
- **Rendering systems** end in `RenderingSystem`, each owns its own `SpriteBatch`, and draws with `SamplerState.PointClamp` (pixel art) using `Camera.GetViewMatrix()`. Note `SpriteRenderingSystem` sorts entities by `Id` for stable draw order, and excludes entities `WithNone<Flash>` (flashing sprites are drawn by `FlashRenderingSystem`).

### Components
`Components/` holds plain data classes (primary-constructor style). Tags are empty marker components (`TagPlayer`, `TagEnemy`, `TagBullet`, `TagInactive`). Events are short-lived entities carrying an event component (`EventNextWave`, `EventPlayerProjectileEnemyCollision`) that a system processes and then destroys — this is the inter-system messaging mechanism.

### Collision
Bitmask layer/mask system in `Constants.cs` (`CollisionMasks`: Player, PlayerProjectile, Enemy, EnemyProjectile, Pickup). Entities carry a `CollisionLayer` (what they are) and `CollisionMask` (what they collide with); `CollisionSystem` checks overlaps and emits collision event entities.

### Game data / waves
`Config.cs` holds all tunable data: enemy stats, projectile damage, and the **9 hardcoded waves** as `int[][]` grids (0 = empty, 1–5 = enemy type id). `NextWaveEventSystem` reads the current wave, spawns enemies, and tweens them into formation. `State` (in `Game1.cs`) holds runtime game state (score, lives, wave, flags). `SpriteSheet.cs` is the static atlas map: per-sprite `Frame` rectangles, `BoxCollider`s, and named animations into `Graphics/shmup.png`.

### Supporting libs (`Lib/`)
- `Lib/Tweening/` — a self-contained tweening engine (`Tweener`, `Tween`, easing functions, reflection-based member tweens). Used to animate `Transform.Position` etc. Each screen owns a `Tweener` and must call `_tweener.Update(dt)` every frame.
- `Lib/Pico8.cs` — `Pico8Extensions` generates `circ`/`circfill` textures at load time (cached in `Game1.TextureCache` as `circ-N`/`circfill-N`); `Pico8Color` (in `Constants.cs`) is the 16-color PICO-8 palette used throughout for `Text`/`Blink`.
- `Lib/Timer.cs`, `Lib/SimpleFps.cs` — utility helpers.

## Conventions
- Formatting is **csharpier** with **tabs** for indentation. Run `dotnet csharpier .` before considering work done.
- Color name collision: `Components.Color` vs XNA's `Color` — files commonly alias `using XnaColor = Microsoft.Xna.Framework.Color;`. Match the surrounding file.
- C# latest features are used freely: primary constructors, collection expressions (`[]`), target-typed `new`, switch expressions.
