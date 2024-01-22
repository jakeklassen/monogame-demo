using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class TimeToLiveSystem(World world) : SystemBase<GameTime>(world)
	{
		private GameTime _gameTime;
		private readonly QueryDescription _query = new QueryDescription().WithAll<TimeToLive>();

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(
				in _query,
				(Entity entity) =>
				{
					var ttl = World.Get<TimeToLive>(entity);
					ttl.Value -= (float)_gameTime.ElapsedGameTime.TotalSeconds;

					if (ttl.Value <= 0)
					{
						World.Destroy(entity);
					}
				}
			);
		}
	}
}
