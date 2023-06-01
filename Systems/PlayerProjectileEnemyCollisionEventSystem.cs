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
		private readonly QueryDescription _playerProjectileEnemyCollisionEventQuery =
			new QueryDescription().WithAll<EventPlayerProjectileEnemyCollision>();

		private readonly QueryDescription _enemyQuery = new QueryDescription()
			.WithAll<TagEnemy>()
			.WithNone<Invulnerable>();

		public PlayerProjectileEnemyCollisionEventSystem(World world)
			: base(world) { }

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _playerProjectileEnemyCollisionEventQuery,
				(in Entity entity, ref EventPlayerProjectileEnemyCollision collisionEvent) =>
				{
					var projectile = collisionEvent.ProjectileEntity;
					var projectileTransform = World.Get<Transform>(projectile);

					// Bullet shockwave
					World.Create(
						new Shockwave(radius: 3, targetRadius: 6, color: Pico8Color.Color9, speed: 30),
						new Transform(
							position: new Vector2(
								projectileTransform.Position.X + 2,
								projectileTransform.Position.Y
							),
							rotation: 0,
							scale: Vector2.One
						)
					);

					World.TryGet<Flash>(collisionEvent.EnemyEntity, out var flash);

					if (flash == null)
					{
						World.Add(
							collisionEvent.EnemyEntity,
							new Flash() { Color = Pico8Color.Color7, Duration = 0.1f }
						);
					}

					var enemy = collisionEvent.EnemyEntity;

					if (World.TryGet<Invulnerable>(enemy, out var _invulnerable))
					{
						// Enemy is invulnerable but we still want to destroy the bullet.
						// If we don't, it's possible the enemy will lose invulnerability
						// then get hit by the same projectile that couldn't destroy it
						// before. It's leads to a pretty bad feel. This felt tighter.
						World.Destroy(collisionEvent.ProjectileEntity);
						World.Destroy(entity);

						return;
					}

					ref var enemyHealth = ref World.Get<Health>(enemy);
					var enemyTransform = World.Get<Transform>(enemy);
					var enemySprite = World.Get<Sprite>(enemy);

					// Enemy takes damage
					enemyHealth.Amount -= collisionEvent.Damage;

					// TODO: Need to flash enemy

					// Enemy is dead
					if (enemyHealth.Amount <= 0)
					{
						World.Destroy(enemy);

						var random = new Random();

						// Initial flash
						ExplosionFactory.CreateExplosion(
							World,
							count: 1,
							() =>
								new Direction(
									x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
									y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
								),
							() =>
								new Particle(
									age: 0,
									maxAge: 0,
									color: new XnaColor(
										Pico8Color.Color7.R,
										Pico8Color.Color7.G,
										Pico8Color.Color7.B,
										Pico8Color.Color7.A
									),
									isBlue: false,
									radius: 10,
									shape: Components.Shape.Circle,
									spark: false
								),
							() =>
								new Vector2(
									enemyTransform.Position.X + (enemySprite.CurrentFrame.Width / 2),
									enemyTransform.Position.Y + (enemySprite.CurrentFrame.Height / 2)
								),
							() => new Velocity()
						);

						// Large particles
						ExplosionFactory.CreateExplosion(
							World,
							count: 30,
							() =>
								new Direction(
									x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
									y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
								),
							() =>
								new Particle(
									age: random.NextSingle(0.06f),
									maxAge: 0.266f + random.NextSingle(0.266f),
									color: new XnaColor(
										Pico8Color.Color7.R,
										Pico8Color.Color7.G,
										Pico8Color.Color7.B,
										Pico8Color.Color7.A
									),
									isBlue: false,
									radius: 1 + random.NextSingle(4),
									shape: Components.Shape.Circle,
									spark: false
								),
							() =>
								new Vector2(
									enemyTransform.Position.X + (enemySprite.CurrentFrame.Width / 2),
									enemyTransform.Position.Y + (enemySprite.CurrentFrame.Height / 2)
								),
							() => new Velocity(x: random.NextSingle() * 50, y: random.NextSingle() * 50)
						);

						// Sparks
						ExplosionFactory.CreateExplosion(
							World,
							count: 20,
							() =>
								new Direction(
									x: 1 * Math.Sign((random.NextSingle() * 2) - 1),
									y: 1 * Math.Sign((random.NextSingle() * 2) - 1)
								),
							() =>
								new Particle(
									age: random.NextSingle(0.06f),
									maxAge: 0.266f + random.NextSingle(0.266f),
									color: new XnaColor(
										Pico8Color.Color7.R,
										Pico8Color.Color7.G,
										Pico8Color.Color7.B,
										Pico8Color.Color7.A
									),
									isBlue: true,
									radius: 1 + random.NextSingle(4),
									shape: Components.Shape.Circle,
									spark: true
								),
							() =>
								new Vector2(
									enemyTransform.Position.X + (enemySprite.CurrentFrame.Width / 2),
									enemyTransform.Position.Y + (enemySprite.CurrentFrame.Height / 2)
								),
							() => new Velocity(x: random.NextSingle() * 60, y: random.NextSingle() * 60)
						);

						// If this was the last enemy, spawn the next wave
						if (World.CountEntities(_enemyQuery) == 0)
						{
							World.Create(new EventNextWave());
						}
					}

					World.Destroy(entity);
					World.Destroy(projectile);
				}
			);
		}
	}
}
