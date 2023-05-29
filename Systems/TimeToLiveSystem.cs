using Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class TimeToLiveSystem : EntityProcessingSystem
	{
		public TimeToLiveSystem() : base(Aspect.All(typeof(TimeToLive)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			var entity = GetEntity(entityId);
			var ttl = entity.Get<TimeToLive>();

			ttl.Value -= (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (ttl.Value <= 0)
			{
				DestroyEntity(entityId);
			}
		}
	}
}