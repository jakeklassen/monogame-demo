namespace CherryBomb.Components
{
	// Short-lived event entity: emitted by EnemyPickSystem on the per-wave fire
	// cadence and consumed by TriggerEnemyFireEventSystem, which fires a projectile
	// from a chosen enemy. Source: trigger-enemy-fire-event-system.ts.
	public class EventTriggerEnemyFire { }
}
