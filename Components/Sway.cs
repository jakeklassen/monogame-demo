namespace CherryBomb.Components
{
	// Horizontal sway applied to an attacking enemy's X position while Velocity drives
	// its Y. Ports the edge-aware position.x yoyo tweens from switch-enemy-to-attach-mode.ts:
	// the enemy weaves around CenterX by Amplitude with a sinusoidal motion of the given
	// Period. SwaySystem owns the integration so it never fights the velocity-driven Y.
	public class Sway
	{
		// X the sway oscillates around (set inward when the enemy starts near an edge).
		public float CenterX { get; set; }

		// Half-width of the weave, in pixels.
		public float Amplitude { get; set; }

		// Seconds for a full back-and-forth cycle.
		public float Period { get; set; }

		// Accumulated time, advanced by SwaySystem.
		public float Elapsed { get; set; }

		// Starting phase: +1 sways right first, -1 left first.
		public float Direction { get; set; } = 1f;
	}
}
