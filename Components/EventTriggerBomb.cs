namespace CherryBomb.Components
{
	// Request for a cherry-bomb (screen clear). Emitted by PlayerSystem (B /
	// gamepad X) gated on !State.BombLocked, and consumed by BombSystem when
	// State.WaveReady && !State.BombLocked. Source: event-system.ts / bomb-system.ts.
	public class EventTriggerBomb { }
}
