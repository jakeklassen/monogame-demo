using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using SpaceDrift.Components;
using SpaceDrift.Systems;

namespace SpaceDrift.Screens
{
	// Space-drift phase 1: a flyable ship drifting in a parallax starfield.
	// Owns the fixed-step accumulator loop (sim runs at FixedDt; rendering
	// interpolates by alpha = accumulator / FixedDt). Ported from
	// space-drift/main.ts.
	public sealed class GameplayScreen(Game1 game) : GameScreenBase(game)
	{
		private Texture2D _shmup;
		private Entity _ship;

		private ShipSystem _shipSystem;
		private ShootSystem _shootSystem;
		private HomingSystem _homingSystem;
		private BulletSystem _bulletSystem;
		private EnemyAiSystem _enemyAiSystem;
		private EnemySystem _enemySystem;
		private ParticleSystem _particleSystem;
		private PulseSystem _pulseSystem;
		private WorldRenderingSystem _renderer;

		private float _accumulator;
		private float _alpha = 1f;

		// Comparison toggles (I/P/O/M keys) — see HandleDebugToggles.
		private bool _interpolation = true;
		private bool _subpixel = true;
		private bool _smoothing = true;
		private bool _minimap = true;
		private bool _crt = false;
		private KeyboardState _prevKeys;

		// Seconds the boost has been held (drives the whole-frame shake), and a
		// simple smoothed FPS counter for the HUD.
		private float _boostHeldTime;
		private int _fpsFrames;
		private float _fpsElapsed;
		private int _fpsValue;

		// Delta-time smoothing (Glaiel). MonoGame's per-frame ElapsedGameTime is
		// noisier than rAF / love.update, and that jitter feeds the accumulator ->
		// a wobbling interpolation alpha -> the interpolated camera bounces ±1px
		// against the hard pixel floor (visible as star jitter even at low speed).
		// Snapping near-vsync deltas to exact FixedDt multiples + a short rolling
		// average makes the accumulator advance in lockstep, so alpha is stable.
		private readonly float[] _dtHistory =
		[
			Constants.FixedDt,
			Constants.FixedDt,
			Constants.FixedDt,
			Constants.FixedDt,
		];
		private int _dtHistoryIndex;

		private float SmoothDelta(float raw)
		{
			// 1. Vsync snap: if raw is within 10% of an integer multiple of FixedDt
			//    (a 60/59.94Hz frame, a dropped frame, etc.), snap it exactly so the
			//    accumulator stays phase-locked instead of beating.
			float snapped = raw;
			for (int m = 1; m <= 6; m++)
			{
				float target = m * Constants.FixedDt;
				if (MathF.Abs(raw - target) < Constants.FixedDt * 0.1f)
				{
					snapped = target;
					break;
				}
			}

			// 2. Rolling average over the last few frames to absorb residual jitter
			//    (also smooths high-refresh displays that don't snap).
			_dtHistory[_dtHistoryIndex] = snapped;
			_dtHistoryIndex = (_dtHistoryIndex + 1) % _dtHistory.Length;

			float sum = 0f;
			for (int i = 0; i < _dtHistory.Length; i++)
			{
				sum += _dtHistory[i];
			}

			return sum / _dtHistory.Length;
		}

		public override void LoadContent()
		{
			base.LoadContent();

			_shmup = Game.Content.Load<Texture2D>("Graphics/shmup");

			// One ship at world centre.
			var center = new Vector2(Constants.WorldWidth / 2f, Constants.WorldHeight / 2f);
			_ship = _world.Create(
				new Transform(center, 0f),
				new Previous(center, 0f),
				new Velocity(Vector2.Zero),
				new Ship
				{
					Thrusting = false,
					Boosting = false,
					Fuel = Constants.BoostFuelMax,
				}
			);

			PopulateStars();
			PopulatePlanets();
			SpawnEnemies();

			_shipSystem = new ShipSystem(_world, _ship);
			_shootSystem = new ShootSystem(_world, _ship);
			_homingSystem = new HomingSystem(_world, _ship);
			_bulletSystem = new BulletSystem(_world);
			_enemyAiSystem = new EnemyAiSystem(_world, _ship);
			_enemySystem = new EnemySystem(_world, _ship);
			_particleSystem = new ParticleSystem(_world);
			_pulseSystem = new PulseSystem(_world);
			_renderer = new WorldRenderingSystem(
				_world,
				_ship,
				Game.GraphicsDevice,
				Game.SpriteBatch,
				_shmup,
				Game.FontCache["pico-8"],
				Game.TextureCache
			);
		}

