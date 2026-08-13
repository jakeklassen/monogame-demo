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

		// Windows display scaling as reported by the OS. Diagnostic only — the app is
		// DPI-unaware, so in practice this reads 1.0 (the virtualized value) and window
		// sizing does NOT depend on it.
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

		// Window size = the preferred 1280×960 (Love2D parity), shrunk proportionally
		// to fit within ~88% of the display if the monitor is small — never enlarged,
		// and always kept 4:3. The app is DPI-unaware, so on high-DPI Windows the OS
		// upscales this window like Love2D; the display mode is reported in the same
		// (virtualized) units the window uses, so the clamp is apples-to-apples.
		private (int, int) ComputeWindowSize()
		{
			int w = Constants.PreferredWindowWidth;
			int h = Constants.PreferredWindowHeight;

			try
			{
				var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				DisplaySize = new Point(dm.Width, dm.Height);

				// Fit within 88% of the display on BOTH axes (work-area headroom for
				// taskbars/title bars), preserving aspect. scale ≤ 1 → never grow.
				float scale = MathF.Min(1f, MathF.Min(dm.Width * 0.88f / w, dm.Height * 0.88f / h));
				w = (int)MathF.Round(w * scale);
				h = (int)MathF.Round(h * scale);
			}
			catch
			{
				// Display not queryable yet — use the unclamped preferred size.
			}

			return (w, h);
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
				// Fixed 1280×960 window (Love2D parity), shrunk to fit small displays.
				// The 1024×768 scene target is bilinear-upscaled to fill this backbuffer
				// in the renderer's present; the app is DPI-unaware so the OS scales the
				// whole window up on high-DPI displays for a consistent physical size.
				DpiScale = QueryDpiScale();
				var (winW, winH) = ComputeWindowSize();
				_graphics.PreferredBackBufferWidth = winW;
				_graphics.PreferredBackBufferHeight = winH;
				_graphics.HardwareModeSwitch = false;
				_graphics.IsFullScreen = false;
				_graphics.PreferMultiSampling = false;
				_graphics.SynchronizeWithVerticalRetrace = true;
				_graphics.ApplyChanges();

				Window.AllowUserResizing = false;
				Window.Title = "Space Drift";
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
				// Center on the primary monitor using the ACTUAL client size (the OS may
				// have clamped the requested backbuffer), not the requested values.
				var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
				Window.Position = new Point(
					(displayMode.Width - Window.ClientBounds.Width) / 2,
					(displayMode.Height - Window.ClientBounds.Height) / 2
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
