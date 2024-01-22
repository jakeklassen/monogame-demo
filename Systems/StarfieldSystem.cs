using System;
using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class StarfieldSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _starEntities = new QueryDescription().WithAll<
			Star,
			Transform
		>();
		private readonly Random _random = new();

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _starEntities,
				(ref Star star, ref Transform transform) =>
				{
					if (transform.Position.Y > 128)
					{
						transform.Position = new Vector2(_random.Next(1, 127), -1);
					}
				}
			);
		}
	}
}
