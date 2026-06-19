using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
using SoundEffect = Microsoft.Xna.Framework.Audio.SoundEffect;
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

		// Window size at 100% DPI; scaled up on high-DPI monitors (4x the 128 internal
		// resolution). See the back-buffer setup in the constructor.
		private const int BaseWindowSize = 512;
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
		public Dictionary<string, SoundEffect> SoundCache { get; } = new();
		public Config Config { get; } = new();
		public State State { get; } = new();

		// A snapshot of the last gameplay frame, captured at the moment a
		// game-over/won transition fires and drawn as the backdrop on the
		// GameOver/GameWon screens. Owned/disposed by those screens.
		public Texture2D FrozenFrame { get; set; }
		private readonly GamePadListener _gamePadListener;
		private readonly KeyboardListener _keyboardListener;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			if (IsDesktop)
			{
				// Scale the window by the monitor's DPI so it stays a consistent physical
				// size across displays. The app is per-monitor DPI aware (app.manifest),
				// so Windows otherwise renders a fixed 512px window that looks tiny on
				// high-DPI screens. 512 = 128 internal * 4, and Windows DPI scales come in
				// 25% steps, so the result is always an exact multiple of 128 (crisp).
				var windowSize = (int)Math.Round(BaseWindowSize * GetDpiScale());
				_graphics.PreferredBackBufferWidth = windowSize;
				_graphics.PreferredBackBufferHeight = windowSize;
				// this is for fullscreen but like 'borderless'
				_graphics.HardwareModeSwitch = false;
				_graphics.IsFullScreen = false;
				_graphics.PreferMultiSampling = false;
				_graphics.SynchronizeWithVerticalRetrace = false;
				_graphics.ApplyChanges();

				Window.AllowUserResizing = true;
				Window.Title = "Cherry Bomb";
			}
			else
			{
				// On Android (and any non-desktop head), run fullscreen at the device's
				// native resolution. The BoxingViewportAdapter letterboxes the 128x128
				// logical view to whatever back buffer the device provides, so there is
				// no window to size, title, or center.
				_graphics.IsFullScreen = true;
				_graphics.PreferMultiSampling = false;
				_graphics.SynchronizeWithVerticalRetrace = true;
				_graphics.ApplyChanges();
			}

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

		// True on the desktop heads (Windows/Linux/macOS). False on Android (and any
		// other mobile/console head), where there is no resizable window to size,
		// title, position, or DPI-scale. Used to fence off desktop-only window code.
		private static bool IsDesktop =>
			OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

		// Returns the primary monitor's DPI scale (1.0 at 96 DPI, 1.5 at 150%, etc.).
		// Windows-only; everywhere else (e.g. the X server under WSL handles its own
		// scaling) this is 1.0 and the window keeps its base size.
		private static float GetDpiScale()
		{
			if (OperatingSystem.IsWindows())
			{
				try
				{
					return GetDpiForSystem() / 96f;
				}
				catch (EntryPointNotFoundException)
				{
					// GetDpiForSystem requires Windows 10 1607+; fall back to no scaling.
				}
			}

			return 1f;
		}

		[DllImport("user32.dll")]
		private static extern uint GetDpiForSystem();

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

			if (IsDesktop)
			{
				// Center the window on the primary monitor. Without this, SDL can place
				// it at the top-left of a secondary display on multi-monitor setups.
				// Reads GraphicsAdapter.DefaultAdapter, which is desktop-only.
				var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				Window.Position = new Point(
					(displayMode.Width - _graphics.PreferredBackBufferWidth) / 2,
					(displayMode.Height - _graphics.PreferredBackBufferHeight) / 2
				);
			}

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

			// Load all audio as SoundEffect (music included; looped via
			// SoundEffectInstance). This keeps the MGCB content build working
			// cross-platform without the Song/ffmpeg pipeline.
			string[] audioTracks =
			[
				"big-explosion",
				"boss-music",
				"boss-projectile",
				"enemy-death",
				"enemy-projectile",
				"extra-life",
				"game-over",
				"game-start",
				"game-won-music",
				"no-cherry-bomb",
				"no-spread-shot",
				"pickup",
				"player-death",
				"player-projectile-hit",
				"shoot",
				"spread-shot",
				"title-screen-music",
				"wave-complete",
				"wave-spawn",
			];

			foreach (var track in audioTracks)
			{
				SoundCache.Add(track, Content.Load<SoundEffect>($"Audio/{track}"));
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
