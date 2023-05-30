using System;
using Arch.Core;
using CherryBomb;
using Components;
using EntityFactories;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Systems
{
	public class PlayerProjectileEnemyCollisionEventSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _playerProjectileEnemyCollisionEventQuery = new QueryDescription().WithAll<EventPlayerProjectileEnemyCollision>();

		public PlayerProjectileEnemyCollisionEventSystem(World world) : base(world)
		{
		}

		public override void Update(in GameTime gameTime)
		{
			World.Query(in _playerProjectileEnemyCollisionEventQuery, (in Entity entity, ref EventPlayerProjectileEnemyCollision collisionEvent) =>
			{
				var projectile = collisionEvent.ProjectileEntity;
				var projectileTransform = World.Get<Transform>(projectile);

				World.Create(
					new Shockwave(
						radius: 3,
						targetRadius: 6,
						color: Pico8Color.Color9,
						speed: 30
					),
					new Transform(
						position: new Vector2(projectileTransform.Position.X - 4, projectileTransform.Position.Y - 4),
						rotation: 0,
						scale: Vector2.One
					)
				);

				var enemy = collisionEvent.EnemyEntity;
				var enemyTransform = World.Get<Transform>(enemy);
				var enemySprite = World.Get<Sprite>(enemy);

				var random = new Random();

				// Initial flash
				ExplosionFactory.CreateExplosion(
					World,
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
					World,
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
					World,
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


				World.Destroy(enemy);
				World.Destroy(entity);
				World.Destroy(projectile);
			});
		}
	}
}