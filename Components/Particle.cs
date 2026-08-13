namespace CherryBomb.Components
{
	public enum ParticleKind
	{
		Flame,
		Smoke,
	}

	// A short-lived exhaust pixel. Kind selects its color ramp.
	public struct Particle
	{
		public float Age;
		public float MaxAge;
		public ParticleKind Kind;
		public float Size;
	}
}
