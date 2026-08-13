namespace CherryBomb.Systems
{
	// Per-frame HUD readout, computed by GameplayScreen and handed to the renderer.
	// Mirrors the values space-drift/main.ts writes into its HUD each frame.
	public struct HudState
	{
		public int Fps;
		public int Speed;
		public int ChargeCount; // pending homing-volley size (0/3/5/8)
		public float ChargeSeconds; // seconds of charge held, 0..HomingChargeMax
		public float Fuel; // 0..1
		public bool Boosting;

		// Comparison toggles shown along the bottom.
		public bool Smoothing;
		public bool Interpolation;
		public bool Subpixel;
		public bool Minimap;
		public bool Crt;
		public bool Gamepad;

		// Seconds the boost has been held (drives the whole-frame shake).
		public float BoostHeldTime;
	}
}
