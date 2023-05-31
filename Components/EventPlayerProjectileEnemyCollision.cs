using Arch.Core;

namespace Components
{
	public class EventPlayerProjectileEnemyCollision
	{
		public Entity ProjectileEntity { get; set; }
		public Entity EnemyEntity { get; set; }
		public int Damage { get; set; }

		public EventPlayerProjectileEnemyCollision(
			Entity projectileEntity,
			Entity enemyEntity,
			int damage
		)
		{
			ProjectileEntity = projectileEntity;
			EnemyEntity = enemyEntity;
			Damage = damage;
		}
	}
}
