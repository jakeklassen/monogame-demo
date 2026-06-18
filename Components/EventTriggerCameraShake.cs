namespace CherryBomb.Components
{
	// Request for a camera shake. Emitted by the spread-shot path. There is no
	// consumer yet — the camera-shake system arrives in M5 — but emitting it now
	// keeps the spread-shot wiring complete.
	public struct EventTriggerCameraShake(float strength, float durationMs)
	{
		public float Strength { get; set; } = strength;
		public float DurationMs { get; set; } = durationMs;
	}
}
