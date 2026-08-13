using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CherryBomb.Lib;
using CherryBomb.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Screens;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb
{
	// Space Drift (phase 1). The game renders at a low internal resolution
	// (GameWidth×GameHeight) and blits up ×Scale to a WindowWidth×WindowHeight
	// backbuffer; the sub-pixel smoothing happens in WorldRenderingSystem, so
	// Game1 stays thin: graphics setup, a shared SpriteBatch and caches, and the
	// ScreenManager handing off to GameplayScreen.
	public class Game1 : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private readonly ScreenManager _screenManager;

		private readonly SimpleFps _fps = new();
		private BitmapFont _font;

		// Re-entrancy guard: ApplyChanges inside the resize handler re-fires
		// ClientSizeChanged.
		private bool _resizing;

		public Dictionary<string, BitmapFont> FontCache { get; } = new();
		public SpriteBatch SpriteBatch { get; private set; }
		public Dictionary<string, Texture2D> TextureCache { get; } = new();

		// Windows' default timer resolution is ~15.6ms, which makes MonoGame's
		// frame pacing jitter (visible as micro-stutter in smooth scrolling). Drop
		// it to 1ms for precise pacing — a well-known MonoGame stutter fix. Paired
		// with timeEndPeriod in Dispose. Windows-only (no-op elsewhere).
		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint TimeBeginPeriod(uint uMilliseconds);

		[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
		private static extern uint TimeEndPeriod(uint uMilliseconds);

		private const uint TimerResolutionMs = 1;

		// Windows display scaling as reported by the OS (1.0 at 96 DPI, 1.5 at 150%).
		// Informational / diagnostic only now — window sizing is display-relative.
		public float DpiScale { get; private set; } = 1f;

		// The primary display size, for the on-screen diagnostic readout.
		public Point DisplaySize { get; private set; }

		[DllImport("user32.dll")]
		private static extern uint GetDpiForSystem();

		private static float QueryDpiScale()
		{
			if (OperatingSystem.IsWindows())
			{
				try
				{
					return GetDpiForSystem() / 96f;
				}
				catch (EntryPointNotFoundException)
				{
					// GetDpiForSystem needs Windows 10 1607+; fall back to 1.0.
				}
			}

			return 1f;
		}

		// Initial window size: ~85% of the display height at the native 4:3 aspect,
		// so it's comfortably large on any monitor. Falls back to the native size if
		// the display mode isn't queryable yet.
		private (int, int) ComputeWindowSize()
		{
			try
			{
				var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				DisplaySize = new Point(dm.Width, dm.Height);
				int h = (int)MathF.Round(dm.Height * 0.85f);
				int w = (int)MathF.Round(h * (float)Constants.WindowWidth / Constants.WindowHeight);
				return (w, h);
			}
			catch
			{
				return (Constants.WindowWidth, Constants.WindowHeight);
			}
		}

		public Game1()
		{
			if (OperatingSystem.IsWindows())
			{
				TimeBeginPeriod(TimerResolutionMs);
			}

			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			if (IsDesktop)
			{
				// Size the window RELATIVE TO THE DISPLAY (~85% of its height, native
				// 4:3), not to a fixed pixel size — so it's comfortably large on any
				// monitor regardless of DPI scaling. The native 1024×768 scene is
				// bilinear-upscaled to fill this backbuffer in the renderer's present.
				DpiScale = QueryDpiScale();
				var (winW, winH) = ComputeWindowSize();
				_graphics.PreferredBackBufferWidth = winW;
				_graphics.PreferredBackBufferHeight = winH;
				_graphics.HardwareModeSwitch = false;
				_graphics.IsFullScreen = false;
				_graphics.PreferMultiSampling = false;
				_graphics.SynchronizeWithVerticalRetrace = true;
				_graphics.ApplyChanges();

				Window.AllowUserResizing = true;
				Window.Title = "Space Drift";
				Window.ClientSizeChanged += OnClientSizeChanged;
			}
			else
			{
				// Non-desktop heads (Android): fullscreen at native resolution.
				_graphics.IsFullScreen = true;
				_graphics.PreferMultiSampling = false;
				_graphics.SynchronizeWithVerticalRetrace = true;
				_graphics.ApplyChanges();
			}

			// Disable for a better experience with higher refresh rate monitors.
			IsFixedTimeStep = false;

			_screenManager = new ScreenManager();
			Components.Add(_screenManager);
		}

		// User dragged the window edge: match the backbuffer to the new client
		// size so the scene target's bilinear present fills it (no stretch/letterbox).
		private void OnClientSizeChanged(object sender, EventArgs e)
		{
			if (_resizing)
				return;

			int w = Math.Max(320, Window.ClientBounds.Width);
			int h = Math.Max(240, Window.ClientBounds.Height);
			if (w == _graphics.PreferredBackBufferWidth && h == _graphics.PreferredBackBufferHeight)
				return;

			_resizing = true;
			_graphics.PreferredBackBufferWidth = w;
			_graphics.PreferredBackBufferHeight = h;
			_graphics.ApplyChanges();
			_resizing = false;
		}

		// True on the desktop heads (Windows/Linux/macOS). False on Android.
		private static bool IsDesktop =>
			OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

		protected override void Initialize()
		{
			base.Initialize();

			// Created before the first screen loads so rendering systems can share
			// this single SpriteBatch instead of each allocating their own.
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			if (IsDesktop)
			{
				// Center the window on the primary monitor.
				var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				Window.Position = new Point(
					(displayMode.Width - _graphics.PreferredBackBufferWidth) / 2,
					(displayMode.Height - _graphics.PreferredBackBufferHeight) / 2
				);
			}

			_screenManager.ReplaceScreen(new GameplayScreen(this));
		}

		protected override void LoadContent()
		{
			_font = Content.Load<BitmapFont>("Font/pico-8");
			FontCache.Add("pico-8", _font);

			// PICO-8 circle textures, cached for later phases (HUD, planets, FX).
			for (int radius = 1; radius <= 32; radius++)
			{
				TextureCache.Add(
					$"circfill-{radius}",
					Pico8Extensions.CircFill(GraphicsDevice, radius, XnaColor.White)
				);
				TextureCache.Add(
					$"circ-{radius}",
					Pico8Extensions.Circ(GraphicsDevice, radius, XnaColor.White)
				);
			}
		}

		protected override void UnloadContent()
		{
			base.UnloadContent();

			SpriteBatch.Dispose();
		}

		protected override void Dispose(bool disposing)
		{
			if (OperatingSystem.IsWindows())
			{
				TimeEndPeriod(TimerResolutionMs);
			}

			base.Dispose(disposing);
		}

		protected override void Update(GameTime gameTime)
		{
			if (
				GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
				|| Keyboard.GetState().IsKeyDown(Keys.Escape)
			)
			{
				Exit();
			}

			_fps.Update(gameTime);

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			// The active screen's WorldRenderingSystem owns the full frame (render
			// target pass + backbuffer clear + blit), so this clear is just a safe
			// default for the very first frame before a screen has drawn.
			GraphicsDevice.Clear(Palette.SpaceColor);

			base.Draw(gameTime);
		}
	}
}
