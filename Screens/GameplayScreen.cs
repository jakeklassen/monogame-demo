using CherryBomb.Components;
using CherryBomb.EntityFactories;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CherryBomb.Screens
{
	public class GameplayScreen(Game1 game) : GameScreenBase(game)
	{
		private Texture2D _spriteSheetTexture;

		public override void LoadContent()
		{
			base.LoadContent();
			_spriteSheetTexture = Game.Content.Load<Texture2D>("Graphics/shmup");

			_updateSystems.Add(new NextWaveEventSystem(_world, Game.State, Game.Config, _tweener));
			_updateSystems.Add(new TimeToLiveSystem(_world));
			_updateSystems.Add(new BlinkSystem(_world));
			_updateSystems.Add(new PlayerSystem(_world));
			_updateSystems.Add(new MovementSystem(_world));
			// After movement so children follow the parent's freshly-updated position.
			_updateSystems.Add(new LocalTransformSystem(_world));
			_updateSystems.Add(new BoundToViewportSystem(_world, Game1.Viewport));
			_updateSystems.Add(new DestroyOnViewportExitSystem(_world));
			_updateSystems.Add(new CollisionSystem(_world, Game.Config));
			_updateSystems.Add(new PlayerProjectileEnemyCollisionEventSystem(_world));
			_updateSystems.Add(new ParticleSystem(_world));
			_updateSystems.Add(new InvulnerableSystem(_world));
			_updateSystems.Add(new StarfieldSystem(_world));
			_updateSystems.Add(new SpriteAnimationSystem(_world));
			_updateSystems.Add(new ShockwaveSystem(_world));

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
				new FlashRenderingSystem(_world, Game.SpriteBatch, Game.Camera, _spriteSheetTexture)
			);
			_drawSystems.Add(
				new ShockwaveRenderingSystem(
					_world,
					Game.SpriteBatch,
					Game.Camera,
					Game.TextureCache
				)
			);
			_drawSystems.Add(
				new ParticleRenderingSystem(
					_world,
					Game.SpriteBatch,
					Game.Camera,
					Game.TextureCache
				)
			);
			_drawSystems.Add(
				new TextRenderingSystem(_world, Game.SpriteBatch, Game.Camera, Game.FontCache)
			);
			_drawSystems.Add(new DebugRenderingSystem(_world, Game, Game.SpriteBatch, Game.Camera));

			StarFactory.CreateStarfield(_world, Game1.TargetWidth, Game1.TargetHeight, 100);

			var player = _world.Create();

			_world.Add(player, new BoundToViewport());
			_world.Add(
				player,
				new BoxCollider(
					SpriteSheet.Player.BoxCollider.Width,
					SpriteSheet.Player.BoxCollider.Height,
					new Vector2(
						SpriteSheet.Player.BoxCollider.Offset.X,
						SpriteSheet.Player.BoxCollider.Offset.Y
					)
				)
			);
			_world.Add(player, new CollisionLayer(CollisionMasks.Player));
			_world.Add(
				player,
				new CollisionMask(
					CollisionMasks.Enemy | CollisionMasks.EnemyProjectile | CollisionMasks.Pickup
				)
			);
			_world.Add(player, new Direction());
			_world.Add(player, new Sprite(new Rectangle(16, 0, 8, 8)));
			_world.Add(player, new TagPlayer());
			_world.Add(
				player,
				new Transform(new Vector2((Game1.TargetWidth / 2) - 4, 100), 0f, Vector2.One)
			);
			_world.Add(player, new Velocity(60, 60));

			// Player thruster: a child entity parented to the player. Its position is
			// driven each frame by LocalTransformSystem (player position + offset),
			// so it stays attached as the player moves. Offset is one sprite-height
			// below the player. First consumer of the Parent/LocalTransform plumbing.
			var thruster = _world.Create();

			_world.Add(thruster, new Parent(player));
			_world.Add(thruster, new LocalTransform(new Vector2(0, 8)));
			_world.Add(thruster, new Transform(Vector2.Zero, 0f, Vector2.One));
			_world.Add(thruster, new Sprite(SpriteSheet.Player.Thruster.Frame));
			_world.Add(
				thruster,
				SpriteAnimation.Factory(
					new AnimationDetails
					{
						Name = "player-thruster",
						SourceX = SpriteSheet.Player.Thruster.Animations["Thrust"].SourceX,
						SourceY = SpriteSheet.Player.Thruster.Animations["Thrust"].SourceY,
						Width = SpriteSheet.Player.Thruster.Animations["Thrust"].Width,
						Height = SpriteSheet.Player.Thruster.Animations["Thrust"].Height,
						FrameWidth = SpriteSheet.Player.Thruster.Animations["Thrust"].FrameWidth,
						FrameHeight = SpriteSheet.Player.Thruster.Animations["Thrust"].FrameHeight,
					},
					durationSeconds: 0.1f
				)
			);

			_world.Create(new EventNextWave());
		}
	}
}
