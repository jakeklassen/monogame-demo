using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Enemy movement AI. Ported EXACTLY from space-drift/sim.ts enemyAiSystem.
	// Each live enemy flies with the player's handling (turn the nose toward a
	// goal, thrust along it, grip drags the slide) minus the boost. It patrols
	// random waypoints until the player comes on screen, then pursues; it drops
	// back to patrol once the player is well off screen. Enemies separate from one
	// another so they swarm rather than stack.
	//
	// Collected into a list first (not walked as a live query) because the
	// separation step nests enemies × enemies.
	public sealed class EnemyAiSystem(World world, Entity ship)
	{
		private const float DegToRad = MathF.PI / 180f;

		private readonly World _world = world;
		private readonly Entity _ship = ship;
		private readonly Random _rng = new();
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			Enemy,
			Transform,
			Previous,
			Velocity
		>();
		private readonly List<Entity> _enemies = [];

		public void Update(float dt)
		{
			_enemies.Clear();
			_world.Query(in _query, (Entity e, ref Enemy en) => _enemies.Add(e));

			var shipPos = _world.Get<Transform>(_ship).Position;
			float maxTurn = Constants.ShipRotationSpeed * dt;

			foreach (var ee in _enemies)
			{
				ref var en = ref _world.Get<Enemy>(ee);
				if (en.RespawnTimer > 0f)
					continue;

				ref var tf = ref _world.Get<Transform>(ee);
				ref var prev = ref _world.Get<Previous>(ee);
				ref var vel = ref _world.Get<Velocity>(ee);

				prev.Position = tf.Position;
				prev.Rotation = tf.Rotation;

				// Sight is tied to the viewport: an enemy only spots the player once it
				// is on screen, and disengages once it drops well past the edge — so
				// nothing rushes in from off-screen.
				float px = shipPos.X - tf.Position.X;
				float py = shipPos.Y - tf.Position.Y;
				float halfW = Constants.GameWidth / 2f;
				float halfH = Constants.GameHeight / 2f;
				bool onScreen = MathF.Abs(px) <= halfW && MathF.Abs(py) <= halfH;
				bool offScreen =
					MathF.Abs(px) > halfW + Constants.EnemySightLoseMargin
					|| MathF.Abs(py) > halfH + Constants.EnemySightLoseMargin;
				if (en.State == EnemyState.Patrol && onScreen)
					en.State = EnemyState.Engage;
				else if (en.State == EnemyState.Engage && offScreen)
					en.State = EnemyState.Patrol;

				// Pick a goal point. Both branches always thrust (like a player holding
				// the stick), so momentum + the capped turn rate make it bank and arc.
				float goalX;
				float goalY;
				if (en.State == EnemyState.Engage)
				{
					// Pursuit: aim straight at the player. Momentum + the turn cap make
					// it close in, blow past, and loop back — it reaches the player
					// instead of holding distance.
					goalX = shipPos.X;
					goalY = shipPos.Y;
				}
				else
				{
					en.RepathTimer -= dt;
					float wdx = en.Waypoint.X - tf.Position.X;
					float wdy = en.Waypoint.Y - tf.Position.Y;
					if (
						wdx * wdx + wdy * wdy
							<= Constants.EnemyWaypointReached * Constants.EnemyWaypointReached
						|| en.RepathTimer <= 0f
					)
					{
						en.Waypoint = new Vector2(
							ClampTo(
								tf.Position.X
									+ RndRange(
										-Constants.EnemyPatrolRadius,
										Constants.EnemyPatrolRadius
									),
								Constants.WorldWidth
							),
							ClampTo(
								tf.Position.Y
									+ RndRange(
										-Constants.EnemyPatrolRadius,
										Constants.EnemyPatrolRadius
									),
								Constants.WorldHeight
							)
						);
						en.RepathTimer = Constants.EnemyRepathTime;
					}
					goalX = en.Waypoint.X;
					goalY = en.Waypoint.Y;
				}

				// Turn the nose toward the goal, short way, capped at the turn rate.
				float ddx = goalX - tf.Position.X;
				float ddy = goalY - tf.Position.Y;
				float targetDeg = MathF.Atan2(ddx, -ddy) / DegToRad;
				float diff = targetDeg - tf.Rotation;
				diff = ((diff + 180f) % 360f + 360f) % 360f - 180f;
				tf.Rotation += Math.Clamp(diff, -maxTurn, maxTurn);

				float rad = tf.Rotation * DegToRad;
				float hx = MathF.Sin(rad);
				float hy = -MathF.Cos(rad);

				// Thrust along the nose (always).
				vel.Value.X += hx * Constants.EnemyThrust * dt;
				vel.Value.Y += hy * Constants.EnemyThrust * dt;

				// Grip: forward/lateral split dragged separately (same as the ship).
				float perpX = -hy;
				float perpY = hx;
				float fwd = vel.Value.X * hx + vel.Value.Y * hy;
				float lat = vel.Value.X * perpX + vel.Value.Y * perpY;
				float newFwd = fwd * MathF.Max(0f, 1f - Constants.ShipForwardDrag * dt);
				float newLat = lat * MathF.Max(0f, 1f - Constants.ShipLateralDrag * dt);
				vel.Value.X = hx * newFwd + perpX * newLat;
				vel.Value.Y = hy * newFwd + perpY * newLat;

				// Separation: push apart from other live enemies so they swarm rather
				// than stack. Applied after grip so it isn't over-damped.
				foreach (var oe in _enemies)
				{
					if (oe.Id == ee.Id)
						continue;
					ref var oen = ref _world.Get<Enemy>(oe);
					if (oen.RespawnTimer > 0f)
						continue;
					ref var otf = ref _world.Get<Transform>(oe);
					float sx = tf.Position.X - otf.Position.X;
					float sy = tf.Position.Y - otf.Position.Y;
					float d2 = sx * sx + sy * sy;
					if (d2 > 0f && d2 < Constants.EnemySeparation * Constants.EnemySeparation)
					{
						float d = MathF.Sqrt(d2);
						float push =
							(Constants.EnemySeparation - d)
							/ Constants.EnemySeparation
							* Constants.EnemySeparationForce
							* dt;
						vel.Value.X += sx / d * push;
						vel.Value.Y += sy / d * push;
					}
				}

				tf.Position.X += vel.Value.X * dt;
				tf.Position.Y += vel.Value.Y * dt;
			}
		}

		private float RndRange(float a, float b) => a + _rng.NextSingle() * (b - a);

		private static float ClampTo(float v, float max) => MathF.Max(0f, MathF.Min(max, v));
	}
}
