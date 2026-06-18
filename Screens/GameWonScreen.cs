using CherryBomb.Components;
using CherryBomb.Lib;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Screens
{
	// Win scene: a frozen snapshot of the last gameplay frame as a backdrop, the
	// game-won music, a blinking prompt, and any-input -> TitleScreen. The highscore
	// is persisted (new record highlighted) and shown. Source: scenes/game-won-screen.ts.
	public class GameWonScreen(Game1 game) : GameScreenBase(game)
	{
		private SoundSystem _soundSystem;

		public override void LoadContent()
		{
			base.LoadContent();

			_soundSystem = new SoundSystem(_world, Game.SoundCache);

			_updateSystems.Add(_soundSystem);
			_updateSystems.Add(new BlinkSystem(_world));

			_drawSystems.Add(
				new TextRenderingSystem(_world, Game.SpriteBatch, Game.Camera, Game.FontCache)
			);

			SoundSystem.Play(_world, "game-won-music");

			// "Congratulations"
			var title = _world.Create();
			_world.Add(
				title,
				new Text()
				{
					Alignment = Alignment.Center,
					Color = Pico8Color.Color12,
					Content = "Congratulations",
					Font = "pico-8",
				}
			);
			_world.Add(
				title,
				new Transform(new Vector2(Game1.TargetWidth / 2, 40), 0f, Vector2.One)
			);

			// Persist + show highscore.
			var (highscore, isNew) = Highscore.Update(Game.State.Score);

			if (isNew)
			{
				var newHighscore = _world.Create();
				_world.Add(
					newHighscore,
					new Blink(
						colors: [Pico8Color.Color7, Pico8Color.Color10],
						colorSequence: [0, 1],
						durationSeconds: 0.2f
					)
				);
				_world.Add(
					newHighscore,
					new Text()
					{
						Alignment = Alignment.Center,
						Color = Pico8Color.Color12,
						Content = $"new highscore!: {highscore}",
						Font = "pico-8",
					}
				);
				_world.Add(
					newHighscore,
					new Transform(new Vector2(Game1.TargetWidth / 2, 66), 0f, Vector2.One)
				);
			}

			var scoreText = _world.Create();
			_world.Add(
				scoreText,
				new Text()
				{
					Alignment = Alignment.Center,
					Color = Pico8Color.Color12,
					Content = $"score: {highscore}",
					Font = "pico-8",
				}
			);
			_world.Add(
				scoreText,
				new Transform(new Vector2(Game1.TargetWidth / 2, 60), 0f, Vector2.One)
			);

			// Blinking prompt.
			var prompt = _world.Create();
			_world.Add(
				prompt,
				new Blink(
					colors: [Pico8Color.Color5, Pico8Color.Color6, Pico8Color.Color7],
					colorSequence: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 1, 1, 0],
					durationSeconds: 0.5f
				)
			);
			_world.Add(
				prompt,
				new Text()
				{
					Alignment = Alignment.Center,
					Color = Pico8Color.Color6,
					Content = "Press Any Key To Start",
					Font = "pico-8",
				}
			);
			_world.Add(
				prompt,
				new Transform(new Vector2(Game1.TargetWidth / 2, 90), 0f, Vector2.One)
			);
		}

		public override void Update(GameTime gameTime)
		{
			if (KeyboardExtended.GetState().WasAnyKeyJustDown() || IsAnyButtonDown())
			{
				ScreenManager.ReplaceScreen(new TitleScreen(Game));

				return;
			}

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			// Frozen last-gameplay-frame backdrop, stretched to fill the viewport.
			if (Game.FrozenFrame != null)
			{
				Game.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp);
				Game.SpriteBatch.Draw(
					Game.FrozenFrame,
					Game.GraphicsDevice.PresentationParameters.Bounds,
					XnaColor.White
				);
				Game.SpriteBatch.End();
			}

			base.Draw(gameTime);
		}

		public override void UnloadContent()
		{
			_soundSystem?.StopAll();

			Game.FrozenFrame?.Dispose();
			Game.FrozenFrame = null;

			base.UnloadContent();
		}

		private static bool IsAnyButtonDown()
		{
			var currentState = GamePad.GetState(PlayerIndex.One);
			var buttonList = new Buttons[] { Buttons.A, Buttons.B, Buttons.X, Buttons.Y };

			foreach (var button in buttonList)
			{
				if (currentState.IsButtonDown(button))
				{
					return true;
				}
			}

			return false;
		}
	}
}
