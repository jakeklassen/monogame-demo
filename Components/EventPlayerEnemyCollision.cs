using Arch.Core;

namespace CherryBomb.Components
{
	// Emitted when the player overlaps an enemy or an enemy projectile. The
	// EnemyEntity is whatever the player touched (a TagEnemy or a TagEnemyBullet);
	// PlayerEnemyCollisionEventSystem consumes it, applies damage, and destroys an
	// enemy bullet (enemies themselves are left alone — they keep flying).
	public class EventPlayerEnemyCollision(Entity playerEntity, Entity enemyEntity)
	{
		public Entity PlayerEntity { get; set; } = playerEntity;
		public Entity EnemyEntity { get; set; } = enemyEntity;
	}
}
