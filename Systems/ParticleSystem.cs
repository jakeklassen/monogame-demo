using System;
using System.Collections.Generic;
using Arch.Core;
using CherryBomb.Components;

namespace CherryBomb.Systems
{
	// Fixed-step particle simulation. Ported from space-drift/sim.ts particleSystem:
	// age, apply light drag, integrate, and reap expired particles.
	public sealed class ParticleSystem(World world)
	{
		private readonly World _world = world;
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			Particle,
			Transform,
			Velocity
		>();
		private readonly List<Entity> _dead = [];

		public void Update(float dt)
		{
			_dead.Clear();
			float drag = MathF.Max(0f, 1f - 3f * dt);

			_world.Query(
				in _query,
				(Entity entity, ref Particle p, ref Transform tf, ref Velocity vel) =>
				{
					p.Age += dt;
					if (p.Age >= p.MaxAge)
					{
						_dead.Add(entity);
						return;
					}

					tf.Position.X += vel.Value.X * dt;
					tf.Position.Y += vel.Value.Y * dt;
					vel.Value.X *= drag;
					vel.Value.Y *= drag;
				}
			);

			foreach (var entity in _dead)
			{
				_world.Destroy(entity);
			}
		}
	}
}
