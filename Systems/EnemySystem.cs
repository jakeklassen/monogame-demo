using System;
using Arch.Core;
using Microsoft.Xna.Framework;
using SpaceDrift.Components;

namespace SpaceDrift.Systems
{
	// Decay hit flashes and respawn dead enemies near the ship after the delay.
	// Ported EXACTLY from space-drift/sim.ts enemySystem.
	public sealed class EnemySystem(World world, Entity ship)
	{
		private readonly World _world = world;
		private readonly Entity _ship = ship;
		private readonly Random _rng = new();
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			Enemy,
			Transform,
			Previous,
			Velocity
		>();

		public void Update(float dt)
		{
			var shipPos = _world.Get<Transform>(_ship).Position;

			_world.Query(
				in _query,
				(ref Enemy en, ref Transform tf, ref Previous prev, ref Velocity vel) =>
				{
					if (en.HitFlash > 0f)
						en.HitFlash = MathF.Max(0f, en.HitFlash - dt);

					if (en.RespawnTimer > 0f)
					{
						en.RespawnTimer -= dt;
						if (en.RespawnTimer <= 0f)
						{
							// Respawn out near the sight edge, reset to a fresh patrol.
							float angle = RndRange(0f, MathF.PI * 2f);
							float dist = RndRange(180f, 260f);
							float nx = shipPos.X + MathF.Cos(angle) * dist;
							float ny = shipPos.Y + MathF.Sin(angle) * dist;
							tf.Position = new Vector2(nx, ny);
							// Match previous so interpolation doesn't smear across the teleport.
							prev.Position = new Vector2(nx, ny);
							prev.Rotation = tf.Rotation;
							vel.Value = Vector2.Zero;
							en.RespawnTimer = 0f;
							en.Health = Constants.EnemyHealth;
							en.State = EnemyState.Patrol;
							en.Waypoint = new Vector2(
								nx
									+ RndRange(
										-Constants.EnemyPatrolRadius,
										Constants.EnemyPatrolRadius
									),
								ny
									+ RndRange(
										-Constants.EnemyPatrolRadius,
										Constants.EnemyPatrolRadius
									)
							);
							en.RepathTimer = RndRange(1f, Constants.EnemyRepathTime);
						}
					}
				}
			);
		}

		private float RndRange(float a, float b) => a + _rng.NextSingle() * (b - a);
	}
}
