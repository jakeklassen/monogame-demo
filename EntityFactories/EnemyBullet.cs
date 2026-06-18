using System;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;

namespace CherryBomb.EntityFactories
{
	public static class EnemyBulletFactory
	{
		// Enemy config ids (Config / SpriteSheet). Mirrors the TS EnemyType enum.
		private const int YellowShip = 4;
		private const int Boss = 5;

		// Spawns an animated enemy projectile from an enemy entity.
		//
		// angleDegrees == 0 means straight down: velocity = (sin a, cos a) * speed,
		// matching the TS source (enemy/enemy-bullets.ts). MovementSystem multiplies
		// velocity by direction, so velocity stores the absolute magnitude and the
		// direction carries the sign. The fire position is offset per enemy type so
		// the bullet leaves the muzzle. Non-boss shooters get a brief white flash;
		// the SFX is optional so spreads can play a single pooled sound instead.
		public static Entity Fire(
			World world,
			Entity enemy,
			float angleDegrees,
			float speed,
			bool triggerSound
		)
		{
			var enemyType = world.Has<EnemyType>(enemy) ? world.Get<EnemyType>(enemy).Value : 0;
			var enemyPosition = world.Has<Transform>(enemy)
				? world.Get<Transform>(enemy).Position
				: Vector2.Zero;

			// Default muzzle (matches TS: +3, +6). Yellow ship and boss fire from lower.
			var firePosition = new Vector2(enemyPosition.X + 3f, enemyPosition.Y + 6f);

			if (enemyType == YellowShip)
			{
				firePosition = new Vector2(enemyPosition.X + 6f, enemyPosition.Y + 13f);
			}
			else if (enemyType == Boss)
			{
				firePosition = new Vector2(enemyPosition.X + 13f, enemyPosition.Y + 22f);
			}

			var bullet = SpawnBullet(world, firePosition, angleDegrees, speed);

			// White fire-flash on the shooter (non-boss), 120ms. Reuses the Flash path.
			// Set-or-add so a concurrent hit-flash doesn't double-add the component.
			if (enemyType != Boss && world.IsAlive(enemy))
			{
				var flash = new Flash() { Color = Pico8Color.Color7, Duration = 0.12f };

				if (world.Has<Flash>(enemy))
				{
					world.Set(enemy, flash);
				}
				else
				{
					world.Add(enemy, flash);
				}
			}

			if (triggerSound)
			{
				SoundSystem.Play(world, "enemy-projectile");
			}

			return bullet;
		}

		// Evenly-spaced ring of `count` bullets. baseAngle (0..1) rotates the whole ring;
		// defaults to random like the TS. A single SFX is played for the volley.
		public static Entity[] FireSpread(
			World world,
			Entity enemy,
			int count,
			float speed,
			float? baseAngle = null
		)
		{
			var @base = baseAngle ?? (float)Random.Shared.NextDouble();
			var bullets = new Entity[count];

			for (var i = 0; i < count; i++)
			{
				// +i mirrors the TS jitter so the ring isn't perfectly symmetric.
				var angle = ((360f / count) * i) + (@base * 360f) + i;

				bullets[i] = Fire(world, enemy, angle, speed, triggerSound: false);
			}

			SoundSystem.Play(world, "enemy-projectile");

			return bullets;
		}

		// Fires a single bullet aimed at targetPosition via atan2 (degrees), accounting
		// for the enemy's sprite half-width. Source: aimedFire in enemy/enemy-bullets.ts.
		public static Entity AimedFire(
			World world,
			Entity enemy,
			Vector2 targetPosition,
			float speed
		)
		{
			var enemyPosition = world.Has<Transform>(enemy)
				? world.Get<Transform>(enemy).Position
				: Vector2.Zero;
			var halfWidth = world.Has<Sprite>(enemy)
				? world.Get<Sprite>(enemy).CurrentFrame.Width / 2f
				: 0f;

			var angleRadians = (float)
				Math.Atan2(
					targetPosition.X + 4f - (enemyPosition.X + halfWidth),
					targetPosition.Y + 4f - (enemyPosition.Y + halfWidth)
				);

			return Fire(
				world,
				enemy,
				angleDegrees: angleRadians * (180f / (float)Math.PI),
				speed,
				triggerSound: true
			);
		}

		private static Entity SpawnBullet(
			World world,
			Vector2 position,
			float angleDegrees,
			float speed
		)
		{
			var angleRadians = MathHelper.ToRadians(angleDegrees);
			var velocityX = (float)Math.Sin(angleRadians) * speed;
			var velocityY = (float)Math.Cos(angleRadians) * speed;

			var direction = new Direction(Math.Sign(velocityX), Math.Sign(velocityY));

			var idle = SpriteSheet.EnemyBullet.Animations["Pulse"];

			var bullet = world.Create();

			world.Add(bullet, SpriteSheet.EnemyBullet.BoxCollider);
			world.Add(bullet, new CollisionLayer(CollisionMasks.EnemyProjectile));
			world.Add(bullet, new CollisionMask(CollisionMasks.Player));
			world.Add(bullet, direction);
			world.Add(bullet, new Sprite(SpriteSheet.EnemyBullet.Frame));
			world.Add(
				bullet,
				SpriteAnimation.Factory(
					animationDetails: new AnimationDetails()
					{
						Name = "enemy-bullet",
						SourceX = idle.SourceX,
						SourceY = idle.SourceY,
						Width = idle.Width,
						Height = idle.Height,
						FrameWidth = idle.FrameWidth,
						FrameHeight = idle.FrameHeight,
					},
					durationSeconds: 0.25f,
					loop: true,
					frameSequence: [0, 1, 2, 1]
				)
			);
			world.Add(bullet, new TagEnemyBullet());
			world.Add(bullet, new Transform(position, 0f, Vector2.One));
			world.Add(bullet, new Velocity(Math.Abs(velocityX), Math.Abs(velocityY)));

			return bullet;
		}
	}
}
