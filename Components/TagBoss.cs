namespace CherryBomb.Components
{
	// Marks the wave-9 boss so it is handled by the boss-specific systems
	// (BossSystem, PlayerProjectileBossCollisionEventSystem, DestroyBossEventSystem)
	// rather than the generic enemy attack / fire / death paths. Source: tagBoss.
	public struct TagBoss { }
}
