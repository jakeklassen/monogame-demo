using System;
using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class StarfieldSystem : SystemBase<GameTime>
	{
		private readonly QueryDescription _starEntities = new QueryDescription().WithAll<
			Star,
			Transform
		>();
		private readonly Random _random = new();

		public StarfieldSystem(World world)
			: base(world) { }

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
