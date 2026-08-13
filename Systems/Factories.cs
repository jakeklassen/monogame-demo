using System;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Entity factories ported from space-drift/factories.ts. Shared by the combat
	// systems so bullet / enemy / particle creation lives in one place.
	public static class Factories
	{
		public static Entity CreateBullet(
			World world,
			float x,
			float y,
			float rotation,
			float vx,
			float vy
		)
		{
			var pos = new Vector2(x, y);
			return world.Create(
				new Transform(pos, rotation),
				new Previous(pos, rotation),
				new Velocity(new Vector2(vx, vy)),
				new Bullet { Age = 0f, MaxAge = Constants.BulletLifetime }
			);
		}

		public static Entity CreateHomingBullet(
			World world,
			float x,
			float y,
			float rotation,
			float vx,
			float vy,
			Entity target
		)
		{
			var pos = new Vector2(x, y);
			return world.Create(
				new Transform(pos, rotation),
				new Previous(pos, rotation),
				new Velocity(new Vector2(vx, vy)),
				new Bullet { Age = 0f, MaxAge = Constants.HomingLifetime },
				new Homing { TurnRate = Constants.HomingTurnRate, Target = target }
			);
		}

		public static void CreateParticle(
			World world,
			float x,
			float y,
			float vx,
			float vy,
			float maxAge,
			ParticleKind kind
		)
		{
			world.Create(
				new Transform(new Vector2(x, y), 0f),
				new Velocity(new Vector2(vx, vy)),
				new Particle
				{
					Age = 0f,
					MaxAge = maxAge,
					Kind = kind,
					Size = 1f,
				}
			);
		}

		public static void CreatePlanet(
			World world,
			Random rng,
			float x,
			float y,
			float radius,
			PlanetPalette palette
		)
		{
			float RndRange(float a, float b) => a + rng.NextSingle() * (b - a);
			world.Create(
				new Transform(new Vector2(x, y), 0f),
				new Planet
				{
					Radius = radius,
					Dark = palette.Dark,
					Base = palette.Base,
					Light = palette.Light,
				},
				new Pulse
				{
					Time = RndRange(0f, MathF.PI * 2f),
					Speed = RndRange(0.5f, 1.0f),
					Amplitude = 1f,
				}
			);
		}

		public static Entity CreateEnemy(World world, Random rng, float x, float y)
		{
			float RndRange(float a, float b) => a + rng.NextSingle() * (b - a);
			var pos = new Vector2(x, y);
			return world.Create(
				new Transform(pos, RndRange(0f, 360f)),
				new Previous(pos, 0f),
				new Velocity(Vector2.Zero),
				new Enemy
				{
					Health = Constants.EnemyHealth,
					HitFlash = 0f,
					RespawnTimer = 0f,
					State = EnemyState.Patrol,
					Waypoint = new Vector2(
						x + RndRange(-Constants.EnemyPatrolRadius, Constants.EnemyPatrolRadius),
						y + RndRange(-Constants.EnemyPatrolRadius, Constants.EnemyPatrolRadius)
					),
					RepathTimer = RndRange(1f, Constants.EnemyRepathTime),
				}
			);
		}
	}
}
