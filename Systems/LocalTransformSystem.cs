using Arch.Core;
using CherryBomb.Components;
using Microsoft.Xna.Framework;

namespace CherryBomb.Systems
{
	// Positions child entities relative to their parent each frame:
	//   child.Transform.Position = parent.Transform.Position + child.LocalTransform.Position
	// Parent is stored as an Arch Entity value handle, so we guard with
	// World.IsAlive before dereferencing. Orphans (parent destroyed) are
	// themselves destroyed so they don't linger at a stale position.
	// Source: systems/local-transform-system.ts
	public class LocalTransformSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _children = new QueryDescription().WithAll<
			LocalTransform,
			Parent,
			Transform
		>();

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _children,
				(
					Entity entity,
					ref LocalTransform localTransform,
					ref Parent parent,
					ref Transform transform
				) =>
				{
					if (!World.IsAlive(parent.Entity) || !World.Has<Transform>(parent.Entity))
					{
						World.Destroy(entity);

						return;
					}

					ref var parentTransform = ref World.Get<Transform>(parent.Entity);

					transform.Position = parentTransform.Position + localTransform.Position;
				}
			);
		}
	}
}
