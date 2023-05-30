using System.Collections.Generic;
using Arch.Core;
using CherryBomb;
using Components;
using EntityFactories;
using Lib.Tweening;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
		private readonly World _world;
		private readonly List<SystemBase<GameTime>> _updateSystems = new();
		private readonly List<SystemBase<GameTime>> _drawSystems = new();

		public GameplayScreen(Game1 game)
			: base(game)
		{
			_world = World.Create();
		}

		public override void Initialize()
		{
			base.Initialize();
		}

		public override void LoadContent()
		{
			base.LoadContent();
			_spriteSheetTexture = Game.Content.Load<Texture2D>("Graphics/shmup");

			_updateSystems.Add(new NextWaveEventSystem(_world, Game, _tweener));
			_updateSystems.Add(new TimeToLiveSystem(_world));
			_updateSystems.Add(new BlinkSystem(_world));
			_updateSystems.Add(new PlayerSystem(_world));
			_updateSystems.Add(new MovementSystem(_world));
			_updateSystems.Add(new DestroyOnViewportExitSystem(_world));
			_updateSystems.Add(new CollisionSystem(_world));
			_updateSystems.Add(new PlayerProjectileEnemyCollisionEventSystem(_world));
			_updateSystems.Add(new ParticleSystem(_world));
			_updateSystems.Add(new InvulnerableSystem(_world));
			_updateSystems.Add(new StarfieldSystem(_world));
			_updateSystems.Add(new SpriteAnimationSystem(_world));
			_updateSystems.Add(new ShockwaveSystem(_world));

			_drawSystems.Add(new StarfieldRenderingSystem(_world, Game.GraphicsDevice, Game.Camera));
			_drawSystems.Add(
				new SpriteRenderingSystem(_world, Game.GraphicsDevice, Game.Camera, _spriteSheetTexture)
			);
			_drawSystems.Add(
				new ShockwaveRenderingSystem(_world, Game.GraphicsDevice, Game.Camera, Game.TextureCache)
			);
			_drawSystems.Add(
				new ParticleRenderingSystem(_world, Game.GraphicsDevice, Game.Camera, Game.TextureCache)
			);
			_drawSystems.Add(
				new TextRenderingSystem(_world, Game.GraphicsDevice, Game.Camera, Game.FontCache)
			);

			StarFactory.CreateStarfield(_world, Game1.TargetWidth, Game1.TargetHeight, 100);

			var player = _world.Create();

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
				new Transform(new Vector2((Game1.TargetWidth / 2) + 4, 100), 0f, Vector2.One)
			);
			_world.Add(player, new Velocity(60, 60));

			_world.Create(new EventNextWave());
		}

		public override void UnloadContent()
		{
			base.UnloadContent();

			World.Destroy(_world);
		}

		public override void Update(GameTime gameTime)
		{
			_tweener.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			if (KeyboardExtended.GetState().WasAnyKeyJustDown())
			{
				//
			}

			foreach (var system in _updateSystems)
			{
				system.Update(gameTime);
			}
		}

		public override void Draw(GameTime gameTime)
		{
			foreach (var system in _drawSystems)
			{
				system.Update(gameTime);
			}
		}
	}
}
