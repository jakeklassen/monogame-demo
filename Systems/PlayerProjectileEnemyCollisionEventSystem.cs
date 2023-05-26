using System;
using CherryBomb;
using Components;
using EntityFactories;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class PlayerProjectileEnemyCollisionEventSystem : EntityProcessingSystem
	{
		private ComponentMapper<EventPlayerProjectileEnemyCollision> _eventPlayerProjectileEnemyCollisionMapper;

		public PlayerProjectileEnemyCollisionEventSystem() : base(Aspect.All(typeof(EventPlayerProjectileEnemyCollision)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
			_eventPlayerProjectileEnemyCollisionMapper = mapperService.GetMapper<EventPlayerProjectileEnemyCollision>();
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			var collisionEvent = _eventPlayerProjectileEnemyCollisionMapper.Get(entityId);

			var projectile = GetEntity(collisionEvent.ProjectileEntityId);
			var projectileTransform = projectile.Get<Transform>();

			var bulletShockwaveEntity = CreateEntity();

			bulletShockwaveEntity.Attach(new Shockwave(
				radius: 3,
				targetRadius: 6,
				color: Pico8Color.Color9,
				speed: 30
			));

			bulletShockwaveEntity.Attach(new Transform(
				position: new Vector2(projectileTransform.Position.X - 4, projectileTransform.Position.Y - 4),
				rotation: 0,
				scale: Vector2.One
			));

			var enemy = GetEntity(collisionEvent.EnemyEntityId);
			var enemyTransform = enemy.Get<Transform>();
			var enemySprite = enemy.Get<Sprite>();

			var random = new Random();

			// Initial flash
			ExplosionFactory.CreateExplosion(
				createEntityFn: CreateEntity,
				count: 1,
				() => new Direction(
					x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
					y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
				),
				() => new Particle(
					age: 0,
					maxAge: 0,
					color: new XnaColor(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A),
					isBlue: false,
					radius: 10,
					shape: Components.Shape.Circle,
					spark: false
				),
				() => new Vector2(
					enemyTransform.Position.X - (enemySprite.CurrentFrame.Width / 2),
					enemyTransform.Position.Y - (enemySprite.CurrentFrame.Height / 2)
				),
				() => new Velocity()
			);

			// Large particles
			ExplosionFactory.CreateExplosion(
				createEntityFn: CreateEntity,
				count: 30,
				() => new Direction(
					x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
					y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
				),
				() => new Particle(
					age: random.NextSingle(0.06f),
					maxAge: 0.266f + random.NextSingle(0.266f),
					color: new XnaColor(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A),
					isBlue: false,
					radius: 1 + random.NextSingle(4),
					shape: Components.Shape.Circle,
					spark: false
				),
				() => new Vector2(
					enemyTransform.Position.X - (enemySprite.CurrentFrame.Width / 2),
					enemyTransform.Position.Y - (enemySprite.CurrentFrame.Height / 2)
				),
				() => new Velocity(
					x: random.NextSingle() * 50,
					y: random.NextSingle() * 50
				)
			);

			// Sparks
			ExplosionFactory.CreateExplosion(
				createEntityFn: CreateEntity,
				count: 20,
				() => new Direction(
					x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
					y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
				),
				() => new Particle(
					age: random.NextSingle(0.06f),
					maxAge: 0.266f + random.NextSingle(0.266f),
					color: new XnaColor(Pico8Color.Color7.R, Pico8Color.Color7.G, Pico8Color.Color7.B, Pico8Color.Color7.A),
					isBlue: true,
					radius: 1 + random.NextSingle(4),
					shape: Components.Shape.Circle,
					spark: true
				),
				() => new Vector2(
					enemyTransform.Position.X - (enemySprite.CurrentFrame.Width / 2),
					enemyTransform.Position.Y - (enemySprite.CurrentFrame.Height / 2)
				),
				() => new Velocity(
					x: random.NextSingle() * 60,
					y: random.NextSingle() * 60
				)
			);

			// Destory the entities
			DestroyEntity(collisionEvent.ProjectileEntityId);
			DestroyEntity(collisionEvent.EnemyEntityId);
			DestroyEntity(entityId);
		}
	}
}