using System.Collections.Generic;
using CherryBomb.Lib;
using CherryBomb.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Screens;
using MonoGame.Extended.ViewportAdapters;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb
{
	public class State
	{
		public bool BombLocked { get; set; } = true;
		public int Cherries { get; set; } = 0;
		public int Score { get; set; } = 0;
		public int Lives { get; set; } = 1;
		public int MaxLives { get; set; } = 4;
		public bool Paused { get; set; } = false;
		public bool GameOver { get; set; } = false;
		public int Wave { get; set; } = 0;
		public bool WaveReady { get; set; } = false;
		public int MaxWaves { get; set; } = 9;

		public void Reset()
		{
			BombLocked = true;
			Cherries = 0;
			Score = 0;
			Lives = MaxLives;
			Paused = false;
			GameOver = false;
			Wave = 0;
			WaveReady = false;
		}
	}

	public class Game1 : Game
	{
		public const int TargetWidth = 128;
		public const int TargetHeight = 128;
		private readonly GraphicsDeviceManager _graphics;
		private readonly ScreenManager _screenManager;

		private readonly SimpleFps _fps = new();
		private BitmapFont _font;

		private bool _hasToggledVsync = false;
		private bool _hasToggledFixedTimeStep = false;

		public static Rectangle Viewport => new(0, 0, TargetWidth, TargetHeight);
		public OrthographicCamera Camera { get; private set; }
		public Dictionary<string, BitmapFont> FontCache { get; } = new();
		public SpriteBatch SpriteBatch { get; private set; }
		public Dictionary<string, Texture2D> TextureCache { get; } = new();
		public Config Config { get; } = new();
		public State State { get; } = new();
		private readonly GamePadListener _gamePadListener;
		private readonly KeyboardListener _keyboardListener;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			_graphics.PreferredBackBufferWidth = 512;
			_graphics.PreferredBackBufferHeight = 512;
			// this is for fullscreen but like 'borderless'
			_graphics.HardwareModeSwitch = false;
			_graphics.IsFullScreen = false;
			_graphics.PreferMultiSampling = false;
			_graphics.SynchronizeWithVerticalRetrace = false;
			_graphics.ApplyChanges();

			Window.AllowUserResizing = true;

			// Disable for a better experience with higher refresh rate monitors
			IsFixedTimeStep = false;

			_screenManager = new ScreenManager();

			_keyboardListener = new KeyboardListener();
			_gamePadListener = new GamePadListener();

			Components.Add(new InputListenerComponent(this, _keyboardListener, _gamePadListener));
			Components.Add(_screenManager);

			_keyboardListener.KeyReleased += (sender, args) =>
			{
				if (args.Key == Keys.D)
				{
					Config.Debug = !Config.Debug;
				}
			};
		}

		protected override void Initialize()
		{
			base.Initialize();

			var viewportAdapter = new BoxingViewportAdapter(
				Window,
				GraphicsDevice,
				TargetWidth,
				TargetHeight
			);
			Camera = new OrthographicCamera(viewportAdapter);

			// Created here (before the first screen loads) so rendering systems can
			// share this single SpriteBatch instead of each allocating their own.
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			// Center the window on the primary monitor. Without this, SDL can place
			// it at the top-left of a secondary display on multi-monitor setups.
			var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
			Window.Position = new Point(
				(displayMode.Width - _graphics.PreferredBackBufferWidth) / 2,
				(displayMode.Height - _graphics.PreferredBackBufferHeight) / 2
			);

			_screenManager.ReplaceScreen(new TitleScreen(this));
		}

		protected override void LoadContent()
		{
			_font = Content.Load<BitmapFont>("Font/pico-8");

			FontCache.Add("pico-8", _font);

			for (int radius = 1; radius <= 32; radius++)
			{
				var filedCircleTexture = Pico8Extensions.CircFill(
					GraphicsDevice,
					radius,
					XnaColor.White
				);
				TextureCache.Add($"circfill-{radius}", filedCircleTexture);

				var circleTexture = Pico8Extensions.Circ(GraphicsDevice, radius, XnaColor.White);
				TextureCache.Add($"circ-{radius}", circleTexture);
			}
		}

		protected override void UnloadContent()
		{
			base.UnloadContent();

			SpriteBatch.Dispose();
		}

		protected override void Update(GameTime gameTime)
		{
			// MonoGame.Extended 6.0 requires advancing the extended input state once
			// per frame; otherwise WasAnyKeyJustDown/WasKeyPressed never see transitions.
			// Must run before base.Update so the active screen reads a fresh snapshot.
			KeyboardExtended.Update();

			if (
				GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
				|| Keyboard.GetState().IsKeyDown(Keys.Escape)
			)
			{
				Exit();
			}

			if (Keyboard.GetState().IsKeyDown(Keys.F) && _hasToggledFixedTimeStep == false)
			{
				_hasToggledFixedTimeStep = true;
				IsFixedTimeStep = !IsFixedTimeStep;

				_graphics.ApplyChanges();
			}

			if (Keyboard.GetState().IsKeyUp(Keys.F) && _hasToggledFixedTimeStep)
			{
				_hasToggledFixedTimeStep = false;
			}

			if (Keyboard.GetState().IsKeyDown(Keys.V) && _hasToggledVsync == false)
			{
				_hasToggledVsync = true;
				_graphics.SynchronizeWithVerticalRetrace =
					!_graphics.SynchronizeWithVerticalRetrace;

				_graphics.ApplyChanges();
			}

			if (Keyboard.GetState().IsKeyUp(Keys.V) && _hasToggledVsync)
			{
				_hasToggledVsync = false;
			}

			_fps.Update(gameTime);

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			// Clear the framebuffer; the active screen's rendering systems do the
			// actual drawing during base.Draw (via the ScreenManager component).
			GraphicsDevice.Clear(XnaColor.Black);

			base.Draw(gameTime);
		}
	}
}
