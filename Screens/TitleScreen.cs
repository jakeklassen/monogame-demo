using System;
using CherryBomb;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using CherryBomb.Lib.Tweening;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;

namespace CherryBomb.Screens
{
	public class TitleScreen(Game1 game) : GameScreenBase(game)
	{
		private readonly Random _random = new();
		private Texture2D _spriteSheetTexture;
		private SoundSystem _soundSystem;

		public override void LoadContent()
		{
			base.LoadContent();

			_spriteSheetTexture = Game.Content.Load<Texture2D>("Graphics/shmup");

			_soundSystem = new SoundSystem(_world, Game.SoundCache);

			_updateSystems.Add(_soundSystem);
			_updateSystems.Add(new BlinkSystem(_world));
			_updateSystems.Add(new MovementSystem(_world));
			_updateSystems.Add(new StarfieldSystem(_world));

			_drawSystems.Add(new StarfieldRenderingSystem(_world, Game.SpriteBatch, Game.Camera));
			_drawSystems.Add(
				new SpriteRenderingSystem(
					_world,
					Game.SpriteBatch,
					Game.Camera,
					_spriteSheetTexture
				)
			);
			_drawSystems.Add(
				new TextRenderingSystem(_world, Game.SpriteBatch, Game.Camera, Game.FontCache)
			);

			StarFactory.CreateStarfield(_world, Game1.TargetWidth, Game1.TargetHeight, 100);

			var alien = _world.Create();
			_world.Add(alien, new Sprite(new Rectangle(40, 8, 8, 8)));

			var transform = new Transform(
				new Vector2((Game1.TargetWidth / 2) - 4, 31),
				0f,
				Vector2.One
			);
			_world.Add(alien, transform);

			_tweener
				.TweenTo(
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
						transform.Position = new Vector2(
							30 + _random.NextInt64(60),
							transform.Position.Y
						);
					}
				});

			var logo = _world.Create();
			_world.Add(logo, new Sprite(new Rectangle(32, 104, 95, 14)));
			_world.Add(
				logo,
				new Transform(new Vector2((Game1.TargetWidth / 2) - 47, 30), 0f, Vector2.One)
			);

			var v1Text = _world.Create();
			_world.Add(
				v1Text,
				new Text()
				{
					Alignment = Alignment.Left,
					Color = Pico8Color.Color1,
					Content = "V1",
					Font = "pico-8",
				}
			);
			_world.Add(v1Text, new Transform(Vector2.One, 0f, Vector2.One));

			var subtitle = _world.Create();
			_world.Add(
				subtitle,
				new Text()
				{
					Color = Pico8Color.Color6,
					Content = "Short Shwave Shmup",
					Font = "pico-8",
				}
			);
			_world.Add(
				subtitle,
				new Transform(new Vector2(Game1.TargetWidth / 2, 45), 0f, Vector2.One)
			);

			var pressAnyKeyToStart = _world.Create();
			_world.Add(
				pressAnyKeyToStart,
				new Blink(
					colors: [Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7],
					colorSequence: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0],
					durationSeconds: 0.5f
				)
			);
			_world.Add(
				pressAnyKeyToStart,
				new Text()
				{
					Color = Pico8Color.Color6,
					Content = "Press Any Key To Start",
					Font = "pico-8",
				}
			);
			_world.Add(
				pressAnyKeyToStart,
				new Transform(new Vector2(Game1.TargetWidth / 2, 90), 0f, Vector2.One)
			);

			// Highscore (persisted across runs; updated on game-over/won).
			var highscoreText = _world.Create();
			_world.Add(
				highscoreText,
				new Text()
				{
					Color = Pico8Color.Color12,
					Content = $"highscore: {Lib.Highscore.Load()}",
					Font = "pico-8",
				}
			);
			_world.Add(
				highscoreText,
				new Transform(new Vector2(Game1.TargetWidth / 2, 55), 0f, Vector2.One)
			);

			var fireControls = _world.Create();
			_world.Add(
				fireControls,
				new Text()
				{
					Color = Pico8Color.Color5,
					Content = "A:Shoot B:Spread X:Bomb",
					Font = "pico-8",
				}
			);
			_world.Add(
				fireControls,
				new Transform(new Vector2(Game1.TargetWidth / 2, 100), 0f, Vector2.One)
			);

			var moveControls = _world.Create();
			_world.Add(
				moveControls,
				new Text()
				{
					Color = Pico8Color.Color5,
					Content = "DPad / Stick (Move)",
					Font = "pico-8",
				}
			);
			_world.Add(
				moveControls,
				new Transform(new Vector2(Game1.TargetWidth / 2, 110), 0f, Vector2.One)
			);

			// Loop the title music. The SoundSystem retains the looping instance
			// so UnloadContent can stop it on screen exit.
			SoundSystem.Play(_world, "title-screen-music", loop: true);
		}

		public override void UnloadContent()
		{
			_soundSystem?.StopAll();

			base.UnloadContent();
		}

		public override void Update(GameTime gameTime)
		{
			if (KeyboardExtended.GetState().WasAnyKeyJustDown() || IsAnyButtonDown())
			{
				ScreenManager.ReplaceScreen(new GameplayScreen(Game));
				return;
			}

			base.Update(gameTime);
		}

		private static bool IsAnyButtonDown()
		{
			var currentState = GamePad.GetState(PlayerIndex.One);
			var buttonList = new Buttons[] { Buttons.A, Buttons.B, Buttons.X, Buttons.Y };
			var anyButtonPressed = false;

			foreach (var button in buttonList)
			{
				if (currentState.IsButtonDown(button))
				{
					anyButtonPressed = true;

					break;
				}
			}

			return anyButtonPressed;
		}
	}
}
