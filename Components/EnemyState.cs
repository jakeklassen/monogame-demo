namespace Components
{
	public enum EnemyStateType
	{
		Spawned,
		Flyin,
		Protect,
		Attack,
		BossReady,
		Boss1,
		Boss2,
		Boss3,
		Boss4,
	}

	public class EnemyState
	{
		public EnemyStateType Value { get; set; } = EnemyStateType.Spawned;
	}
}