using System;

using Components;

using Microsoft.Xna.Framework;

using MonoGame.Extended.Entities;

namespace EntityFactories
{
	public static class ExplosionFactory
	{
		public static void CreateExplosion(
			Func<Entity> createEntityFn,
			int count,
			Func<Direction> directionFn,
			Func<Particle> particleFn,
			Func<Vector2> positionFn,
			Func<Velocity> velocityFn
		)
		{
			for (int i = 0; i < count; i++)
			{
				var explosion = createEntityFn();

				explosion.Attach(directionFn());
				explosion.Attach(particleFn());
				explosion.Attach(new Transform(positionFn(), 0f, new Vector2(1f, 1f)));
				explosion.Attach(velocityFn());
			}

		}
	}
}