namespace Components
{
	public class EventPlayerProjectileEnemyCollision
	{
		public int ProjectileEntityId { get; set; }
		public int EnemyEntityId { get; set; }

		public EventPlayerProjectileEnemyCollision(int projectileEntityId, int enemyEntityId)
		{
			ProjectileEntityId = projectileEntityId;
			EnemyEntityId = enemyEntityId;
		}
	}
}