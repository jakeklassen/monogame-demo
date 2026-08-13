namespace CherryBomb.Components
{
	public struct Ship
	{
		public bool Thrusting;
		public bool Boosting; // boost held and fuel remaining this frame
		public float Fuel; // boost fuel, 0..BoostFuelMax
	}
}
