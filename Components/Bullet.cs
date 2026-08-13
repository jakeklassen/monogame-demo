namespace SpaceDrift.Components
{
	// A player shot: flies along Transform.Rotation, expires after MaxAge.
	public struct Bullet
	{
		public float Age;
		public float MaxAge;
	}
}
