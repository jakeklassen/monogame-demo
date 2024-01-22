using Arch.Core;
using CherryBomb;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class DestroyOnViewportExitSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			BoxCollider,
			Transform
		>();

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _query,
				(Entity entity) =>
				{
					World.TryGet<EnemyState>(entity, out var enemyState);

					if (enemyState?.Value == EnemyStateType.Flyin)
					{
						// Don't destroy enemies that are flying in.
						// They start off screen.
						return;
					}

					var boxCollider = World.Get<BoxCollider>(entity);
					var transform = World.Get<Transform>(entity);

					if (
						transform.Position.X < -boxCollider.Width
						|| transform.Position.X > Game1.TargetWidth + boxCollider.Width
						|| transform.Position.Y < -boxCollider.Height
						|| transform.Position.Y > Game1.TargetHeight + boxCollider.Height
					)
					{
						World.Destroy(entity);
					}
				}
			);
		}
	}
}
