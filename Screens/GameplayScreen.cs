using Arch.Core;
using CherryBomb.Components;
using CherryBomb.EntityFactories;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace CherryBomb.Screens
{
	public class GameplayScreen(Game1 game) : GameScreenBase(game)
	{
		private Texture2D _spriteSheetTexture;
		private SoundSystem _soundSystem;

		private readonly QueryDescription _gameOverQuery =
			new QueryDescription().WithAll<EventGameOver>();
		private readonly QueryDescription _gameWonQuery =
			new QueryDescription().WithAll<EventGameWon>();

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
				new PlayerEnemyCollisionEventSystem(_world, Game.State, Game.Config, _scheduler)
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
			_updateSystems.Add(new SpriteOutlineAnimationSystem(_world));
			_updateSystems.Add(new ShockwaveSystem(_world));
			// Cherry-bomb: input emits EventTriggerBomb (PlayerSystem); this consumes
			// it and ripple-spawns bomb projectiles via the scheduler.
			_updateSystems.Add(new BombSystem(_world, Game.State, _scheduler));
			// Muzzle flash shrink + lifetime.
			_updateSystems.Add(new MuzzleFlashSystem(_world));
			// Consume sound events emitted this frame.
			_updateSystems.Add(_soundSystem);
			// Camera shake last: jitters the shared camera right before Draw so all
			// camera-matrix rendering shakes together (non-accumulating).
			_updateSystems.Add(new CameraShakeSystem(_world, Game.Camera));

			_drawSystems.Add(new StarfieldRenderingSystem(_world, Game.SpriteBatch, Game.Camera));
			// Sprite outlines draw behind the sprites they wrap (cherry pickup).
			_drawSystems.Add(
				new SpriteOutlineRenderingSystem(
					_world,
					Game.SpriteBatch,
					Game.Camera,
					_spriteSheetTexture
				)
			);
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
			// Muzzle flash FX overlay, on top of the world but under HUD text.
			_drawSystems.Add(
				new MuzzleFlashRenderingSystem(
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
			// Cherry icon (top-right). Source: gameplay-screen.ts -> (108, 1).
			var cherryIcon = _world.Create();
			_world.Add(cherryIcon, new Sprite(SpriteSheet.Cherry.Frame));
			_world.Add(cherryIcon, new Transform(new Vector2(108, 1), 0f, Vector2.One));

			// Score text on the SAME top row as the hearts, to their right.
			// Source: gameplay-screen.ts -> (40, 2), Color12, left-aligned,
			// "Score:{score}". ScoreSystem keeps the content in sync with State.Score.
			var scoreText = _world.Create();
			_world.Add(
				scoreText,
				new Text()
				{
					Alignment = Alignment.Left,
					Color = Pico8Color.Color12,
					Content = $"Score:{Game.State.Score}",
					Font = "pico-8",
				}
			);
			_world.Add(scoreText, new TagScoreText());
			_world.Add(scoreText, new Transform(new Vector2(40, 2), 0f, Vector2.One));

			// Cherry count text (top-right, just right of the cherry icon).
			// Source: gameplay-screen.ts -> (118, 2), Color14, left-aligned.
			// CherryTextSystem keeps the count in sync with State.Cherries.
			var cherryText = _world.Create();
			_world.Add(
				cherryText,
				new Text()
				{
					Alignment = Alignment.Left,
					Color = Pico8Color.Color14,
					Content = $"{Game.State.Cherries}",
					Font = "pico-8",
				}
			);
			_world.Add(cherryText, new TagCherryText());
			_world.Add(cherryText, new Transform(new Vector2(118, 2), 0f, Vector2.One));

			_world.Create(new EventNextWave());

			// Play the game-start jingle when the gameplay screen begins.
			// Source: gameplay-screen.ts initialize().
			SoundSystem.Play(_world, "game-start");
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			// M1 emits EventGameOver (out of lives) and M4 emits EventGameWon (boss
			// death sequence) but neither had a consumer until now. Detect either,
			// freeze the current frame for the backdrop, and swap screens.
			if (_world.CountEntities(_gameOverQuery) > 0)
			{
				CaptureFrozenFrame();
				ScreenManager.ReplaceScreen(new GameOverScreen(Game));

				return;
			}

			if (_world.CountEntities(_gameWonQuery) > 0)
			{
				CaptureFrozenFrame();
				ScreenManager.ReplaceScreen(new GameWonScreen(Game));
			}
		}

		// Snapshots the back buffer (the last drawn gameplay frame) into a texture so
		// the GameOver/GameWon screen can draw a frozen-frame backdrop. Stored on Game1.
		private void CaptureFrozenFrame()
		{
			var device = Game.GraphicsDevice;
			var width = device.PresentationParameters.BackBufferWidth;
			var height = device.PresentationParameters.BackBufferHeight;

			var data = new XnaColor[width * height];
			device.GetBackBufferData(data);

			Game.FrozenFrame?.Dispose();

			var snapshot = new Texture2D(device, width, height);
			snapshot.SetData(data);

			Game.FrozenFrame = snapshot;
		}

		public override void UnloadContent()
		{
			_soundSystem?.StopAll();

			base.UnloadContent();
		}
	}
}
