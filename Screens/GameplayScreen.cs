using System;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;

namespace CherryBomb.Screens
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
		private ParticleSystem _particleSystem;
		private PulseSystem _pulseSystem;
		private WorldRenderingSystem _renderer;

		private float _accumulator;
		private float _alpha = 1f;

		// Comparison toggles (I/P/O keys) — see HandleDebugToggles.
		private bool _interpolation = true;
		private bool _subpixel = true;
		private bool _smoothing = true;
		private KeyboardState _prevKeys;

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

			_shipSystem = new ShipSystem(_world, _ship);
			_particleSystem = new ParticleSystem(_world);
			_pulseSystem = new PulseSystem(_world);
			_renderer = new WorldRenderingSystem(
				_world,
				_ship,
				Game.GraphicsDevice,
				Game.SpriteBatch,
				_shmup
			);
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
				_shipSystem.Update(Constants.FixedDt, input);
				_particleSystem.Update(Constants.FixedDt);
				_accumulator -= Constants.FixedDt;
			}

			// Frame-rate (cosmetic) system runs on the real delta.
			_pulseSystem.Update(frame);

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
			_prevKeys = keys;
		}

		public override void Draw(GameTime gameTime)
		{
			_renderer.Draw(_alpha, _subpixel);
			DrawDebugReadout();
		}

		// Top-left readout of the comparison toggles (green = ON, red = OFF), so
		// the I/P/O state is visible while diagnosing the movement feel. Debug-only.
		private void DrawDebugReadout()
		{
			var font = Game.FontCache["pico-8"];
			var sb = Game.SpriteBatch;

			sb.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.PointClamp,
				null,
				null,
				null,
				Matrix.CreateScale(3f)
			);
			DrawToggle(sb, font, "O SMOOTHING", _smoothing, 2f);
			DrawToggle(sb, font, "I INTERP", _interpolation, 10f);
			DrawToggle(sb, font, "P SUBPIXEL", _subpixel, 18f);
			sb.End();
		}

		private static void DrawToggle(
			SpriteBatch sb,
			BitmapFont font,
			string label,
			bool on,
			float y
		)
		{
			sb.DrawString(
				font,
				$"{label} {(on ? "ON" : "OFF")}",
				new Vector2(2f, y),
				on ? Palette.Green : Palette.Red
			);
		}

		public override void UnloadContent()
		{
			_renderer?.Dispose();

			base.UnloadContent();
		}
	}
}
