# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A MonoGame (DesktopGL, .NET 10) top-down space shooter called **Space Drift**. The
repo directory and csproj are still named `monogame-demo`, but the assembly, root
namespace, and game are **SpaceDrift** (`<AssemblyName>`/`<RootNamespace>` in the
csproj; window title "Space Drift").

It is a **faithful port of a web prototype**: `~/code/gamedev/pixel-art-smoother-movement/src/space-drift`
(TypeScript on the author's own "objecs" ECS, rendered with pixi.js). A Love2D
port of the same game lives at `~/code/gamedev/love2d-typescript`. When in doubt
about behaviour or tuning, the web source is the source of truth — most systems
and constants are ported near 1:1 and cite their origin in comments.

The game renders at an internal **256×192** resolution, blitted ×**4** into a
1024×768 scene target, which is then bilinear-upscaled to fill a DPI-scaled
window (~1280×960). The signature feature is **buttery sub-pixel-smooth camera
movement** (see the render pipeline below).

> NOTE: this repo was pivoted from an earlier wave-based shmup ("CherryBomb"),
> recoverable from git history / the `cherrybomb` branch. A few inert leftovers
> may remain (e.g. `Systems/SystemBase.cs` is unused).

## Commands

```bash
mise exec -- dotnet build          # build; MonoGame.Content.Builder.Task compiles Content/ automatically
mise exec -- dotnet run            # build + run (mise provides the .NET 10 toolchain)
mise exec -- dotnet csharpier format .   # format all C# — csharpier 1.x needs the `format` subcommand; uses TABS
```

`mise exec --` is used because the system .NET may be an older SDK; mise pins .NET
10. There is no test suite. Content builds automatically during `dotnet build`.

### Content pipeline notes
- `Content/Content.mgcb` is the asset manifest. Edit with `dotnet mgcb-editor` or by hand.
- `pipeline-references/MonoGame.Extended.Content.Pipeline.dll` must exist for the content build (referenced via `MonoGameExtendedPipelineReferencePath` in the csproj and `/reference:` in the .mgcb). Do not delete it.
- Sprites (`Graphics/shmup.png`) use magenta (`255,0,255`) as the transparency color key.

### No custom shaders
Bloom + CRT (toggle **C**) are done **shader-free** (render-target downsample for
bloom; generated scanline/vignette overlays), on purpose: compiling an HLSL `.fx`
for DesktopGL needs Wine/`mgfxc`, which isn't available on the Linux/WSL build box
and would break the cross-platform build. Screen curvature and animated noise are
the only effects omitted (they genuinely require a compiled shader).

### In-game keys
Fly with **WASD / arrows / left stick**; **Space / A** shoot; **X / B** hold to
charge a homing volley; **Z / RT** boost; **S / down / LT** brake. Debug toggles:
**I** interpolation, **P** sub-pixel blit, **O** delta-time smoothing, **M**
minimap, **C** CRT/bloom (shown along the bottom status line). **Esc** / gamepad
**Back** quits. Handled in `GameplayScreen.HandleDebugToggles` and `Game1.Update`.

## Architecture

Built on **Arch ECS** plus **MonoGame.Extended** (screen manager, bitmap fonts).

### Game1 (thin shell) + window/DPI
`Game1` sets up graphics, a shared `SpriteBatch`, caches (`FontCache`,
`TextureCache` — the latter holds generated `circ-N`/`circfill-N` textures), and
hands off to the `ScreenManager`, booting straight into `GameplayScreen`. It also
owns **window sizing**: the app is DPI-aware (under `dotnet run` the process
inherits `dotnet.exe`'s per-monitor-aware manifest regardless), so it sizes the
window to `PreferredWindow* (1280×960) × the real DPI scale` and lets the scene
target bilinear-fill it — a comfortable physical size that matches the Love2D
build. See `app.manifest` and `Game1.ComputeWindowSize`. A `winmm` timer-resolution
bump reduces frame-pacing jitter on Windows.

### One ECS World per screen
`GameplayScreen` creates its own Arch `World`, spawns the ship / stars / planets /
enemies (via `Factories`), and runs the **fixed-timestep accumulator loop**: sim
systems step at `Constants.FixedDt` (1/60); rendering interpolates between the
previous and current transforms by `alpha = accumulator / FixedDt`. Delta-time
smoothing (vsync-snap + rolling average) keeps the accumulator phase-locked so the
sub-pixel camera doesn't jitter.

### Systems (plain classes, explicit order)
Systems are plain classes constructed with the `World` plus whatever they need,
exposing `Update(float dt)` or `Update(float dt, in InputState)`. They are NOT
built on `SystemBase` (that file is a dead leftover). **Order matters** and mirrors
the web `main.ts`: ship → shoot → homing → bullet → enemyAI → enemy → particle,
then `pulse` on the real frame delta. Systems query via Arch
`QueryDescription().WithAll<...>()`; when a step both iterates and makes structural
changes (spawns/among nested loops), it collects entities into a `List<Entity>`
first, then processes.

### Rendering — the sub-pixel-smooth pipeline
`Systems/WorldRenderingSystem` owns the **entire** frame (ported from the web
`render.ts` + HUD from `main.ts`). The smooth-movement crux:
1. World content (planets, exhaust, enemies, bullets, reticle) is drawn into a
   low-res `worldRT` (257×193) at `floor(worldPos) - floor(cam)` — whole low-res
   pixels, never sub-pixel inside the RT.
2. That RT is blitted ×Scale to the scene at a **whole-screen-pixel offset**
   carrying the camera fraction (`-round(frac(cam) × Scale)`) — this is what makes
   motion buttery instead of shimmering.
3. Stars (screen-space parallax, streak at high speed), the pinned ship (with
   bank-frame cross-fade), planet light, and the minimap compose the 1024×768
   `sceneRT`.
4. `sceneRT` is bilinear-upscaled to the backbuffer (soft, Love2D-style), with the
   optional bloom/CRT chain and a whole-frame boost shake. The **HUD is drawn last,
   directly on the backbuffer with point sampling** so text stays crisp.

Everything uses `SamplerState.PointClamp` for the pixel-art passes.

### Components / data
`Components/` holds plain structs: `Transform` (Position + Rotation°), `Previous`
(interpolation snapshot), `Velocity`, `Ship`, `Bullet`, `Homing`, `Enemy`,
`Particle`, `Planet`, `Star`, `Pulse`. There are no tag/event-entity messaging
patterns here (unlike the old CherryBomb); collision is direct circle tests in
`BulletSystem`. `Constants.cs` holds all tunable data (ported from `constants.ts`).
`Palette.cs` is the Pico-8 palette + planet palettes. `Input.cs` samples keyboard +
gamepad into an immutable `InputState`.

### Supporting libs (`Lib/`)
- `Lib/Pico8.cs` — generates `circ`/`circfill` textures at load (cached as `circ-N`/`circfill-N`); note `circfill-1` is a plus, not a disc, so tiny dots are drawn as solid squares instead.
- `Lib/SimpleFps.cs`, `Lib/Timer.cs` — utility helpers.

### Android head (`platforms/Android/`)
`SpaceDrift.Android.csproj` source-links the shared game code (not a project
reference) and builds the same `Content.mgcb` for Android. It is **excluded from
the desktop build** (`<Compile Remove="platforms/**">`) and is not a current focus.

## Conventions
- Formatting is **csharpier** with **tabs**. Run `dotnet csharpier format .` before considering work done.
- Color name collision: files alias `using XnaColor = Microsoft.Xna.Framework.Color;` where needed. Match the surrounding file.
- C# latest features are used freely: primary constructors, collection expressions (`[]`), target-typed `new`, switch expressions.
