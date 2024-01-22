using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class MovementSystem(World world) : SystemBase<GameTime>(world)
	{
		private readonly QueryDescription _entitiesToMove = new QueryDescription().WithAll<
			Direction,
			Transform,
			Velocity
		>();
		private GameTime _gameTime;

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _entitiesToMove,
				(ref Direction direction, ref Velocity velocity, ref Transform transform) =>
				{
					var velocityVector = new Vector2(velocity.X, velocity.Y);
					var directionVector = new Vector2(direction.X, direction.Y);

					transform.Position +=
						velocityVector * directionVector * (float)_gameTime.ElapsedGameTime.TotalSeconds;
				}
			);
		}
	}
}
