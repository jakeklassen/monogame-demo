using Arch.Core;
using Components;
using Microsoft.Xna.Framework;

namespace Systems
{
	public class TimeToLiveSystem : SystemBase<GameTime>
	{
		private GameTime _gameTime;
		private readonly QueryDescription _blinkEntities = new QueryDescription().WithAll<TimeToLive>();

		public TimeToLiveSystem(World world) : base(world)
		{
		}

		public override void Update(in GameTime gameTime)
		{
			_gameTime = gameTime;

			World.Query(in _blinkEntities, (in Entity entity) =>
			{
				var ttl = World.Get<TimeToLive>(entity);
				ttl.Value -= (float)_gameTime.ElapsedGameTime.TotalSeconds;

				if (ttl.Value <= 0)
				{
					World.Destroy(entity);
				}
			});
		}
	}
}