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
	public class GameplayScreen : GameScreen
	{
		private new Game1 Game => (Game1)base.Game;
		private Texture2D _spriteSheetTexture;
		private readonly Tweener _tweener = new();
		private World _world;

		public GameplayScreen(Game1 game)
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
				.AddSystem(new NextWaveEventSystem(Game, _tweener))
				.AddSystem(new TimeToLiveSystem())
				.AddSystem(new BlinkSystem())
				.AddSystem(new PlayerSystem())
				.AddSystem(new MovementSystem())
				.AddSystem(new DestroyOnViewportExitSystem())
				.AddSystem(new CollisionSystem())
				.AddSystem(new PlayerProjectileEnemyCollisionEventSystem())
				.AddSystem(new ParticleSystem())
				.AddSystem(new InvulnerableSystem())
				.AddSystem(new SpriteAnimationSystem())
				.AddSystem(new StarfieldSystem())
				.AddSystem(new StarfieldRenderingSystem(Game.GraphicsDevice, Game.Camera))
				.AddSystem(new SpriteRenderingSystem(Game.GraphicsDevice, Game.Camera, _spriteSheetTexture))
				.AddSystem(new ShockwaveRenderingSystem(Game.GraphicsDevice, Game.Camera, Game.TextureCache))
				.AddSystem(new ParticleRenderingSystem(Game.GraphicsDevice, Game.Camera, Game.TextureCache))
				.AddSystem(new TextRenderingSystem(Game.GraphicsDevice, Game.Camera, Game.FontCache))
				.Build();

			StarFactory.CreateStarfield(_world, Game1.TargetWidth, Game1.TargetHeight, 100);

			var player = _world.CreateEntity();

			player.Attach(new CollisionLayer(CollisionMasks.Player));
			player.Attach(new CollisionMask(CollisionMasks.Enemy | CollisionMasks.EnemyProjectile | CollisionMasks.Pickup));
			player.Attach(new Direction());
			player.Attach(new Sprite(new Rectangle(16, 0, 8, 8)));
			player.Attach(new TagPlayer());
			player.Attach(
				new Transform(new Vector2((Game1.TargetWidth / 2) + 4, 100), 0f, Vector2.One)
			);
			player.Attach(new Velocity(60, 60));

			_world.CreateEntity().Attach(new EventNextWave());

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
				// 
			}
		}

		public override void Draw(GameTime gameTime)
		{

		}
	}
}