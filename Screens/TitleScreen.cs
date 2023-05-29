using System;
using CherryBomb;
using Components;
using EntityFactories;
using Lib.Tweening;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Entities;
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
		private World _world;

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

			_world = new WorldBuilder()
				.AddSystem(new BlinkSystem())
				.AddSystem(new MovementSystem())
				.AddSystem(new StarfieldSystem())
				.AddSystem(new StarfieldRenderingSystem(Game.GraphicsDevice, Game.Camera))
				.AddSystem(new SpriteRenderingSystem(Game.GraphicsDevice, Game.Camera, _spriteSheetTexture))
				.AddSystem(new TextRenderingSystem(Game.GraphicsDevice, Game.Camera, Game.FontCache))
				.Build();

			StarFactory.CreateStarfield(_world, Game1.TargetWidth, Game1.TargetHeight, 100);

			var alien = _world.CreateEntity();
			alien.Attach(new Sprite(new Rectangle(40, 8, 8, 8)));
			alien.Attach(new Transform(new Vector2((Game1.TargetWidth / 2) - 4, 31), 0f, Vector2.One));


			_tweener.TweenTo(
				target: alien.Get<Transform>(),
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

			var logo = _world.CreateEntity();
			logo.Attach(new Sprite(new Rectangle(32, 104, 95, 14)));
			logo.Attach(
				new Transform(new Vector2((Game1.TargetWidth / 2) - 47, 30), 0f, Vector2.One)
			);

			var v1Text = _world.CreateEntity();
			v1Text.Attach(new Text()
			{
				Alignment = Alignment.Left,
				Color = Pico8Color.Color2,
				Content = "v1",
				Font = "pico-8"
			});
			v1Text.Attach(
				new Transform(Vector2.One, 0f, Vector2.One)
			);

			var subtitle = _world.CreateEntity();
			subtitle.Attach(new Text()
			{
				Color = Pico8Color.Color6,
				Content = "Short Shwave Shmup",
				Font = "pico-8"
			});
			subtitle.Attach(
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 45),
					0f,
					Vector2.One
				)
			);

			var pressAnyKeyToStart = _world.CreateEntity();
			pressAnyKeyToStart.Attach(new Blink(
				colors: new[] { Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7 },
				colorSequence: new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0 },
				durationSeconds: 0.5f
			));
			pressAnyKeyToStart.Attach(new Text()
			{
				Color = Pico8Color.Color6,
				Content = "Press Any Key To Start",
				Font = "pico-8"
			});
			pressAnyKeyToStart.Attach(
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 90),
					0f,
					Vector2.One
				)
			);

			var fireControls = _world.CreateEntity();
			fireControls.Attach(new Text()
			{
				Color = Pico8Color.Color6,
				Content = "Z (Shoot) X (Spread Shot)",
				Font = "pico-8"
			});
			fireControls.Attach(
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 100),
					0f,
					Vector2.One
				)
			);

			var moveControls = _world.CreateEntity();
			moveControls.Attach(new Text()
			{
				Color = Pico8Color.Color6,
				Content = "Arrow Keys (Move)",
				Font = "pico-8"
			});
			moveControls.Attach(
				new Transform(
					new Vector2(Game1.TargetWidth / 2, 110),
					0f,
					Vector2.One
				)
			);

			Game.Components.Add(_world);
		}

		public override void UnloadContent()
		{
			base.UnloadContent();

			_world.Dispose();
		}

		public override void Update(GameTime gameTime)
		{
			_tweener.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			if (KeyboardExtended.GetState().WasAnyKeyJustDown())
			{
				ScreenManager.LoadScreen(new GameplayScreen(Game));
			}
		}

		public override void Draw(GameTime gameTime)
		{

		}
	}
}