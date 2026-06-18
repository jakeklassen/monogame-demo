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
		private SoundSystem _soundSystem;

		public override void LoadContent()
		{
			base.LoadContent();
			_spriteSheetTexture = Game.Content.Load<Texture2D>("Graphics/shmup");

			// Start each run from a clean slate: full lives, score 0, not game-over.
			Game.State.Reset();

			_soundSystem = new SoundSystem(_world, Game.SoundCache);

			_updateSystems.Add(
				new NextWaveEventSystem(_world, Game.State, Game.Config, _tweener, _scheduler)
			);
			_updateSystems.Add(new TimeToLiveSystem(_world));
			_updateSystems.Add(new BlinkSystem(_world));
			// Input / fire.
			_updateSystems.Add(new PlayerSystem(_world, Game.State));
			// Enemy AI: cadence emits attack/fire events, then the handlers select
			// enemies and switch them into attack runs / make them fire.
			_updateSystems.Add(new EnemyPickSystem(_world, Game.State, Game.Config));
			_updateSystems.Add(new TriggerEnemyAttackEventSystem(_world, _scheduler));
			_updateSystems.Add(new TriggerEnemyFireEventSystem(_world));
			// Per-type attack behaviour: tweak velocity/direction before movement.
			_updateSystems.Add(new LateralHunterSystem(_world));
			_updateSystems.Add(new YellowShipSystem(_world));
			// Boss (wave 9) 4-phase attack cycle: sets direction/velocity + fires,
			// so it must run before MovementSystem applies the motion.
			_updateSystems.Add(new BossSystem(_world));
			// Movement.
			_updateSystems.Add(new MovementSystem(_world));
			// Horizontal weave for attacking enemies; owns X so it runs after movement.
			_updateSystems.Add(new SwaySystem(_world));
			// After movement so children follow the parent's freshly-updated position.
			_updateSystems.Add(new LocalTransformSystem(_world));
			_updateSystems.Add(new BoundToViewportSystem(_world, Game1.Viewport));
			_updateSystems.Add(new DestroyOnViewportExitSystem(_world));
			// Collision detection then the collision-event handlers.
			_updateSystems.Add(new CollisionSystem(_world, Game.Config));
			_updateSystems.Add(
				new PlayerProjectileEnemyCollisionEventSystem(_world, Game.State, Game.Config)
			);
			_updateSystems.Add(
				new PlayerEnemyCollisionEventSystem(_world, Game.State, Game.Config)
			);
			// Boss damage / hurt-flash path (kept separate from the enemy-death path
			// so the boss is never destroyed by it) and the scripted win sequence.
			_updateSystems.Add(new PlayerProjectileBossCollisionEventSystem(_world));
			_updateSystems.Add(
				new DestroyBossEventSystem(_world, Game.State, Game.Config, _scheduler)
			);
			_updateSystems.Add(new PlayerPickupCollisionEventSystem(_world, Game.State));
			// HUD text sync (diff State -> Text content).
			_updateSystems.Add(new ScoreSystem(_world, Game.State));
			_updateSystems.Add(new CherryTextSystem(_world, Game.State));
			_updateSystems.Add(new ParticleSystem(_world));
			_updateSystems.Add(new InvulnerableSystem(_world));
			_updateSystems.Add(new StarfieldSystem(_world));
			_updateSystems.Add(new SpriteAnimationSystem(_world));
			_updateSystems.Add(new ShockwaveSystem(_world));
			// Consume sound events emitted this frame.
			_updateSystems.Add(_soundSystem);

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
			// HUD hearts drawn on top of the world.
			_drawSystems.Add(
				new LivesRenderingSystem(
					_world,
					Game.SpriteBatch,
					Game.Camera,
					_spriteSheetTexture,
					Game.State
				)
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

			// --- HUD entities -------------------------------------------------
			// Score text (top area, just under the hearts). ScoreSystem keeps the
			// content in sync with State.Score; seed it with the current value.
			var scoreText = _world.Create();
			_world.Add(
				scoreText,
				new Text()
				{
					Alignment = Alignment.Left,
					Color = Pico8Color.Color7,
					Content = $"Score:{Game.State.Score}",
					Font = "pico-8",
				}
			);
			_world.Add(scoreText, new TagScoreText());
			_world.Add(scoreText, new Transform(new Vector2(1, 10), 0f, Vector2.One));

			// Cherry icon (top-right) plus its count text. CherryTextSystem keeps the
			// count in sync with State.Cherries.
			var cherryIcon = _world.Create();
			_world.Add(cherryIcon, new Sprite(SpriteSheet.Cherry.Frame));
			_world.Add(
				cherryIcon,
				new Transform(new Vector2(Game1.TargetWidth - 9, 0), 0f, Vector2.One)
			);

			var cherryText = _world.Create();
			_world.Add(
				cherryText,
				new Text()
				{
					Alignment = Alignment.Right,
					Color = Pico8Color.Color7,
					Content = $"{Game.State.Cherries}",
					Font = "pico-8",
				}
			);
			_world.Add(cherryText, new TagCherryText());
			_world.Add(
				cherryText,
				new Transform(new Vector2(Game1.TargetWidth - 10, 1), 0f, Vector2.One)
			);

			_world.Create(new EventNextWave());
		}

		public override void UnloadContent()
		{
			_soundSystem?.StopAll();

			base.UnloadContent();
		}
	}
}
