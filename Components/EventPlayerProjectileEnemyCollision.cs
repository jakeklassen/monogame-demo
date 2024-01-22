using Arch.Core;

namespace Components
{
	public class EventPlayerProjectileEnemyCollision(
		Entity projectileEntity,
		Entity enemyEntity,
		int damage
		)
	{
		public Entity ProjectileEntity { get; set; } = projectileEntity;
		public Entity EnemyEntity { get; set; } = enemyEntity;
		public int Damage { get; set; } = damage;
	}
}