		// Ring PlanetCount planets around the spawn at increasing distance, each a
		// random type/size (ported from factories.ts populateWorld).
		private void PopulatePlanets()
		{
			var rng = new Random();
			float cx = Constants.WorldWidth / 2f;
			float cy = Constants.WorldHeight / 2f;
			float RndRange(float a, float b) => a + rng.NextSingle() * (b - a);

			for (int i = 0; i < Constants.PlanetCount; i++)
			{
				float angle =
					(float)i / Constants.PlanetCount * MathF.PI * 2f + RndRange(-0.35f, 0.35f);
				float distance = RndRange(72f, 108f) + i * RndRange(55f, 90f);
				float radius = rng.Next(10, 27); // rndInt(10, 26) inclusive
				var palette = Palette.PlanetPalettes[rng.Next(Palette.PlanetPalettes.Length)];
				Factories.CreatePlanet(
					_world,
					rng,
					cx + MathF.Cos(angle) * distance,
					cy + MathF.Sin(angle) * distance,
					radius,
					palette
				);
			}
		}

		// A handful of enemies ringed loosely around the spawn (ported from
		// main.ts). They patrol until the player drifts on screen, then engage.
		private void SpawnEnemies()
		{
			var rng = new Random();
			float cx = Constants.WorldWidth / 2f;
			float cy = Constants.WorldHeight / 2f;
			for (int i = 0; i < Constants.EnemyCount; i++)
			{
				float angle = (float)i / Constants.EnemyCount * MathF.PI * 2f;
				float dist = 150f + i * 40f;
				Factories.CreateEnemy(
					_world,
					rng,
					cx + MathF.Cos(angle) * dist,
					cy + MathF.Sin(angle) * dist
				);
			}
		}

		// Parallax star layers, far → near (ported from factories.ts STAR_LAYERS /
		// createStar). ~250 stars scattered across the world with varied depth,
		// size, and twinkle.
		private void PopulateStars()
		{
			var rng = new Random();

			float RndRange(float min, float max) => min + rng.NextSingle() * (max - min);

			(int count, float depth, Color[] colors, float bigChance)[] layers =
			[
				(116, 0.3f, [Palette.DarkGray, Palette.Lavender], 0f),
				(80, 0.55f, [Palette.Lavender, Palette.LightGray], 0f),
				(54, 0.85f, [Palette.LightGray, Palette.White], 0.2f),
			];

			foreach (var layer in layers)
			{
				for (int i = 0; i < layer.count; i++)
				{
					var pos = new Vector2(
						RndRange(0f, Constants.WorldWidth),
						RndRange(0f, Constants.WorldHeight)
					);
					var color = layer.colors[rng.Next(layer.colors.Length)];
					float size = rng.NextSingle() < layer.bigChance ? 2f : 1f;

					_world.Create(
						new Transform(pos, 0f),
						new Star
						{
							Color = color,
							Size = size,
							Depth = layer.depth,
						},
						new Pulse
						{
							Time = RndRange(0f, MathF.PI * 2f),
							// Visible-but-subtle twinkle (~2-5s per cycle).
							Speed = RndRange(1.2f, 3.0f),
							Amplitude = RndRange(0.35f, 0.65f),
						}
					);
				}
			}
		}

