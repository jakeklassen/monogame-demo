using System.Collections.Generic;
using Components;
using EntityFactories;
using Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Entities;
using MonoGame.Extended.ViewportAdapters;
using Systems;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb
{
	public class Game1 : Game
	{
		public const int TargetWidth = 128;
		public const int TargetHeight = 128;

		private OrthographicCamera _camera;

		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;

		private readonly SimpleFps _fps = new();
		private BitmapFont _font;

		private Texture2D _playerTexture;
		private Texture2D _spriteSheetTexture;

		private World _world;

		private bool _hasToggledVsync = false;
		private bool _hasToggledFixedTimeStep = false;

		private readonly Dictionary<string, Texture2D> _textureCache = new();

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
			_graphics.SynchronizeWithVerticalRetrace = true;
			_graphics.ApplyChanges();

			// Disable for a better experience with higher refresh rate monitors
			IsFixedTimeStep = false;
		}

		protected override void Initialize()
		{
			base.Initialize();

			var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, TargetWidth, TargetHeight);
			_camera = new OrthographicCamera(viewportAdapter);

			_world = new WorldBuilder()
				.AddSystem(new PlayerSystem())
				.AddSystem(new MovementSystem())
				.AddSystem(new DestroyOnViewportExitSystem())
				.AddSystem(new ParticleSystem())
				.AddSystem(new ShockwaveSystem())
				.AddSystem(new StarfieldSystem())
				.AddSystem(new StarfieldRenderingSystem(_graphics.GraphicsDevice, _camera))
				.AddSystem(new SpriteRenderingSystem(_graphics.GraphicsDevice, _camera, _spriteSheetTexture))
				.AddSystem(new ShockwaveRenderingSystem(_graphics.GraphicsDevice, _camera, _textureCache))
				.AddSystem(new ParticleRenderingSystem(_graphics.GraphicsDevice, _camera, _textureCache))
				.Build();

			{
				var player = _world.CreateEntity();

				player.Attach(new Direction());
				player.Attach(new Sprite(new Rectangle(16, 0, 8, 8)));
				player.Attach(new TagPlayer());
				player.Attach(
					new Transform(new Vector2((TargetWidth / 2) + (_playerTexture.Width / 2), (TargetHeight / 2) + (_playerTexture.Height / 2)), 0f, Vector2.One)
				);
				player.Attach(new Velocity(60, 60));
			}

			StarFactory.CreateStarfield(_world, TargetWidth, TargetHeight, 100);

			Components.Add(_world);
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			_font = Content.Load<BitmapFont>("Font/pico-8");
			_playerTexture = Content.Load<Texture2D>("Graphics/player-ship");
			_spriteSheetTexture = Content.Load<Texture2D>("Graphics/shmup");

			for (int radius = 1; radius <= 32; radius++)
			{
				var filedCircleTexture = Pico8Extensions.CircFill(GraphicsDevice, radius, XnaColor.White);
				_textureCache.Add($"circfill-{radius}", filedCircleTexture);

				var circleTexture = Pico8Extensions.Circ(GraphicsDevice, radius, XnaColor.White);
				_textureCache.Add($"circ-{radius}", circleTexture);
			}
		}

		protected override void UnloadContent()
		{
			base.UnloadContent();

			_spriteBatch.Dispose();
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
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
				_graphics.SynchronizeWithVerticalRetrace = !_graphics.SynchronizeWithVerticalRetrace;

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
			GraphicsDevice.Clear(XnaColor.Black);

			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, _camera.GetViewMatrix());

			// spriteBatch.Draw(textureCache["circ-1"], new Vector2(10, 94), Color.White);
			// spriteBatch.Draw(textureCache["circ-2"], new Vector2(16, 94), Color.White);
			// spriteBatch.Draw(textureCache["circ-3"], new Vector2(24, 94), Color.White);
			// spriteBatch.Draw(textureCache["circ-4"], new Vector2(34, 94), Color.White);
			// spriteBatch.Draw(textureCache["circ-5"], new Vector2(46, 94), Color.White);
			// spriteBatch.Draw(textureCache["circ-6"], new Vector2(60, 94) - new Vector2(6, 6), Color.White);
			// spriteBatch.DrawRectangle(new Rectangle(60, 94, 1, 1), Color.White, 1f);

			// spriteBatch.Draw(textureCache["circfill-1"], new Vector2(10, 110), Color.White);
			// spriteBatch.Draw(textureCache["circfill-2"], new Vector2(16, 110), Color.White);
			// spriteBatch.Draw(textureCache["circfill-3"], new Vector2(24, 110), Color.White);
			// spriteBatch.Draw(textureCache["circfill-4"], new Vector2(34, 110), Color.White);
			// spriteBatch.Draw(textureCache["circfill-5"], new Vector2(46, 110), Color.White);
			// spriteBatch.Draw(textureCache["circfill-6"], new Vector2(60, 110), Color.White);

			_spriteBatch.DrawString(_font, $"Is Fixed TimeStep: {IsFixedTimeStep}", new Vector2(2f, 25f), XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, $"Entity #: {_world.EntityCount}", new Vector2(2f, 32f), XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, $"Vsync: {_graphics.SynchronizeWithVerticalRetrace}", new Vector2(2f, 40f), XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			// fps.DrawFps(spriteBatch, font, new Vector2(2f, 55f), Color.White);

			_spriteBatch.End();

			base.Draw(gameTime);
		}
	}
}