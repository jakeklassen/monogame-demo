using Arch.Core;

namespace Components
{
	public class EventPlayerProjectileEnemyCollision
	{
		public Entity ProjectileEntity { get; set; }
		public Entity EnemyEntity { get; set; }

		public EventPlayerProjectileEnemyCollision(Entity projectileEntity, Entity enemyEntity)
		{
			ProjectileEntity = projectileEntity;
			EnemyEntity = enemyEntity;
		}
	}
}