		public override void Update(GameTime gameTime)
		{
			HandleDebugToggles();

			float raw = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float frame = MathF.Min(_smoothing ? SmoothDelta(raw) : raw, Constants.MaxFrameTime);
			_accumulator += frame;

			// Sample once per frame; reused for every fixed step this frame (the
			// device state is constant across the loop, matching the source).
			var input = Input.Sample();

			while (_accumulator >= Constants.FixedDt)
			{
				// Order matches space-drift/main.ts: ship, shoot, homing, bullet,
				// enemy AI, enemy (flash/respawn), particles.
				_shipSystem.Update(Constants.FixedDt, input);
				_shootSystem.Update(Constants.FixedDt, input);
				_homingSystem.Update(Constants.FixedDt, input);
				_bulletSystem.Update(Constants.FixedDt);
				_enemyAiSystem.Update(Constants.FixedDt);
				_enemySystem.Update(Constants.FixedDt);
				_particleSystem.Update(Constants.FixedDt);
				_accumulator -= Constants.FixedDt;
			}

			// Frame-rate (cosmetic) system runs on the real delta.
			_pulseSystem.Update(frame);

			// Track how long boost has been held for the whole-frame shake.
			bool boosting = _world.Get<Ship>(_ship).Boosting;
			_boostHeldTime = boosting ? _boostHeldTime + frame : 0f;

			_alpha = _interpolation ? _accumulator / Constants.FixedDt : 1f;
		}

		// Comparison toggles (mirror the source demos): I = interpolation,
		// P = sub-pixel blit, O = delta-time smoothing. For diagnosing the
		// smooth-movement feel; strip before release.
		private void HandleDebugToggles()
		{
			var keys = Keyboard.GetState();
			if (keys.IsKeyDown(Keys.I) && _prevKeys.IsKeyUp(Keys.I))
				_interpolation = !_interpolation;
			if (keys.IsKeyDown(Keys.P) && _prevKeys.IsKeyUp(Keys.P))
				_subpixel = !_subpixel;
			if (keys.IsKeyDown(Keys.O) && _prevKeys.IsKeyUp(Keys.O))
				_smoothing = !_smoothing;
			if (keys.IsKeyDown(Keys.M) && _prevKeys.IsKeyUp(Keys.M))
				_minimap = !_minimap;
			if (keys.IsKeyDown(Keys.C) && _prevKeys.IsKeyUp(Keys.C))
				_crt = !_crt;
			_prevKeys = keys;
		}

		public override void Draw(GameTime gameTime)
		{
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Smoothed FPS over a ~0.5s window.
			_fpsFrames++;
			_fpsElapsed += dt;
			if (_fpsElapsed >= 0.5f)
			{
				_fpsValue = (int)MathF.Round(_fpsFrames / _fpsElapsed);
				_fpsFrames = 0;
				_fpsElapsed = 0f;
			}

			var ship = _world.Get<Ship>(_ship);
			float speed = _world.Get<Velocity>(_ship).Value.Length();

			var hud = new HudState
			{
				Fps = _fpsValue,
				Speed = (int)MathF.Round(speed),
				ChargeCount = _homingSystem.ChargeCount,
				ChargeSeconds = _homingSystem.ChargeSeconds,
				Fuel = Math.Clamp(ship.Fuel / Constants.BoostFuelMax, 0f, 1f),
				Boosting = ship.Boosting,
				Smoothing = _smoothing,
				Interpolation = _interpolation,
				Subpixel = _subpixel,
				Minimap = _minimap,
				Crt = _crt,
				Gamepad = GamePad.GetState(PlayerIndex.One).IsConnected,
				BoostHeldTime = _boostHeldTime,
			};

			_renderer.Draw(
				_alpha,
				_subpixel,
				_homingSystem.LockTarget,
				_homingSystem.Charging,
				dt,
				hud
			);
		}

		public override void UnloadContent()
		{
			_renderer?.Dispose();

			base.UnloadContent();
		}
	}
}
