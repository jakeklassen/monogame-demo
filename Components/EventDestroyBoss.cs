namespace CherryBomb.Components
{
	// Emitted once by PlayerProjectileBossCollisionEventSystem when the boss's
	// health drops to zero. Consumed by DestroyBossEventSystem, which runs the
	// scripted win sequence. Source: eventDestroyBoss.
	public struct EventDestroyBoss { }
}
