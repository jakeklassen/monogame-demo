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
				// Fixed backbuffer at the exact window size so the ×Scale blit stays
				// an integer (pixel-perfect). No DPI scaling — that would change the
				// backbuffer size and break the integer scale.
				_graphics.PreferredBackBufferWidth = Constants.WindowWidth;
				_graphics.PreferredBackBufferHeight = Constants.WindowHeight;
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
