using System;
using Arch.Core;
using CherryBomb.Components;
using CherryBomb.Systems;
using Microsoft.Xna.Framework;

namespace CherryBomb.EntityFactories
{
	public static class EnemyBulletFactory
	{
		// Spawns an animated enemy projectile travelling along angleRadians at speed.
		// angle 0 = straight down (matches the TS source: vx = sin(a)*s, vy = cos(a)*s).
		// MovementSystem multiplies velocity by direction, so velocity is stored as the
		// absolute magnitude and direction carries the sign. For M1 only straight-down
		// fire is used; the angle parameter keeps the door open for M3 spreads/aimed.
		public static Entity Fire(World world, Vector2 position, float angleRadians, float speed)
		{
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

			SoundSystem.Play(world, "enemy-projectile");

			return bullet;
		}
	}
}
