namespace CherryBomb.Components
{
	// ENABLE-FLAG CONVENTION
	// State that toggles frequently (e.g. invulnerability frames flickering on/off
	// many times per second) should ride a bool/Enabled field on a *persistent*
	// component rather than being added/removed each toggle. In Arch every
	// World.Add/Remove is an archetype migration (the entity is copied to a
	// different chunk), so churning components on hot paths is expensive.
	//
	// This component is currently still add/removed by InvulnerableSystem (it is
	// added once per hit and removed once when its timer expires, so the churn is
	// low). When the port introduces rapid flicker toggling, flip to an `Enabled`
	// flag here and have systems check it instead of presence. The Parent /
	// LocalTransform components added in this milestone follow the convention:
	// they are added once and live for the entity's lifetime.
	public class Invulnerable
	{
		public float Duration { get; set; }

		// Total time (seconds) the invulnerability lasts, captured when granted so the
		// opacity blink can restore full opacity when the window ends. Defaults to
		// Duration via the setter on InvulnerableSystem's first tick when zero.
		public float ElapsedSeconds { get; set; }
	}
}
