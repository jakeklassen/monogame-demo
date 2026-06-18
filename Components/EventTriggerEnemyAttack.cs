namespace CherryBomb.Components
{
	// Short-lived event entity: emitted by EnemyPickSystem on the per-wave attack
	// cadence and consumed by TriggerEnemyAttackEventSystem, which may switch a
	// formation enemy into its attack run. Source: trigger-enemy-attack-event-system.ts.
	public class EventTriggerEnemyAttack { }
}
