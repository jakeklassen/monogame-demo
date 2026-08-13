using Arch.Core;

namespace SpaceDrift.Components
{
	// Makes a bullet steer toward Target at up to TurnRate deg/s.
	public struct Homing
	{
		public float TurnRate;
		public Entity Target;
	}
}
