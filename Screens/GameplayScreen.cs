using System;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
			float frame = MathF.Min(
				(float)gameTime.ElapsedGameTime.TotalSeconds,
				Constants.MaxFrameTime
			);
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

			_alpha = _accumulator / Constants.FixedDt;
		}

		public override void Draw(GameTime gameTime)
		{
			_renderer.Draw(_alpha);
		}

		public override void UnloadContent()
		{
			_renderer?.Dispose();

			base.UnloadContent();
		}
	}
}
