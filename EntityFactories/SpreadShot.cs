using System;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.EntityFactories
{
	public static class SpreadShotFactory
	{
		// Fires count*2 + 1 big bullets in a 90 degree fan (225 -> 135 degrees, where
		// 180 is straight up), spawns a white shockwave, flashes the player + thruster,
		// grants ~1000ms invulnerability (M1's opacity blink), and emits a camera-shake
		// request. Velocity is stored as magnitude with Direction carrying the sign, to
		// match MovementSystem (velocity * direction).
		public static void Fire(World world, Entity player, Entity thruster, int count, float speed)
		{
			var playerTransform = world.Get<Transform>(player);

			// White shockwave (r3 -> 25).
			world.Create(
				new Shockwave(radius: 3, targetRadius: 25, color: Pico8Color.Color7, speed: 105),
				new Transform(
					position: new Vector2(
						playerTransform.Position.X + 3,
						playerTransform.Position.Y + 3
					),
					rotation: 0f,
					scale: Vector2.One
				)
			);

			// Flash the player sprite.
			if (!world.Has<Flash>(player))
			{
				world.Add(player, new Flash() { Color = Pico8Color.Color7, Duration = 0.1f });
			}

			// Flash the thruster sprite.
			if (world.IsAlive(thruster) && !world.Has<Flash>(thruster))
			{
				world.Add(thruster, new Flash() { Color = Pico8Color.Color7, Duration = 0.1f });
			}

			// ~1000ms invulnerability (reuses M1's opacity blink in InvulnerableSystem).
			if (!world.Has<Invulnerable>(player))
			{
				world.Add(player, new Invulnerable() { Duration = 1f });
			}

			// Camera shake request (no consumer until M5 — emitting is intentional).
			world.Create(new EventTriggerCameraShake(strength: 3, durationMs: 200));

			var total = count * 2;

			for (var i = 0; i <= total; i++)
			{
				// Evenly space the bullets from 225 to 135 degrees.
				var angle = 225f - ((i * 90f) / total);
				var radians = MathHelper.ToRadians(angle);

				var velocityX = (float)Math.Sin(radians) * speed;
				var velocityY = (float)Math.Cos(radians) * speed;

				var direction = new Direction(Math.Sign(velocityX), Math.Sign(velocityY));

				world.Create(
					new BoxCollider(
						SpriteSheet.BigBullet.BoxCollider.Width,
						SpriteSheet.BigBullet.BoxCollider.Height
					),
					new CollisionLayer(CollisionMasks.PlayerProjectile),
					new CollisionMask(CollisionMasks.Enemy),
					direction,
					new Sprite(SpriteSheet.BigBullet.Frame),
					new TagBigBullet(),
					new Transform(
						position: new Vector2(
							playerTransform.Position.X + 3,
							playerTransform.Position.Y + 6
						),
						rotation: 0f,
						scale: Vector2.One
					),
					new Velocity(Math.Abs(velocityX), Math.Abs(velocityY))
				);
			}
		}
	}
}
