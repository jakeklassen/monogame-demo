using Microsoft.Xna.Framework;

namespace CherryBomb
{
	// Ported from space-drift/constants.ts. All tunable game data lives here so
	// later phases (combat, enemies, planets, homing) have the numbers on hand.
	// NOTE: the namespace stays CherryBomb for this phase (a later phase renames
	// the whole project to SpaceDrift).
	public static class Constants
	{
		// ── Resolution & render pipeline ────────────────────────────────────────
		// Low-res viewport, upscaled ×SCALE. SCALE stays an integer so pixel art
		// never shimmers. 256×192 ×4 = 1024×768 (4:3, 32px ship).
		public const int GameWidth = 256;
		public const int GameHeight = 192;
		public const int Scale = 4;

		// The world render target is one pixel larger than the view so the
		// sub-pixel blit offset (up to one low-res pixel) never reveals an
		// uncovered edge. 257×193.
		public const int CanvasWidth = GameWidth + 1;
		public const int CanvasHeight = GameHeight + 1;

		// The scene render target size (the low-res view blitted ×Scale). The window
		// backbuffer is a SEPARATE, larger size (see PreferredWindow*), and this scene
		// target is bilinear-presented to fill it.
		public const int WindowWidth = GameWidth * Scale; // 1024
		public const int WindowHeight = GameHeight * Scale; // 768

		// Desired on-screen window size, matching the Love2D build (1280×960, 4:3) so
		// the game is a comfortable, consistent physical size across platforms. The
		// app runs DPI-unaware (see app.manifest) so the OS upscales this like Love2D,
		// instead of rendering it at true pixels (tiny on high-DPI Windows). Clamped
		// down to fit small displays at window-creation time.
		public const int PreferredWindowWidth = 1280;
		public const int PreferredWindowHeight = 960;

		// A large area to drift around in.
		public const int WorldWidth = 1536;
		public const int WorldHeight = 1536;

		// Fixed-timestep simulation; rendering interpolates between steps.
		public const float FixedDt = 1f / 60f;
		public const float MaxFrameTime = 0.25f;

		// ── Ship tuning — a deliberately "tight" asteroids feel ─────────────────
		public const float ShipRotationSpeed = 210f; // degrees / second
		public const float ShipThrust = 280f; // pixels / second^2
		public const float ShipBrake = 0.6f; // reverse-thrust fraction on brake

		// Grip handling: velocity is split into forward (along the nose) and
		// lateral (sideways) components each step and dragged separately. High
		// lateral drag = the ship "grips" and goes where it points.
		public const float ShipForwardDrag = 2.5f;
		public const float ShipLateralDrag = 9f; // strong grip → go where you point

		// Absolute speed clamp — only the Z boost reaches it.
		public const float ShipMaxSpeed = 520f;

		// Boost (Z): huge forward thrust, gated by a fuel meter. Tapping fires a
		// punchy dash; holding sustains until the meter drains, then it refills.
		public const float ShipBoostThrust = 1500f;
		public const float BoostFuelMax = 1f; // full tank (arbitrary units)
		public const float BoostDrain = 0.6f; // fuel/sec while boosting
		public const float BoostRefill = 0.32f; // fuel/sec while not boosting
		public const float BoostDashCost = 0.18f; // fuel spent on a tap-dash
		public const float BoostDashImpulse = 170f; // forward px/s from a tap-dash

		// Star streaking during high-speed flight.
		public const float StreakThreshold = 140f;
		public const float StreakK = 0.07f;
		public const float StreakMax = 46f;

		// Whole-frame boost shake.
		public const float BoostShakeDelay = 0.25f; // seconds into boost before it starts
		public const float BoostShakeRamp = 0.12f; // ease-in time so it doesn't pop
		public const float BoostShakeAmp = 1.0f; // sustained amplitude, game px

		// ── Shooting (wired later) ──────────────────────────────────────────────
		public const float BulletSpeed = 320f; // px/s added on top of ship velocity
		public const float BulletLifetime = 1.1f; // seconds before a bullet expires
		public const float BulletRadius = 2f; // px, for hit tests
		public const float ShootInterval = 0.13f; // seconds between shots while held
		public const float MuzzleOffset = 5f; // px ahead of the ship centre
		public const float ShotSpread = 3f; // ± offset of the double-wide shot

		// ── Homing charge shot (wired later) ────────────────────────────────────
		public const float HomingChargeMax = 3f; // seconds to full charge
		public const float HomingLockMargin = 6f; // px past the view edge still on-screen
		public const float HomingSpeed = 250f; // px/s constant cruise speed
		public const float HomingTurnRate = 540f; // deg/s base steering
		public const float HomingCloseDist = 80f; // px range where turn rate ramps up
		public const float HomingTurnCloseBoost = 6f; // extra turn-rate multiplier point blank
		public const float HomingSeekDelay = 0.13f; // seconds flown straight before homing
		public const float HomingProximity = 3f; // px bonus hit radius for homing missiles
		public const float HomingSpreadDeg = 82f; // wide initial fan-out across the volley
		public const float HomingStagger = 0.06f; // seconds between symmetric pairs
		public const float HomingLifetime = 3.0f; // seconds before a homing missile expires

		// ── Enemy (wired later) ─────────────────────────────────────────────────
		public const int EnemyCount = 3;
		public const int EnemyHealth = 3;
		public const float EnemyRadius = 5f;
		public const float EnemyHitFlash = 0.08f;
		public const float EnemyRespawnDelay = 1.2f;
		public const float EnemyThrust = 240f;
		public const float EnemySightLoseMargin = 48f;
		public const float EnemyPatrolRadius = 200f;
		public const float EnemyWaypointReached = 22f;
		public const float EnemyRepathTime = 4f;
		public const float EnemySeparation = 26f;
		public const float EnemySeparationForce = 480f;

		// ── Planets (wired later) ───────────────────────────────────────────────
		public const int PlanetCount = 7;

		// Shared light direction for all planets (up-and-to-the-left). Normalized.
		public const float LightDirX = -0.7071f;
		public const float LightDirY = -0.7071f;

		// ── Minimap ─────────────────────────────────────────────────────────────
		// Geometry in game (low-res) pixels — drawn into its own buffer and blitted
		// up ×Scale so it shares the pixel grid with the world.
		public const int MinimapRadius = 14;
		public const int MinimapMargin = 4;
		public const int MinimapDiameter = MinimapRadius * 2 + 1; // 29
		public const float MinimapZoom = 1f / 32f; // minimap px per world px (~900px span)
		public const float MinimapTickSweep = 0.6f; // heading-tick arc width, radians

		// ── HUD bars (in scene / low-res-×Scale pixels) ─────────────────────────
		public const int ChargeBarX = 52;
		public const int ChargeBarY = 44;
		public const int ChargeBarW = 120;
		public const int ChargeBarH = 6;
		public const int FuelBarX = 52;
		public const int FuelBarY = 60;
		public const int FuelBarW = 120;
		public const int FuelBarH = 10;
	}
}
