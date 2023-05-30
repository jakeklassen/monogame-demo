using System;
using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace EntityFactories
{
	public static class ExplosionFactory
	{
		public static void CreateExplosion(
			World world,
			int count,
			Func<Direction> directionFn,
			Func<Particle> particleFn,
			Func<Vector2> positionFn,
			Func<Velocity> velocityFn
		)
		{
			for (int i = 0; i < count; i++)
			{
				var explosion = world.Create();

				world.Add(explosion, directionFn());
				world.Add(explosion, particleFn());
				world.Add(explosion, new Transform(positionFn(), 0f, new Vector2(1f, 1f)));
				world.Add(explosion, velocityFn());
			}
		}
	}
}
