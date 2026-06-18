namespace CherryBomb.Components
{
	// A small white muzzle flash. Its Size shrinks ~0.5px/frame (in MuzzleFlashSystem)
	// and the entity self-deletes once it shrinks away or exceeds DurationMs. Paired
	// with Parent + LocalTransform so it tracks the player's gun. Source: muzzle-flash-*.
	public class MuzzleFlash(float size, float durationMs)
	{
		public float Size { get; set; } = size;
		public float DurationMs { get; set; } = durationMs;
		public float ElapsedMs { get; set; } = 0f;
	}
}
