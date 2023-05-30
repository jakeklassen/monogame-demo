using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb;
using Components;
using EntityFactories;
using Lib.Tweening;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input;
using MonoGame.Extended.Screens;
using Systems;

namespace Screens
{
	public class TitleScreen : GameScreen
	{
		private new Game1 Game => (Game1)base.Game;
		private readonly Random _random = new();
		private Texture2D _spriteSheetTexture;
		private readonly Tweener _tweener = new();
		private readonly World _world = World.Create();
		private readonly List<SystemBase<GameTime>> _updateSystems = new();
		private readonly List<SystemBase<GameTime>> _drawSystems = new();

		public TitleScreen(Game1 game)
			: base(game)
		{
		}

		public override void Initialize()
		{
			base.Initialize();
		}

		public override void LoadContent()
		{
			base.LoadContent();

			_spriteSheetTexture = Game.Content.Load<Texture2D>("Graphics/shmup");

			_updateSystems.Add(new BlinkSystem(_world));
			_updateSystems.Add(new MovementSystem(_world));
			_updateSystems.Add(new StarfieldSystem(_world));

			_drawSystems.Add(new StarfieldRenderingSystem(_world, Game.GraphicsDevice, Game.Camera));
			_drawSystems.Add(new SpriteRenderingSystem(_world, Game.GraphicsDevice, Game.Camera, _spriteSheetTexture));
			_drawSystems.Add(new TextRenderingSystem(_world, Game.GraphicsDevice, Game.Camera, Game.FontCache));

			StarFactory.CreateStarfield(_world, Game1.TargetWidth, Game1.TargetHeight, 100);

			var alien = _world.Create<Sprite, Transform>();
			_world.Add(alien, new Sprite(new Rectangle(40, 8, 8, 8)));

			var transform = new Transform(new Vector2((Game1.TargetWidth / 2) - 4, 31), 0f, Vector2.One);
			_world.Add(alien, transform);

			_tweener.TweenTo(
				target: transform,
				expression: transform => transform.Position,
				toValue: new Vector2(0, -7),
				duration: 1.8f,
				delay: 0.5f
			)
				.AutoReverse()
				.Easing(EasingFunctions.Linear)
				.RepeatForever(0.5f)
				.Relative()
				.OnRepeat(action =>
				{
					if (action.Target is Transform)
					{
						var transform = action.Target as Transform;
						transform.Position = new Vector2(30 + _random.NextInt64(60), transform.Position.Y);
					}
				});

			var logo = _world.Create();
			_world.Add(logo, new Sprite(new Rectangle(32, 104, 95, 14)));
			_world.Add(
				logo,
				new Transform(new Vector2((Game1.TargetWidth / 2) - 47, 30), 0f, Vector2.One)
			);

			var v1Text = _world.Create();
			_world.Add(v1Text,
			new Text()
			{
				Alignment = Alignment.Left,
				Color = Pico8Color.Color2,
				Content = "v1",
				Font = "pico-8"
			});
			_world.Add(
				v1Text,
				new Transform(Vector2.One, 0f, Vector2.One)
			);

			var subtitle = _world.Create();
			_world.Add(
				subtitle,
				new Text()
				{
					Color = Pico8Color.Color6,
					Content = "Short Shwave Shmup",
					Font = "pico-8"
				});
			_world.Add(
				subtitle,
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 45),
					0f,
					Vector2.One
				)
			);

			var pressAnyKeyToStart = _world.Create();
			_world.Add(
				pressAnyKeyToStart,
				new Blink(
					colors: new[] { Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7 },
					colorSequence: new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0 },
					durationSeconds: 0.5f
				)
			);
			_world.Add(
				pressAnyKeyToStart,
				new Text()
				{
					Color = Pico8Color.Color6,
					Content = "Press Any Key To Start",
					Font = "pico-8"
				});
			_world.Add(
				pressAnyKeyToStart,
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 90),
					0f,
					Vector2.One
				)
			);

			var fireControls = _world.Create();
			_world.Add(
				fireControls,
				new Text()
				{
					Color = Pico8Color.Color6,
					Content = "Z (Shoot) X (Spread Shot)",
					Font = "pico-8"
				});
			_world.Add(
				fireControls,
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 100),
					0f,
					Vector2.One
				)
			);

			var moveControls = _world.Create();
			_world.Add(
				moveControls,
				new Text()
				{
					Color = Pico8Color.Color6,
					Content = "Arrow Keys (Move)",
					Font = "pico-8"
				});
			_world.Add(
				moveControls,
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 110),
					0f,
					Vector2.One
				)
			);
		}

		public override void UnloadContent()
		{
			base.UnloadContent();

			World.Destroy(_world);
		}

		public override void Update(GameTime gameTime)
		{
			_tweener.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			if (KeyboardExtended.GetState().WasAnyKeyJustDown())
			{
				ScreenManager.LoadScreen(new GameplayScreen(Game));
			}

			foreach (var system in _updateSystems)
			{
				system.Update(in gameTime);
			}
		}

		public override void Draw(GameTime gameTime)
		{
			foreach (var system in _drawSystems)
			{
				system.Update(in gameTime);
			}
		}
	}
}