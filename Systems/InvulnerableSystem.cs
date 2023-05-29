using Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace Systems
{
	public class InvulnerableSystem : EntityProcessingSystem
	{
		public InvulnerableSystem() : base(Aspect.All(typeof(Invulnerable)))
		{
		}

		public override void Initialize(IComponentMapperService mapperService)
		{
		}

		public override void Process(GameTime gameTime, int entityId)
		{
			var entity = GetEntity(entityId);
			var invulnerable = entity.Get<Invulnerable>();

			invulnerable.Duration -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;

			if (invulnerable.Duration <= 0)
			{
				entity.Detach<Invulnerable>();
			}
		}
	}
}