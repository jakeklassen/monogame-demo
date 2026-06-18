using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.EntityFactories
{
	public static class CherryFactory
	{
		// Spawns a cherry pickup at position. It falls straight down (Direction 0,1 +
		// Velocity 0,30; MovementSystem multiplies velocity by direction). Carries
		// CollisionLayer Pickup / CollisionMask Player so the player collects it.
		public static Entity Create(World world, Vector2 position)
		{
			var cherry = world.Create();

			world.Add(
				cherry,
				new BoxCollider(
					SpriteSheet.Cherry.BoxCollider.Width,
					SpriteSheet.Cherry.BoxCollider.Height,
					SpriteSheet.Cherry.BoxCollider.Offset
				)
			);
			world.Add(cherry, new CollisionLayer(CollisionMasks.Pickup));
			world.Add(cherry, new CollisionMask(CollisionMasks.Player));
			world.Add(cherry, new Direction(0, 1));
			world.Add(cherry, new Sprite(SpriteSheet.Cherry.Frame));
			// Animated 1px outline (cherry Color7 <-> Color14 @100ms) so the pickup
			// reads clearly against the playfield. Rendered behind the sprite by
			// SpriteOutlineRenderingSystem. Source: M2 cherry + sprite-outline-*.
			world.Add(cherry, new SpriteOutline() { Color = Pico8Color.Color7 });
			world.Add(
				cherry,
				new SpriteOutlineAnimation(
					colors: [Pico8Color.Color7, Pico8Color.Color14],
					colorSequence: [0, 1],
					durationSeconds: 0.2f
				)
			);
			world.Add(cherry, new TagPickup());
			world.Add(cherry, new Transform(position, 0f, Vector2.One));
			world.Add(cherry, new Velocity(0, 30));

			return cherry;
		}
	}
}
