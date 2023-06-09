using System;
using Arch.Core;
using CherryBomb;
using Components;

using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace EntityFactories
{
	public static class StarFactory
	{
		public static Entity CreateStar(World world, Vector2 position)
		{
			var randomVelocities = new[] { 60, 30, 20 };
			Random random = new();

			var entity = world.Create();
			world.Add(entity, new Direction(0, 1));
			world.Add(entity, new Transform(position, 0f, new Vector2(1f, 1f)));

			var star = new Star(Pico8Color.Color7);
			var velocity = new Velocity(0f, randomVelocities[random.Next(0, randomVelocities.Length)]);

			// Adjust star color based on velocity
			if (velocity.Y < 30)
			{
				star.Color = Pico8Color.Color1;
			}
			else if (velocity.Y < 60)
			{
				star.Color = Pico8Color.Color13;
			}

			world.Add(entity, star);
			world.Add(entity, velocity);

			return entity;
		}

		public static void CreateStarfield(World world, int width, int height, int starCount)
		{
			Random random = new();

			for (int i = 0; i < starCount; i++)
			{
				int x = random.Next(0, width);
				int y = random.Next(0, height);

				CreateStar(world, new Vector2(x, y));
			}
		}
	}
}
