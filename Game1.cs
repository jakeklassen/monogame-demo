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
using MonoGame.Extended.Screens;
using MonoGame.Extended.ViewportAdapters;
using Screens;
using Systems;
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

		private Texture2D _spriteSheetTexture;

		private World _world;

		private bool _hasToggledVsync = false;
		private bool _hasToggledFixedTimeStep = false;

		public OrthographicCamera Camera { get; private set; }
		public Dictionary<string, BitmapFont> FontCache { get; } = new();
		public SpriteBatch SpriteBatch { get; private set; }
		public Dictionary<string, Texture2D> TextureCache { get; } = new();
		public Config Config { get; } = new();
		public State State { get; } = new();

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

			_screenManager = new ScreenManager();

			Components.Add(_screenManager);
		}

		protected override void Initialize()
		{
			base.Initialize();

			var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, TargetWidth, TargetHeight);
			Camera = new OrthographicCamera(viewportAdapter);

			// _world = new WorldBuilder()
			// 	.AddSystem(new PlayerSystem())
			// 	.AddSystem(new MovementSystem())
			// 	.AddSystem(new DestroyOnViewportExitSystem())
			// 	.AddSystem(new CollisionSystem())
			// 	.AddSystem(new PlayerProjectileEnemyCollisionEventSystem())
			// 	.AddSystem(new ParticleSystem())
			// 	.AddSystem(new ShockwaveSystem())
			// 	.AddSystem(new StarfieldSystem())
			// 	.AddSystem(new StarfieldRenderingSystem(_graphics.GraphicsDevice, Camera))
			// 	.AddSystem(new SpriteRenderingSystem(_graphics.GraphicsDevice, Camera, _spriteSheetTexture))
			// 	.AddSystem(new ShockwaveRenderingSystem(_graphics.GraphicsDevice, Camera, TextureCache))
			// 	.AddSystem(new ParticleRenderingSystem(_graphics.GraphicsDevice, Camera, TextureCache))
			// 	.Build();

			// {
			// 	var player = _world.CreateEntity();

			// 	player.Attach(new CollisionLayer(CollisionMasks.Player));
			// 	player.Attach(new CollisionMask(CollisionMasks.Enemy | CollisionMasks.EnemyProjectile | CollisionMasks.Pickup));
			// 	player.Attach(new Direction());
			// 	player.Attach(new Sprite(new Rectangle(16, 0, 8, 8)));
			// 	player.Attach(new TagPlayer());
			// 	player.Attach(
			// 		new Transform(new Vector2((TargetWidth / 2) + 4, 100), 0f, Vector2.One)
			// 	);
			// 	player.Attach(new Velocity(60, 60));
			// }

			// {
			// 	var enemy = _world.CreateEntity();

			// 	enemy.Attach(new BoxCollider(8, 8));
			// 	enemy.Attach(new CollisionLayer(CollisionMasks.Enemy));
			// 	enemy.Attach(new CollisionMask(CollisionMasks.Player | CollisionMasks.PlayerProjectile));
			// 	enemy.Attach(new Sprite(new Rectangle(40, 8, 8, 8)));
			// 	enemy.Attach(new TagEnemy());
			// 	enemy.Attach(new Transform(new Vector2((TargetWidth / 2) + 4, 32), 0f, Vector2.One));
			// }

			// StarFactory.CreateStarfield(_world, TargetWidth, TargetHeight, 100);

			// Components.Add(_world);

			_screenManager.LoadScreen(new GameplayScreen(this));
		}

		protected override void LoadContent()
		{
			SpriteBatch = new SpriteBatch(GraphicsDevice);

			_font = Content.Load<BitmapFont>("Font/pico-8");
			_spriteSheetTexture = Content.Load<Texture2D>("Graphics/shmup");

			FontCache.Add("pico-8", _font);

			for (int radius = 1; radius <= 32; radius++)
			{
				var filedCircleTexture = Pico8Extensions.CircFill(GraphicsDevice, radius, XnaColor.White);
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

			SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, Camera.GetViewMatrix());

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

			// _spriteBatch.DrawString(_font, $"Is Fixed TimeStep: {IsFixedTimeStep}", new Vector2(2f, 25f), XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			// SpriteBatch.DrawString(_font, $"Entity #: {_world.EntityCount}", new Vector2(2f, 32f), XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			// _spriteBatch.DrawString(_font, $"Vsync: {_graphics.SynchronizeWithVerticalRetrace}", new Vector2(2f, 40f), XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			// fps.DrawFps(spriteBatch, font, new Vector2(2f, 55f), Color.White);

			SpriteBatch.End();

			base.Draw(gameTime);
		}
	}
}