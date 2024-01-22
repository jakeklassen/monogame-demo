using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class BoundToViewportSystem(World world, Rectangle viewport) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _query = new QueryDescription().WithAll<
			BoundToViewport,
			BoxCollider,
			Transform
		>();
		private readonly Rectangle _viewport = viewport;

		public override void Update(in GameTime gameTime)
		{
			World.Query(
				in _query,
				(ref BoxCollider boxCollider, ref Transform transform) =>
				{
					if (transform.Position.X + boxCollider.Offset.X > _viewport.Width - boxCollider.Width)
					{
						transform.Position = new Vector2(
							_viewport.Width - boxCollider.Width - boxCollider.Offset.X,
							transform.Position.Y
						);
					}
					else if (transform.Position.X + boxCollider.Offset.X < 0)
					{
						transform.Position = new Vector2(-boxCollider.Offset.X, transform.Position.Y);
					}

					if (transform.Position.Y + boxCollider.Offset.Y > _viewport.Height - boxCollider.Height)
					{
						transform.Position = new Vector2(
							transform.Position.X,
							_viewport.Height - boxCollider.Height - boxCollider.Offset.Y
						);
					}
					else if (transform.Position.Y + boxCollider.Offset.Y < 0)
					{
						transform.Position = new Vector2(transform.Position.X, -boxCollider.Offset.Y);
					}
				}
			);
		}
	}
}
