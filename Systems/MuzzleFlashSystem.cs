using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Shrinks muzzle-flash circles ~0.5px/frame and self-deletes them once they
	// vanish or outlive their duration. Positioning to the player gun is handled by
	// LocalTransformSystem (the flash carries Parent + LocalTransform). Source: muzzle-flash-system.ts.
	public class MuzzleFlashSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			MuzzleFlash,
			Transform
		>();

		public override void Update(in GameTime gameTime)
		{
			var dtMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

			World.Query(
				in _query,
				(Entity entity) =>
				{
					var muzzleFlash = World.Get<MuzzleFlash>(entity);

					muzzleFlash.ElapsedMs += dtMs;
					muzzleFlash.Size -= 0.5f;

					if (muzzleFlash.Size < 0 || muzzleFlash.ElapsedMs >= muzzleFlash.DurationMs)
					{
						World.Destroy(entity);
					}
				}
			);
		}
	}
}
